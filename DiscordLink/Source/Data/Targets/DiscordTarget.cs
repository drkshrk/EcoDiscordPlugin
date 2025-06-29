using System;
using System.ComponentModel;

namespace Eco.Plugins.DiscordLink
{
    public abstract class DiscordTarget
    {
        [Browsable(false), Description("Generated unique identifier")]
        public Guid Id { get; set; } = Guid.NewGuid();

        public abstract bool IsValid();
    }
}
