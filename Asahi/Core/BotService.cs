﻿using System.Reflection;
using Asahi.Database;
using Asahi.Modules.Highlights;
using Asahi.Modules.Seigen;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Asahi;

public class BotService(
    DiscordSocketClient client,
    BotConfig config,
    DbService dbService,
    ILogger<BotService> logger,
    CommandHandler commandHandler,
    IServiceProvider services) : BackgroundService
{
    public const string WebhookDefaultName = "Asahi Webhook";

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        MessageContents.AddRedButtonDefault = false;

        var args = Environment.GetCommandLineArgs();
        var migrationEnabled = !(args.Contains("nomigrate") || args.Contains("nukedb"));
        await dbService.Initialize(migrationEnabled);

#if DEBUG
        if (Environment.GetCommandLineArgs().Contains("nukedb"))
        {
            logger.LogDebug("Nuking the DB...");

            await dbService.ResetDatabase();

            logger.LogDebug("Nuked!");
        }
#endif

        client.Log += Client_Log;

        client.Ready += Client_Ready;

        // should make these work for more than just RoleManagementService really but the need hasn't appeared yet so
        client.GuildMemberUpdated += Client_GuildMemberUpdated;
        client.UserLeft += Client_UserLeft;
        client.UserJoined += Client_UserJoined;
        client.ReactionAdded += Client_ReactionAdded;
        client.MessageReceived += Client_MessageReceived;

        await client.LoginAsync(TokenType.Bot, config.BotToken);
        await client.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (ExecuteTask == null)
            return;

        await client.LogoutAsync();
        await client.StopAsync();

        await base.StopAsync(cancellationToken);
    }

    private Task Client_UserLeft(SocketGuild guild, SocketUser user) => services.GetRequiredService<RoleManagementService>().OnUserLeft(guild, user);

    private Task Client_UserJoined(SocketGuildUser user) => services.GetRequiredService<RoleManagementService>().OnUserJoined(user);

    private async Task Client_GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cacheable, SocketGuildUser user)
    {
        if (!cacheable.HasValue)
            return;

        if (!user.Roles.SequenceEqual(cacheable.Value.Roles))
        {
            await services.GetRequiredService<RoleManagementService>().OnUserRolesUpdated(cacheable, user);
        }
    }

    private async Task Client_ReactionAdded(
        Cacheable<IUserMessage, ulong> cachedMessage,
        Cacheable<IMessageChannel, ulong> originChannel,
        SocketReaction reaction)
    {
        logger.LogTrace("Reaction added");

        if (reaction.User.IsSpecified && reaction.User.Value.IsBot)
            return;

        var highlightsService = services.GetRequiredService<HighlightsTrackingService>();

        await highlightsService.safetySemaphore.WaitAsync();
        _ = Task.Run(() => highlightsService.CheckMessageForHighlights(cachedMessage, reaction));
    }

    private Task Client_MessageReceived(SocketMessage msg)
    {
        if (msg.Channel is not SocketGuildChannel)
            return Task.CompletedTask;

        var highlightsService = services.GetRequiredService<HighlightsTrackingService>();
        highlightsService.AddMessageToCache(msg);

        return Task.CompletedTask;
    }

    private Task Client_Log(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Trace,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information,
        };

        if (message.Exception is not null)
        {
            logger.Log(level, message.Exception, "{Source} | {Message}", message.Source, message.Message);
        }
        else
        {
            logger.Log(level, "{Source} | {Message}", message.Source, message.Message);
        }
        return Task.CompletedTask;
    }

    private async Task Client_Ready()
    {
        logger.LogInformation("Logged in as {user}#{discriminator} ({id})", client.CurrentUser?.Username, client.CurrentUser?.Discriminator, client.CurrentUser?.Id);

        await commandHandler.OnReady(Assembly.GetExecutingAssembly());

        // see comment in ExecuteAsync
        var roleManagement = services.GetRequiredService<RoleManagementService>();
        await roleManagement.CacheAndResolve();
    }
}