using System;

namespace Eco.Plugins.DiscordLink.Events
{
    #pragma warning disable format
    [Flags]
    public enum DlEventType : System.UInt64
    {
        Timer                       = 1L << 0,
        ManualStart                 = 1L << 1,
        ForceUpdate                 = 1L << 2,
        DiscordClientConnected      = 1L << 3,
        DiscordClientDisconnected   = 1L << 4,
        DiscordRolesGranted         = 1L << 5,
        DiscordRolesRevoked         = 1L << 6,
        ServerStarted               = 1L << 7,
        ServerStopped               = 1L << 8,
        WorldReset                  = 1L << 9,
        AccountLinkVerified         = 1L << 10,
        AccountLinkRemoved          = 1L << 11,
        EcoMessageSent              = 1L << 12,
        DiscordMessageSent          = 1L << 13,
        DiscordMessageEdited        = 1L << 14,
        DiscordMessageDeleted       = 1L << 15,
        DiscordReactionAdded        = 1L << 16,
        DiscordReactionRemoved      = 1L << 17,
        DiscordMemberRemoved        = 1L << 18,
        Join                        = 1L << 19,
        Login                       = 1L << 20,
        Logout                      = 1L << 21,
        Trade                       = 1L << 22,
        TradeWatcherDisplayAdded    = 1L << 23,
        TradeWatcherDisplayRemoved  = 1L << 24,
        WorkOrderCreated            = 1L << 25,
        PostedWorkParty             = 1L << 26,
        CompletedWorkParty          = 1L << 27,
        JoinedWorkParty             = 1L << 28,
        LeftWorkParty               = 1L << 29,
        WorkedWorkParty             = 1L << 30,
        Vote                        = 1L << 31,
        ElectionStarted             = 1L << 32,
        ElectionStopped             = 1L << 33,
        CurrencyCreated             = 1L << 34,
        EnteredDemographic          = 1L << 35,
        LeftDemographic             = 1L << 36,
        GainedSpecialty             = 1L << 37,
        LostSpecialty               = 1L << 38,
        LeveledUpSpecialty          = 1L << 39,
        AccumulatedServerLog        = 1L << 40,

        // Matched with other plugins
        SettlementFounded           = 1L << 61,
        AccumulatedTrade            = 1L << 62,
    }
    #pragma warning restore format
}
