﻿using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public class SnippetInput : Input
    {
        public override void Initialize()
        {
            _ = ReloadSnippets();
            base.Initialize();
        }

        public override void OnConfigChanged()
        {
            _ = ReloadSnippets();
        }

        protected override TriggerType GetTriggers()
        {
            return TriggerType.DiscordMessage;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, TriggerType trigger, object data)
        {
            if (!(data is DiscordMessage message)) return;

            bool isSnippetChannel = false;
            foreach( ChannelLink link in DLConfig.Data.SnippetChannels)
            {
                if (link.DiscordGuild.ToLower() == message.Channel.Guild.Name.ToLower()
                    && link.DiscordChannel == message.Channel.Name )
                {
                    isSnippetChannel = true;
                    break;
                }
            }
            if (isSnippetChannel)
            {
                await ReloadSnippets();
            }
        }

        private async Task ReloadSnippets()
        {
            DiscordLink plugin = DiscordLink.Obj;
            if (plugin == null) return;
            foreach (ChannelLink snippetChannel in DLConfig.Data.SnippetChannels)
            {
                if (!snippetChannel.IsValid()) continue;
                DiscordGuild discordGuild = plugin.GuildByNameOrId(snippetChannel.DiscordGuild);
                if (discordGuild == null) continue;
                DiscordChannel discordChannel = discordGuild.ChannelByNameOrId(snippetChannel.DiscordChannel);
                if (discordChannel == null) continue;
                if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;

                IReadOnlyList<DiscordMessage> snippetChannelMessages = await DiscordUtil.GetMessagesAsync(discordChannel);
                if (snippetChannelMessages == null) continue;

                DLStorage.Instance.Snippets.Clear();
                foreach (DiscordMessage channelMessage in snippetChannelMessages)
                {
                    Match match = MessageUtil.SnippetRegex.Match(channelMessage.Content);
                    if (match.Groups.Count == 3)
                    {
                        DLStorage.Instance.Snippets.Add(match.Groups[1].Value.ToLower(), match.Groups[2].Value);
                    }
                }
            }
        }
    }
}
