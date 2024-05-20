﻿using System.ComponentModel.DataAnnotations;

namespace Asahi.Database.Models;

// not sure why you'd want multiple, but I want the db to be able to support it such the need comes up
// overengineered I know
// edit: maybe celebrating general anniversaries
//       "my discord account was created xyz date woo" type thing
public class BirthdayConfig
{
    [MaxLength(32)]
    public required string Name { get; set; }

    public required ulong GuildId { get; set; }

    public ulong BirthdayRole { get; set; } = 0ul;

    /// <summary>
    /// How long users are allowed to edit their birthday for once they set it. Set to zero for infinite time.
    /// </summary>
    public int EditWindowSeconds { get; set; } = 1800; // 30 minutes

    public string DisplayName { get; set; } = "birthday";

    public string EmbedTitleText { get; set; } = $"{UsernamePlaceholder}, your birthday has been set!";

    public string EmbedDescriptionText { get; set; } = "You will receive a hoisted birthday role when the day arrives!";

    public string EmbedFooterText { get; set; } = "To prevent abuse, you can change your birthday date for the next 30 minutes. " +
                                             "To change it after that, ask the mods.";

    public string DeniedText { get; set; } = "To prevent abuse, you're no longer allowed to change your set birthday. If you still wish to, please ask the mods.";

    public EmbedColorSource EmbedColorSource { get; set; } = EmbedColorSource.UsersRoleColor;

    /// <remarks>0 means no embed color.</remarks>>
    public uint FallbackEmbedColor { get; set; } = 0u;

    public const string UsernamePlaceholder = "{displayname}";
}