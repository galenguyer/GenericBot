﻿using Discord;
using Discord.WebSocket;
using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GenericBot.CommandModules
{
    public class InfoModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command ping = new Command("ping");
            ping.WorksInDms = true;
            ping.Description = "Make sure the bot is up";
            ping.ToExecute += async (context) =>
            {
                await context.Message.ReplyAsync("Pong!");
            };
            commands.Add(ping);

            Command info = new Command("info");
            info.WorksInDms = true;
            info.Description = "Provides some general info about the bot";
            info.ToExecute += async (context) =>
            {
                string prefix = Core.GetPrefix(context);

                string config = info.GetPermissions(context) >= Command.PermissionLevels.Admin ? $" Admins can also run `{prefix}confighelp` to see everything you can set up" : "";

                var builder = new EmbedBuilder()
                    .WithTitle("GenericBot: An All-Purpose Almost-Decent Bot")
                    .WithDescription("GenericBot aims to provide an almost full featured moderation and fun box experience in one convenient package")
                    .WithUrl("https://github.com/MasterChief-John-117/GenericBot")
                    .WithColor(new Color(0xFF))
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"Made by {Core.DiscordClient.GetUser(169918990313848832).ToString()} | Hosted by {Core.DiscordClient.GetUser(152905790959779840).ToString()}")
                            .WithIconUrl(Core.DiscordClient.GetUser(169918990313848832).GetAvatarUrl());
                    })
                    .WithThumbnailUrl(Core.DiscordClient.CurrentUser.GetAvatarUrl().Replace("size=128", "size=2048"))
                    .AddField($"Links", $"GenericBot is currently in a closed state, however if you wish to use it in your own server please get in contact with the developer, whose username is in the footer\nAlso, the source code is public on [github](https://github.com/MasterChief-John-117/GenericBot). You can also open bug reports on GitHub ")
                    .AddField($"Getting Started", $"See everything you can make me do with `{prefix}help`. {config}")
                    .AddField($"Self Assignable Roles", $"One of the most common public features GenericBot is used for is roles a user can assign to themself. To see all the avaible roles, do `{prefix}userroles`. You can join a role with `{prefix}iam [rolename]` or leave a role with `{prefix}iamnot [rolename]`.")
                    .AddField($"Moderation", $"GenericBot provides a wide range of tools for moderators to track users and infractions. It keeps track of all of a user's usernames, nicknames, and logged infractions, including kicks and timed or permanent bans. Users can be searched for either by ID, or by username or nickname, whether it be current or an old name. (All data is stored in an encrypted database, and data from one server is completely inaccessible by another server)")
                    .AddField($"Fun!", $"In addition to being a highly effective moderator toolkit, GenericBot has some fun commands, such as `{prefix}dog`, `{prefix}cat`, or `{prefix}jeff`. You can also create your own custom commands for rapid-fire memery or whatever else tickles your fancy");
                var embed = builder.Build();

                await context.Channel.SendMessageAsync("", embed: embed);
            };
            commands.Add(info);

            Command configinfo = new Command("configinfo");
            configinfo.WorksInDms = true;
            configinfo.RequiredPermission = Command.PermissionLevels.Admin;
            configinfo.Description = "Show all the options to configure with syntax for each";
            configinfo.ToExecute += async (context) =>
            {
                string prefix = Core.GetPrefix(context);

                var builder = new EmbedBuilder()
                    .WithTitle("GenericBot: Config Information")
                    .WithDescription($"The `{prefix}config` command is huge and confusing. This aims to make it a bit simpler (For more general assistance, try `{prefix}info`)")
                    .WithUrl("https://github.com/MasterChief-John-117/GenericBot")
                    .WithColor(new Color(0xEF4347))
                    .WithFooter(footer =>
                    {
                        footer
                            .WithText($"If you have questions or notice any errors, please contact {Core.DiscordClient.GetUser(169918990313848832).ToString()}");
                    })
                    .WithThumbnailUrl("https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Gear_1.svg/1000px-Gear_1.svg.png")
                    .AddField("AdminRoles", $"Add or remove Admin Roles by ID\nSyntax: `{prefix}config adminroles <add/remove> [roleId]`")
                    .AddField("ModeratorRoles (ModRoles)", $"Add or remove Moderator Roles by ID\nSyntax: `{prefix}config modroles <add/remove> [roleId]`")
                    .AddField("UserRoles", $"Add or remove User-Assignable Roles by ID\nSyntax: `{prefix}config userroles <add/remove> [roleId]`")
                    .AddField("Prefix", $"Set the prefix to a given string. If [prefixString] is empty it gets set to the default of `{Core.GetGlobalPrefix()}`\nSyntax: `{prefix}config prefix [prefixString]`")
                    .AddField("Logging", $"Set the channel for logging by Id\nSyntax: `{prefix}config logging channelId [channelId]`\n\nToggle ignoring channels for logging by Id. Lists all ignored channels if channelId is empty\nSyntax`{prefix}config logging ignoreChannel [channelId]`")
                    .AddField("MutedRoleId", $"Set the role assigned by the `{prefix}mute` command. Set [roleId] to `0` to disable muting\nSyntax: `{prefix}config mutedRoleId [roleId]`")
                    .AddField("Verification", $"Get or Set the RoleId assigned for verification. Leave [roleId] empty to get the current role. Use `0` for the [roleId] to disable verification\nSyntax: `{prefix}config verification roleId [roleId]`\n\nGet or set the message sent for verification. Leave [message] empty to get the current message\nSyntax: `{prefix}config verification message [message]`")
                    .AddField("AutoRole", $"Add or remove a role to be automatically granted by Id\nSyntax: `{prefix}config autorole <add/remove> [roleId]`");
                var embed = builder.Build();

                await context.Channel.SendMessageAsync("", embed: embed);
            };

            commands.Add(configinfo);

            Command help = new Command("help");
            help.Description = "The help command, duh";
            help.RequiredPermission = Command.PermissionLevels.User;
            help.Aliases = new List<string> { "halp" };
            help.WorksInDms = true;
            help.ToExecute += async (context) =>
            {
                string commandList = "";
                string[] levels = { "User Commands", "Moderator Commands", "Admin Commands", "Guild Owner Commands", "Global Admin Commands", "Bot Owner Commands" };

                if (string.IsNullOrEmpty(context.ParameterString))
                {
                    commandList += "Bot Commands:\n";
                    for (int i = 0; i <= GetIntFromPerm(help.GetPermissions(context)); i++)
                    {
                        commandList += $"\n{levels[i]}: ";
                        commandList += Core.Commands
                            .Where(c => c.RequiredPermission == GetPermFromInt(i))
                            .OrderBy(c => c.RequiredPermission)
                            .ThenBy(c => c.Name)
                            .Select(c => $"`{c.Name}`")
                            .ToList().SumAnd();
                    }
                    if (!(context.Channel is SocketDMChannel))
                    {
                        commandList += "\n\nCustom Commands:\n";
                        commandList += Core.GetCustomCommands(context.Guild.Id).Result
                            .OrderBy(c => c.Name)
                            .Select(c => $"`{c.Name}`")
                            .ToList().SumAnd();
                    }
                }
                else
                {
                    string param = context.ParameterString.ToLower();
                    int cmdCount = 0;
                    cmdCount += Core.Commands
                        .Where(c => c.RequiredPermission <= help.GetPermissions(context))
                        .Count(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)));
                    if (!(context.Channel is SocketDMChannel))
                    {
                        cmdCount += Core.GetCustomCommands(context.Guild.Id).Result
                            .Count(c => c.Name.ToLower().Contains(param));
                    }

                    if (cmdCount > 10)
                    {
                        commandList += "Bot Commands:\n";
                        for (int i = 0; i <= GetIntFromPerm(help.GetPermissions(context)); i++)
                        {
                            commandList += $"\n{levels[i]}: ";
                            commandList += Core.Commands
                                .Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                                .Where(c => c.RequiredPermission == GetPermFromInt(i))
                                .OrderBy(c => c.RequiredPermission)
                                .ThenBy(c => c.Name)
                                .Select(c => $"`{c.Name}`")
                                .ToList().SumAnd();
                        }
                        if (!(context.Channel is SocketDMChannel))
                        {
                            commandList += "\n\nCustom Commands:\n";
                            Core.GetCustomCommands(context.Guild.Id).Result
                                .Where(c => c.Name.ToLower().Contains(param))
                                .OrderBy(c => c.Name)
                                .Select(c => $"`{c.Name}`")
                                .ToList().SumAnd();
                        }
                    }
                    else
                    {
                        var cmds = Core.Commands
                            .Where(c => c.RequiredPermission <= help.GetPermissions(context))
                            .Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                            .OrderBy(c => c.RequiredPermission)
                            .ThenBy(c => c.Name);

                        foreach (var cmd in cmds)
                        {
                            commandList += $"`{cmd.Name}`: {cmd.Description} (`{cmd.Usage}`)\n";
                            if (cmd.Aliases.Any())
                            {
                                commandList += $"\tAliases: {cmd.Aliases.Select(c => $"`{c}`").ToList().SumAnd()}\n";
                            }
                        }

                        if (!(context.Channel is SocketDMChannel))
                        {
                            var ccmds = Core.GetCustomCommands(context.Guild.Id).Result
                                .Where(c => c.Name.ToLower().Contains(param))
                                .OrderBy(c => c.Name);
                            foreach (var cmd in ccmds)
                            {
                                commandList += $"`{cmd.Name}`: Custom Command\n";
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(commandList))
                {
                    await context.Message.ReplyAsync($"Could not find any commands matching `{context.ParameterString}`");
                    return;
                }

                foreach (var str in commandList.SplitSafe())
                {
                    await context.Message.ReplyAsync(str);
                }
            };
            commands.Add(help);

            Command global = new Command("global");
            global.Description = "Get the global information for the bot";
            global.WorksInDms = true;
            global.RequiredPermission = Command.PermissionLevels.GlobalAdmin;
            global.ToExecute += async (context) =>
            {
                string stats = $"**Global Stats:** `{DateTime.Now}`\n" +
                               $"SessionID: `{Core.Logger.SessionId}`\n" +
                               //$"Build Number: `{GenericBot.BuildId}`\n\n" +
                               $"Servers: `{Core.DiscordClient.Guilds.Count}`\n" +
                               $"Users: `{Core.DiscordClient.Guilds.Sum(g => g.Users.Count)}`\n" +
                               $"Shards: `{Core.DiscordClient.Shards.Count}`\n" +
                               //$"CPU Usage: `{Math.Round(GenericBot.CpuCounter.NextValue())}`% \n" +
                               $"Memory: `{Math.Round(GC.GetTotalMemory(true) / (1024.0 * 1024.0), 2)} MB`\n" +
                               $"Uptime: `{(DateTime.Now - Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")}`\n\n";

                foreach (var shard in Core.DiscordClient.Shards)
                {
                    stats += $"Shard `{shard.ShardId}`: `{shard.Guilds.Count}` Guilds (`{shard.Guilds.Sum(g => g.Users.Count)}` Users)\n";
                }

                await context.Message.ReplyAsync(stats);
            };
            commands.Add(global);

            return commands;
        }

        private Command.PermissionLevels GetPermFromInt(int inp)
        {
            switch (inp)
            {
                case 0:
                    return Command.PermissionLevels.User;
                case 1:
                    return Command.PermissionLevels.Moderator;
                case 2:
                    return Command.PermissionLevels.Admin;
                case 3:
                    return Command.PermissionLevels.GuildOwner;
                case 4:
                    return Command.PermissionLevels.GlobalAdmin;
                case 5:
                    return Command.PermissionLevels.BotOwner;
                default:
                    return Command.PermissionLevels.User;
            }
        }
        private int GetIntFromPerm(Command.PermissionLevels inp)
        {
            switch (inp)
            {
                case Command.PermissionLevels.User:
                    return 0;
                case Command.PermissionLevels.Moderator:
                    return 1;
                case Command.PermissionLevels.Admin:
                    return 2;
                case Command.PermissionLevels.GuildOwner:
                    return 3;
                case Command.PermissionLevels.GlobalAdmin:
                    return 4;
                case Command.PermissionLevels.BotOwner:
                    return 5;
                default:
                    return 0;
            }
        }
    }
}
