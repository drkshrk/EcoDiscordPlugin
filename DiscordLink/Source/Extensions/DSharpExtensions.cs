using DSharpPlus.Commands;
using DSharpPlus.Entities;
using Eco.Shared.Utils;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Extensions
{
    public static class DSharpExtensions
    {
        #region InteractionContext

        public static ulong GetSenderId(this CommandContext command)
        {
            DiscordUser user = command.Member ?? command.User;
            return user.Id;
        }

        public static string GetSenderName(this CommandContext command)
        {
            if (command.Member != null)
                return command.Member.DisplayName;
            else
                return command.User.Username;
        }

        #endregion

        #region DiscordClient

        public static string[] GuildNames(this DSharpPlus.DiscordClient client) => client.Guilds.Values.Select(guild => guild.Name).ToArray();

        public static DiscordGuild GetDefaultGuild(this DSharpPlus.DiscordClient client) => client.Guilds.FirstOrDefault().Value;

        public static DiscordGuild GetGuildByName(this DSharpPlus.DiscordClient client, string name) => client.Guilds.Values.FirstOrDefault(guild => guild.Name == name);

        #endregion

        #region DiscordGuild

        public static string GetLogName(this DiscordGuild guild) => $"\"{guild.Name}\" ({guild.Id})";

        public static DiscordRole GetRoleByName(this DiscordGuild guild, string roleName)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Name.EqualsCaseInsensitive(roleName));
        }

        public static DiscordRole GetRoleById(this DiscordGuild guild, ulong roleId)
        {
            return guild.Roles.Values.FirstOrDefault(role => role.Id == roleId);
        }

        #endregion

        #region DiscordChannel

        public static string GetLogName(this DiscordChannel channel) => $"\"{channel.Name}\" ({channel.Id})";

        public static bool HasNameOrId(this DiscordChannel channel, string nameOrChannelId)
        {
            if (nameOrChannelId.TryParseSnowflakeId(out ulong channelId))
                return channel.Id == channelId;

            return channel.Name.EqualsCaseInsensitive(nameOrChannelId);
        }

        #endregion

        #region DiscordUser

        public static string GetLogName(this DiscordUser user) => $"\"{user.Username}\" ({user.Id})";

        public static bool HasNameOrId(this DiscordUser user, string nameOrUserId)
        {
            if (nameOrUserId.TryParseSnowflakeId(out ulong userId))
                return user.Id == userId;

            return user.Username.EqualsCaseInsensitive(nameOrUserId);
        }

        public static async Task<DiscordMember> LookupMember(this DiscordUser user)
        {
            DiscordClient client = DiscordLink.Obj.Client;
            DiscordMember member = client.GetMemberById(user.Id);
            if (member == null)
                member = await client.FetchMemberAsync(user.Id, expectNotFound: true);

            return member;
        }

        #endregion

        #region DiscordMember

        public static string GetLogName(this DiscordMember member) => $"\"{member.DisplayName}\" ({member.Id})";

        public static DiscordRole GetHighestHierarchyRole(this DiscordMember member)
        {
            return member.Roles.OrderByDescending(r => r.Position).FirstOrDefault();
        }

        public static string GetHighestHierarchyRoleName(this DiscordMember member)
        {
            string topRoleName = "Member";
            if (member.IsOwner)
            {
                topRoleName = "Owner";
            }
            else
            {
                DiscordRole topRole = member.GetHighestHierarchyRole();
                if (topRole != null)
                    topRoleName = topRole.Name;
            }
            return topRoleName;
        }

        public static bool HasNameOrMemberId(this DiscordMember member, string nameOrId)
        {
            if (nameOrId.TryParseSnowflakeId(out ulong Id))
                return member.Id == Id;

            return member.Username.EqualsCaseInsensitive(nameOrId) || member.Username.EqualsCaseInsensitive(nameOrId);
        }

        public static bool HasRole(this DiscordMember member, DiscordRole role)
        {
            return member.Roles.Any(memberRole => memberRole.Id == role.Id);
        }

        public static bool HasRoleWithName(this DiscordMember member, string roleName)
        {
            return member.Roles.Any(role => role.Name.EqualsCaseInsensitive(roleName));
        }

        public static bool IsAdmin(this DiscordMember member)
        {
            return member.Roles.Any(role => DiscordLinkConfig.AdminRoles.Any(adminRoleName => adminRoleName.EqualsCaseInsensitive(role.Name)));
        }

        #endregion

        #region DiscordRole

        public static string GetLogName(this DiscordRole role) => $"\"{role.Name}\" ({role.Id})";

        public static bool HasNameOrId(this DiscordRole role, string nameOrId)
        {
            if (nameOrId.TryParseSnowflakeId(out ulong roleId))
                return role.Id == roleId;

            return role.Name.EqualsCaseInsensitive(nameOrId);
        }

        #endregion

        #region DiscordMessage

        public static string GetLogId(this DiscordMessage message) => $"({message.Id})";
        public static string GetLogData(this DiscordMessage message)
        {
            return $"Channel: {message.GetChannel()}" +
                $"\nAuthor: {message.Author}" +
                $"\nMessage: {message.Content}" +
                $"\nAttachments ({message.Attachments.Count}): {string.Join(", ", message.Attachments.Select(a => $"{a.FileName} ({a.FileSize} bytes)"))}";
        }
        public static DiscordChannel GetChannel(this DiscordMessage message) => message.Channel ?? DiscordLink.Obj.Client.GetChannelById(message.ChannelId);

        #endregion

        #region DiscordEmoji

        public static string GetLogName(this DiscordEmoji emoji) => $"{emoji.GetDiscordName()} ({emoji.Id})";

        #endregion
    }
}