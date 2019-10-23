using Discord.WebSocket;
using GenericBot.Database;
using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;

namespace GenericBot.CommandModules
{
    class LookupModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command find = new Command("find");
            find.Description = "Get information about a user currently on the server from a ID or Mention";
            find.Usage = "whois @user";
            find.RequiredPermission = Command.PermissionLevels.Moderator;
            find.Aliases = new List<string> { "whois" };
            find.ToExecute += async (context) =>
            {
                List<DatabaseUser> foundUsers = new List<DatabaseUser>();

                if (context.Message.MentionedUsers.Any())
                {
                    foundUsers.AddRange(context.Message.MentionedUsers.Select(m => 
                    Core.GetUserFromGuild(m.Id, context.Guild.Id)));
                }
                else if ((context.ParameterString.Length > 16 && context.ParameterString.Length < 19) && ulong.TryParse(context.ParameterString, out ulong id))
                {
                    foundUsers.Add(Core.GetUserFromGuild(id, context.Guild.Id));
                }
                else if (context.ParameterString.Length < 3)
                {
                    await context.Message.ReplyAsync($"I can't search for that, it's dangerously short and risks a crash.");
                    return;
                }

                else
                {
                    foreach (var user in Core.GetAllUsers(context.Guild.Id))
                    {
                        try
                        {
                            if (!user.Nicknames.IsEmpty())
                            {
                                if (user.Nicknames.Any(n => n.ToLower().Contains(context.ParameterString.ToLower())))
                                {
                                    foundUsers.Add(user);
                                }
                            }

                            if (!user.Usernames.IsEmpty())
                            {
                                if (user.Usernames.Any(n => n.ToLower().Contains(context.ParameterString.ToLower())))
                                {
                                    foundUsers.Add(user);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            await Core.Logger.LogErrorMessage(ex, context);
                            if (new Command("test").GetPermissions(context) >= Command.PermissionLevels.GlobalAdmin)
                                await context.Message.ReplyAsync($"```\n{ex.Message}\n{ex.StackTrace}\n{user.Id} : {user.Usernames.Count} | {user.Nicknames.Count}\n```");
                        }
                    }
                    foundUsers = foundUsers.Distinct().ToList();
                }

                if (foundUsers.Count > 5)
                {
                    string info =
                        $"Found `{foundUsers.Count}` users. Their first stored usernames are:\n{foundUsers.Select(u => $"{u.Usernames.First().Escape()} (`{u.Id}`)").ToList().SumAnd()}" +
                        $"\nTry using more precise search parameters";

                    foreach (var str in info.MessageSplit(','))
                    {
                        await context.Message.ReplyAsync(str.TrimStart(','));
                    }

                    return;
                }
                else if (foundUsers.Count == 0)
                {
                    await context.Message.ReplyAsync($"No users found");
                }

                foreach (var dbUser in foundUsers)
                {
                    string nicks = "", usernames = "", warnings = "";
                    if (dbUser.Usernames != null && !dbUser.Usernames.IsEmpty())
                    {
                        usernames = dbUser.Usernames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                    }

                    if (dbUser.Nicknames != null && !dbUser.Nicknames.IsEmpty())
                    {
                        nicks = dbUser.Nicknames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                    }

                    if (dbUser.Warnings != null && !dbUser.Warnings.IsEmpty())
                    {
                        warnings = string.Join("\n", dbUser.Warnings);
                    }

                    // Fetch the Discord user from the cache or the API
                    // This works even if the user has left the server, and allows us to extract *some* information
                    IUser user = Core.DiscordClient.GetUser(dbUser.Id);

                    // Fetch the per-server user data, this returns null if the user has left the server
                    SocketGuildUser guildUser = context.Message.GetGuild().GetUser(dbUser.Id);

                    // Build the embed!
                    EmbedBuilder eb = new EmbedBuilder();
                    if (user != null) eb.WithAuthor(user);
                    else
                    {
                        // User has deleted their account, fall back and show last known username
                        var usernameString = dbUser.Usernames.LastOrDefault() ?? "(unknown)";
                        eb.WithAuthor($"{usernameString} (last known)");
                    }

                    // We put short-form data in the description field (appears at the top of the card),
                    // separated by a newline
                    var description = $"**ID:** {dbUser.Id}\n**Mention:** <@!{dbUser.Id}>\n";

                    if (user == null) description += "**Status:** User deleted\n";
                    else if (guildUser == null) description += "**Status:** Not on server\n";
                    else if (guildUser.Status == UserStatus.Idle) description += "**Status:** Idle\n";
                    else if (guildUser.Status == UserStatus.Invisible) description += "**Status:** Invisible\n";
                    else if (guildUser.Status == UserStatus.Offline) description += "**Status:** Offline\n";
                    else if (guildUser.Status == UserStatus.Online) description += "**Status:** Online\n";
                    else if (guildUser.Status == UserStatus.AFK) description += "**Status:** AFK\n";
                    else if (guildUser.Status == UserStatus.DoNotDisturb) description += "**Status:** Do not Disturb\n";

                    var createdDate = SnowflakeUtils.FromSnowflake(dbUser.Id).LocalDateTime;
                    description += $"**Created at:** {createdDate:yyyy-MM-dd HH\\:mm\\:ss zzzz} GMT (about {(DateTime.UtcNow - createdDate).Days} days ago)\n";
                    
                    if (guildUser != null)
                    {
                        // Only relevant if the user is on-server
                        if (guildUser.JoinedAt.HasValue)
                            description += $"**Joined at:** {guildUser.JoinedAt.Value.LocalDateTime:yyyy-MM-dd HH\\:mm\\:ss zzzz} GMT (about {(DateTime.UtcNow - guildUser.JoinedAt.Value).Days} days ago)\n ";
                    }
                    
                    if ((dbUser.Usernames?.Count ?? 0) > 0)
                        eb.AddField($"Past usernames ({dbUser.Usernames.Count})", usernames.Truncate(1024, $"\u2026 (`{Core.GetPrefix(context)}names {dbUser.Id}`)"));
                    
                    if ((dbUser.Nicknames?.Count ?? 0) > 0)
                        eb.AddField($"Past nicknames ({dbUser.Nicknames.Count})", nicks.Truncate(1024, $"\u2026 (`{Core.GetPrefix(context)}nicks {dbUser.Id}`)"));
                    
                    if ((dbUser.Warnings?.Count ?? 0) > 0)
                        eb.AddField($"Warnings ({dbUser.Warnings.Count})", warnings.Truncate(1024, $"\u2026 (`{Core.GetPrefix(context)}warns {dbUser.Id}`)"));
                    
                    eb.WithDescription(description);

                    await context.Channel.SendMessageAsync(embed: eb.Build());
                }
            };
            commands.Add(find);
            
            Command names = new Command("names");
            names.RequiredPermission = Command.PermissionLevels.Moderator;
            names.Aliases = new List<string>() {"usernames"};
            names.Usage = "name <id>";
            names.Description = "Shows the full list of logged usernames for a given user by their ID";
            names.ToExecute += async (context) =>
            {
                if (!ulong.TryParse(context.ParameterString, out var id))
                {
                    await context.Message.ReplyAsync($"You must pass a plain user ID.");
                    return;
                }

                var user = Core.GetUserFromGuild(id, context.Guild.Id);
                if (user == null)
                {
                    await context.Message.ReplyAsync($"User not found in database for this guild.");
                    return;
                }
                
                var usernames = user.Usernames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                foreach (var s in $"<@!{user.Id}> has had the following nicknames: {usernames}".MessageSplit(',')) 
                    await context.Message.ReplyAsync(s);
            };
            commands.Add(names);
            
            Command nicksCmd = new Command("nicks");
            nicksCmd.RequiredPermission = Command.PermissionLevels.Moderator;
            nicksCmd.Aliases = new List<string>() {"nicknames"};
            nicksCmd.Usage = "nicknames <id>";
            nicksCmd.Description = "Shows the full list of logged nicknames for a given user by their ID";
            nicksCmd.ToExecute += async (context) =>
            {
                if (!ulong.TryParse(context.ParameterString, out var id))
                {
                    await context.Message.ReplyAsync($"You must pass a plain user ID.");
                    return;
                }

                var user = Core.GetUserFromGuild(id, context.Guild.Id);
                if (user == null)
                {
                    await context.Message.ReplyAsync($"User not found in database for this guild.");
                    return;
                }
                
                var nicknames = user.Nicknames.Distinct().Select(n => $"`{n.Replace("`", "'")}`").ToList().SumAnd();
                foreach (var s in $"<@!{user.Id}> has had the following nicknames: {nicknames}".MessageSplit(',')) 
                    await context.Message.ReplyAsync(s);
            };
            commands.Add(nicksCmd);

            Command updateDb = new Command("updatedb");
            updateDb.RequiredPermission = Command.PermissionLevels.GlobalAdmin;
            updateDb.ToExecute += async (context) =>
            {
                await context.Guild.DownloadUsersAsync();
                int i = 0;
                foreach(var user in context.Guild.Users)
                {
                    var dbUser = Core.GetUserFromGuild(user.Id, context.Guild.Id);
                    dbUser.AddNickname(user);
                    dbUser.AddUsername(user.Username);
                    Core.SaveUserToGuild(dbUser, context.Guild.Id);
                    i++;
                }
                await context.Message.ReplyAsync($"Updated `{i}` users.");
            };
            commands.Add(updateDb);

            return commands;
        }
    }
}
