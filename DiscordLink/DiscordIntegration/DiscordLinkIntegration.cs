﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Eco.Plugins.DiscordLink.IntegrationTypes
{
    public enum TriggerType
    {
        Timer   = 1 << 0,
        Startup = 1 << 1,
        Login   = 1 << 2,
        Trade   = 1 << 3,
    }

    public abstract class DiscordLinkIntegration
    {
        protected TriggerType _triggerTypeField = 0;
        protected object _updateLock = new object();
        public DiscordLinkIntegration()
        {
            Initialize();
        }

        ~DiscordLinkIntegration()
        {
            Shutdown();
        }

        public virtual void Initialize()
        { }

        public virtual void Shutdown()
        { }

        public virtual void OnConfigChanged()
        { }

        public virtual void Update(DiscordLink plugin, TriggerType trigger, object data)
        {
            if ((_triggerTypeField & trigger) == 0) return;
            if (plugin == null) return;

            lock (_updateLock) // Make sure that the Update function doesn't get overlapping executions
            {
                UpdateInternal(plugin, trigger, data);
            }
        }

        protected abstract void UpdateInternal(DiscordLink plugin, TriggerType trigger, object data);
    }
}
