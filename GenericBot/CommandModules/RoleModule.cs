using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    public class RoleModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command UserRoles = new Command("userroles");
            UserRoles.Description = $"Show all user roles on this server";
            UserRoles.Usage = "userroles";
            UserRoles.ToExecute += async (context) =>
            {
                string prefix = Core.GetPrefix(context);
                string message = $"You can use `{prefix}iam` and `{prefix}iamnot` with any of these roles:\n _\\*(Pro tip: You can add/remove more than one role at a time by seperating each role with a comma like `{prefix}iam role0, role1, role2, etc`)*_\n";
                var config = Core.GetGuildConfig(context.Guild.Id);
                foreach (var group in config.UserRoles.Where(g => !string.IsNullOrEmpty(g.Key) && g.Value.Count > 0).OrderBy(g => g.Key))
                {
                    message += $"**{group.Key}:** ";
                    foreach (var role in context.Guild.Roles
                        .Where(r => group.Value.Contains(r.Id))
                        .OrderBy(r => r.Name))
                    {
                        if ((context.Author as SocketGuildUser).Roles.Contains(role))
                            message += "\\✔ ";
                        else
                            message += "✘";
                        message += $"`{role.Name}`, ";
                    }
                    message += "\n";
                }

                if (config.UserRoles[""].Count > 0)
                {
                    message += $"**Ungrouped:** ";
                    foreach (var role in context.Guild.Roles
                        .Where(r => config.UserRoles[""].Contains(r.Id))
                        .OrderBy(r => r.Name))
                    {
                        if ((context.Author as SocketGuildUser).Roles.Contains(role))
                            message += "\\✔ ";
                        else
                            message += "✘";
                        message += $"`{role.Name}`, ";
                    }
                    message += "\n";
                }

                message = message.Trim(' ', ',', '\n');
                //message += $"\n You can also use `{prefix}rolestore save` to backup your assigned roles";

                foreach (var str in message.MessageSplit())
                {
                    await context.Message.ReplyAsync(str);
                }
            };
            commands.Add(UserRoles);

            Command iam = new Command("iam");
            iam.Description = "Join a User Role";
            iam.Usage = "iam <role name>";
            iam.Aliases = new List<string> { "join" };
            iam.ToExecute += async (context) =>
            {
                List<IMessage> messagesToDelete = new List<IMessage>();

                if (context.Parameters.IsEmpty())
                {
                    messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription("Please select a role to join").WithColor(new Color(0xFFFF00)).Build()).Result);
                }

                foreach (var roleName in context.ParameterString.Trim(',', ' ').Split(','))
                {
                    if (string.IsNullOrWhiteSpace(roleName))
                        continue;
                    var roles = context.Guild.Roles.Where(r => r.Name.ToLower().Contains(roleName.ToLower().Trim()))
                        .Where(r => Core.GetGuildConfig(context.Guild.Id).UserRoles.Any(rg => rg.Value.Contains(r.Id)));
                    if (!roles.Any())
                    {
                        messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"Could not find any user roles matching `{roleName}`").WithColor(new Color(0xFFFF00)).Build()).Result);
                    }
                    else
                    {
                        try
                        {
                            var role = roles.Any(r => r.Name.ToLower() == roleName.ToLower())
                                ? roles.First(r => r.Name.ToLower() == roleName.ToLower())
                                : roles.First();
                            if (context.Guild.GetUser(context.Author.Id).Roles.Any(r => r.Id == roles.First().Id))
                            {
                                messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"You already have that role!").WithColor(new Color(0xFFFF00)).Build()).Result);
                            }
                            else
                            {
                                await context.Guild.GetUser(context.Author.Id).AddRoleAsync(role);
                                messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"Assigned you `{role.Name}`").WithColor(new Color(0x9B00)).Build()).Result);
                            }
                        }
                        catch (Exception e)
                        {
                            await Core.Logger.LogErrorMessage(e, context);
                            messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"I may not have permissions to do that").WithColor(new Color(0xFFFF00)).Build()).Result);
                        }
                    }
                }

                await Task.Delay(15 * 1000);
                try
                {
                    messagesToDelete.ForEach(m => GenericBot.ClearedMessageIds.Add(m.Id));
                    messagesToDelete.Add(context.Message);
                    await (context.Channel as ITextChannel).DeleteMessagesAsync(messagesToDelete);
                }
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    try
                    {
                        foreach (var m in messagesToDelete)
                        {
                            await m.DeleteAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        await Core.Logger.LogErrorMessage(e, context);
                    }
                }
            };
            commands.Add(iam);

            Command iamnot = new Command("iamnot");
            iamnot.Description = "Leave a User Role";
            iamnot.Usage = "iamnot <role name>";
            iamnot.Aliases = new List<string> { "leave" };
            iamnot.ToExecute += async (context) =>
            {
                List<IMessage> messagesToDelete = new List<IMessage>();

                if (context.Parameters.IsEmpty())
                {
                    messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription("Please select a role to remove").WithColor(new Color(0xFFFF00)).Build()).Result);
                }

                foreach (var roleName in context.ParameterString.Trim(',', ' ').Split(','))
                {
                    if (string.IsNullOrWhiteSpace(roleName))
                        continue;
                    var roles = context.Guild.Roles.Where(r => r.Name.ToLower().Contains(roleName.ToLower().Trim()))
                        .Where(r => Core.GetGuildConfig(context.Guild.Id).UserRoles.Any(rg => rg.Value.Contains(r.Id)));
                    if (!roles.Any())
                    {
                        messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"Could not find any user roles matching `{roleName}`").WithColor(new Color(0xFFFF00)).Build()).Result);
                    }
                    else
                    {
                        try
                        {
                            var role = roles.Any(r => r.Name.ToLower() == roleName.ToLower())
                                ? roles.First(r => r.Name.ToLower() == roleName.ToLower())
                                : roles.First();
                            if (!context.Guild.GetUser(context.Author.Id).Roles.Any(r => r.Id == roles.First().Id))
                            {
                                messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"You don't have that role!").WithColor(new Color(0xFFFF00)).Build()).Result);
                            }
                            else
                            {
                                await context.Guild.GetUser(context.Author.Id).RemoveRoleAsync(role);
                                messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"Removed `{role.Name}`").WithColor(new Color(0x9B00)).Build()).Result);
                            }
                        }
                        catch (Exception e)
                        {
                            await Core.Logger.LogErrorMessage(e, context);
                            messagesToDelete.Add(context.Channel.SendMessageAsync("", embed: new EmbedBuilder().WithDescription($"I may not have permissions to do that").WithColor(new Color(0xFFFF00)).Build()).Result);
                        }
                    }
                }

                await Task.Delay(15 * 1000);
                try
                {
                    messagesToDelete.ForEach(m => GenericBot.ClearedMessageIds.Add(m.Id));
                    messagesToDelete.Add(context.Message);
                    await (context.Channel as ITextChannel).DeleteMessagesAsync(messagesToDelete);
                }
                catch (Exception ex)
                {
                    await Core.Logger.LogErrorMessage(ex, context);
                    try
                    {
                        foreach (var m in messagesToDelete)
                        {
                            await m.DeleteAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        await Core.Logger.LogErrorMessage(e, context);
                    }
                }
            };
            commands.Add(iamnot);

            Command getrole = new Command("getrole");
            getrole.Description = "Get the ID of a role";
            getrole.Usage = "getrole <role name>";
            getrole.RequiredPermission = Command.PermissionLevels.Moderator;
            getrole.ToExecute += async (context) =>
            {
                string message = $"Roles matching `{context.ParameterString}`:\n";
                foreach (var role in context.Guild.Roles.Where(r => Regex.IsMatch(r.Name, context.ParameterString, RegexOptions.IgnoreCase)).OrderBy(r => r.Name))
                {
                    message += $"{role.Name} (`{role.Id}`)\n";
                }

                foreach (var str in message.MessageSplit())
                {
                    await context.Message.ReplyAsync(str);
                }
            };
            commands.Add(getrole);

            Command membersOf = new Command("membersof");
            membersOf.Description = "List all members of a role";
            membersOf.Usage = "membersof <rolename>";
            membersOf.RequiredPermission = Command.PermissionLevels.Moderator;
            membersOf.ToExecute += async (context) =>
            {
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync($"You need to specify a role");
                    return;
                }
                string result = "";
                foreach (var role in context.Guild.Roles.OrderByDescending(r => r.Position).Where(r => new Regex(context.ParameterString, RegexOptions.IgnoreCase).IsMatch(r.Name) && r.Name != "@everyone"))
                {
                    result += $"\n**`{role.Name}` ({role.Members.Count()} Members)**\n";
                    foreach (var user in role.Members.OrderBy(u => u.Username))
                    {
                        if (!string.IsNullOrEmpty(user.Nickname)) result += $"{user.Nickname.Replace('`', '\'').Replace("_", "\\_")} ";
                        else result += $"{user.Username.Replace('`', '\'').Replace("_", "\\_")} ";
                        result += $"(`{user.ToString().Replace('`', '\'')}`)\n";
                    }
                }

                foreach (var str in result.MessageSplit('\n'))
                {
                    await context.Message.ReplyAsync(str);
                }

            };
            commands.Add(membersOf);

            Command createRole = new Command("createRole");
            createRole.Description = "Create a new role with default permissions";
            createRole.Usage = "createRole <name>";
            createRole.RequiredPermission = Command.PermissionLevels.Admin;
            createRole.ToExecute += async (context) =>
            {
                RestRole role;

                role = context.Guild.CreateRoleAsync(context.ParameterString, GuildPermissions.None).Result;
                await context.Message.ReplyAsync($"Created new role `{role.Name}` with ID `{role.Id}`");
            };
            commands.Add(createRole);

            Command createUserRole = new Command("createUserRole");
            createUserRole.Description = "Create a new role with default permissions and add it to the public role list";
            createUserRole.Usage = "createUserRole <name>";
            createUserRole.RequiredPermission = Command.PermissionLevels.Admin;
            createUserRole.ToExecute += async (context) =>
            {
                RestRole role;

                role = context.Guild.CreateRoleAsync(context.ParameterString, GuildPermissions.None).Result;
                var gc = Core.GetGuildConfig(context.Guild.Id);
                if (gc.UserRoles.ContainsKey(""))
                    gc.UserRoles[""].Add(role.Id);
                else
                    gc.UserRoles.Add("", new List<ulong> { role.Id });
                Core.SaveGuildConfig(gc);
                await context.Message.ReplyAsync($"Created new role `{role.Name}` with ID `{role.Id}` and added it to the user roles");
            };
            commands.Add(createUserRole);


            Command verify = new Command("verify");
            verify.RequiredPermission = Command.PermissionLevels.User;
            verify.ToExecute += async (context) =>
            {
                List<SocketUser> users = new List<SocketUser>();
                var guildConfig = Core.GetGuildConfig(context.Guild.Id);

                if (context.Parameters.IsEmpty())
                {
                    if ((context.Author as SocketGuildUser).Roles.Any(r => r.Id == guildConfig.VerifiedRole))
                    {
                        await context.Message.ReplyAsync("You're already verified");
                        return;
                    }
                    users.Add(context.Author);
                }
                else
                {
                    foreach (var user in context.Message.GetMentionedUsers())
                    {
                        if ((user as SocketGuildUser).Roles.Any(r => r.Id == guildConfig.VerifiedRole))
                        {
                            await context.Message.ReplyAsync($"{user.Username} is already verified");
                        }
                        else
                        {
                            users.Add(user);
                        }
                    }
                }


                if (guildConfig.VerifiedRole == 0)
                {
                    await context.Message.ReplyAsync($"Verification is disabled on this server");
                    return;
                }

                if ((string.IsNullOrEmpty(guildConfig.VerifiedMessage) || guildConfig.VerifiedMessage.Split().Length < 32 || !context.Guild.Roles.Any(r => r.Id == guildConfig.VerifiedRole)))
                {
                    await context.Message.ReplyAsync(
                        $"It looks like verifiction is configured improperly (either the message is too short or the role does not exist.) Please contact your server administrator to resolve it.");
                    return;
                }

                List<SocketUser> failed = new List<SocketUser>();
                List<SocketUser> success = new List<SocketUser>();
                foreach (var user in users)
                {
                    string message = $"Hey {user.Username}! To get verified on **{context.Guild.Name}** reply to this message with the hidden code in the message below\n\n"
                                     + Core.GetGuildConfig(context.Guild.Id).VerifiedMessage;

                    string verificationMessage =
                        VerificationEngine.InsertCodeInMessage(message, VerificationEngine.GetVerificationCode(user.Id, context.Guild.Id));

                    try
                    {
                        await user.GetOrCreateDMChannelAsync().Result.SendMessageAsync(verificationMessage);
                        success.Add(user);
                    }
                    catch (Exception ex)
                    {
                        await Core.Logger.LogErrorMessage(ex, context);
                        failed.Add(user);
                    }
                }

                string reply = "";
                if (success.Any())
                {
                    reply += $"I've sent {success.Select(u => u.Username).ToList().SumAnd()} instructions!";
                }
                if (failed.Any())
                {
                    reply += $" {failed.Select(u => u.Username).ToList().SumAnd()} could not be messaged.";
                }
                await context.Message.ReplyAsync(reply);
            };
            commands.Add(verify);

            return commands;
        }
    }
}
