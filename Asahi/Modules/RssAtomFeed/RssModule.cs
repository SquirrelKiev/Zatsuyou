﻿using Asahi.Database;
using Asahi.Database.Models.Rss;
using BotBase.Modules;
using CodeHollow.FeedReader;
using Discord.Interactions;
using Fergun.Interactive;
using Fergun.Interactive.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Asahi.Modules.RssAtomFeed;

[Group("rss", "Commands relating to RSS/Atom feeds.")]
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class RssModule(DbService dbService, RssTimerService rts, InteractiveService interactive, HttpClient http) : BotModule
{
    [SlashCommand("add-feed", "Adds a feed.")]
    public async Task AddFeedSlash(
        [Summary(description: "The RSS/Atom url.")]
        string url, 
        [Summary(description: "The channel to send updates to.")]
        IMessageChannel channel,
        [Summary(description: "The ID of the feed to edit.")]
        uint? id = null)
    {
        await CommonConfig(async (context, builder) =>
        {
            if (await context.RssFeedListeners.AnyAsync(
                    x => x.GuildId == Context.Guild.Id &&
                         x.ChannelId == channel.Id &&
                         x.FeedUrl == url))
            {
                return new ConfigChangeResult(false, "You already have this feed added for this channel!");
            }

            Feed? feed;
            try
            {
                http.MaxResponseContentBufferSize = 8000000;
                using var req = await http.GetAsync(url);
                var xml = await req.Content.ReadAsStringAsync();

                feed = FeedReader.ReadFromString(xml);

                if (!RssTimerService.ValidateFeed(feed))
                {
                    return new ConfigChangeResult(false, "Feed isn't valid!");
                }
            }
            catch (Exception ex)
            {
                return new ConfigChangeResult(false, $"Failed to get feed. `{ex.GetType()}`: `{ex.Message}`");
            }

            if (id == null)
            {
                context.Add(new RssFeedListener()
                {
                    GuildId = Context.Guild.Id,
                    ChannelId = channel.Id,
                    FeedUrl = url
                });
            }
            else
            {
                var existing = await context.GetFeed(id.Value, Context.Guild.Id);
                if (existing == null)
                    return new ConfigChangeResult(false, "Could not find feed with that ID.");

                existing.ChannelId = channel.Id;
                existing.FeedUrl = url;
            }

            var eb = new EmbedBuilder();
            if (!string.IsNullOrWhiteSpace(feed.ImageUrl))
                eb.WithThumbnailUrl(feed.ImageUrl);

            eb.WithUrl(url);

            if (!string.IsNullOrWhiteSpace(feed.Title))
                eb.WithTitle(feed.Title);

            var us = await Context.Guild.GetCurrentUserAsync();
            var roleColor = QuotingHelpers.GetUserRoleColorWithFallback(us, Color.Green);
            eb.WithColor(roleColor);

            if (!string.IsNullOrWhiteSpace(feed.Description))
                eb.WithFields(new EmbedFieldBuilder().WithName("Description").WithValue(feed.Description));

            eb.WithDescription("Added feed.");

            return new ConfigChangeResult(true, "Added feed.", [eb.Build()], true);
        });
    }

    [SlashCommand("rm-feed", "Removes a feed.")]
    public async Task RemoveFeedSlash(
        [Summary(description: "The ID of the feed to remove.")]
        uint id)
    {
        await CommonConfig(async (context, builder) =>
        {
            var feed = await context.GetFeed(id, Context.Guild.Id);

            if (feed == null)
                return new ConfigChangeResult(false, "No feed found with that ID.");

            context.Remove(feed);

            return new ConfigChangeResult(true, "Removed feed.");
        });
    }

    [SlashCommand("list-feeds", "Lists the feeds within the server.")]
    public async Task ListFeedsSlash()
    {
        await DeferAsync();

        await using var context = dbService.GetDbContext();

        var feeds = await context.RssFeedListeners.Where(x => x.GuildId == Context.Guild.Id).ToArrayAsync();

        var us = await Context.Guild.GetCurrentUserAsync();
        if (feeds.Length == 0)
        {
            await FollowupAsync(embeds: ConfigUtilities.CreateEmbeds(us,
                new EmbedBuilder(), new ConfigChangeResult(true, "No feeds.")));

            return;
        }

        var roleColor = QuotingHelpers.GetUserRoleColorWithFallback(us, Color.Green);

        var pages = feeds
            .Select(x => $"* ({x.Id}) <#{x.ChannelId}> - {x.FeedUrl}")
            .Chunk(10).Select(x => new PageBuilder().WithColor(roleColor)
                .WithDescription(string.Join('\n', x)));

        var paginator = new StaticPaginatorBuilder()
            .WithPages(pages)
            .WithOptions(
            [
                new PaginatorButton("<", PaginatorAction.Backward, ButtonStyle.Secondary),
                new PaginatorButton("Jump", PaginatorAction.Jump, ButtonStyle.Secondary),
                new PaginatorButton(">", PaginatorAction.Forward, ButtonStyle.Secondary),
                new PaginatorButton(BaseModulePrefixes.RED_BUTTON, null, "X", ButtonStyle.Danger),
            ])
            .WithActionOnCancellation(ActionOnStop.DeleteMessage)
            .WithActionOnTimeout(ActionOnStop.DisableInput)
            .WithUsers(Context.User);

        await interactive.SendPaginatorAsync(paginator.Build(), Context.Interaction, TimeSpan.FromMinutes(2), InteractionResponseType.DeferredChannelMessageWithSource);
    }

#if DEBUG
    [SlashCommand("force-poll", "[DEBUG] Poll for feed updates.")]
    public async Task DebugSlash()
    {
        await DeferAsync();

        await rts.PollFeeds();

        await FollowupAsync("Polled. Check logs for more info.");
    }
#endif

    private Task<bool> CommonConfig(Func<BotDbContext, EmbedBuilder, Task<ConfigChangeResult>> updateAction)
    {
        return ConfigUtilities.CommonConfig(Context, dbService, updateAction);
    }
}