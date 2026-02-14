using DSharpPlus.Entities;
using Eco.Gameplay.Objects;
using Eco.Plugins.DiscordLink.Events;
using Eco.Shared.Serialization;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class ChannelLink : DiscordTarget, ICloneable
    {
        [Description("Discord channel by id.")]
        [TypeConverter(typeof(DiscordChannelPropertyConverter))]
        public ulong DiscordChannelId
        {
            get { return _discordChannelId; }
            set
            {
                _discordChannelId = value;

                // Init can only with an active Discord connection - We handle runtime inits here and startup inits from the config system
                if (DiscordLink.Obj.Status != StatusState.Connected)
                    return;

                if(Initialize() && OnChannelLinkVerifiedInRuntime != null)
                {
                    OnChannelLinkVerifiedInRuntime.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private ulong _discordChannelId = 0;

        [Browsable(false), JsonIgnore]
        public DiscordChannel Channel { get; private set; }

        [Browsable(false), JsonIgnore]
        public bool Verified { get; private set; }

        public delegate Task OnChannelLinkVerifiedDelegate(object sender, EventArgs e);
        public static event OnChannelLinkVerifiedDelegate OnChannelLinkVerifiedInRuntime;

        public ChannelLink()
        {
            Reset();
        }

        public override string ToString()
        {
            return IsValid() ? $"#{Channel.Name}" : $"<Unknown Channel Name> ({DiscordChannelId})";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }

        public override bool IsValid() => DiscordChannelId != 0 && Channel != null;

        public virtual bool Initialize()
        {
            if (DiscordChannelId == 0)
                return false;

            DiscordChannel channel = DiscordLink.Obj.Client.GetChannelById(DiscordChannelId);
            if (channel == null)
                return false;

            Channel = channel;
            Verified = true;

            return true;
        }

        protected virtual void Reset()
        {
            Channel = null;
            Verified = false;
        }

        public virtual bool MakeCorrections()
        {
            return false;
        }

        public bool IsChannel(DiscordChannel channel)
        {
            return DiscordChannelId == channel.Id;
        }
    }
}
