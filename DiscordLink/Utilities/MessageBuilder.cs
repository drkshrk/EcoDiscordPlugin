﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Plugins.Networking;

namespace Eco.Plugins.DiscordLink.Utilities
{
    static class MessageBuilder
    {
        public enum ServerInfoComponentFlag
        {
            Name = 1 << 0,
            Description = 1 << 1,
            Logo = 1 << 2,
            ServerAddress = 1 << 3,
            PlayerCount = 1 << 4,
            PlayerList = 1 << 5,
            TimeSinceStart = 1 << 6,
            TimeRemaining = 1 << 7,
            MeteorHasHit = 1 << 8,
            All = ~0
        }

        public static DiscordColor EmbedColor = DiscordColor.Green;

        public static string EmbedToText(string textContent, DiscordEmbed embedContent)
        {
            string message = "";
            if (!string.IsNullOrEmpty(textContent))
            {
                message += textContent + "\n\n";
            }
            if (embedContent != null)
            {
                if (!string.IsNullOrEmpty(embedContent.Title))
                {
                    message += embedContent.Title + "\n\n";
                }

                foreach (DiscordEmbedField field in embedContent.Fields)
                {
                    message += "**" + field.Name + "**\n" + field.Value + "\n\n";
                }

                if (!string.IsNullOrEmpty(embedContent.Footer?.Text))
                {
                    message += embedContent.Footer.Text;
                }
            }
            return message.Trim();
        }

        public static DiscordEmbed GetServerInfo(ServerInfoComponentFlag flag)
        {
            var plugin = DiscordLink.Obj;
            if (plugin == null) return null;

            var config = DLConfig.Data;
            var serverInfo = NetworkManager.GetServerInfo();

            var builder = new DiscordEmbedBuilder();

            builder.WithColor(EmbedColor);

            if (flag.HasFlag(ServerInfoComponentFlag.Name))
            {
                builder.WithTitle($"**{FirstNonEmptyString(config.ServerName, MessageUtil.StripTags(serverInfo.Description), "[Server Title Missing]")} " + "Server Status" + "**\n" + DateTime.Now.ToShortDateString() + " : " + DateTime.Now.ToShortTimeString());
            }
            else
            {
                DateTime time = DateTime.Now;
                int utcOffset = TimeZoneInfo.Local.GetUtcOffset(time).Hours;
                builder.WithTitle("**" + "Server Status" + "**\n" + "[" + DateTime.Now.ToString("yyyy-MM-dd : HH:mm", CultureInfo.InvariantCulture) + " UTC " + (utcOffset != 0 ? (utcOffset >= 0 ? "+" : "-") + utcOffset : "") + "]");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.Description))
            {
                builder.WithDescription(FirstNonEmptyString(config.ServerDescription, MessageUtil.StripTags(serverInfo.Description), "No server description is available."));
            }

            if (flag.HasFlag(ServerInfoComponentFlag.Logo) && !string.IsNullOrWhiteSpace(config.ServerLogo))
            {
                try
                {
                    builder.WithThumbnail(config.ServerLogo);
                }
                catch (UriFormatException e)
                {
                    Logger.Debug("Failed to include thumbnail in Server Info embed. Error: " + e);
                }
            }

            if (flag.HasFlag(ServerInfoComponentFlag.ServerAddress))
            {
                string fieldText = "-- No address configured --";
                if (!string.IsNullOrEmpty(config.ServerAddress))
                {
                    fieldText = config.ServerAddress;
                }
                else if (!string.IsNullOrEmpty(serverInfo.Address))
                {
                    fieldText = serverInfo.Address;
                }
                builder.AddField("Server Address", fieldText);
            }

            if (flag.HasFlag(ServerInfoComponentFlag.PlayerCount))
            {
                builder.AddField("Online Players", $"{UserManager.OnlineUsers.Where(user => user.Client.Connected).Count()}/{serverInfo.TotalPlayers}");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.PlayerList))
            {
                IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
                string playerList = onlineUsers.Count() > 0 ? string.Join("\n", onlineUsers) : "-- No players online --";
                builder.AddField("Online Players", GetPlayerList());
            }

            if (flag.HasFlag(ServerInfoComponentFlag.TimeSinceStart))
            {
                TimeSpan timeSinceStartSpan = new TimeSpan(0, 0, (int)serverInfo.TimeSinceStart);
                builder.AddField("Time Since Game Start", $"{timeSinceStartSpan.Days} Days, {timeSinceStartSpan.Hours} hours, {timeSinceStartSpan.Minutes} minutes");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.TimeRemaining))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                bool meteorHasHit = timeRemainingSpan.Seconds < 0;
                timeRemainingSpan = meteorHasHit ? new TimeSpan(0, 0, 0) : timeRemainingSpan;
                builder.AddField("Time Left Until Meteor", $"{timeRemainingSpan.Days} Days, {timeRemainingSpan.Hours} hours, {timeRemainingSpan.Minutes} minutes");
            }

            if (flag.HasFlag(ServerInfoComponentFlag.MeteorHasHit))
            {
                TimeSpan timeRemainingSpan = new TimeSpan(0, 0, (int)serverInfo.TimeLeft);
                builder.AddField("Meteor Has Hit", timeRemainingSpan.Seconds < 0 ? "Yes" : "No");
            }

            return builder.Build();
        }

        public static string GetPlayerCount()
        {
            IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
            int numberTotal = NetworkManager.GetServerInfo().TotalPlayers;
            int numberOnline = onlineUsers.Count();
            return $"{numberOnline}/{numberTotal}";
        }

        public static string GetPlayerList()
        {
            IEnumerable<string> onlineUsers = UserManager.OnlineUsers.Where(user => user.Client.Connected).Select(user => user.Name);
            string playerList = onlineUsers.Count() > 0 ? string.Join("\n", onlineUsers) : "-- No players online --";
            return playerList;
        }

        public static string GetAboutMessage()
        {
            return "DiscordLink is a plugin mod that runs on this server." +
                "\nIt connects the game server to a Discord bot in order to perform seamless communication between Eco and Discord." +
                "\nThis enables you to chat with players who are currently not online in Eco, but are available on Discord." +
                "\nDiscordLink can also be used to display information about the Eco server in Discord, such as who is online and what items are available on the market." +
                "\n\nFor more information, visit \"www.github.com/Eco-DiscordLink/EcoDiscordPlugin\".";
        }

        private static string FirstNonEmptyString(params string[] strings)
        {
            return strings.FirstOrDefault(str => !string.IsNullOrEmpty(str)) ?? "";
        }
    }
}
