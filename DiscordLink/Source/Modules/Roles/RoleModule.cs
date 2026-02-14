using DSharpPlus;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Events;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public abstract class RoleModule : Module
    {
        protected override DlEventType GetTriggers()
        {
            return DlEventType.ForceUpdate;
        }

        protected override async Task<bool> ShouldRun()
        {
            return DiscordLink.Obj.Client.BotHasPermission(DiscordPermissions.ManageRoles);
        }
    }
}
