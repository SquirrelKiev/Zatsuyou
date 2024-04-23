﻿// <auto-generated />
using Asahi.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Asahi.Migrations.SqliteMigrations
{
    [DbContext(typeof(SqliteContext))]
    partial class SqliteContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("Asahi.Database.Models.CachedHighlightedMessage", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("HighlightBoardGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HighlightBoardName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("HighlightMessageIds")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("OriginalMessageChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("OriginalMessageId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("HighlightMessageIds")
                        .IsUnique();

                    b.HasIndex("HighlightBoardGuildId", "HighlightBoardName");

                    b.ToTable("CachedHighlightedMessages");
                });

            modelBuilder.Entity("Asahi.Database.Models.CachedUserRole", b =>
                {
                    b.Property<ulong>("RoleId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.HasKey("RoleId", "UserId");

                    b.HasIndex("RoleId");

                    b.ToTable("CachedUsersRoles");
                });

            modelBuilder.Entity("Asahi.Database.Models.CustomCommand", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Contents")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsRaw")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("OwnerId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("CustomCommands");
                });

            modelBuilder.Entity("Asahi.Database.Models.EmoteAlias", b =>
                {
                    b.Property<string>("EmoteName")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<ulong>("HighlightBoardGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HighlightBoardName")
                        .HasColumnType("TEXT");

                    b.Property<string>("EmoteReplacement")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("EmoteName", "HighlightBoardGuildId", "HighlightBoardName");

                    b.HasIndex("HighlightBoardGuildId", "HighlightBoardName");

                    b.ToTable("EmoteAlias");
                });

            modelBuilder.Entity("Asahi.Database.Models.GuildConfig", b =>
                {
                    b.Property<ulong>("GuildId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Prefix")
                        .IsRequired()
                        .HasMaxLength(8)
                        .HasColumnType("TEXT");

                    b.HasKey("GuildId");

                    b.ToTable("GuildConfigs");
                });

            modelBuilder.Entity("Asahi.Database.Models.HighlightBoard", b =>
                {
                    b.Property<ulong>("GuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<int>("AutoReactEmoteChoicePreference")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AutoReactFallbackEmoji")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<int>("AutoReactMaxAttempts")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AutoReactMaxReactions")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EmbedColorSource")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("FallbackEmbedColor")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("FilterSelfReactions")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FilteredChannels")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<bool>("FilteredChannelsIsBlockList")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("HighlightsMuteRole")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("LoggingChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MaxMessageAgeSeconds")
                        .HasColumnType("INTEGER");

                    b.HasKey("GuildId", "Name");

                    b.ToTable("HighlightBoards");
                });

            modelBuilder.Entity("Asahi.Database.Models.HighlightThreshold", b =>
                {
                    b.Property<ulong>("OverrideId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("HighlightBoardGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("HighlightBoardName")
                        .HasColumnType("TEXT");

                    b.Property<int>("BaseThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HighActivityMessageLookBack")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HighActivityMessageMaxAgeSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<float>("HighActivityMultiplier")
                        .HasColumnType("REAL");

                    b.Property<int>("MaxThreshold")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UniqueUserDecayDelaySeconds")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UniqueUserMessageMaxAgeSeconds")
                        .HasColumnType("INTEGER");

                    b.Property<float>("UniqueUserMultiplier")
                        .HasColumnType("REAL");

                    b.HasKey("OverrideId", "HighlightBoardGuildId", "HighlightBoardName");

                    b.HasIndex("HighlightBoardGuildId", "HighlightBoardName");

                    b.ToTable("HighlightThreshold");
                });

            modelBuilder.Entity("Asahi.Database.Models.Trackable", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("AssignableGuild")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("AssignableRole")
                        .HasColumnType("INTEGER");

                    b.Property<uint>("Limit")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("LoggingChannel")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MonitoredGuild")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MonitoredRole")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AssignableRole", "MonitoredRole")
                        .IsUnique();

                    b.ToTable("Trackables");
                });

            modelBuilder.Entity("Asahi.Database.Models.TrackedUser", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<uint>("TrackableId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TrackableId", "UserId")
                        .IsUnique();

                    b.ToTable("TrackedUsers");
                });

            modelBuilder.Entity("Asahi.Database.Models.CachedHighlightedMessage", b =>
                {
                    b.HasOne("Asahi.Database.Models.HighlightBoard", "HighlightBoard")
                        .WithMany("HighlightedMessages")
                        .HasForeignKey("HighlightBoardGuildId", "HighlightBoardName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("HighlightBoard");
                });

            modelBuilder.Entity("Asahi.Database.Models.EmoteAlias", b =>
                {
                    b.HasOne("Asahi.Database.Models.HighlightBoard", "HighlightBoard")
                        .WithMany("EmoteAliases")
                        .HasForeignKey("HighlightBoardGuildId", "HighlightBoardName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("HighlightBoard");
                });

            modelBuilder.Entity("Asahi.Database.Models.HighlightThreshold", b =>
                {
                    b.HasOne("Asahi.Database.Models.HighlightBoard", "HighlightBoard")
                        .WithMany("Thresholds")
                        .HasForeignKey("HighlightBoardGuildId", "HighlightBoardName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("HighlightBoard");
                });

            modelBuilder.Entity("Asahi.Database.Models.TrackedUser", b =>
                {
                    b.HasOne("Asahi.Database.Models.Trackable", "Trackable")
                        .WithMany()
                        .HasForeignKey("TrackableId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Trackable");
                });

            modelBuilder.Entity("Asahi.Database.Models.HighlightBoard", b =>
                {
                    b.Navigation("EmoteAliases");

                    b.Navigation("HighlightedMessages");

                    b.Navigation("Thresholds");
                });
#pragma warning restore 612, 618
        }
    }
}
