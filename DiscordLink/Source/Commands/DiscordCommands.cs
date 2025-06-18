using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Eco.Core.Utils;
using Eco.Gameplay.GameActions;
using Eco.Gameplay.Players;
using Eco.Gameplay.Settlements;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Moose.Data;
using Eco.Moose.Features;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.Message;
using Eco.Moose.Utils.TextUtils;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Plugins.Networking;
using Eco.Shared.IoC;
using Eco.Shared.Utils;
using Eco.Simulation.WorldLayers.Layers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Moose.Data.CommandData;
using static Eco.Moose.Data.Enums;
using static Eco.Plugins.DiscordLink.DiscordCommands;
using static Eco.Plugins.DiscordLink.Utilities.MessageBuilder;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordCommandContext : DiscordLinkCommandContext
    {
        public DiscordCommandContext(CommandContext command, ResponseTiming timing)
        {
            base.Interface = ApplicationInterfaceType.Discord;
            Command = command;
            Timing = timing;
        }

        public CommandContext Command { get; private set; }
        
        public void MarkResponded()
        {
            HasRespondedToUser = true;
        }

        public ResponseTiming Timing { get; private set; }
        public bool HasRespondedToUser { get; private set; } = false;
    }

    public class DiscordCommands
    {
        #region Commands Base

        public enum PermissionType
        {
            User,
            Admin
        }

        public enum ResponseTiming
        {
            Immediate,
            Delayed
        }

        public delegate Task DiscordCommand(DiscordCommandContext ctx, params string[] parameters);

        private static async Task ExecuteCommand<TRet>(PermissionType requiredPermission, DiscordCommandContext ctx, DiscordCommand command)
        {
            try
            {
                if (ctx.Command.Channel.IsPrivate)
                    Logger.Debug($"{ctx.Command.User.Username} invoked Discord command \"/{command.Method.Name}\" in DM");
                else
                    Logger.Debug($"{ctx.Command.User.Username} invoked Discord command \"/{command.Method.Name}\" in channel {ctx.Command.Channel.Name}");

                // Block commands from non-admins if the server isn't fully ready yet
                if (!ctx.Command.Member.IsAdmin() && DiscordLink.Obj.Status != StatusState.Connected)
                {
                    await RespondToCommand(ctx, $"DiscordLink is not in a ready state to execute commands.\nCurrent status is `{DiscordLink.Obj.GetStatus()}`");
                    return;
                }

                if (ctx.Timing == ResponseTiming.Delayed)
                {
                    await ctx.Command.DeferResponseAsync();
                }

                if (!IsCommandAllowedForUser(ctx, requiredPermission))
                {
                    string permittedRolesDesc = (DiscordLinkConfig.AdminRoles.Count() > 0) ? string.Join("\n- ", DiscordLinkConfig.AdminRoles.ToArray()) : "No admin roles configured";
                    await RespondToCommand(ctx, $"You lack the `{requiredPermission}` level permission required to execute this command.\nThe permitted roles are:\n```- {permittedRolesDesc}```");
                    return;
                }

                await command(ctx);
            }
            catch (Exception e)
            {
                Logger.Exception($"An error occurred while attempting to execute a Discord command", e);
                await RespondToCommand(ctx, $"An error occurred while attempting to run that command. Error message: {e}");
            }
        }

        private static async Task RespondToCommand(DiscordCommandContext ctx, string fullTextContent, DiscordLinkEmbed embedContent) => await RespondToCommand(ctx, fullTextContent, embedContent.SingleItemAsEnumerable());

        private static async Task RespondToCommand(DiscordCommandContext ctx, string fullTextContent, IEnumerable<DiscordLinkEmbed> embedContent = null)
        {
            async static Task Respond(DiscordCommandContext ctx, string textContent, IEnumerable<DiscordLinkEmbed> embedContent)
            {
                string bulderText = string.Empty;
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    if (textContent.Length < DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT)
                        bulderText = textContent;
                    else
                        bulderText = $"{textContent.Substring(0, DLConstants.DISCORD_MESSAGE_CHARACTER_LIMIT - 4)}...";
                }

                List<DiscordEmbed> builderEmbeds = new List<DiscordEmbed>();
                if (embedContent != null)
                {
                    foreach (DiscordLinkEmbed embed in embedContent)
                    {
                        builderEmbeds = MessageUtils.BuildDiscordEmbeds(embed);
                    }
                }

                // Send initial response
                if (ctx.Timing == ResponseTiming.Immediate)
                {
                    DiscordInteractionResponseBuilder builder = new DiscordInteractionResponseBuilder();
                    builder.Content = bulderText;
                    if (builderEmbeds.Count > 0)
                        builder.AddEmbed(builderEmbeds.First());

                    ctx.MarkResponded();
                    await ctx.Command.RespondAsync(builder);
                }
                else if (ctx.Timing == ResponseTiming.Delayed)
                {
                    DiscordWebhookBuilder builder = new DiscordWebhookBuilder();
                    builder.Content = bulderText;
                    if (builderEmbeds.Count > 0)
                        builder.AddEmbed(builderEmbeds.First());

                    ctx.MarkResponded();
                    await ctx.Command.EditResponseAsync(builder);
                }

                // Send any remaining embeds as follow up messages
                for (int i = 1; i < builderEmbeds.Count; ++i)
                {
                    DiscordFollowupMessageBuilder builder = new DiscordFollowupMessageBuilder();
                    builder.AddEmbed(builderEmbeds[i]);

                    await ctx.Command.FollowupAsync(builder);
                }
            }

            string errorMessage = string.Empty;
            try
            {
                DiscordClient client = DiscordLink.Obj.Client;
                if (!client.ChannelHasPermission(ctx.Command.Channel, DiscordPermissions.SendMessages) || !client.ChannelHasPermission(ctx.Command.Channel, DiscordPermissions.ReadMessageHistory))
                {
                    Logger.Error($"Failed to respond to command \"{ctx.Command.Command.Name}\" in channel \"{ctx.Command.Channel}\" as the bot lacks permissions for sending and/or reading messages in this channel.");
                    return;
                }

                fullTextContent = MessageUtils.FormatMessageForApplication(ApplicationInterfaceType.Discord, fullTextContent);
                if (embedContent == null)
                {
                    await Respond(ctx, fullTextContent, null);
                }
                else
                {
                    // Either make sure we have permission to use embeds or convert the embed to text
                    if (client.ChannelHasPermission(ctx.Command.Channel, DiscordPermissions.EmbedLinks))
                    {
                        await Respond(ctx, fullTextContent, embedContent);
                    }
                    else
                    {
                        await Respond(ctx, $"{fullTextContent}\n{string.Join("\n\n", embedContent.Select(embed => embed.AsDiscordText()))}", null);
                    }
                }
            }
            catch (NotFoundException e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\\nException {e}\nMessage: {e.JsonMessage}";
            }
            catch (BadRequestException e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\nException {e}\nRequest Error: {e.Errors}";
            }
            catch (Exception e)
            {
                errorMessage = $"An error occurred while attempting to respond to command\nException: {e}";
            }

            if (!errorMessage.IsEmpty())
            {
                Logger.Error(errorMessage);
                try
                {
                    await Respond(ctx, errorMessage, null);
                }
                catch { } // If we fail, it's probably for the same reason as above, so let's not spam the log
            }
        }

        private static bool IsCommandAllowedForUser(DiscordCommandContext ctx, PermissionType requiredPermission)
        {
            return requiredPermission switch
            {
                PermissionType.User => true,
                PermissionType.Admin => DiscordLink.Obj.Client.MemberIsAdmin(ctx.Command.Member),
                _ => false,
            };
        }

        #endregion

        #region User Feedback

        public static async Task ReportCommandError(DiscordCommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task ReportCommandInfo(DiscordCommandContext ctx, string message)
        {
            await RespondToCommand(ctx, message);
        }

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, DiscordLinkEmbed embed) => await DisplayCommandData(ctx, title, embed.SingleItemAsEnumerable());

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, IEnumerable<DiscordLinkEmbed> embeds)
        {
            await RespondToCommand(ctx, title, embeds);
        }

        public static async Task DisplayCommandData(DiscordCommandContext ctx, string title, string content)
        {
            await RespondToCommand(ctx, $"**{title}**\n```{content}```");
        }

        #endregion

        #region Eco Commands

        [Command("EcoCommand")]
        [Description("Executes an ingame command.")]
        public async Task EcoCommand(CommandContext command,
            [Parameter("Command")][Description("The Eco command to run.")] string commandString)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                RemoteEcoCommandClient Client = new RemoteEcoCommandClient(ctx);
                await ServiceHolder<IChatManager>.Obj.ExecuteCommandAsync(Client, commandString);

                if(!ctx.HasRespondedToUser) // Some commands may defer their response and some may not respond at all => Explain to the user that it's the Eco command that's at fault
                {
                    await ReportCommandInfo(ctx, "Awaiting Eco command response...");
                }
            });
        }

        #endregion

        #region Plugin Management

        [Command("Update")]
        [Description("Forces an update of most internal systems.")]
        public async Task Update(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Update(ctx);
            });
        }

        [Command("RestartPlugin")]
        [Description("Restarts the DiscordLink plugin.")]
        public async Task RestartPlugin(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RestartPlugin(ctx);
            });
        }

        [Command("ReloadConfig")]
        [Description("Reloads the DiscordLink config.")]
        public async Task ReloadConfig(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ReloadConfig(ctx);
            });
        }

        [Command("ResetPersistentData")]
        [Description("Removes all persistent storage data.")]
        public async Task ResetPersistentData(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ResetPersistentData(ctx);
            });
        }

        [Command("ResetWorldData")]
        [Description("Resets world data as if a new world had been created.")]
        public async Task ResetWorldData(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ResetWorldData(ctx);
            });
        }

        [Command("PersistentStorageData")]
        [Description("Displays a description of the persistent storage data.")]
        public async Task PersistentStorageData(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PersistentStorageData(ctx);
            });
        }

        [Command("WorldStorageData")]
        [Description("Displays a description of the world storage data.")]
        public async Task WorldStorageData(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorldStorageData(ctx);
            });
        }

        #endregion

        #region Eco Server Management

        [Command("ServerShutdown")]
        [Description("Shuts down the Eco server.")]
        public async Task ServerShutdown(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ServerShutdown(ctx);
            });
        }

        #endregion

        #region Discord Server Management

        [Command("ClearRoles")]
        [Description("Deletes all Discord roles created and tracked by DiscordLink.")]
        public async Task ClearRoles(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ClearRoles(ctx);
            });
        }

        [Command("ReinstallCommands")]
        [Description("Reinstalls all Discord commands registered by DiscordLink and cleans up any dead commands.")]
        public async Task ReinstallCommands(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await DiscordLink.Obj.Client.ReinstallCommands();
                await RespondToCommand(ctx, "All commands reinstalled - Restart the discord client (ctrl+r) to re-fetch the command list");
            });
        }

        #endregion

        #region Meta

        [Command("Version")]
        [Description("Displays the installed and latest available plugin version.")]
        public async Task Version(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Version")
                    .WithDescription(TextUtils.StripTags(MessageBuilder.Shared.GetVersionMessage()));

                await RespondToCommand(ctx, null, embed);
            });
        }

        [Command("About")]
        [Description("Displays information about the DiscordLink plugin.")]
        public async Task About(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("About DiscordLink")
                    .WithDescription(MessageBuilder.Shared.GetAboutMessage());

                await RespondToCommand(ctx, null, embed);
            });
        }

        [Command("Documentation")]
        [Description("Opens the documentation web page.")]
        public async Task Documentation(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, "The documentation can be found here: <https://github.com/Eco-DiscordLink/EcoDiscordPlugin>");
            });
        }

        [Command("PluginStatus")]
        [Description("Displays the current plugin status.")]
        public async Task PluginStatus(CommandContext command,
            [Parameter("Verbose")][Description("Use verbose output with extra information.")] bool verbose = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await RespondToCommand(ctx, await MessageBuilder.Shared.GetDisplayStringAsync(verbose));
            });
        }

        [Command("VerifyConfig")]
        [Description("Checks configuration setup and reports any errors.")]
        public async Task VerifyConfig(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyConfig(ctx);
            });
        }

        [Command("VerifyPermissions")]
        [Description("Checks all permissions and intents needed and reports any missing ones.")]
        public async Task VerifyPermissions(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.All);
            });
        }

        [Command("VerifyIntents")]
        [Description("Checks all intents needed and reports any missing ones.")]
        public async Task VerifyIntents(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.Intents);
            });
        }

        [Command("VerifyServerPermissions")]
        [Description("Checks all server permissions needed and reports any missing ones.")]
        public async Task VerifyServerPermissions(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.VerifyPermissions(ctx, MessageBuilder.PermissionReportComponentFlag.ServerPermissions);
            });
        }

        [Command("VerifyChannelPermissions")]
        [Description("Checks all permissions needed for the given channel and reports any missing ones.")]
        public async Task CheckChannelPermissions(CommandContext command,
            [Parameter("Channel")][Description("Name or ID of the channel to check permissions for. Defaults to the current channel.")][SlashAutoCompleteProvider<DiscordTextChannelAutoCompleteProvider>] string channelNameOrId = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(channelNameOrId))
                    await SharedCommands.VerifyPermissionsForChannel(ctx, ctx.Command.Channel);
                else
                    await SharedCommands.VerifyPermissionsForChannel(ctx, channelNameOrId);
            });
        }

        [Command("ListLinkedChannels")]
        [Description("Presents a list of all channel links.")]
        public async Task ListLinkedChannels(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ListChannelLinks(ctx);
            });
        }

        [Command("Echo")]
        [Description("Sends a message to Eco and back to Discord again if a chat link is configured for the channel.")]
        public async Task Echo(CommandContext commmand,
            [Parameter("Message")][Description("The message to send. Defaults to a random message.")] string message = "",
            [Parameter("EcoChannel")][Description("The eco channel you want to test.")][SlashAutoCompleteProvider<EcoTextChannelAutoCompleteProvider>] string ecoChannel = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(commmand, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(message))
                {
                    Random rnd = new Random();
                    switch (rnd.Next(1, 5))
                    {
                        case 1:
                            message = "One thing has suddenly ceased to lead to another.";
                            break;

                        case 2:
                            message = "Nothing travels faster than the speed of light with the possible exception of bad news, which obeys its own special laws.";
                            break;

                        case 3:
                            message = "Life... is like a grapefruit. It's orange and squishy, and has a few pips in it, and some folks have half a one for breakfast.";
                            break;

                        case 4:
                            message = "So long and thanks for all the fish.";
                            break;

                        case 5:
                            message = "Time is an illusion. Lunch-time doubly so.";
                            break;
                    }
                }

                List<string> targetEcoChannelNames = new List<string>();

                User dummyUser = new User(Guid.Empty, "DiscordLinkSteam", "DiscordLinkTwitch", "DiscordLink", null);

                if (!string.IsNullOrWhiteSpace(ecoChannel))
                {
                    ChatSent action = new ChatSent();
                    action.Citizen = dummyUser;
                    action.Message = message;
                    action.Tag = $"#{ecoChannel}";

                    Message.SendChatToChannel(null, ecoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                    targetEcoChannelNames.Add(ecoChannel);

                    DiscordLink.Obj.ActionPerformed(action);
                }
                else
                {
                    bool linkFound = false;
                    foreach (ChatChannelLink chatLink in DiscordLinkConfig.ChatLinksForDiscordChannel(ctx.Command.Channel))
                    {
                        ChatSent action = new ChatSent();
                        action.Citizen = dummyUser;
                        action.Message = message;
                        action.Tag = $"#{chatLink.EcoChannel}";

                        Message.SendChatToChannel(null, chatLink.EcoChannel, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(chatLink.EcoChannel);
                        linkFound = true;

                        DiscordLink.Obj.ActionPerformed(action);
                    }

                    if (!linkFound)
                    {
                        ChatSent action = new ChatSent();
                        action.Citizen = dummyUser;
                        action.Message = message;
                        action.Tag = $"#{DLConstants.DEFAULT_CHAT_CHANNEL}";
                        

                        Message.SendChatToChannel(null, DLConstants.DEFAULT_CHAT_CHANNEL, $"{DLConstants.ECHO_COMMAND_TOKEN} {message}");
                        targetEcoChannelNames.Add(DLConstants.DEFAULT_CHAT_CHANNEL);

                        DiscordLink.Obj.ActionPerformed(action);
                    }
                }

                await RespondToCommand(ctx, $"Message sent to the following Eco channel(s): {string.Join(',', targetEcoChannelNames)}");
            });
        }

        #endregion

        #region Account Linking

        [Command("LinkInformation")]
        [Description("Presents information about account linking.")]
        public async Task LinkInformation(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Eco --> Discord Account Linking")
                    .WithDescription(MessageBuilder.Shared.GetLinkAccountInfoMessage());

                await RespondToCommand(ctx, null, embed);
            });
        }

        [Command("UnlinkAccount")]
        [Description("Unlinks the Discord account from a linked Eco account.")]
        public async Task UnlinkAccount(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                bool result = UserLinkManager.RemoveLinkedUser(command.Member);
                if (result)
                    await ReportCommandInfo(ctx, $"Eco account unlinked.");
                else
                    await ReportCommandError(ctx, $"No linked Eco account could be found.");
            });
        }

        #endregion

        #region Lookups

        [Command("ServerStatus")]
        [Description("Displays the Server Info status.")]
        public async Task ServerStatus(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await DisplayCommandData(ctx, string.Empty, MessageBuilder.Discord.GetServerInfo(MessageBuilder.ServerInfoComponentFlag.All));
            });
        }

        [Command("PlayerList")]
        [Description("Lists the players currently online on the server.")]
        public async Task PlayerList(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                DiscordLinkEmbed embed = new DiscordLinkEmbed()
                    .WithTitle("Players")
                    .WithDescription(MessageBuilder.Shared.GetOnlinePlayerList());
                await DisplayCommandData(ctx, string.Empty, embed);
            });
        }

        [Command("PlayerReport")]
        [Description("Displays the Player Report for the given player.")]
        public async Task PlayerReport(CommandContext command,
            [Parameter("Player")][Description("Name or ID of the player for which to display the report.")][SlashAutoCompleteProvider<PlayerAutoCompleteProvider>] string playerNameOrId = "",
            [Parameter("Report")][Description("Which type of information the report should include.")] PlayerReportComponentFlag reportType = PlayerReportComponentFlag.All)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PlayerReport(ctx, playerNameOrId, reportType);
            });
        }

        [Command("CurrencyReport")]
        [Description("Displays the Currency Report for the given currency.")]
        public async Task CurrencyReport(CommandContext command,
            [Parameter("Currency")][Description("Name or ID of the currency for which to display a report.")][SlashAutoCompleteProvider<CurrencyAutoCompleteProvider>] string currencyNameOrId,
            [Parameter("TopHoldersCount")][Description("How many top account holders to include in the report")] long maxTopHoldersCount = ServerConfigDefaultValues.MaxTopCurrencyHolderCount,
            [Parameter("ShowTradeCount")][Description("Should the total trade count for the currency be displayed in the report?")] bool useTradeCount = true,
            [Parameter("ShowBacking")][Description("Should information about the currency backing be displayed in the report?")] bool useBackingInfo = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.CurrencyReport(ctx, currencyNameOrId, (int)maxTopHoldersCount, useBackingInfo, useTradeCount);
            });
        }

        [Command("CurrenciesReport")]
        [Description("Displays a report for the top used currencies.")]
        public async Task CurrenciesReport(CommandContext command,
            [Parameter("Type")][Description("The type of currencies to include in the report.")] CurrencyType currencyType = CurrencyType.All,
            [Parameter("MaxPerType")][Description("How many currencies per type to display reports for.")] long maxCurrenciesPerType = DLConstants.CURRENCY_REPORT_COMMAND_MAX_CURRENCIES_PER_TYPE_DEFAULT,
            [Parameter("HolderCount")][Description("How many top account holders per currency to include in the report.")] long holdersPerCurrency = DLConstants.CURRENCY_REPORT_COMMAND_MAX_TOP_HOLDERS_PER_CURRENCY_DEFAULT)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.CurrenciesReport(ctx, currencyType, (int)maxCurrenciesPerType, (int)holdersPerCurrency);
            });
        }

        [Command("ElectionReport")]
        [Description("Displays the Election Report for the given election.")]
        public async Task ElectionReport(CommandContext command,
            [Parameter("Election")][Description("Name or ID of the election for which to display a report.")][SlashAutoCompleteProvider<ElectionAutoCompleteProvider>] string electionNameOrId)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ElectionReport(ctx, electionNameOrId);
            });
        }

        [Command("ElectionsReport")]
        [Description("Displays a report for the currently active elections.")]
        public async Task ElectionsReport(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ElectionsReport(ctx);
            });
        }

        [Command("WorkPartyReport")]
        [Description("Displays the Work Party Report for the given work party.")]
        public async Task WorkPartyReport(CommandContext command,
            [Parameter("WorkParty")][Description("Name or ID of the work party for which to display a report.")][SlashAutoCompleteProvider<WorkPartyAutoCompleteProvider>] string workPartyNameOrId)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartyReport(ctx, workPartyNameOrId);
            });
        }

        [Command("WorkPartiesReport")]
        [Description("Displays a report for the currently active work parties.")]
        public async Task WorkPartiesReport(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.WorkPartiesReport(ctx);
            });
        }

        [Command("SkillReport")]
        [Description("Displays a report for skill distribution.")]
        public async Task SkillReport(CommandContext command,
            [Parameter("IncludeScrollNoStar")][Description("If true; includes skills where players have only consumed a scroll but not consumed a star.")] bool includeScrollNoStar = false,
            [Parameter("IncludeInactive")][Description("If true; includes players who are currently not in the active demographic.")] bool includeInactive = false,
            [Parameter("SettlementFilter")][Description("Optional name or ID of a settlement for filtering players.")][SlashAutoCompleteProvider<SettlementAutoCompleteProvider>] string settlementFilterNameOrId = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                Settlement? settlementFilter = null;
                if (!string.IsNullOrWhiteSpace(settlementFilterNameOrId))
                {
                    settlementFilter = Lookups.SettlementByNameOrId(settlementFilterNameOrId);
                    if (settlementFilter == null)
                    {
                        await ReportCommandError(ctx, $"No settlement with the name or ID \"{settlementFilterNameOrId}\" could be found.");
                        return;
                    }
                }

                SpecialtyAssignmentData specialtyData = Skills.GetPlayerSpecialtyData(settlementFilter, includeScrollNoStar: includeScrollNoStar);
                DiscordLinkEmbed report = MessageBuilder.Discord.GetSpecialtiesReport(specialtyData, includeScrollNoStar, includeInactive, settlementFilter);
                await DisplayCommandData(ctx, string.Empty, report);
            });
        }

        #endregion

        #region Images

        [Command("ShowLayer")]
        [Description("Posts a link to the requested layer image.")]
        public async Task ShowLayer(CommandContext command,
            [Parameter("LayerName")][Description("Name of the world layer to show. The layer must must be a visible layer.")][SlashAutoCompleteProvider<LayerAutoCompleteProvider>] string layerName,
            [Parameter("ShowLayerHistory")][Description("If true; will post an animated gif showing how the history of the layer has changed per hour.")] bool showLayerHistory = false,
            [Parameter("ShowTerrainComparison")][Description("If true; will post a comparison gif showing the world terrain.")] bool showComparsionTerrain = false)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                IEnumerable<WorldLayer> layers = command.Member.IsAdmin() ? Lookups.Layers : Lookups.VisibleLayers;
                WorldLayer layer = layers.FirstOrDefault(layer => layer.Name.EqualsCaseInsensitive(layerName));
                if (layer == null)
                {
                    layer = Lookups.Layers.FirstOrDefault(layer => layer.Name.EqualsCaseInsensitive(layerName));
                    if (layer != null)
                        await ReportCommandError(ctx, $"{layer.Name} is not a visible layer.");
                    else
                        await ReportCommandError(ctx, $"No layer named \"{layerName}\" could be found.");
                    return;
                }

                string layerFileName = showLayerHistory ? layer.Name : $"{layer.Name}Latest";
                string terrainFileName = showLayerHistory ? "Terrain" : "TerrainLatest";
                string output = showComparsionTerrain
                ? $"{LayerUtils.GetLayerLink(layerFileName)}\n{LayerUtils.GetLayerLink(terrainFileName)}"
                : $"{LayerUtils.GetLayerLink(layerFileName)}";
                await ReportCommandInfo(ctx, output);
            });
        }

        [Command("ShowMap")]
        [Description("Posts a link to an image showing the world map.")]
        public async Task ShowMap(CommandContext command,
            [Parameter("MapType")][Description("The representation of the world map image.")] MapRepresentationType mapType = MapRepresentationType.Preview)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                string layerFileName = LayerUtils.GetLayerName(mapType);
                if (layerFileName.IsEmpty())
                {
                    await ReportCommandError(ctx, "Failed to resolve mapType parameter");
                    return;
                }

                await ReportCommandInfo(ctx, $"{LayerUtils.GetLayerLink(layerFileName)}");
            });
        }

        [Command("ShowWorldHistory")]
        [Description("Posts a link to a gif showing the world history.")]
        public async Task ShowWorldHistory(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                string webServerUrl = NetworkManager.Config.WebServerUrl;
                if (webServerUrl.IsEmpty())
                {
                    await ReportCommandError(ctx, "Web server URL not configured - Ensure that network config parameter `WebServerUrl` is set.");
                    return;
                }

                string layerFileName = "Terrain";
                await ReportCommandInfo(ctx, $"{LayerUtils.GetLayerLink(layerFileName)}");
            });
        }

        #endregion

        #region Invites

        [Command("PostInviteMessage")]
        [Description("Posts a Discord invite message to the Eco chat.")]
        public async Task PostInviteMessage(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.PostInviteMessage(ctx);
            });
        }

        #endregion

        #region Trades

        [Command("Trades")]
        [Description("Displays available trades by player, tag, item or store.")]
        public async Task Trades(CommandContext command,
            [Parameter("SearchName")][Description("The player name or item name for which to display trades. Case insensitive and auto completed.")][SlashAutoCompleteProvider<TradeTargetAutoCompleteProvider>] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Delayed);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                if (string.IsNullOrWhiteSpace(searchName))
                {
                    await ReportCommandInfo(ctx, "Please provide the name of a player, tag, item or store to search for.");
                    return;
                }

                LookupResult lookupRes = DynamicLookup.Lookup(searchName, Constants.TRADE_LOOKUP_MASK);
                if (lookupRes.Result != LookupResultTypes.SingleMatch)
                {
                    if (lookupRes.Result == LookupResultTypes.MultiMatch)
                        await ReportCommandInfo(ctx, lookupRes.ErrorMessage);
                    else
                        await ReportCommandError(ctx, lookupRes.ErrorMessage);
                    return;
                }
                object matchedEntity = lookupRes.Matches.First();
                LookupTypes matchedEntityType = lookupRes.MatchedTypes;
                string matchedEntityName = DynamicLookup.GetEntityName(matchedEntity);

                TradeOfferList offerList = Trade.FindOffers(matchedEntity, matchedEntityType);
                MessageBuilder.Discord.FormatTrades(matchedEntityName, matchedEntityType, offerList, out DiscordLinkEmbed embed);
                await DisplayCommandData(ctx, null, embed);
            });
        }

        [Command("dlt")]
        [Description("Shorthand for the Trades command.")]
        public async Task DLT(CommandContext command,
            [Parameter("SearchName")][Description("The player name or item name for which to display trades. Case insensitive and auto completed.")][SlashAutoCompleteProvider<TradeTargetAutoCompleteProvider>] string searchName)
        {
            await Trades(command, searchName);
        }

        [Command("WatchTradeDisplay")]
        [Description("Creates a live updated display of available trades by player, tag, item or store.")]
        public async Task WatchTradeDisplay(CommandContext command,
            [Parameter("SearchName")][Description("The player name or item name for which to display trades.")][SlashAutoCompleteProvider<TradeTargetAutoCompleteProvider>] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            });
        }

        [Command("UnwatchTradeDisplay")]
        [Description("Removes the live updated display of available trades for a player, tag, item or store.")]
        public async Task UnwatchTradeDisplay(CommandContext command,
            [Parameter("SearchName")][Description("The player, tag, item or store name for which to display trades.")][SlashAutoCompleteProvider<TradeDisplayWatcherAutoCompleteProvider>] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Display);
            });
        }

        [Command("WatchTradeFeed")]
        [Description("Creates a trade feed filtered by a search query.")]
        public async Task WatchTradeFeed(CommandContext command,
            [Parameter("SearchName")][Description("The player, tag, item or store name for which to post trades.")][SlashAutoCompleteProvider<TradeTargetAutoCompleteProvider>] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.AddTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            });
        }

        [Command("UnwatchTradeFeed")]
        [Description("Removes the trade watcher feed for a player, tag, item or store.")]
        public async Task UnwatchTradeFeed(CommandContext command,
            [Parameter("SearchName")][Description("The player, tag item or store name for which to remove trades.")][SlashAutoCompleteProvider<TradeFeedWatcherAutoCompleteProvider>] string searchName)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.RemoveTradeWatcher(ctx, searchName, Modules.ModuleArchetype.Feed);
            });
        }

        [Command("ListTradeWatchers")]
        [Description("Lists all trade watchers for the calling user.")]
        public async Task ListTradeWatchers(CommandContext command)
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.ListTradeWatchers(ctx);
            });
        }

        #endregion

        #region Snippets

        [Command("Snippet")]
        [Description("Posts a predefined snippet to Eco or Discord.")]
        public async Task Snippet(CommandContext command,
            [Parameter("Key")][Description("Key of the snippet to post. Displays the key list if omitted.")][SlashAutoCompleteProvider<SnippetAutoCompleteProvider>] string snippetKey = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ctx, ApplicationInterfaceType.Discord, ctx.Command.GetSenderName(), snippetKey);
            });
        }

        [Command("EcoSnippet")]
        [Description("Posts a predefined snippet to Eco.")]
        public async Task EcoSnippet(CommandContext command,
            [Parameter("Key")][Description("Key of the snippet to post. Displays the key list if omitted.")][SlashAutoCompleteProvider<SnippetAutoCompleteProvider>] string snippetKey = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.User, ctx, async (lCtx, args) =>
            {
                await SharedCommands.Snippet(ctx, ApplicationInterfaceType.Eco, ctx.Command.GetSenderName(), snippetKey);
            });
        }

        #endregion

        #region Message Relaying

        [Command("Announce")]
        [Description("Announces a message to everyone or a specified user.")]
        public async Task Announce(CommandContext command,
            [Parameter("Message")][Description("The message to send.")] string message,
            [Parameter("MessageType")][Description("The type of message to send.")] MessageTypes messageType = MessageTypes.Notification,
            [Parameter("Player")][Description("Name or ID of the player to send the message to. Sends to everyone if omitted.")][SlashAutoCompleteProvider<PlayerAutoCompleteProvider>] string recipientUserNameOrId = "")
        {
            DiscordCommandContext ctx = new DiscordCommandContext(command, ResponseTiming.Immediate);
            await ExecuteCommand<object>(PermissionType.Admin, ctx, async (lCtx, args) =>
            {
                if (message.IsEmpty())
                {
                    await ReportCommandError(ctx, $"Failed to send message - Message can not be empty");
                    return;
                }

                User recipient = null;
                if (!recipientUserNameOrId.IsEmpty())
                {
                    recipient = Lookups.Users.FirstOrDefault(user => user.Name.EqualsCaseInsensitive(recipientUserNameOrId) || user.Id.ToString().EqualsCaseInsensitive(recipientUserNameOrId));
                    if (recipient == null)
                    {
                        await ReportCommandError(ctx, $"No player with the name or ID \"{recipientUserNameOrId}\" could be found.");
                        return;
                    }
                }


                if (recipient != null && messageType != Moose.Data.Enums.MessageTypes.NotificationOffline && !recipient.IsOnline)
                {
                    await ReportCommandError(ctx, $"Failed to send message - {recipient.Name} is offline.");
                    return;
                }

                string formattedMessage = messageType switch
                {
                    Moose.Data.Enums.MessageTypes.Chat => $"{ctx.Command.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageTypes.Info => $"{ctx.Command.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageTypes.Warning => $"{ctx.Command.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageTypes.Error => $"{ctx.Command.Member.DisplayName}: {message}",
                    Moose.Data.Enums.MessageTypes.Notification => $"[{ctx.Command.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageTypes.NotificationOffline => $"[{ctx.Command.Member.DisplayName}]\n\n{message}",
                    Moose.Data.Enums.MessageTypes.Popup => $"[{ctx.Command.Member.DisplayName}]\n{message}",
                };

                bool result = true;
                switch (messageType)
                {
                    case Moose.Data.Enums.MessageTypes.Chat:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendChatToUser(null, recipient, formattedMessage);
                            }
                            else
                            {
                                result = Message.SendChatToDefaultChannel(null, formattedMessage);
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageTypes.Info:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendInfoBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendInfoBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageTypes.Warning:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendWarningBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendWarningBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageTypes.Error:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendErrorBoxToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendErrorBoxToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageTypes.Popup:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendPopupToUser(recipient, formattedMessage);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendPopupToUser(onlineUser, formattedMessage) && result;
                                }
                            }
                            break;
                        }
                    case Moose.Data.Enums.MessageTypes.Notification:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: false);
                            }
                            else
                            {
                                foreach (User onlineUser in UserManager.OnlineUsers)
                                {
                                    result = Message.SendNotificationToUser(onlineUser, formattedMessage, sendOffline: false) && result;
                                }
                            }
                            break;
                        }

                    case Moose.Data.Enums.MessageTypes.NotificationOffline:
                        {
                            if (recipient != null)
                            {
                                result = Message.SendNotificationToUser(recipient, message, sendOffline: true);
                            }
                            else
                            {
                                foreach (User user in UserManager.Users)
                                {
                                    result = Message.SendNotificationToUser(user, formattedMessage, sendOffline: true) && result;
                                }
                            }
                            break;
                        }
                }

                string sendContext = recipient == null ? "all players" : recipient.Name;
                if (result)
                    await ReportCommandInfo(ctx, $"Message delivered to {sendContext}.");
                else
                    await ReportCommandError(ctx, $"Failed to send message to {sendContext}.");
            });
        }

        #endregion
    }
}