<<<<<<< HEAD
﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
=======
﻿using DSharpPlus;
>>>>>>> upstream/master
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Eco.Core;
using Eco.Core.Plugins;
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
<<<<<<< HEAD
using Eco.Gameplay.Systems.Chat;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IConfigurablePlugin, IShutdownablePlugin, IGameActionAware
    {
        public readonly Version PluginVersion = new Version(2, 0, 1);

        public const string InviteCommandLinkToken = "[LINK]";
        public const string EchoCommandToken = "[ECHO]";
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        protected string NametagColor = "7289DAFF";
        private PluginConfig<DiscordConfig> _configOptions;
        private DiscordConfig _prevConfigOptions; // Used to detect differences when the config is saved
        private DiscordClient _discordClient;
        private CommandsNextExtension _commands;
        private Timer _ecoStatusStartupTimer = null;
        private Timer _guildVerificationOutputTimer = null;
        private Timer _staticVerificationOutputDelayTimer = null;
        private string _currentBotToken;
        private string _status = "No Connection Attempt Made";
        private ChatLogger _chatLogger = new ChatLogger();

        // Finds the tags used by Eco message formatting (color codes, badges, links etc)
        private static readonly Regex EcoNameTagRegex = new Regex("<[^>]*>");

        // Discord mention matching regex: Match all characters followed by a mention character(@ or #) character (including that character) until encountering any type of whitespace, end of string or a new mention character
        private static readonly Regex DiscordMentionRegex = new Regex("([@#].+?)(?=\\s|$|@|#)");
=======
using Eco.Plugins.DiscordLink.IntegrationTypes;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordLink : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin, IConfigurablePlugin, IGameActionAware
    {
        public readonly Version PluginVersion = new Version(2, 1, 0);

        private const int FIRST_DISPLAY_UPDATE_DELAY_MS = 20000;

        public event EventHandler OnClientStarted;
        public event EventHandler OnClientStopped;
        public event EventHandler OnDiscordMaybeReady;

        public static DiscordLink Obj { get { return PluginManager.GetPlugin<DiscordLink>(); } }
        public static string BasePath { get { return Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\"; } }
        public ThreadSafeAction<object, string> ParamChanged { get; set; }
        public DiscordClient DiscordClient { get; private set; }
        public const string EchoCommandToken = "[ECHO]";

        private string _status = "No Connection Attempt Made";
        private readonly List<DiscordLinkIntegration> _integrations = new List<DiscordLinkIntegration>();
        private CommandsNextExtension _commands;
        private Timer _discordDataMaybeAvailable = null;
        private Timer _tradePostingTimer = null;
>>>>>>> upstream/master

        public override string ToString()
        {
            return "DiscordLink";
        }

<<<<<<< HEAD
        public IPluginConfig PluginConfig
        {
            get { return _configOptions; }
        }

        public DiscordConfig DiscordPluginConfig
        {
            get { return PluginConfig.GetConfig() as DiscordConfig; }
        }

        public string GetStatus()
        {
            return _status;
=======
        public string GetStatus()
        {
            return _status;
        }

        public IPluginConfig PluginConfig
        {
            get { return DLConfig.Instance.PluginConfig; }
        }

        public object GetEditObject()
        {
            return DLConfig.Data;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            DLConfig.Instance.HandleConfigChanged();
>>>>>>> upstream/master
        }

        public void Initialize(TimedTask timer)
        {
<<<<<<< HEAD
            Logger.Info("Plugin version is " + PluginVersion);

            SetupConfig();
=======
            SetupConfig();
            Logger.Initialize();
            Logger.Info("Plugin version is " + PluginVersion);

>>>>>>> upstream/master
            if (!SetUpClient())
            {
                return;
            }

            ConnectAsync().Wait();

<<<<<<< HEAD
            if (_configOptions.Config.LogChat)
            {
                _chatLogger.Start();
            }
=======
            // Triggered on a timer that starts when the Discord client connects.
            // It is likely that the client object has fetched all the relevant data, but there are not guarantees.
            OnDiscordMaybeReady += (obj, args) =>
            {
                InitializeIntegrations();
                UpdateIntegrations(TriggerType.Startup, null);
            };
>>>>>>> upstream/master
        }

        public void Shutdown()
        {
<<<<<<< HEAD
            if (_configOptions.Config.LogChat)
            {
                _chatLogger.Stop();
            }
        }

        private void SetupConfig()
        {
            _configOptions = new PluginConfig<DiscordConfig>("DiscordLink");
            _prevConfigOptions = (DiscordConfig)_configOptions.Config.Clone();
            DiscordPluginConfig.PlayerConfigs.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            DiscordPluginConfig.ChatChannelLinks.CollectionChanged += (obj, args) => { OnConfigChanged(); };
            DiscordPluginConfig.EcoStatusDiscordChannels.CollectionChanged += (obj, args) => { OnConfigChanged(); };
=======
            ShutdownIntegrations();
            Logger.Shutdown();
        }

        private void SetupConfig()
        {
            DLConfig config = DLConfig.Instance;
            config.Initialize();
            config.OnTokenChanged += (obj, args) =>
            {
                Logger.Info("Discord Bot Token changed - Reinitialising client");
                _ = RestartClient();
            };
            config.OnConfigChanged += (obj, args) =>
            {
                _integrations.ForEach(async integration => await integration.OnConfigChanged());
            };
        }

        public void ActionPerformed(GameAction action)
        {
            switch (action)
            {
                case ChatSent chatSent:
                    OnMessageReceivedFromEco(chatSent);
                    break;
            
                case FirstLogin firstLogin:
                    UpdateIntegrations(TriggerType.Login, firstLogin);
                    break;
            
                case Play play:
                    UpdateIntegrations(TriggerType.Login, play);
                    break;
            
                case CurrencyTrade currencyTrade:
                    UpdateIntegrations(TriggerType.Trade, currencyTrade);
                    break;
            
                case PostedWorkParty postedWorkParty:
                    UpdateIntegrations(TriggerType.PostedWorkParty, postedWorkParty);
                    break;
            
                case CompletedWorkParty completedWorkParty:
                    UpdateIntegrations(TriggerType.CompletedWorkParty, completedWorkParty);
                    break;
            
                case JoinedWorkParty joinedWorkParty:
                    UpdateIntegrations(TriggerType.JoinedWorkParty, joinedWorkParty);
                    break;
            
                case LeftWorkParty leftWorkParty:
                    UpdateIntegrations(TriggerType.LeftWorkParty, leftWorkParty);
                    break;
            
                case WorkedForWorkParty workedParty:
                    UpdateIntegrations(TriggerType.WorkedWorkParty, workedParty);
                    break;

                case Vote vote:
                    UpdateIntegrations(TriggerType.Vote, vote);
                    break;

                case StartElection startElection:
                    UpdateIntegrations(TriggerType.StartElection, startElection);
                    break;

                case LostElection lostElection:
                    UpdateIntegrations(TriggerType.StopElection, lostElection);
                    break;

                case WonElection wonElection:
                    UpdateIntegrations(TriggerType.StopElection, wonElection);
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }

        void InitializeIntegrations()
        {
            _integrations.Add(new DiscordChatFeed());   // Discord -> Eco
            _integrations.Add(new EcoChatFeed());       // Eco -> Discord
            _integrations.Add(new ChatlogFeed());
            _integrations.Add(new ServerInfoDisplay());
            _integrations.Add(new TradeFeed());
            _integrations.Add(new SnippetInput());
            _integrations.Add(new WorkPartyDisplay());
            _integrations.Add(new PlayerDisplay());
            _integrations.Add(new ElectionDisplay());

            _integrations.ForEach(async integration => await integration.Initialize());
        }

        void ShutdownIntegrations()
        {
            _integrations.ForEach(async integration => await integration.Shutdown());
            _integrations.Clear();
        }
        
        void UpdateIntegrations(TriggerType trigger, object data)
        {
            _integrations.ForEach(async integration => await integration.Update(this, trigger, data));
>>>>>>> upstream/master
        }

        #region DiscordClient Management

        private bool SetUpClient()
        {
            _status = "Setting up client";

<<<<<<< HEAD
            bool BotTokenIsNull = String.IsNullOrWhiteSpace(DiscordPluginConfig.BotToken);
            if (BotTokenIsNull)
            {
                VerifyConfig(VerificationFlags.Static); // Make the user aware of the empty bot token
            }
            _currentBotToken = DiscordPluginConfig.BotToken;

            if(BotTokenIsNull) return false; // Do not attempt to initialize if the bot token is empty
=======
            bool BotTokenIsNull = string.IsNullOrWhiteSpace(DLConfig.Data.BotToken);
            if (BotTokenIsNull)
            {
                DLConfig.Instance.VerifyConfig(DLConfig.VerificationFlags.Static); // Make the user aware of the empty bot token
            }

            if (BotTokenIsNull) return false; // Do not attempt to initialize if the bot token is empty
>>>>>>> upstream/master

            try
            {
                // Create the new client
<<<<<<< HEAD
                _discordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = _currentBotToken,
                    TokenType = TokenType.Bot
                });

                _discordClient.ClientErrored += async args => { Logger.Error("A Discord client error occurred. Error messages was: " + args.EventName + " " + args.Exception.ToString()); };
                _discordClient.SocketErrored += async args => { Logger.Error("A socket error occurred. Error message was: " + args.Exception.ToString()); };
                _discordClient.SocketClosed += async args => { Logger.DebugVerbose("Socket Closed: " + args.CloseMessage + " " + args.CloseCode); };
                _discordClient.Resumed += async args => { Logger.Info("Resumed connection"); };
                _discordClient.Ready += async args =>
                {
                    // Queue up the check for unverified channels
                    _linkVerificationTimeoutTimer = new Timer(async innerArgs =>
                    {
                        _linkVerificationTimeoutTimer = null;
                        ReportUnverifiedChannels();
                        _verifiedLinks.Clear();
                    }, null, LINK_VERIFICATION_TIMEOUT_MS, Timeout.Infinite);

                    // Run EcoStatus once when the server has started
                    _ecoStatusStartupTimer = new Timer(async innerArgs =>
                    {
                        _ecoStatusStartupTimer = null;
                        UpdateEcoStatus();
                    }, null, ECO_STATUS_FIRST_UPDATE_DELAY_MS, Timeout.Infinite);

                    // Avoid writing async while the server is still outputting initialization info
                    _staticVerificationOutputDelayTimer = new Timer(async innerArgs =>
                    {
                        _staticVerificationOutputDelayTimer = null;
                         VerifyConfig(VerificationFlags.Static);
                    }, null, STATIC_VERIFICATION_OUTPUT_DELAY_MS, Timeout.Infinite);
                };
                _discordClient.GuildAvailable += async args =>
                {
                    _guildVerificationOutputTimer = new Timer(async innerArgs =>
                    {
                        _guildVerificationOutputTimer = null;
                        VerifyConfig(VerificationFlags.ChannelLinks);
                    }, null, GUILD_VERIFICATION_OUTPUT_DELAY_MS, Timeout.Infinite);
                };

                // Set up the client to use CommandsNext
                _commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefixes = _configOptions.Config.DiscordCommandPrefix.SingleItemAsEnumerable()
                });
                _commands.RegisterCommands<DiscordDiscordCommands>();

                _ecoStatusUpdateTimer = new Timer(this.UpdateEcoStatusOnTimer, null, 0, ECO_STATUS_TIMER_INTERAVAL_MS);

=======
                DiscordClient = new DiscordClient(new DiscordConfiguration
                {
                    AutoReconnect = true,
                    Token = DLConfig.Data.BotToken,
                    TokenType = TokenType.Bot,
                    MinimumLogLevel = DLConfig.Data.BackendLogLevel
                });

                DiscordClient.ClientErrored += async args => { Logger.Error("A Discord client error occurred. Error messages was: " + args.EventName + " " + args.Exception.ToString()); };
                DiscordClient.SocketErrored += async args => { Logger.Error("A socket error occurred. Error message was: " + args.Exception.ToString()); };
                DiscordClient.SocketClosed += async args => { Logger.DebugVerbose("Socket Closed: " + args.CloseMessage + " " + args.CloseCode); };
                DiscordClient.Resumed += async args => { Logger.Info("Resumed connection"); };
                DiscordClient.Ready += async args =>
                {
                    DLConfig.Instance.EnqueueFullVerification();

                    _discordDataMaybeAvailable = new Timer(innerArgs =>
                    {
                        OnDiscordMaybeReady?.Invoke(this, EventArgs.Empty);
                        SystemUtil.StopAndDestroyTimer(ref _discordDataMaybeAvailable);
                    }, null, FIRST_DISPLAY_UPDATE_DELAY_MS, Timeout.Infinite);
                };

                DiscordClient.GuildAvailable += async args =>
                {
                    DLConfig.Instance.EnqueueGuildVerification();
                };

                DiscordClient.MessageDeleted += async args =>
                {
                    _integrations.ForEach(async integration => await integration.OnMessageDeleted(args.Message));
                };

                // Set up the client to use CommandsNext
                _commands = DiscordClient.UseCommandsNext(new CommandsNextConfiguration
                {
                    StringPrefixes = DLConfig.Data.DiscordCommandPrefix.SingleItemAsEnumerable()
                });
                _commands.RegisterCommands<DiscordCommands>();

                OnClientStarted?.Invoke(this, EventArgs.Empty);
>>>>>>> upstream/master
                return true;
            }
            catch (Exception e)
            {
<<<<<<< HEAD
                Logger.Error("Error occurred while creating the Discord client. Error message: " + e );
=======
                Logger.Error("Error occurred while creating the Discord client. Error message: " + e);
>>>>>>> upstream/master
            }

            return false;
        }

        void StopClient()
        {
            // Stop various timers that may have been set up so they do not trigger while the reset is ongoing
<<<<<<< HEAD
            SystemUtil.StopAndDestroyTimer(ref _linkVerificationTimeoutTimer);
            SystemUtil.StopAndDestroyTimer(ref _ecoStatusStartupTimer);
            SystemUtil.StopAndDestroyTimer(ref _ecoStatusUpdateTimer);
            SystemUtil.StopAndDestroyTimer(ref _staticVerificationOutputDelayTimer);
            SystemUtil.StopAndDestroyTimer(ref _guildVerificationOutputTimer);

            // Clear all the stored message references as they may become invalid if the token has changed
            _ecoStatusMessages.Clear();

            // If we were waiting to verify channel links, we need to clear this list or risk false positives
            _verifiedLinks.Clear();

            if (_discordClient != null)
=======
            DLConfig.Instance.DequeueAllVerification();
            SystemUtil.StopAndDestroyTimer(ref _discordDataMaybeAvailable);
            SystemUtil.StopAndDestroyTimer(ref _tradePostingTimer);

            ShutdownIntegrations();

            if (DiscordClient != null)
>>>>>>> upstream/master
            {
                StopRelaying();

                // If DisconnectAsync() is called in the GUI thread, it will cause a deadlock
<<<<<<< HEAD
                SystemUtil.SynchronousThreadExecute( () =>
                {
                    _discordClient.DisconnectAsync().Wait();
                });
                _discordClient.Dispose();
                _discordClient = null;
=======
                SystemUtil.SynchronousThreadExecute(() =>
                {
                    DiscordClient.DisconnectAsync().Wait();
                });
                DiscordClient.Dispose();
                DiscordClient = null;

                OnClientStopped?.Invoke(this, EventArgs.Empty);
>>>>>>> upstream/master
            }
        }

        public async Task<bool> RestartClient()
        {
            StopClient();
            bool result = SetUpClient();
            if (result)
            {
                await ConnectAsync();
            }
            return result;
        }

        public async Task<object> ConnectAsync()
        {
            try
            {
                _status = "Attempting connection...";
<<<<<<< HEAD
                await _discordClient.ConnectAsync();
=======
                await DiscordClient.ConnectAsync();
>>>>>>> upstream/master
                BeginRelaying();
                Logger.Info("Connected to Discord");
                _status = "Connection successful";
            }
            catch (Exception e)
            {
                Logger.Error("Error occurred when connecting to Discord: Error message: " + e.Message);
                _status = "Connection failed";
            }

            return null;
        }

        public async Task<object> DisconnectAsync()
        {
            try
            {
                StopRelaying();
<<<<<<< HEAD
                await _discordClient.DisconnectAsync();
=======
                await DiscordClient.DisconnectAsync();
>>>>>>> upstream/master
            }
            catch (Exception e)
            {
                Logger.Error("An Error occurred when disconnecting from Discord: Error message: " + e.Message);
                _status = "Connection failed";
            }

            return null;
        }

<<<<<<< HEAD
        public DiscordClient DiscordClient => _discordClient;

=======
>>>>>>> upstream/master
        #endregion

        #region Discord Guild Access

<<<<<<< HEAD
        public string[] GuildNames => _discordClient.GuildNames();
        public DiscordGuild DefaultGuild => _discordClient.DefaultGuild();
        
        public DiscordGuild GuildByName(string name)
        {
            return _discordClient?.Guilds.Values.FirstOrDefault(guild => guild.Name?.ToLower() == name.ToLower());
=======
        public string[] GuildNames => DiscordClient.GuildNames();
        public DiscordGuild DefaultGuild => DiscordClient.DefaultGuild();

        public DiscordGuild GuildByName(string name)
        {
            return DiscordClient?.Guilds.Values.FirstOrDefault(guild => guild.Name?.ToLower() == name.ToLower());
>>>>>>> upstream/master
        }

        public DiscordGuild GuildByNameOrId(string nameOrId)
        {
            var maybeGuildId = DSharpExtensions.TryParseSnowflakeId(nameOrId);
<<<<<<< HEAD
            return maybeGuildId != null ? _discordClient.Guilds[maybeGuildId.Value] : GuildByName(nameOrId);
=======
            return maybeGuildId != null ? DiscordClient.Guilds[maybeGuildId.Value] : GuildByName(nameOrId);
>>>>>>> upstream/master
        }

        #endregion

        #region Message Sending

        public async Task<string> SendDiscordMessage(string message, string channelNameOrId, string guildNameOrId)
        {
<<<<<<< HEAD
            if (_discordClient == null) return "No discord client";
=======
            if (DiscordClient == null) return "No discord client";
>>>>>>> upstream/master

            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            await DiscordUtil.SendAsync(channel, message);
            return "Message sent";
        }

<<<<<<< HEAD
        public async Task<string> SendDiscordMessageAsUser(string message, User user, string channelNameOrId, string guildNameOrId)
=======
        public async Task<string> SendDiscordMessageAsUser(string message, User user, string channelNameOrId, string guildNameOrId, bool allowGlobalMentions = false)
>>>>>>> upstream/master
        {
            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null) return "No guild of that name found";

            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if (channel == null) return "No channel of that name or ID found in that guild";
<<<<<<< HEAD
            await DiscordUtil.SendAsync(channel, FormatDiscordMessage(message, channel, user.Name));
            return "Message sent";
        }

        public async Task<String> SendDiscordMessageAsUser(string message, User user, DiscordChannel channel)
        {
            await DiscordUtil.SendAsync(channel, FormatDiscordMessage(message, channel, user.Name));
=======
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name, allowGlobalMentions));
            return "Message sent";
        }

        public async Task<String> SendDiscordMessageAsUser(string message, User user, DiscordChannel channel, bool allowGlobalMentions = false)
        {
            await DiscordUtil.SendAsync(channel, MessageUtil.FormatMessageForDiscord(message, channel, user.Name, allowGlobalMentions));
>>>>>>> upstream/master
            return "Message sent";
        }

        #endregion

        #region Message Relaying

<<<<<<< HEAD
        private string EcoUserSteamId = "DiscordLinkSteam";
        private string EcoUserSlgId = "DiscordLinkSlg";
        private string EcoUserName = "Discord";

        private User _ecoUser;
        public User EcoUser =>
            _ecoUser ?? (_ecoUser = UserManager.GetOrCreateUser(EcoUserSteamId, EcoUserSlgId, EcoUserName));

        public void ActionPerformed(GameAction action)
        {
            switch(action)
            {
                case ChatSent chatSent:
                    OnMessageReceivedFromEco(chatSent);
                break;

                case FirstLogin firstLogin:
                case Play play:
                    UpdateEcoStatus();
                    break;

                default:
                    break;
            }
        }

        public Result ShouldOverrideAuth(GameAction action)
        {
            return new Result(ResultType.None);
        }
=======
        private const string EcoUserSteamId = "DiscordLinkSteam";
        private const string EcoUserSlgId = "DiscordLinkSlg";

        private User _ecoUser;
        public User EcoUser => _ecoUser ??= UserManager.GetOrCreateUser(EcoUserSteamId, EcoUserSlgId, DLConfig.Data.EcoBotName);
>>>>>>> upstream/master

        private void BeginRelaying()
        {
            ActionUtil.AddListener(this);
<<<<<<< HEAD
            _discordClient.MessageCreated += OnDiscordMessageCreateEvent;
=======
            DiscordClient.MessageCreated += OnDiscordMessageCreateEvent;
>>>>>>> upstream/master
        }

        private void StopRelaying()
        {
            ActionUtil.RemoveListener(this);
<<<<<<< HEAD
            _discordClient.MessageCreated -= OnDiscordMessageCreateEvent;
        }

        private ChannelLink GetLinkForEcoChannel(string discordChannelNameOrId)
        {
            return DiscordPluginConfig.ChatChannelLinks.FirstOrDefault(link => link.DiscordChannel == discordChannelNameOrId);
        }

        private ChannelLink GetLinkForDiscordChannel(string ecoChannelName)
        {
            var lowercaseEcoChannelName = ecoChannelName.ToLower();
            return DiscordPluginConfig.ChatChannelLinks.FirstOrDefault(link => link.EcoChannel.ToLower() == lowercaseEcoChannelName);
=======
            DiscordClient.MessageCreated -= OnDiscordMessageCreateEvent;
        }

        public ChatChannelLink GetLinkForEcoChannel(string discordChannelNameOrId)
        {
            return DLConfig.Data.ChatChannelLinks.FirstOrDefault(link => link.DiscordChannel == discordChannelNameOrId);
        }

        public ChatChannelLink GetLinkForDiscordChannel(string ecoChannelName)
        {
            var lowercaseEcoChannelName = ecoChannelName.ToLower();
            return DLConfig.Data.ChatChannelLinks.FirstOrDefault(link => link.EcoChannel.ToLower() == lowercaseEcoChannelName);
>>>>>>> upstream/master
        }

        public void LogEcoMessage(ChatSent chatMessage)
        {
            Logger.DebugVerbose("Eco Message Processed:");
            Logger.DebugVerbose("Message: " + chatMessage.Message);
            Logger.DebugVerbose("Tag: " + chatMessage.Tag);
            Logger.DebugVerbose("Sender: " + chatMessage.Citizen);
        }

        public void LogDiscordMessage(DiscordMessage message)
        {
            Logger.DebugVerbose("Discord Message Processed");
            Logger.DebugVerbose("Message: " + message.Content);
            Logger.DebugVerbose("Channel: " + message.Channel.Name);
            Logger.DebugVerbose("Sender: " + message.Author);
        }

        public void OnMessageReceivedFromEco(ChatSent chatMessage)
        {
            LogEcoMessage(chatMessage);

<<<<<<< HEAD
            // Ignore messages sent by our bot
            if (chatMessage.Citizen.Name == EcoUser.Name && !chatMessage.Message.StartsWith(EchoCommandToken))
            {
                return;
            }

            // Remove the # character from the start.
            var channelLink = GetLinkForDiscordChannel(chatMessage.Tag.Substring(1));
            var channel = channelLink?.DiscordChannel;
            var guild = channelLink?.DiscordGuild;

            if (!String.IsNullOrWhiteSpace(channel) && !String.IsNullOrWhiteSpace(guild))
            {
                ForwardMessageToDiscordChannel(chatMessage, channel, guild);
            }
=======
            // Ignore commands and messages sent by our bot
            if (chatMessage.Citizen.Name == EcoUser.Name) return;
            if (chatMessage.Message.StartsWith(EchoCommandToken)) return;

            UpdateIntegrations(TriggerType.EcoMessage, chatMessage);
>>>>>>> upstream/master
        }

        public async Task OnDiscordMessageCreateEvent(MessageCreateEventArgs messageArgs)
        {
            OnMessageReceivedFromDiscord(messageArgs.Message);
        }

        public void OnMessageReceivedFromDiscord(DiscordMessage message)
        {
            LogDiscordMessage(message);
<<<<<<< HEAD
            if (message.Author == _discordClient.CurrentUser) { return; }
            if (message.Content.StartsWith(_configOptions.Config.DiscordCommandPrefix)) { return; }
            
            var channelLink = GetLinkForEcoChannel(message.Channel.Name) ?? GetLinkForEcoChannel(message.Channel.Id.ToString());
            var channel = channelLink?.EcoChannel;
            if (!String.IsNullOrWhiteSpace(channel))
            {
                ForwardMessageToEcoChannel(message, channel);
            }
        }

        private async void ForwardMessageToEcoChannel(DiscordMessage message, string channelName)
        {
            Logger.DebugVerbose("Sending Discord message to Eco channel: " + channelName);
            var author = await message.Channel.Guild.MaybeGetMemberAsync(message.Author.Id);
            var nametag = author != null
                ? Text.Bold(Text.Color(NametagColor, author.DisplayName))
                : message.Author.Username;
            var text = $"#{channelName} {nametag}: {GetReadableContent(message)}";
            ChatManager.SendChat(text, EcoUser);

            if (_configOptions.Config.LogChat)
            {
                _chatLogger.Write(message);
            }
        }

        private void ForwardMessageToDiscordChannel(ChatSent chatMessage, string channelNameOrId, string guildNameOrId)
        {
            Logger.DebugVerbose("Sending Eco message to Discord channel " + channelNameOrId + " in guild " + guildNameOrId);
            var guild = GuildByNameOrId(guildNameOrId);
            if (guild == null)
            {
                Logger.Error("Failed to forward Eco message from user " + StripTags(chatMessage.Citizen.Name) + " as no guild with the name or ID " + guildNameOrId + " exists");
                return;
            }
            var channel = guild.ChannelByNameOrId(channelNameOrId);
            if(channel == null)
            {
                Logger.Error("Failed to forward Eco message from user " + StripTags(chatMessage.Citizen.Name) + " as no channel with the name or ID " + channelNameOrId + " exists in the guild " + guild.Name);
                return;
            }

            DiscordUtil.SendAsync(channel, FormatDiscordMessage(chatMessage.Message, channel, chatMessage.Citizen.Name));

            if (_configOptions.Config.LogChat)
            {
                _chatLogger.Write(chatMessage);
            }
        }

        private String GetReadableContent(DiscordMessage message)
        {
            var content = message.Content;
            foreach (var user in message.MentionedUsers)
            {
                if (user == null) { continue; }
                DiscordMember member = message.Channel.Guild.Members.FirstOrDefault(m => m.Value?.Id == user.Id).Value;
                if (member == null) { continue; }
                String name = "@" + member.DisplayName;
                content = content.Replace($"<@{user.Id}>", name).Replace($"<@!{user.Id}>", name);
            }
            foreach (var role in message.MentionedRoles)
            {
                if (role == null) continue;
                content = content.Replace($"<@&{role.Id}>", $"@{role.Name}");
            }
            foreach (var channel in message.MentionedChannels)
            {
                if (channel == null) continue;
                content = content.Replace($"<#{channel.Id}>", $"#{channel.Name}");
            }
            return content;
        }

        #endregion

        #region Message Formatting

        public static string StripTags(string toStrip)
        {
            return EcoNameTagRegex.Replace(toStrip, String.Empty);
        }

        public string FormatDiscordMessage(string message, DiscordChannel channel, string username = "" )
        {
            string formattedMessage = (username.IsEmpty() ? "" : $"**{username.Replace("@", "")}**:") + StripTags(message); // All @ characters are removed from the name in order to avoid unintended mentions of the sender
            formattedMessage = formattedMessage.Replace("@here", "@ here");
            formattedMessage = formattedMessage.Replace("@everyone", "@ everyone");
            return FormatDiscordMentions(formattedMessage, channel);
        }

        private string FormatDiscordMentions(string message, DiscordChannel channel)
        {
            return DiscordMentionRegex.Replace(message, capture =>
            {
                string match = capture.ToString().Substring(1).ToLower(); // Strip the mention character from the match
                Func<string, string, string> FormatMention = (name, mention) =>
                {
                    if (match == name)
                    {
                        return mention;
                    }

                    string beforeMatch = "";
                    int matchStartIndex = match.IndexOf(name);
                    if (matchStartIndex > 0) // There are characters before @username
                    {
                        beforeMatch = match.Substring(0, matchStartIndex);
                    }

                    string afterMatch = "";
                    int matchStopIndex = matchStartIndex + name.Length - 1;
                    int numCharactersAfter = match.Length - 1 - matchStopIndex;
                    if (numCharactersAfter > 0) // There are characters after @username
                    {
                        afterMatch = match.Substring(matchStopIndex + 1, numCharactersAfter);
                    }
                    
                    return beforeMatch + mention + afterMatch; // Add whatever characters came before or after the username when replacing the match in order to avoid changing the message context
                };

                ChannelLink link = _configOptions.Config.GetChannelLinkFromDiscordChannel(channel.Guild.Name, channel.Name);
                bool allowRoleMentions = (link == null ? true : link.AllowRoleMentions);
                bool allowMemberMentions = (link == null ? true : link.AllowUserMentions);
                bool allowChannelMentions = (link == null ? true : link.AllowChannelMentions);

                if (capture.ToString()[0] == '@')
                {
                    if (allowRoleMentions)
                    {
                        foreach (var role in channel.Guild.Roles.Values) // Checking roles first in case a user has a name identiacal to that of a role
                        {
                            if (!role.IsMentionable) continue;

                            string name = role.Name.ToLower();
                            if (match.Contains(name))
                            {
                                return FormatMention(name, role.Mention);
                            }
                        }
                    }

                    if (allowMemberMentions)
                    {
                        foreach (var member in channel.Guild.Members.Values)
                        {
                            string name = member.DisplayName.ToLower();
                            if (match.Contains(name))
                            {
                                return FormatMention(name, member.Mention);
                            }
                        }
                    }
                }
                else if(capture.ToString()[0] == '#' && allowChannelMentions)
                {
                    foreach(var listChannel in channel.Guild.Channels.Values)
                    {
                        string name = listChannel.Name.ToLower();
                        if(match.Contains(name))
                        {
                            return FormatMention(name, listChannel.Mention);
                        }
                    }
                }

                return capture.ToString(); // No match found, just return the original string
            });
        }

        #endregion

        #region EcoStatus
        private Timer _ecoStatusUpdateTimer = null;
        private const int ECO_STATUS_TIMER_INTERAVAL_MS = 60000;
        private const int ECO_STATUS_FIRST_UPDATE_DELAY_MS = 20000;
        private Dictionary<EcoStatusChannel, ulong> _ecoStatusMessages = new Dictionary<EcoStatusChannel, ulong>();

        private void UpdateEcoStatusOnTimer(Object stateInfo)
        {
            UpdateEcoStatus();
        }

        private static object _lock = new object();
        private void UpdateEcoStatus()
        {
            lock (_lock)
            {
                if (_discordClient == null) return;

                foreach (EcoStatusChannel statusChannel in _configOptions.Config.EcoStatusDiscordChannels)
                {
                    DiscordGuild discordGuild = _discordClient.GuildByName(statusChannel.DiscordGuild);
                    if (discordGuild == null) continue;
                    DiscordChannel discordChannel = discordGuild.ChannelByName(statusChannel.DiscordChannel);
                    if (discordChannel == null) continue;

                    if (!DiscordUtil.ChannelHasPermission(discordChannel, Permissions.ReadMessageHistory)) continue;
                    bool HasEmbedPermission = DiscordUtil.ChannelHasPermission(discordChannel, Permissions.EmbedLinks);

                    DiscordMessage ecoStatusMessage = null;
                    bool created = false;
                    ulong statusMessageID;
                    if (_ecoStatusMessages.TryGetValue(statusChannel, out statusMessageID))
                    {
                        try
                        {
                             ecoStatusMessage = discordChannel.GetMessageAsync(statusMessageID).Result;
                        }
                        catch(System.AggregateException)
                        {
                            _ecoStatusMessages.Remove(statusChannel); // The message has been removed, take it out of the list
                        }
                        catch(Exception e)
                        {
                            Logger.Error("Error occurred when attempting to read message with ID " + statusMessageID + " from channel \"" + discordChannel.Name + "\". Error message: " + e);
                            continue;
                        }
                    }
                    else
                    {
                        IReadOnlyList<DiscordMessage> ecoStatusChannelMessages = DiscordUtil.GetMessagesAsync(discordChannel).Result;
                        if (ecoStatusChannelMessages == null) continue;

                        foreach(DiscordMessage message in ecoStatusChannelMessages)
                        {
                            // We assume that it's our status message if it has parts of our string in it
                            if(message.Author == _discordClient.CurrentUser 
                                && (HasEmbedPermission ? (message.Embeds.Count == 1 && message.Embeds[0].Title.Contains("Live Server Status**")) : message.Content.Contains("Live Server Status**")))
                            {
                                ecoStatusMessage = message;
                                break;
                            }
                        }

                        // If we couldn't find a status message, create a new one
                        if(ecoStatusMessage == null)
                        {
                            ecoStatusMessage = DiscordUtil.SendAsync(discordChannel, null, MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true)).Result;
                            created = true;
                        }

                        if (ecoStatusMessage != null) // SendAsync may return null in case an exception is raised
                        {
                            _ecoStatusMessages.Add(statusChannel, ecoStatusMessage.Id);
                        }
                    }

                    if (ecoStatusMessage != null && !created) // It is pointless to update the message if it was just created
                    {
                        DiscordUtil.ModifyAsync(ecoStatusMessage, "", MessageBuilder.GetEcoStatus(GetEcoStatusFlagForChannel(statusChannel), isLiveMessage: true));
                    }
                }
            }
        }

        private static MessageBuilder.EcoStatusComponentFlag GetEcoStatusFlagForChannel(EcoStatusChannel statusChannel)
        {
            MessageBuilder.EcoStatusComponentFlag statusFlag = 0;
            if(statusChannel.UseName)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Name;
            if (statusChannel.UseDescription)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Description;
            if (statusChannel.UseLogo)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.Logo;
            if(statusChannel.UseAddress)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.ServerAddress;
            if (statusChannel.UsePlayerCount)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerCount;
            if (statusChannel.UsePlayerList)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.PlayerList;
            if (statusChannel.UseTimeSinceStart)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeSinceStart;
            if (statusChannel.UseTimeRemaining)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.TimeRemaining;
            if (statusChannel.UseMeteorHasHit)
                statusFlag |= MessageBuilder.EcoStatusComponentFlag.MeteorHasHit;

            return statusFlag;
        }

        #endregion

        #region Configuration

        private List<String> _verifiedLinks = new List<string>();
        private Timer _linkVerificationTimeoutTimer = null;
        private const int LINK_VERIFICATION_TIMEOUT_MS = 15000;
        private const int STATIC_VERIFICATION_OUTPUT_DELAY_MS = 2000;
        private const int GUILD_VERIFICATION_OUTPUT_DELAY_MS = 3000;

        enum VerificationFlags
        {
            Static = 1 << 0,
            ChannelLinks = 1 << 1,
            All = ~0
        }

        public static DiscordLink Obj
        {
            get { return PluginManager.GetPlugin<DiscordLink>(); }
        }

        public object GetEditObject()
        {
            return _configOptions.Config;
        }

        public void OnEditObjectChanged(object o, string param)
        {
            OnConfigChanged();
        }

        public void OnConfigChanged()
        {
            // Do not verify if change occurred as this function is going to be called again in that case
            // Do not verify the config in case the bot token has been changed, as the client will be restarted and that will trigger verification
            bool tokenChanged = DiscordPluginConfig.BotToken != _currentBotToken;
            bool correctionMade = !SaveConfig();

            if (DiscordPluginConfig.BotToken != _currentBotToken)
            {
                // Reinitialise client.
                Logger.Info("Discord Bot Token changed - Reinitialising client.");
                RestartClient();
            }

            if ( !correctionMade && !tokenChanged)
            {
                VerifyConfig();
            }   
        }

        protected bool SaveConfig() // Returns true if no correction was needed
        {
            bool correctionMade = false;

            // Discord Command Prefix
            if (_configOptions.Config.DiscordCommandPrefix != _prevConfigOptions.DiscordCommandPrefix)
            {
                if (string.IsNullOrEmpty(_configOptions.Config.DiscordCommandPrefix))
                {
                    _configOptions.Config.DiscordCommandPrefix = DiscordConfig.DefaultValues.DiscordCommandPrefix;
                    correctionMade = true;

                    Logger.Info("Command prefix found empty - Resetting to default.");
                }
                Logger.Info("Command prefix changed - Restart required to take effect.");
            }

            // Chat channel links
            foreach (ChannelLink link in _configOptions.Config.ChatChannelLinks)
            {
                if (string.IsNullOrWhiteSpace(link.DiscordChannel)) continue;

                string original = link.DiscordChannel;
                if (link.DiscordChannel != link.DiscordChannel.ToLower()) // Discord channels are always lowercase
                {
                    link.DiscordChannel = link.DiscordChannel.ToLower();
                }

                if (link.DiscordChannel.Contains(" ")) // Discord channels always replace spaces with dashes
                {
                    link.DiscordChannel = link.DiscordChannel.Replace(' ', '-');
                }

                if (link.DiscordChannel != original)
                {
                    correctionMade = true;
                    Logger.Info("Corrected Discord channel name in Channel Link with Guild \"" + link.DiscordGuild + "\" from \"" + original + "\" to \"" + link.DiscordChannel + "\"");
                }
            }

            // Eco status Discord channels
            foreach (EcoStatusChannel statusChannel in _configOptions.Config.EcoStatusDiscordChannels) // TODO[MonzUn] Create a reusable way to fix erronous channel links
            {
                if (string.IsNullOrWhiteSpace(statusChannel.DiscordChannel)) continue;

                string original = statusChannel.DiscordChannel;
                if (statusChannel.DiscordChannel != statusChannel.DiscordChannel.ToLower())
                {
                    statusChannel.DiscordChannel = statusChannel.DiscordChannel.ToLower();
                }

                if (statusChannel.DiscordChannel.Contains(" "))
                {
                    statusChannel.DiscordChannel = statusChannel.DiscordChannel.Replace(' ', '-');
                }

                if (statusChannel.DiscordChannel != original)
                {
                    correctionMade = true;
                    Logger.Info("Corrected Discord channel name in Eco Status Channel with Guild name/ID \"" + statusChannel.DiscordGuild + "\" from \"" + original + "\" to \"" + statusChannel.DiscordChannel + "\"");
                }
            }

            // Chatlog toggle
            if (_configOptions.Config.LogChat && !_prevConfigOptions.LogChat)
            {
                Logger.Info("Chatlog enabled");
                _chatLogger.Start();
            }
            else if(!_configOptions.Config.LogChat && _prevConfigOptions.LogChat)
            {
                Logger.Info("Chatlog disabled");
                _chatLogger.Stop();
            }

            // Chatlog path
            if(string.IsNullOrEmpty(_configOptions.Config.ChatlogPath))
            {
                _configOptions.Config.ChatlogPath = Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\Chatlog.txt";
                correctionMade = true;
            }

            if( _configOptions.Config.ChatlogPath != _prevConfigOptions.ChatlogPath)
            {
                Logger.Info("Chatlog path changed. New path: " + _configOptions.Config.ChatlogPath);
                _chatLogger.Restart();
            }

            // Eco command channel
            if(string.IsNullOrEmpty(_configOptions.Config.EcoCommandChannel))
            {
                _configOptions.Config.EcoCommandChannel = DiscordConfig.DefaultValues.EcoCommandChannel;
                correctionMade = true;
            }

            // Invite Message
            if (string.IsNullOrEmpty(_configOptions.Config.InviteMessage))
            {
                _configOptions.Config.InviteMessage = DiscordConfig.DefaultValues.InviteMessage;
                correctionMade = true;
            }

            _ecoStatusMessages.Clear(); // The status channels may have changed so we should find the messages again;

            _configOptions.SaveAsync();
            _prevConfigOptions = (DiscordConfig)_configOptions.Config.Clone();

            return !correctionMade;
        }

        private void VerifyConfig(VerificationFlags verificationFlags = VerificationFlags.All)
        {
            List<string> errorMessages = new List<string>();

            if(_discordClient == null)
            {
                errorMessages.Add("[General Verification] No Discord client connected.");
            }

            if (verificationFlags.HasFlag(VerificationFlags.Static))
            {
                // Bot Token
                if(String.IsNullOrWhiteSpace(_configOptions.Config.BotToken))
                {
                    errorMessages.Add("[Bot Token] Bot token not configured. See Github page for install instructions.");
                }

                // Player configs
                foreach (DiscordPlayerConfig playerConfig in _configOptions.Config.PlayerConfigs)
                {
                    if (string.IsNullOrWhiteSpace(playerConfig.Username)) continue;

                    bool found = false;
                    foreach (User user in UserManager.Users)
                    {
                        if (user.Name == playerConfig.Username)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        errorMessages.Add("[Player Configs] No user with name \"" + playerConfig.Username + "\" was found");
                    }
                }

                // Eco command channel
                if (!string.IsNullOrWhiteSpace(_configOptions.Config.EcoCommandChannel) && _configOptions.Config.EcoCommandChannel.Contains("#"))
                {
                    errorMessages.Add("[Eco Command Channel] Channel name contains a channel indicator (#). The channel indicator will be added automatically and adding one manually may cause message sending to fail");
                }

                if (!string.IsNullOrWhiteSpace(_configOptions.Config.InviteMessage) && !_configOptions.Config.InviteMessage.Contains(InviteCommandLinkToken))
                {
                    errorMessages.Add("[Invite Message] Message does not contain the invite link token " + InviteCommandLinkToken + ". If the invite link has been added manually, consider adding it to the network config instead");
                }

                // Report errors
                if (errorMessages.Count <= 0)
                {
                    Logger.Info("Static configuration verification completed without errors");
                }
                else
                {
                    string concatenatedMessages = "";
                    foreach (string message in errorMessages)
                    {
                        concatenatedMessages += message + "\n";
                    }
                    Logger.Error("Static configuration errors detected!\n" + concatenatedMessages.Trim());
                }
            }

            if (verificationFlags.HasFlag(VerificationFlags.ChannelLinks) && _discordClient != null) // Discord guild and channel information isn't available the first time this function is called
            {
                // Channel links
                foreach (ChannelLink chatLink in _configOptions.Config.ChatChannelLinks)
                {
                    if (string.IsNullOrWhiteSpace(chatLink.DiscordGuild) || string.IsNullOrWhiteSpace(chatLink.DiscordChannel) || string.IsNullOrWhiteSpace(chatLink.EcoChannel)) continue;

                    var guild = GuildByNameOrId(chatLink.DiscordGuild);
                    if (guild == null)
                    {
                        continue; // The channel will always fail if the guild fails
                    }
                    var channel = guild.ChannelByNameOrId(chatLink.DiscordChannel);
                    if (channel == null)
                    {
                        continue;
                    }

                    string linkID = chatLink.ToString();
                    if (!_verifiedLinks.Contains(linkID))
                    {
                        if(!DiscordUtil.ChannelHasPermission(channel, Permissions.SendMessages))
                        {
                            Logger.Info("Channel " + linkID + " lacks permission SendMessages");
                        }
                        else
                        {
                            _verifiedLinks.Add(linkID);
                            Logger.Info("Channel Link Verified: " + linkID);
                        }
                    }
                }

                // Eco status Discord channels
                foreach (EcoStatusChannel statusLink in _configOptions.Config.EcoStatusDiscordChannels)
                {
                    if (string.IsNullOrWhiteSpace(statusLink.DiscordGuild) || string.IsNullOrWhiteSpace(statusLink.DiscordChannel)) continue;

                    var guild = GuildByNameOrId(statusLink.DiscordGuild);
                    if (guild == null)
                    {
                        continue; // The channel will always fail if the guild fails
                    }
                    var channel = guild.ChannelByNameOrId(statusLink.DiscordChannel);
                    if (channel == null)
                    {
                        continue;
                    }

                    string linkID = statusLink.ToString();
                    if (!_verifiedLinks.Contains(linkID))
                    {
                        bool hasPermissions = true;
                        if (!DiscordUtil.ChannelHasPermission(channel, Permissions.SendMessages))
                        {
                            Logger.Info("Channel " + linkID + " lacks permission SendMessages");
                            hasPermissions = false;
                        }

                        if (!DiscordUtil.ChannelHasPermission(channel, Permissions.ReadMessageHistory))
                        {
                            Logger.Info("Channel " + linkID + " lacks permission ReadMessageHistory");
                            hasPermissions = false;
                        }

                        if (!DiscordUtil.ChannelHasPermission(channel, Permissions.ManageMessages))
                        {
                            Logger.Info("Channel " + linkID + " lacks permission ManageMessages");
                            hasPermissions = false;
                        }

                        if(hasPermissions)
                        {
                            _verifiedLinks.Add(linkID);
                            Logger.Info("Channel Link Verified: " + linkID);
                        }
                    }
                }

                if(_verifiedLinks.Count >= _configOptions.Config.ChatChannelLinks.Count + _configOptions.Config.EcoStatusDiscordChannels.Count)
                {
                    Logger.Info("All channel links sucessfully verified");
                }
                else if(_linkVerificationTimeoutTimer == null) // If no timer is used, then the discord guild info should already be set up
                {
                    ReportUnverifiedChannels();
                }
            }
        }

        private void ReportUnverifiedChannels()
        {
            if (_verifiedLinks.Count >= _configOptions.Config.ChatChannelLinks.Count + _configOptions.Config.EcoStatusDiscordChannels.Count) return; // All are verified; nothing to report.

            List<string> unverifiedLinks = new List<string>();
            foreach (ChannelLink chatLink in _configOptions.Config.ChatChannelLinks)
            {
                if (string.IsNullOrWhiteSpace(chatLink.DiscordGuild) || string.IsNullOrWhiteSpace(chatLink.DiscordChannel) || string.IsNullOrWhiteSpace(chatLink.EcoChannel)) continue;

                string linkID = chatLink.ToString();
                if (!_verifiedLinks.Contains(linkID))
                {
                    unverifiedLinks.Add(linkID);
                }
            }
           
            foreach (EcoStatusChannel statusLink in _configOptions.Config.EcoStatusDiscordChannels)
            {
                if (string.IsNullOrWhiteSpace(statusLink.DiscordGuild) || string.IsNullOrWhiteSpace(statusLink.DiscordChannel)) continue;

                string linkID = statusLink.ToString();
                if (!_verifiedLinks.Contains(linkID))
                {
                    unverifiedLinks.Add(linkID);
                }
            }

            if(unverifiedLinks.Count > 0)
            {
                Logger.Info("Unverified channels detected:\n" + String.Join("\n", unverifiedLinks));
            }
        }

=======

            // Ignore commands and messages sent by our bot
            if (message.Author == DiscordClient.CurrentUser) return;
            if (message.Content.StartsWith(DLConfig.Data.DiscordCommandPrefix)) return;

            UpdateIntegrations(TriggerType.DiscordMessage, message);
        }
>>>>>>> upstream/master
        #endregion

        #region Player Configs

        public DiscordPlayerConfig GetOrCreatePlayerConfig(string identifier)
        {
<<<<<<< HEAD
            var config = DiscordPluginConfig.PlayerConfigs.FirstOrDefault(user => user.Username == identifier);
=======
            var config = DLConfig.Data.PlayerConfigs.FirstOrDefault(user => user.Username == identifier);
>>>>>>> upstream/master
            if (config == null)
            {
                config = new DiscordPlayerConfig
                {
                    Username = identifier
                };
                AddOrReplacePlayerConfig(config);
            }

            return config;
        }

        public bool AddOrReplacePlayerConfig(DiscordPlayerConfig config)
        {
<<<<<<< HEAD
            var removed = DiscordPluginConfig.PlayerConfigs.Remove(config);
            DiscordPluginConfig.PlayerConfigs.Add(config);
            SavePlayerConfig();
            return removed;
        }

        public void SavePlayerConfig()
        {
            _configOptions.SaveAsync();
        }

=======
            var playerConfigs = DLConfig.Data.PlayerConfigs;
            var removed = playerConfigs.Remove(config);
            playerConfigs.Add(config);
            DLConfig.Instance.Save();
            return removed;
        }

>>>>>>> upstream/master
        public DiscordChannel GetDefaultChannelForPlayer(string identifier)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            if (playerConfig.DefaultChannel == null
<<<<<<< HEAD
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Guild)
                || String.IsNullOrEmpty(playerConfig.DefaultChannel.Channel))
=======
                || string.IsNullOrEmpty(playerConfig.DefaultChannel.Guild)
                || string.IsNullOrEmpty(playerConfig.DefaultChannel.Channel))
>>>>>>> upstream/master
            {
                return null;
            }

            return GuildByName(playerConfig.DefaultChannel.Guild).ChannelByName(playerConfig.DefaultChannel.Channel);
        }

        public void SetDefaultChannelForPlayer(string identifier, string guildName, string channelName)
        {
            var playerConfig = GetOrCreatePlayerConfig(identifier);
            playerConfig.DefaultChannel.Guild = guildName;
            playerConfig.DefaultChannel.Channel = channelName;
<<<<<<< HEAD
            SavePlayerConfig();
=======
            DLConfig.Instance.Save();
>>>>>>> upstream/master
        }

        #endregion
    }
<<<<<<< HEAD

    public class DiscordConfig : ICloneable
    {
        public static class DefaultValues
        {
            public const string DiscordCommandPrefix = "?";
            public const string EcoCommandChannel = "General";
            public const string InviteMessage = "Join us on Discord!\n" + DiscordLink.InviteCommandLinkToken;
        }

        public object Clone() // Be careful not to change the original object here as that will trigger endless recursion.
        {
            return new DiscordConfig
            {
                BotToken = this.BotToken,
                DiscordCommandPrefix = this.DiscordCommandPrefix,
                ServerName = this.ServerName,
                ServerDescription = this.ServerDescription,
                ServerLogo = this.ServerLogo,
                ServerAddress = this.ServerAddress,
                Debug = this.Debug,
                LogChat = this.LogChat,
                ChatlogPath = this.ChatlogPath,
                PlayerConfigs = new ObservableCollection<DiscordPlayerConfig>(this.PlayerConfigs.Select(t => t.Clone()).Cast<DiscordPlayerConfig>()),
                ChatChannelLinks = new ObservableCollection<ChannelLink>(this.ChatChannelLinks.Select(t => t.Clone()).Cast<ChannelLink>()),
                EcoStatusDiscordChannels = new ObservableCollection<EcoStatusChannel>(this.EcoStatusDiscordChannels.Select(t => t.Clone()).Cast<EcoStatusChannel>())
            };
        }

        public ChannelLink GetChannelLinkFromDiscordChannel(string guild, string channelName)
        {
            foreach(ChannelLink channelLink in ChatChannelLinks)
            {
                if(channelLink.DiscordGuild.ToLower() == guild.ToLower() && channelLink.DiscordChannel.ToLower() == channelName.ToLower())
                {
                    return channelLink;
                }
            }
            return null;
        }

        public ChannelLink GetChannelLinkFromEcoChannel(string channelName)
        {
            foreach (ChannelLink channelLink in ChatChannelLinks)
            {
                if (channelLink.EcoChannel.ToLower() == channelName.ToLower())
                {
                    return channelLink;
                }
            }
            return null;
        }

        [Description("The token provided by the Discord API to allow access to the bot. This setting can be changed while the server is running and will in that case trigger a reconnection to Discord."), Category("Bot Configuration")]
        public string BotToken { get; set; }

        [Description("The prefix to put before commands in order for the Discord bot to recognize them as such. This setting requires a restart to take effect."), Category("Command Settings")]
        public string DiscordCommandPrefix { get; set; } = DefaultValues.DiscordCommandPrefix;

        [Description("Discord channels in which to display the Eco status view. WARNING - Any messages in these channels will be deleted. This setting can be changed while the server is running."), Category("Channel Configuration")]
        public ObservableCollection<EcoStatusChannel> EcoStatusDiscordChannels { get; set; } = new ObservableCollection<EcoStatusChannel>();

        [Description("The name of the Eco server, overriding the name configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerName { get; set; }

        [Description("The description of the Eco server, overriding the description configured within Eco. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerDescription { get; set; }

        [Description("The logo of the server as a URL. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerLogo { get; set; }

        [Description("The address (URL or IP) of the server. Overrides the automatically detected IP. This setting can be changed while the server is running."), Category("Server Details")]
        public string ServerAddress { get; set; }

        [Description("A mapping from user to user config parameters. This setting can be changed while the server is running.")]
        public ObservableCollection<DiscordPlayerConfig> PlayerConfigs = new ObservableCollection<DiscordPlayerConfig>();

        [Description("Channels to connect together. This setting can be changed while the server is running."), Category("Channel Configuration")]
        public ObservableCollection<ChannelLink> ChatChannelLinks { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Enables debugging output to the console. This setting can be changed while the server is running."), Category("Debugging")]
        public bool Debug { get; set; } = false;

        [Description("Enables logging of chat messages into the file at Chatlog Path. This setting can be changed while the server is running."), Category("Chatlog Configuration")]
        public bool LogChat { get; set; } = false;

        [Description("The path to the chatlog file, including file name and extension. This setting can be changed while the server is running, but the existing chatlog will not transfer."), Category("Chatlog Configuration")]
        public string ChatlogPath { get; set; } = Directory.GetCurrentDirectory() + "\\Mods\\DiscordLink\\Chatlog.txt";

        [Description("The Eco chat channel to use for commands that outputs public messages, excluding the initial # character. This setting can be changed while the server is running."), Category("Command Settings")]
        public string EcoCommandChannel { get; set; } = DefaultValues.EcoCommandChannel;

        [Description("The message to use for the /DiscordInvite command. The invite link is fetched from the network config and will replace the token " + DiscordLink.InviteCommandLinkToken + ". This setting can be changed while the server is running."), Category("Command Settings")]
        public string InviteMessage { get; set; } = DefaultValues.InviteMessage;
    }

    public class DiscordChannelIdentifier : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public string Guild { get; set; }
        public string Channel { get; set; }
    }

    public class DiscordPlayerConfig : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        [Description("ID of the user")]
        public string Username { get; set; }

        private DiscordChannelIdentifier _defaultChannel = new DiscordChannelIdentifier();
        public DiscordChannelIdentifier DefaultChannel
        {
            get { return _defaultChannel; }
            set { _defaultChannel = value; }
        }

        public class DiscordChannelIdentifier
        {
            public string Guild { get; set; }
            public string Channel { get; set; }
        }
    }

    public class ChannelLink : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel + " <--> " + EcoChannel + " (Chat Link)";
        }

        [Description("Discord Guild (Server) by name or ID.")]
        public string DiscordGuild { get; set; }

        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; }

        [Description("Eco Channel to use.")]
        public string EcoChannel { get; set; }

        [Description("Allow mentions of usernames to be forwarded from Eco to the Discord channel")]
        public bool AllowUserMentions { get; set; } = true;

        [Description("Allow mentions of roles to be forwarded from Eco to the Discord channel")]
        public bool AllowRoleMentions { get; set; } = true;

        [Description("Allow mentions of channels to be forwarded from Eco to the Discord channel")]
        public bool AllowChannelMentions { get; set; } = true;
    }

    public class EcoStatusChannel : ICloneable
    {
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public override string ToString()
        {
            return DiscordGuild + " - " + DiscordChannel + " (Eco Status)";
        }

        [Description("Discord Guild (Server) by name or ID.")]
        public string DiscordGuild { get; set; }

        [Description("Discord Channel by name or ID.")]
        public string DiscordChannel { get; set; }

        [Description("Display the server name in the status message.")]
        public bool UseName { get; set; } = true;

        [Description("Display the server description in the status message.")]
        public bool UseDescription { get; set; } = false;

        [Description("Display the server logo in the status message.")]
        public bool UseLogo { get; set; } = true;

        [Description("Display the server IP address in the status message.")]
        public bool UseAddress { get; set; } = true;

        [Description("Display the number of online players in the status message.")]
        public bool UsePlayerCount { get; set; } = true;

        [Description("Display the list of online players in the status message.")]
        public bool UsePlayerList { get; set; } = true;

        [Description("Display the time since the world was created in the status message.")]
        public bool UseTimeSinceStart { get; set; } = true;

        [Description("Display the time remaining until meteor impact in the status message.")]
        public bool UseTimeRemaining { get; set; } = true;

        [Description("Display a boolean for if the metoer has hit yet or not, in the status message.")]
        public bool UseMeteorHasHit { get; set; } = false;
    }
=======
>>>>>>> upstream/master
}
