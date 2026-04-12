using Eco.Gameplay.GameActions;
using Eco.Moose.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class EcoExtensions
    {
        #region ChatMessage

        public static string FormatForLog(this ChatSent message) => $"Author: {message.Citizen.GetTagStrippedName()}\nChannel: {message.Tag}\nMessage: {message.Message}";

        #endregion
    }
}
