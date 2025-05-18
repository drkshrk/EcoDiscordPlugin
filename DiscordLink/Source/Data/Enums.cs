using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;

namespace Eco.Plugins.DiscordLink
{
    #region Non-Game
    public enum ApplicationInterfaceType
    {
        [ChoiceDisplayName("Eco")]
        Eco,
        [ChoiceDisplayName("Discord")]
        Discord
    }

    public enum GlobalMentionPermission
    {
        AnyUser,        // Any user may use mentions
        Admin,          // Only admins may use mentions
        Forbidden       // All use of mentions is forbidden
    };

    public enum DiscordReactionChange
    {
        Added,
        Removed
    }

    public enum ChatSyncDirection
    {
        DiscordToEco,   // Send Discord messages to Eco
        EcoToDiscord,   // Send Eco messages to Discord
        Duplex,         // Send Discord messages to Eco and Eco messages to Discord
    }

    public enum ChatSyncMode
    {
        OptIn,
        OptOut,
    }

    #endregion

    #region Game

    public enum CurrencyType
    {
        [ChoiceDisplayName("All")]
        All,
        [ChoiceDisplayName("Personal")]
        Personal,
        [ChoiceDisplayName("Minted")]
        Minted
    }

    public enum CurrencyTypeDisplayCondition
    {
        Never,          // Never show the currency type
        MintedExists,   // Only show the currency type if a minted currency exists
        NoMintedExists, // Do NOT show the currency type if a minted currency exists
        Always,         // Always show the curreny type
    }

    public enum MapRepresentationType
    {
        [ChoiceDisplayName("Preview")]
        Preview,
        [ChoiceDisplayName("Terrain")]
        Terrain
    }

    #endregion
}
