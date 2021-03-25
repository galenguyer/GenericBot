using Discord;
using Discord.WebSocket;
using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenericBot.CommandModules
{
    class BanModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command ban = new Command("ban");
            ban.Description = "Ban a user from the server, whether or not they're on it";
            ban.Delete = false;
            ban.RequiredPermission = Command.PermissionLevels.Moderator;
            ban.Aliases = new List<string> { "yeet" };
            ban.Usage = $"{ban.Name} <user> <time> <reason>";
            ban.ToExecute += async (context) =>
            {
                // Fail fast if no parameters are provided
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                // Parse out UserId
                ulong userId;
                if (!ulong.TryParse(context.Parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out userId))
                {
                    await context.Message.ReplyAsync("Please provide a UserId or mention the user you want to ban");
                    return;
                }

                // Prevent banning the bot owner
                if (userId == Core.DiscordClient.GetApplicationInfoAsync().Result.Owner.Id)
                {
                    await context.Message.ReplyAsync("Haha lol no");
                    return;
                }

                // Remove UserId from the parameter list
                context.Parameters.RemoveAt(0);

                // Get the time to ban the user for
                var time = new DateTimeOffset();
                try
                {
                    if (context.Parameters[0] == "0" || context.Parameters[0] == "0d")
                        time = DateTimeOffset.MaxValue;
                    else
                        time = context.Parameters[0].ParseTimeString();
                    context.Parameters.RemoveAt(0);
                }
                // In case no time was specified
                catch (FormatException)
                {
                    time = DateTimeOffset.MaxValue;
                }
                catch (ArgumentOutOfRangeException)
                {
                    time = DateTimeOffset.MaxValue;
                }
                // If something very wrong happened
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    await context.Message.ReplyAsync($"An unknown error has occured while trying to parse the time to ban, and the developer has been notified. Please try again or with different options");
                    return;
                }
                string timeMessage = time == DateTimeOffset.MaxValue ? "permanently" : $"for `{(time.AddSeconds(1) - DateTimeOffset.UtcNow).FormatTimeString()}`";

                // Check if the user was already banned
                var bans = context.Guild.GetBansAsync().Result;
                if (bans.Any(b => b.User.Id == userId))
                {
                    await context.Message.ReplyAsync(
                        $"`{bans.First(b => b.User.Id == userId).User}` is already banned with the message `{bans.First(b => b.User.Id == userId).Reason}`");
                    return;
                }
                string reason = context.Parameters.IsEmpty() ? "No Reason Given" : context.Parameters.Rejoin();

                bool dmSuccess = true;
                string dmMessage = $"You have been banned from **{context.Guild.Name}** {timeMessage} " +
                $"for the following reason: \n\n{reason}\n\n";
                
                // Try to DM the user the message, set a flag and continue if they're blocking DMs
                try
                {
                    await context.Guild.GetUser(userId).GetOrCreateDMChannelAsync().Result
                        .SendMessageAsync(dmMessage);
                }
                catch (Exception ex)
                {
                    // Disable this for now, we don't need to report failed ban messages
                    //await Core.Logger.LogErrorMessage(ex, context);
                    dmSuccess = false;
                }

                try
                {
                    // We have to do some stuff to make the Discord audit log
                    // happy with what we send it
                    string auditReason = reason.Replace("\"", "'");
                    if (auditReason.Length > 256)
                    {
                        auditReason = auditReason.Substring(0, 250) + "...";
                    }
                    await context.Guild.AddBanAsync(userId, reason: auditReason);
                }
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    await context.Message.ReplyAsync($"Could not ban the given user. Try checking role hierarchy and permissions");
                    return;
                }

                var user = context.Guild.GetBansAsync().Result.First(u => u.User.Id == userId).User;
                string banMessage = $"Banned `{user}` (`{user.Id}`)";
                // TODO: check if this adds a double :ok_hand:
                if (string.IsNullOrEmpty(reason))
                    banMessage += $" 👌";
                else
                    banMessage += $" for `{reason}`";
                banMessage += $"{timeMessage} 👌";

                if (!dmSuccess) banMessage += "\nThe user could not be messaged";

                var builder = new EmbedBuilder()
                    .WithTitle("User Banned")
                    .WithDescription(banMessage)
                    .WithColor(new Color(0xFFFF00))
                    .WithFooter(footer => {
                        footer
                            .WithText($"By {context.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                    })
                    .WithAuthor(author => {
                        author
                            .WithName(user.ToString())
                            .WithIconUrl(user.GetAvatarUrl());
                    });


                // Load the guild configs and databases, unmute the user, and add a note to their database profile
                var guildconfig = Core.GetGuildConfig(context.Guild.Id);
                if (guildconfig.MutedUsers != null && guildconfig.MutedUsers.Contains(userId))
                    guildconfig.MutedUsers.Remove(userId);
                Core.SaveGuildConfig(guildconfig);
                var bannedUser = Core.GetUserFromGuild(user.Id, context.Guild.Id)
                    .AddWarning(
                        $"Banned {timeMessage} for `{reason}` (By `{context.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");

                // Remove trusted role from rolestore, if present and zero-out points to prevent it being re-added.
                bannedUser.RemoveStoredRole(guildconfig.TrustedRoleId);
                bannedUser.Points = 0;

                Core.SaveUserToGuild(bannedUser, context.Guild.Id);
                Core.SaveBanToGuild(new GenericBan(user.Id, context.Guild.Id, reason, time), context.Guild.Id);

                // Send the ban logs
                await context.Channel.SendMessageAsync("", embed: builder.Build());
                if (guildconfig.LoggingChannelId != 0)
                {
                    await (Core.DiscordClient.GetChannel(guildconfig.LoggingChannelId) as SocketTextChannel)
                        .SendMessageAsync("", embed: builder.Build());
                }
            };
            commands.Add(ban);

            // TODO: purgeing only works on users on the server, but this isn't reflected in the code
            Command purgeban = new Command("purgeban");
            purgeban.Description = "Ban a user from the server, whether or not they're on it, and delete the last 24 hours of their messages";
            purgeban.Delete = false;
            purgeban.RequiredPermission = Command.PermissionLevels.Moderator;
            purgeban.Usage = $"{ban.Name} <user> <time> <reason>";
            purgeban.ToExecute += async (context) =>
            {
                // Fail fast if no parameters are provided
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                // Parse out UserId
                ulong userId;
                if (!ulong.TryParse(context.Parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out userId))
                {
                    await context.Message.ReplyAsync("Please provide a UserId or mention the user you want to ban");
                    return;
                }

                // Prevent banning the bot owner
                if (userId == Core.DiscordClient.GetApplicationInfoAsync().Result.Owner.Id)
                {
                    await context.Message.ReplyAsync("Haha lol no");
                    return;
                }

                // Remove UserId from the parameter list
                context.Parameters.RemoveAt(0);

                // Get the time to ban the user for
                var time = new DateTimeOffset();
                try
                {
                    if (context.Parameters[0] == "0" || context.Parameters[0] == "0d")
                        time = DateTimeOffset.MaxValue;
                    else
                        time = context.Parameters[0].ParseTimeString();
                    context.Parameters.RemoveAt(0);
                }
                // In case no time was specified
                catch (FormatException)
                {
                    time = DateTimeOffset.MaxValue;
                }
                catch (ArgumentOutOfRangeException)
                {
                    time = DateTimeOffset.MaxValue;
                }
                // If something very wrong happened
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    await context.Message.ReplyAsync($"An unknown error has occured while trying to parse the time to ban, and the developer has been notified. Please try again or with different options");
                    return;
                }
                string timeMessage = time == DateTimeOffset.MaxValue ? "permanently" : $"for `{(time.AddSeconds(1) - DateTimeOffset.UtcNow).FormatTimeString()}`";

                // Check if the user was already banned
                var bans = context.Guild.GetBansAsync().Result;
                if (bans.Any(b => b.User.Id == userId))
                {
                    await context.Message.ReplyAsync(
                        $"`{bans.First(b => b.User.Id == userId).User}` is already banned with the message `{bans.First(b => b.User.Id == userId).Reason}`");
                    return;
                }
                string reason = context.Parameters.IsEmpty() ? "No Reason Given" : context.Parameters.Rejoin();

                bool dmSuccess = true;
                string dmMessage = $"You have been banned from **{context.Guild.Name}** {timeMessage} " +
                $"for the following reason: \n\n{reason}\n\n";

                // Try to DM the user the message, set a flag and continue if they're blocking DMs
                try
                {
                    await context.Guild.GetUser(userId).GetOrCreateDMChannelAsync().Result
                        .SendMessageAsync(dmMessage);
                }
                catch (Exception ex)
                {
                    // Disable this for now, we don't need to report failed ban messages
                    //await Core.Logger.LogErrorMessage(ex, context);
                    dmSuccess = false;
                }

                try
                {
                    // We have to do some stuff to make the Discord audit log
                    // happy with what we send it
                    string auditReason = reason.Replace("\"", "'");
                    if (auditReason.Length > 256)
                    {
                        auditReason = auditReason.Substring(0, 250) + "...";
                    }
                    await context.Guild.AddBanAsync(userId, reason: auditReason, pruneDays: 1);
                }
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    await context.Message.ReplyAsync($"Could not ban the given user. Try checking role hierarchy and permissions");
                    return;
                }

                var user = context.Guild.GetBansAsync().Result.First(u => u.User.Id == userId).User;
                string banMessage = $"Purgebanned `{user}` (`{user.Id}`)";
                // TODO: check if this adds a double :ok_hand:
                if (string.IsNullOrEmpty(reason))
                    banMessage += $" 👌";
                else
                    banMessage += $" for `{reason}`";
                banMessage += $"{timeMessage} 👌";

                if (!dmSuccess) banMessage += "\nThe user could not be messaged";

                var builder = new EmbedBuilder()
                    .WithTitle("User Banned")
                    .WithDescription(banMessage)
                    .WithColor(new Color(0xFFFF00))
                    .WithFooter(footer => {
                        footer
                            .WithText($"By {context.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                    })
                    .WithAuthor(author => {
                        author
                            .WithName(user.ToString())
                            .WithIconUrl(user.GetAvatarUrl());
                    });


                // Load the guild configs and databases, unmute the user, and add a note to their database profile
                var guildconfig = Core.GetGuildConfig(context.Guild.Id);
                if (guildconfig.MutedUsers != null && guildconfig.MutedUsers.Contains(userId))
                    guildconfig.MutedUsers.Remove(userId);
                Core.SaveGuildConfig(guildconfig);
                var bannedUser = Core.GetUserFromGuild(user.Id, context.Guild.Id)
                    .AddWarning(
                        $"Purgebanned {timeMessage} for `{reason}` (By `{context.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");

                // Remove trusted role from rolestore, if present and zero-out points to prevent it being re-added.
                bannedUser.RemoveStoredRole(guildconfig.TrustedRoleId);
                bannedUser.Points = 0;

                Core.SaveUserToGuild(bannedUser, context.Guild.Id);
                Core.SaveBanToGuild(new GenericBan(user.Id, context.Guild.Id, reason, time), context.Guild.Id);

                // Send the ban logs
                await context.Channel.SendMessageAsync("", embed: builder.Build());
                if (guildconfig.LoggingChannelId != 0)
                {
                    await (Core.DiscordClient.GetChannel(guildconfig.LoggingChannelId) as SocketTextChannel)
                        .SendMessageAsync("", embed: builder.Build());
                }
            };
            commands.Add(purgeban);

            Command kick = new Command("kick");
            kick.Description = "kick a user from the server, whether or not they're on it";
            kick.Delete = false;
            kick.RequiredPermission = Command.PermissionLevels.Moderator;
            kick.Usage = $"{kick.Name} <user> <reason>";
            kick.ToExecute += async (context) =>
            {
                // Check for commands
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync($"You need to add some arguments. A user, perhaps?");
                    return;
                }

                // Parse out UserId
                ulong userId;
                if (!ulong.TryParse(context.Parameters[0].TrimStart('<', '@', '!').TrimEnd('>'), out userId))
                {
                    await context.Message.ReplyAsync("Try specifying someone to kick first");
                    return;
                }

                // Prevent kicking me
                if (userId == Core.DiscordClient.GetApplicationInfoAsync().Result.Owner.Id)
                {
                    await context.Message.ReplyAsync("Haha lol no");
                    return;
                }

                // Remove UserId from param stack
                context.Parameters.RemoveAt(0);

                string reason = context.Parameters.IsEmpty() ? "No Reason Given" : context.Parameters.Rejoin();

                bool dmSuccess = true;
                string dmMessage = $"You have been kicked from **{context.Guild.Name}** for the following reason: \n\n{reason}\n\n";

                // Try to DM the user the message, set a flag and continue if they're blocking DMs
                try
                {
                    await context.Guild.GetUser(userId).GetOrCreateDMChannelAsync().Result
                        .SendMessageAsync(dmMessage);
                }
                catch(Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    dmSuccess = false;
                }

                var user = context.Guild.GetUser(userId);
                try
                {
                    // We have to do some stuff to make the Discord audit log
                    // happy with what we send it
                    string auditReason = reason.Replace("\"", "'");
                    if (auditReason.Length > 256)
                    {
                        auditReason = auditReason.Substring(0, 250) + "...";
                    }
                    await user.KickAsync(auditReason);
                }
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    await context.Message.ReplyAsync($"Could not kick the given user. Try checking role hierarchy and permissions");
                    return;
                }

                string banMessage = $"Kicked `{user}` (`{user.Id}`)";
                if (string.IsNullOrEmpty(reason))
                    banMessage += $" 👌";
                else
                    banMessage += $" for `{reason}`";

                if (!dmSuccess) banMessage += "\nThe user could not be messaged";

                var builder = new EmbedBuilder()
                    .WithTitle("User Kicked")
                    .WithDescription(banMessage)
                    .WithColor(new Color(0xFFFF00))
                    .WithFooter(footer => {
                        footer
                            .WithText($"By {context.Author} at {DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                    })
                    .WithAuthor(author => {
                        author
                            .WithName(user.ToString())
                            .WithIconUrl(user.GetAvatarUrl());
                    });


                var guildconfig = Core.GetGuildConfig(context.Guild.Id);
                if (guildconfig.MutedUsers.Contains(user.Id))
                    guildconfig.MutedUsers.Remove(user.Id);
                Core.SaveGuildConfig(guildconfig);
                var kickedUser = Core.GetUserFromGuild(user.Id, context.Guild.Id)
                    .AddWarning(
                        $"Kicked {user} for `{reason}` (By `{context.Author}` At `{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT`)");
                Core.SaveUserToGuild(kickedUser, context.Guild.Id);

                // Remove trusted role from rolestore, if present and zero-out points to prevent it being re-added.
                kickedUser.RemoveStoredRole(guildconfig.TrustedRoleId);
                kickedUser.Points = 0;

                // Send the kick logs
                await context.Channel.SendMessageAsync("", embed: builder.Build());
                if (guildconfig.LoggingChannelId != 0)
                {
                    await (Core.DiscordClient.GetChannel(guildconfig.LoggingChannelId) as SocketTextChannel)
                        .SendMessageAsync("", embed: builder.Build());
                }
            };
            commands.Add(kick);

            // TODO: flow here sucks and fails silently at many points
            Command unban = new Command("unban");
            unban.Description = "Unban a user given their ID";
            unban.RequiredPermission = Command.PermissionLevels.Moderator;
            unban.Usage = "unban <userId";
            unban.ToExecute += async (context) =>
            {
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync("Please specify a userid to unban");
                    return;
                }
                if (ulong.TryParse(context.Parameters[0], out ulong bannedUId) && 
                Core.GetBansFromGuild(context.Guild.Id).HasElement(b => b.Id == bannedUId, out GenericBan banToRemove))
                {
                    Core.RemoveBanFromGuild(bannedUId, context.Guild.Id);
                    try
                    {
                        var user = context.Guild.GetBansAsync().Result.First(b => b.User.Id == bannedUId).User;
                        await context.Guild.RemoveBanAsync(bannedUId);

                        await context.Message.ReplyAsync($"Succesfully unbanned `{user}` (`{user.Id}`)");

                        var builder = new EmbedBuilder()
                            .WithTitle("User Unbanned")
                            .WithDescription($"Banned for: {banToRemove.Reason}")
                            .WithColor(new Color(0xFFFF00))
                            .WithFooter(footer => {
                                footer
                                    .WithText($"{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")} GMT");
                            })
                            .WithAuthor(author => {
                                author
                                    .WithName(user.ToString())
                                    .WithIconUrl(user.GetAvatarUrl());
                            })
                            .AddField(new EmbedFieldBuilder().WithName("All Warnings").WithValue(
                                Core.GetUserFromGuild(banToRemove.Id, context.Guild.Id).Warnings.SumAnd()));
                        await ((SocketTextChannel)Core.DiscordClient.GetChannel(Core.GetGuildConfig(context.Guild.Id).LoggingChannelId))
                            .SendMessageAsync("", embed: builder.Build());
                    }
                    catch (Discord.Net.HttpException httpException)
                    {
                        await Core.Logger.LogErrorMessage(httpException, context);
                        await context.Message.ReplyAsync("Could not unban that user. Either I don't have the permissions or they weren't banned");
                    }
                }
            };
            commands.Add(unban);

            return commands;
        }
    }
}
