using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GenericBot.Database;
using GenericBot.Entities;

namespace GenericBot
{
    public static class UserEventHandler
    {
        public static async Task UserUpdated(SocketUser bUser, SocketUser aUser)
        {
            SocketGuildUser beforeUser = bUser as SocketGuildUser;
            SocketGuildUser afterUser = aUser as SocketGuildUser;
            if (beforeUser == null || beforeUser.Username != afterUser.Username || beforeUser.Nickname != afterUser.Nickname)
            {
                var user = Core.GetUserFromGuild(afterUser.Id, afterUser.Guild.Id, log: false);
                if (beforeUser != null)
                {
                    user.AddUsername(beforeUser.Username);
                    user.AddNickname(beforeUser);
                }
                user.AddUsername(afterUser.Username);
                user.AddNickname(afterUser);
                Core.SaveUserToGuild(user, afterUser.Guild.Id, log: false);
            }
        }

        public static async Task UserJoined(SocketGuildUser user)
        {
            #region Database

            var dbUser = Core.GetUserFromGuild(user.Id, user.Guild.Id);
            bool alreadyJoined = dbUser.Usernames != null;
            dbUser.AddUsername(user.Username);
            dbUser.IsPresent = true;
            Core.SaveUserToGuild(dbUser, user.Guild.Id);

            #endregion Databasae

            var guildConfig = Core.GetGuildConfig(user.Guild.Id);

            #region JoinMessage

            if (guildConfig.JoinMessageChannelId != 0 && !string.IsNullOrEmpty(guildConfig.JoinMessage) && user.Guild.Channels.Any(r => r.Id == guildConfig.JoinMessageChannelId))
            {
                await user.Guild.GetTextChannel(guildConfig.JoinMessageChannelId).SendMessageAsync(
                    VerificationEngine.ConstructWelcomeMessage(guildConfig.JoinMessage, user));
            }

            #endregion JoinMessage

            #region Logging


            if (!(guildConfig.VerifiedRole == 0 || (string.IsNullOrEmpty(guildConfig.VerifiedMessage) || guildConfig.VerifiedMessage.Split().Length < 32 || !user.Guild.Roles.Any(r => r.Id == guildConfig.VerifiedRole))))
            {
                string vMessage = $"Hey {user.Username}! To get verified on **{user.Guild.Name}** reply to this message with the hidden code in the message below\n\n"
                                 + guildConfig.VerifiedMessage;

                string verificationMessage =
                    VerificationEngine.InsertCodeInMessage(vMessage, VerificationEngine.GetVerificationCode(user.Id, user.Guild.Id));

                try
                {
                    await user.GetOrCreateDMChannelAsync().Result.SendMessageAsync(verificationMessage);
                }
                catch
                {
                    await Core.Logger.LogErrorMessage(new Exception($"Could not send verification DM to {user} ({user.Id}) on {user.Guild} ({user.Guild.Id})"), null);
                }
            }
            if (guildConfig.MutedUsers.Contains(user.Id))
            {
                try
                {
                    await user.AddRoleAsync(user.Guild.GetRole(guildConfig.MutedRoleId));
                }
                catch(Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, null);
                }
            }
            if(guildConfig.AutoRoleIds != null && guildConfig.AutoRoleIds.Any())
            {
                foreach(var role in guildConfig.AutoRoleIds)
                {
                    try
                    {
                        await user.AddRoleAsync(user.Guild.GetRole(role));
                    }
                    catch(Exception ex)
                    {
                        await Core.Logger.LogErrorMessage(ex, null);
                    }
                }
            }

            if (guildConfig.LoggingChannelId == 0) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder().WithName("User Joined")
                    .WithIconUrl(user.GetAvatarUrl()).WithUrl(user.GetAvatarUrl()))
                .WithColor(114, 137, 218)
                .AddField(new EmbedFieldBuilder().WithName("Username").WithValue(user.ToString().Escape()).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("UserId").WithValue(user.Id).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Mention").WithValue(user.Mention).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("User Number").WithValue(user.Guild.MemberCount + (!alreadyJoined ? " (New Member)" : "")).WithIsInline(true))
                //.AddField(new EmbedFieldBuilder().WithName("Database Number").WithValue(guildDb.Users.Count + (alreadyJoined ? " (Previous Member)" : "")).WithIsInline(true))
                .WithFooter($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");

            if ((DateTimeOffset.Now - user.CreatedAt).TotalDays < 7)
            {
                log.AddField(new EmbedFieldBuilder().WithName("New User")
                    .WithValue($"Account made {(DateTimeOffset.Now - user.CreatedAt).Nice()} ago").WithIsInline(true));
            }

            try
            {
                if (!dbUser.Warnings.IsEmpty())
                {
                    string warns = "";
                    for (int i = 0; i < dbUser.Warnings.Count; i++)
                    {
                        warns += $"{i + 1}: {dbUser.Warnings.ElementAt(i)}\n";
                    }
                    log.AddField(new EmbedFieldBuilder().WithName($"{dbUser.Warnings.Count} Warnings")
                        .WithValue(warns));
                }
            }
            catch (Exception ex)
            {
                await Core.Logger.LogErrorMessage(ex, null);
            }

            await user.Guild.GetTextChannel(guildConfig.LoggingChannelId).SendMessageAsync("", embed: log.Build());

            #endregion Logging
        }

        public static async Task UserLeft(SocketGuildUser user)
        {
            #region Logging

            var guildConfig = Core.GetGuildConfig(user.Guild.Id);

            if (guildConfig.LoggingChannelId == 0) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder().WithName("User Left")
                    .WithIconUrl(user.GetAvatarUrl()).WithUrl(user.GetAvatarUrl()))
                .WithColor(156, 39, 176)
                .AddField(new EmbedFieldBuilder().WithName("Username").WithValue(user.ToString().Escape()).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("UserId").WithValue(user.Id).WithIsInline(true))
                .AddField(new EmbedFieldBuilder().WithName("Mention").WithValue(user.Mention).WithIsInline(true))
                .WithFooter($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");

            await user.Guild.GetTextChannel(guildConfig.LoggingChannelId).SendMessageAsync("", embed: log.Build());

            #endregion Logging

            var dbUser = Core.GetUserFromGuild(user.Id, user.Guild.Id);
            dbUser.IsPresent = false;
            Core.SaveUserToGuild(dbUser, user.Guild.Id);
        }
    }
}
