﻿using Serilog;
using Serilog.Events;
using YamlDotNet.Serialization;

namespace Asahi;

public class BotConfig
{
    [YamlMember(Description = @"Your bot token from https://discord.com/developers/applications. Don't share!")]
    public string BotToken { get; set; } = "BOT_TOKEN_HERE";

    [YamlMember(Description = "The type of database to use.\n" +
                              "Options are \"Sqlite\" and \"Postgresql\".")]
    public DatabaseType Database { get; set; } = DatabaseType.Sqlite;

    [YamlMember(Description = "The connection string for the database specified above.\n" +
                              "Example Postgres string: Host=127.0.0.1;Username=postgres;Password=;Database=botdb\n" +
                              "Example Sqlite string: Data Source=data/BotDb.db")]
    public string DatabaseConnectionString { get; set; } = "Data Source=data/BotDb.db";

    [YamlMember(Description = "The reaction to put on prefix commands when an unhandled error occurs. Will only appear on prefix commands.")]
    public string ErrorEmote { get; set; } = "\u2753"; // ❓

    public string LoadingEmote { get; set; } = "\ud83e\udd14"; // 🤔

    [YamlMember(Description = "A set of UserIDs. Users in this set will be granted permission to use commands to manage the instance itself.\n" +
                              "This is a dangerous permission to grant.")]
    public HashSet<ulong> ManagerUserIds { get; set; } = [0ul];

    [YamlMember(Description = "An optional URL to an instance of Seq. Empty string is interpreted as not wanting Seq.")]
    public string SeqUrl { get; set; } = "";

    [YamlMember(Description = "An optional API key for Seq. Empty string is interpreted as no API key.")]
    public string SeqApiKey { get; set; } = "";

    public LogEventLevel LogEventLevel { get; set; } = LogEventLevel.Verbose;

    [YamlMember(Description = "The default prefix for the bot.")]
    public string DefaultPrefix { get; set; } = "]";

    [YamlMember(Description = "The default UserAgent to use when making web requests.")]
    public string UserAgent { get; set; } = "Asahi/NoSetVersion (https://github.com/SquirrelKiev/Asahi)";
    [YamlMember(Description = "The App ID to use for the Wolfram command. Can get one from https://developer.wolframalpha.com/.")]
    public string WolframAppId { get; set; } = "";

    [YamlMember(Description = "Any users in this list are banned from ever making it to highlights.")]
    public HashSet<ulong> BannedHighlightsUsers { get; set; } = [];

    /// <summary>
    /// For any string here, the following will be replaced:
    /// - {{guilds}} will be substituted with how many guilds (servers) the bot is in.
    /// - {{botUsername}} will be substituted with the bot's username.
    /// </summary>
    [YamlMember(Description = "***** ABOUT PAGE *****\n" +
                              "For any string here, the following will be replaced:\n" +
                              "- {{guilds}} will be substituted with how many guilds (servers) the bot is in.\n" +
                              "- {{botUsername}} will be substituted with the bot's username.\n" +
                              "\n" +
                              "The about page title.")]
    public string AboutPageTitle { get; set; } = "About {{botUsername}}";

    /// <summary>
    /// For any string here, the following will be replaced:
    /// - {{guilds}} will be substituted with how many guilds (servers) the bot is in.
    /// - {{botUsername}} will be substituted with the bot's username.
    /// </summary>
    [YamlMember(Description = "The about page description.")]
    public string AboutPageDescription { get; set; } = "Various miscellaneous tools. " +
                                                                "Originally called Seigen and just had the trackables stuff, " +
                                                                "but has since expanded to include more.";


    [YamlMember(Description = "Fields within the about page.")]
    public AboutField[] AboutPageFields { get; set; } =
    [
        new AboutField
        {
            Name = "Servers",
            Value = "{{guilds}}"
        },
        new AboutField
        {
            Name = "Credits:",
            Value = "Bot by [enonibobble](https://github.com/SquirrelKiev)"
        },
        new AboutField
        {
            Name = "Source Code:",
            Value = "https://github.com/SquirrelKiev/Seigen"
        }
    ];

    public virtual bool IsValid()
    {
        try
        {
            TokenUtils.ValidateToken(TokenType.Bot, BotToken);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Supplied bot token is invalid.");
            return false;
        }

        return true;
    }

    public enum DatabaseType
    {
        Sqlite,
        Postgresql
    }

    public struct AboutField
    {
        /// <summary>
        /// For any string here, the following will be replaced:
        /// - {{guilds}} will be substituted with how many guilds (servers) the bot is in.
        /// - {{botUsername}} will be substituted with the bot's username.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// For any string here, the following will be replaced:
        /// - {{guilds}} will be substituted with how many guilds (servers) the bot is in.
        /// - {{botUsername}} will be substituted with the bot's username.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// For any string here, the following will be replaced:
        /// - {{guilds}} will be substituted with how many guilds (servers) the bot is in.
        /// - {{botUsername}} will be substituted with the bot's username.
        /// </summary>
        public bool Inline { get; set; }
    }
}