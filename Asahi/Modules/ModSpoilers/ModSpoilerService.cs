﻿using System.Diagnostics.CodeAnalysis;
using Asahi.Database;
using Asahi.Database.Models;
using Asahi.Modules.Highlights;
using Discord.Webhook;
using Discord.WebSocket;
using Fergun.Interactive;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Asahi.Modules.Highlights.HighlightsTrackingService;

namespace Asahi.Modules.ModSpoilers;

[Inject(ServiceLifetime.Singleton)]
public class ModSpoilerService(
    DiscordSocketClient client,
    HttpClient httpClient,
    DbService dbService,
    InteractiveService interactive,
    BotConfig botConfig,
    HighlightsTrackingService hts,
    ILogger<ModSpoilerModule> logger)
{
    [method: SetsRequiredMembers]
    public struct SpoilerAttemptResult(bool wasSuccess, string response)
    {
        public required bool wasSuccess = wasSuccess;
        public required string response = response;
    }

    public async Task<SpoilerAttemptResult> SpoilerMessage(IMessage message, bool deleteOg, BotDbContext dbContext, string? context = "")
    {
        if (message.Channel is not IIntegrationChannel channel)
        {
            return new SpoilerAttemptResult(false, $"<#{message.Channel.Id}> does not support webhooks.");
        }

        SocketThreadChannel? threadChannel = null;
        if (message.Channel is SocketThreadChannel tc)
        {
            threadChannel = tc;
            channel = (IIntegrationChannel)threadChannel.ParentChannel;
        }

        if (message.Author is not IGuildUser author)
        {
            return new SpoilerAttemptResult(false, "Author not from Guild.");
        }

        var requiredPerms = ChannelPermission.ManageWebhooks;

        if (deleteOg)
        {
            requiredPerms |= ChannelPermission.ManageMessages;
        }

        if (!(await channel.Guild.GetUserAsync(client.CurrentUser.Id)).GetPermissions(channel)
            .Has(requiredPerms))
        {
            return new SpoilerAttemptResult(false, $"Bot does not have {requiredPerms.Humanize(LetterCasing.Title)} permission in <#{channel.Id}>.");
        }

        var webhook = await channel.GetOrCreateWebhookAsync(BotService.WebhookDefaultName);
        var webhookClient = new DiscordWebhookClient(webhook);

        var cachedHighlightedMessage = await
            dbContext.CachedHighlightedMessages
                .Include(cachedHighlightedMessage => cachedHighlightedMessage.HighlightBoard)
                .FirstOrDefaultAsync(x => x.HighlightMessageIds.Contains(message.Id));
        if (cachedHighlightedMessage == null)
        {
            var contents = message.Content.SpoilerMessage(context);

            List<FileAttachment> attachmentStreams = [];
            List<IDisposable> disposables = [];
            foreach (var attachment in message.Attachments)
            {
                var req = await httpClient.GetAsync(attachment.Url);
                attachmentStreams.Add(new FileAttachment(await req.Content.ReadAsStreamAsync(), attachment.Filename, attachment.Description, true));
                disposables.Add(req);
            }

            var username = author is IWebhookUser webhookUser ? webhookUser.Username : author.DisplayName;
            var avatar = author.GetDisplayAvatarUrl();

            var threadId = threadChannel?.Id;
            await webhookClient.SendFilesAsync(attachmentStreams, contents, username: username, avatarUrl: avatar,
                isTTS: message.IsTTS, allowedMentions: AllowedMentions.None, threadId: threadId);

            if (deleteOg)
                await message.DeleteAsync();

            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }
        else
        {
            // "channel" is assumed to be the logging channel here.
            if (channel is not ITextChannel loggingChannel)
            {
                return new SpoilerAttemptResult(false, "Channel is not a text channel.");
            }

            var ogChannel = await loggingChannel.Guild.GetTextChannelAsync(cachedHighlightedMessage.OriginalMessageChannelId);

            var ogMessage = await ogChannel.GetMessageAsync(cachedHighlightedMessage.OriginalMessageId);

            var quoteEmbedColor = await QuotingHelpers.GetQuoteEmbedColor(
                cachedHighlightedMessage.HighlightBoard.EmbedColorSource,
                cachedHighlightedMessage.HighlightBoard.FallbackEmbedColor,
                ogMessage.Author as IGuildUser,
                client);
            var messageContents = QuotingHelpers.QuoteMessage(ogMessage, quoteEmbedColor, logger, true, true, context);

            var i = 0;
            bool deletedMessage = false;
            foreach (var highlightMessageId in cachedHighlightedMessage.HighlightMessageIds)
            {
                if (i == 0)
                {
                    var firstMessage = messageContents[0];

                    var firstMessageObj = await loggingChannel.GetMessageAsync(cachedHighlightedMessage.HighlightMessageIds[0]);
                    var (uniqueReactionUsersAutoReact, uniqueReactionEmotes) =
                        await hts.UniqueReactionUsersAutoReact(channel.Guild, ogMessage, firstMessageObj);

                    if (firstMessage.embeds == null)
                    {
                        return new SpoilerAttemptResult(false, "New highlight message missing embeds.");
                    }

                    var eb = firstMessage.embeds[0].ToEmbedBuilder();

                    hts.AddReactionsFieldToQuote(eb,
                        uniqueReactionEmotes.Select(x => new ReactionInfo(x)), uniqueReactionUsersAutoReact.Count);

                    firstMessage.embeds[0] = eb.Build();

                    await webhookClient.ModifyMessageAsync(highlightMessageId, props =>
                    {
                        props.Content = firstMessage.body;
                        props.Embeds = firstMessage.embeds;
                        props.Components = firstMessage.components;
                    });
                }
                else
                {
                    await loggingChannel.DeleteMessageAsync(highlightMessageId);
                    deletedMessage = true;
                }

                i++;
            }

            if (deletedMessage)
            {
                var aliases = await dbContext.EmoteAliases.Where(x => x.GuildId == channel.GuildId).ToArrayAsync();

                if (cachedHighlightedMessage.HighlightBoard.AutoReactMaxAttempts != 0)
                {
                    var firstMessage = await loggingChannel.GetMessageAsync(cachedHighlightedMessage.HighlightMessageIds[0]);
                    await hts.AutoReact(cachedHighlightedMessage.HighlightBoard, aliases, ogMessage.Reactions, firstMessage);
                }
            }

            cachedHighlightedMessage.HighlightMessageIds = [cachedHighlightedMessage.HighlightMessageIds[0]];
        }

        return new SpoilerAttemptResult(true, "Spoiler tagged.");
    }

    public async Task ReactionCheck(SocketReaction reaction)
    {
        try
        {
            if (reaction.Channel is not ITextChannel channel)
                return;

            if (reaction.User.Value is not IGuildUser reactor)
                return;

            if (!reactor.GetPermissions(channel).Has(ChannelPermission.ManageMessages))
                return;

            await using var context = dbService.GetDbContext();

            var guildConfig = await context.GetGuildConfig(channel.GuildId);

            if (!TryParseEmote(guildConfig.SpoilerReactionEmote, out var spoilerEmote))
                return;

            if (!reaction.Emote.Equals(spoilerEmote))
                return;

            var requiredPerms = ChannelPermission.ManageWebhooks;

            if (guildConfig.SpoilerBotAutoDeleteContextSetting || guildConfig.SpoilerBotAutoDeleteOriginal)
            {
                requiredPerms |= ChannelPermission.ManageMessages;
            }

            if (!(await channel.Guild.GetUserAsync(client.CurrentUser.Id)).GetPermissions(channel)
                .Has(requiredPerms))
            {
                _ = interactive.DelayedSendMessageAndDeleteAsync(channel, deleteDelay: TimeSpan.FromSeconds(15),
                    text: $"Bot does not have {requiredPerms.Humanize(LetterCasing.Title)} permission in <#{channel.Id}>.");
                return;
            }

            var message = reaction.Message.IsSpecified ? reaction.Message.Value : await reaction.Channel.GetMessageAsync(reaction.MessageId);

            var modContextQuestionMsg =
                await channel.SendMessageAsync(
                    $"<@{reactor.Id}> Enter a context for this spoiler message. Type \"0\" if no additional context is needed, or \"cancel\" if this was an accident.",
                    allowedMentions: new AllowedMentions()
                    {
                        UserIds = [reactor.Id]
                    }, messageReference: new MessageReference(reaction.MessageId, channel.Id, channel.GuildId));

            var contextMsg = await interactive.NextMessageAsync(
                x => x.Channel.Id == channel.Id && x.Author.Id == reactor.Id,
                timeout: TimeSpan.FromMinutes(2));

            if (contextMsg.IsTimeout)
            {
                await modContextQuestionMsg.ModifyAsync(x => x.Content = "Context request timed out.");
                return;
            }

            if (!contextMsg.IsSuccess || contextMsg.Value == null)
            {
                logger.LogError("Failed to get context message for unknown reason!");
                return;
            }

            if (contextMsg.Value.Content.Equals("cancel", StringComparison.InvariantCultureIgnoreCase))
            {
                if (guildConfig.SpoilerBotAutoDeleteContextSetting)
                {
                    await contextMsg.Value.DeleteAsync();
                    await modContextQuestionMsg.DeleteAsync();
                    await message.RemoveReactionAsync(reaction.Emote, reactor);
                }

                return;
            }

            var spoilerContext = contextMsg.Value.Content == "0" ? "" : contextMsg.Value.Content;

            var waitEmote = Emoji.Parse(botConfig.LoadingEmote);
            await contextMsg.Value.AddReactionAsync(waitEmote);

            var spoilerAttempt = await SpoilerMessage(message, guildConfig.SpoilerBotAutoDeleteOriginal, context, spoilerContext);

            await contextMsg.Value.RemoveReactionAsync(waitEmote, client.CurrentUser.Id);

            if (!spoilerAttempt.wasSuccess)
            {
                await channel.SendMessageAsync($"Failed to spoiler tag: `{spoilerAttempt.response}`",
                    allowedMentions: AllowedMentions.None);
                return;
            }

            await context.SaveChangesAsync();

            if (guildConfig.SpoilerBotAutoDeleteContextSetting)
            {
                try
                {
                    await contextMsg.Value.DeleteAsync();
                    await modContextQuestionMsg.DeleteAsync();
                    if (!guildConfig.SpoilerBotAutoDeleteOriginal)
                        await message.RemoveReactionAsync(reaction.Emote, reactor);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete context messages.");
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to do spoiler reaction.");
        }
    }

    public static bool TryParseEmote(string text, [NotNullWhen(true)] out IEmote? emote)
    {
        if (Emote.TryParse(text, out var result))
        {
            emote = result;
            return true;
        }

        if (Emoji.TryParse(text, out var result2))
        {
            emote = result2;
            return true;
        }

        emote = null;
        return false;
    }
}