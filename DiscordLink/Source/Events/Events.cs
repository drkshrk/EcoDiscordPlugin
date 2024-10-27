using System;

namespace Eco.Plugins.DiscordLink.Events
{
    #pragma warning disable format
    [Flags]
    public enum DlEventType : System.UInt64
    {
        Timer                       = 1L << 0,
        ForceUpdate                 = 1L << 1,
        DiscordClientConnected      = 1L << 2,
        DiscordClientDisconnected   = 1L << 3,
        DiscordRolesGranted         = 1L << 4,
        DiscordRolesRevoked         = 1L << 5,
        ServerStarted               = 1L << 6,
        ServerStopped               = 1L << 7,
        WorldReset                  = 1L << 8,
        AccountLinkVerified         = 1L << 9,
        AccountLinkRemoved          = 1L << 10,
        EcoMessageSent              = 1L << 11,
        DiscordMessageSent          = 1L << 12,
        DiscordMessageEdited        = 1L << 13,
        DiscordMessageDeleted       = 1L << 14,
        DiscordReactionAdded        = 1L << 15,
        DiscordReactionRemoved      = 1L << 16,
        DiscordMemberRemoved        = 1L << 17,
        Join                        = 1L << 18,
        Login                       = 1L << 19,
        Logout                      = 1L << 20,
        Trade                       = 1L << 21,
        TradeWatcherDisplayAdded    = 1L << 22,
        TradeWatcherDisplayRemoved  = 1L << 23,
        WorkOrderCreated            = 1L << 24,
        PostedWorkParty             = 1L << 25,
        CompletedWorkParty          = 1L << 26,
        JoinedWorkParty             = 1L << 27,
        LeftWorkParty               = 1L << 28,
        WorkedWorkParty             = 1L << 29,
        Vote                        = 1L << 30,
        ElectionStarted             = 1L << 31,
        ElectionStopped             = 1L << 32,
        CurrencyCreated             = 1L << 33,
        EnteredDemographic          = 1L << 34,
        LeftDemographic             = 1L << 35,
        GainedSpecialty             = 1L << 36,
        LostSpecialty               = 1L << 37,
        AccumulatedServerLog        = 1L << 38,

        // Matched with other plugins
        SettlementFounded           = 1L << 61,
        AccumulatedTrade            = 1L << 62,
    }
    #pragma warning restore format
}
