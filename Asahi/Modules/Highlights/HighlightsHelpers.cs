﻿using System.Text;
using Asahi.Database.Models;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Asahi.Modules.Highlights;

public static class HighlightsHelpers
{
    public static List<MessageContents> QuoteMessage(IMessage message, Color embedColor, ILogger logger,
        bool webhookMode, bool spoilerAll = false)
    {
        var constantUrl = CatboxQts.GetRandomQtUrl();

        //var channelName = message.Channel is SocketThreadChannel threadChannel ? $"#{threadChannel.ParentChannel.Name} • " : "";

        var channelName = $"#{message.Channel.Name}";

        if (message.Channel is SocketThreadChannel threadChannel)
        {
            channelName += $" • #{threadChannel.ParentChannel.Name}";
        }

        var messageContent = message.Content;

        if (spoilerAll)
        {
            messageContent = $"||{messageContent.Replace("|", "\\|")}||";
        }

        var link = message.GetJumpUrl();
        var firstEmbed = new EmbedBuilder()
            .WithDescription(messageContent)
            .WithFooter(channelName)
            .WithTimestamp(message.Timestamp)
            .WithOptionalColor(embedColor)
            .WithUrl(constantUrl);

        if (!webhookMode)
        {
            firstEmbed.WithAuthor(message.Author);
        }

        List<EmbedBuilder> embeds = [firstEmbed];

        var queuedMessages = new List<MessageContents>();

        bool attachedImage = false;
        logger.LogTrace("There are {embedCount} embeds in this message.", message.Embeds.Count);

        foreach (var embed in message.Embeds)
        {
            //logger.LogTrace("embed: {embedJson}", JsonConvert.SerializeObject(embed, Formatting.Indented));

            switch (embed.Type)
            {
                case EmbedType.Image:
                    logger.LogTrace("Adding image embed.");

                    HandleImageEmbed(embed.Url, embeds, embedColor, ref attachedImage, constantUrl);
                    break;
                case EmbedType.Video:
                    logger.LogTrace("Queued video link message.");
                    queuedMessages.Add(new MessageContents(embed.Url, embed: null));
                    break;
                case EmbedType.Link:
                case EmbedType.Article:
                case EmbedType.Rich:
                    logger.LogTrace("Adding rich embed.");
                    embeds.Add(embed.ToEmbedBuilderForce());
                    break;
                default:
                    logger.LogTrace("skipping unknown embed type {embedType}", embed.Type);
                    break;
            }
        }

        var attachmentsValueContents = new StringBuilder();
        var i = 1;
        var tooManyAttachments = false;
        foreach (var attachment in message.Attachments)
        {
            logger.LogTrace("Found attachment {index}.", i);
            var txt = $"[Attachment {i}]({attachment.Url})";
            var tooManyAttachmentsText = $"Plus {message.Attachments.Count - i + 1} more.";
            if (!tooManyAttachments && attachmentsValueContents.Length + txt.Length > 1024 - tooManyAttachmentsText.Length)
            {
                attachmentsValueContents.AppendLine(tooManyAttachmentsText);
                tooManyAttachments = true;
            }
            else if (!tooManyAttachments)
            {
                attachmentsValueContents.AppendLine(txt);
            }

            if (attachment.ContentType.StartsWith("image"))
            {
                logger.LogTrace("attachment is image");

                if (spoilerAll || attachment.IsSpoiler())
                {
                    firstEmbed.Description += $"\n[[Spoiler Image]]({attachment.Url})";
                }
                else
                {
                    HandleImageEmbed(attachment.Url, embeds, embedColor, ref attachedImage, constantUrl);
                }
            }
            else if (attachment.ContentType.StartsWith("video"))
            {

                if (spoilerAll || attachment.IsSpoiler())
                {
                    logger.LogTrace("attachment is spoiler video");
                    firstEmbed.Description += $"\n[[Spoiler Video]]({attachment.Url})";
                }
                else
                {
                    logger.LogTrace("attachment is video, sending as queued message");
                    queuedMessages.Add(new MessageContents(attachment.Url, embed: null));
                }
            }

            i++;
        }

        if (attachmentsValueContents.Length != 0)
        {
            firstEmbed.AddField("Attachments", attachmentsValueContents.ToString().Truncate(1024, false));
        }

        foreach (var sticker in message.Stickers)
        {
            logger.LogTrace("found sticker, sending as image.");

            var stickerUrl = CDN.GetStickerUrl(sticker.Id, sticker.Format);

            HandleImageEmbed(stickerUrl, embeds, embedColor, ref attachedImage, constantUrl);
        }

        if (string.IsNullOrWhiteSpace(embeds[0].Description) && string.IsNullOrWhiteSpace(embeds[0].ImageUrl))
        {
            firstEmbed.WithDescription("*No content.*");
        }

        firstEmbed.AddField("Source", $"[Jump]({link})", true);

        queuedMessages.Insert(0, new MessageContents(
            webhookMode ? "" : link, embeds.Take(10).Select(x => x.Build()).ToArray(), null));

        return queuedMessages;
    }

    private static void HandleImageEmbed(string imageUrl,
        List<EmbedBuilder> embedBuilders,
        Color embedColor,
        ref bool isFirstImageAdded,
        string constantUrl)
    {
        if (!isFirstImageAdded)
        {
            embedBuilders[0].WithImageUrl(imageUrl);
            isFirstImageAdded = true;
        }
        else
        {
            var eb = new EmbedBuilder().WithImageUrl(imageUrl);
            if (!isFirstImageAdded)
            {
                eb.WithOptionalColor(embedColor);
            }

            eb.WithUrl(constantUrl);

            embedBuilders.Add(eb);
        }
    }

    public static async ValueTask<Color> GetQuoteEmbedColor(EmbedColorSource colorSource, Color fallbackColor, IGuildUser? embedAuthor, DiscordSocketClient client)
    {
        Color embedColor = fallbackColor;

        switch (colorSource)
        {
            case EmbedColorSource.UsersRoleColor:
                embedColor = embedAuthor?.RoleIds.Select(x => embedAuthor.Guild.GetRole(x)).FirstOrDefault(x => x.Color != Color.Default)?.Color ?? embedColor;
                break;
            case EmbedColorSource.UsersBannerColor:
                if (embedAuthor != null)
                {
                    var restUser = await client.Rest.GetUserAsync(embedAuthor.Id);

                    embedColor = restUser.BannerColor ?? embedColor;
                }
                break;
            case EmbedColorSource.UsersAccentColor:
                if (embedAuthor != null)
                {
                    var restUser = await client.Rest.GetUserAsync(embedAuthor.Id);

                    embedColor = restUser.AccentColor ?? embedColor;
                }
                break;
            case EmbedColorSource.AlwaysUseFallbackColor:
            default:
                break;
        }

        return embedColor;
    }

    public static EmbedBuilder ToEmbedBuilderForce(this IEmbed embed)
    {
        var imageUrl = embed.Image?.Url;
        var thumbnailUrl = embed.Thumbnail?.Url;
        if (embed is { Type: EmbedType.Article, Thumbnail.Url: not null })
        {
            imageUrl = embed.Thumbnail?.Url;
            thumbnailUrl = null;
        }

        var builder = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = embed.Author?.Name,
                IconUrl = embed.Author?.IconUrl,
                Url = embed.Author?.Url
            },
            Color = embed.Color,
            Description = embed.Description,
            Footer = new EmbedFooterBuilder
            {
                Text = embed.Footer?.Text,
                IconUrl = embed.Footer?.IconUrl
            },
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            Timestamp = embed.Timestamp,
            Title = embed.Title,
            Url = embed.Url
        };

        foreach (var field in embed.Fields)
            builder.AddField(field.Name, field.Value, field.Inline);

        return builder;
    }

    public static int CalculateThreshold(HighlightThreshold thresholdConfig,
        IReadOnlyCollection<HighlightsTrackingService.CachedMessage> messages,
        DateTimeOffset messageSentAt,
        out string debugInfo)
    {
        Dictionary<ulong, double> userWeights = [];

        var orderedMessages = messages
            .OrderByDescending(x => x.timestamp)
            .ToArray();

        var userWeightMessages = orderedMessages.Where(x => x.timestamp <= messageSentAt &&
                                                             x.timestamp >= messageSentAt - TimeSpan.FromSeconds(thresholdConfig.UniqueUserMessageMaxAgeSeconds))
            .ToArray();

        foreach (var message in userWeightMessages)
        {
            var userId = message.authorId;
            if (userWeights.ContainsKey(userId))
                continue;

            var timeSinceLastMessage = messageSentAt - message.timestamp;
            double weight = 1f;

            if (!(timeSinceLastMessage.TotalSeconds <= thresholdConfig.UniqueUserDecayDelaySeconds))
            {
                weight = 1 - (timeSinceLastMessage.TotalSeconds - thresholdConfig.UniqueUserDecayDelaySeconds) /
                    (thresholdConfig.UniqueUserMessageMaxAgeSeconds - thresholdConfig.UniqueUserDecayDelaySeconds);
            }

            userWeights.TryAdd(userId, weight);
        }

        var highActivity = orderedMessages.Length >= thresholdConfig.HighActivityMessageLookBack && 
                                           (messageSentAt - orderedMessages[thresholdConfig.HighActivityMessageLookBack - 1].timestamp)
                                           .TotalSeconds < thresholdConfig.HighActivityMessageMaxAgeSeconds;

        var weightedUserCount = userWeights.Sum(kvp => kvp.Value);

        var highActivityMultiplier = highActivity ? thresholdConfig.HighActivityMultiplier : 1f;

        var threshold = Math.Min(thresholdConfig.MaxThreshold, 
            (thresholdConfig.BaseThreshold + weightedUserCount * thresholdConfig.UniqueUserMultiplier) * highActivityMultiplier);

        debugInfo = $"Threshold is `{threshold}`. weighted users is `{weightedUserCount}`, unweighted users is `{userWeights.Count}`. " +
                    $"{(highActivity ? "`Channel is high activity!` " : "`Normal activity levels.` ")}" + 
                    $"Total of `{orderedMessages.Length}` messages cached, `{userWeightMessages.Length}` of which are being considered for unique user count.";

        return (int)Math.Ceiling(threshold);
    }
}