using System.Collections.Generic;
using System.Linq;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    public class HelpModule
    {
        public List<Command> GetHelpCommands()
        {
            List<Command> helpCommands = new List<Command>();

            Command help = new Command("help");
            help.Description = "The help command, duh";
            help.RequiredPermission = Command.PermissionLevels.User;
            help.Aliases = new List<string> { "halp" };
            help.ToExecute += async (client, msg, paramList) =>
            {
                var guildConfig = GenericBot.GuildConfigs[msg.GetGuild().Id];
                string commands = "";
                var guildCustomCommands = GenericBot.GuildConfigs[msg.GetGuild().Id].CustomCommands;
                string[] levels = { "User Commands", "Moderator Commands", "Admin Commands", "Guild Owner Commands", "Global Admin Commands", "Bot Owner Commands" };

                if (paramList.Empty())
                {
                    commands += "Bot Commands:\n";
                    for (int i = 0; i <= GetIntFromPerm(help.GetPermissions(msg.Author, msg.GetGuild().Id)); i++)
                    {
                        commands += $"\n{levels[i]}: ";
                        commands += GenericBot.Commands
                            .Where(c => c.RequiredPermission == GetPermFromInt(i))
                            .Where(c => c.Name == "g" ? (guildConfig.Giveaway != null && guildConfig.Giveaway.Open) : true)
                            .OrderBy(c => c.RequiredPermission)
                            .ThenBy(c => c.Name)
                            .Select(c => $"`{c.Name}`")
                            .ToList().SumAnd();
                    }

                    if (guildCustomCommands.Count > 0)
                    {
                        commands += "\nCustom Commands:\n";
                        commands += guildCustomCommands
                            .Select(c => $"`{c.Name}`")
                            .OrderBy(c => c)
                            .ToList().SumAnd();
                    }
                }
                else
                {
                    string param = paramList.reJoin().ToLower();
                    int cmdCount = 0;
                    cmdCount += GenericBot.Commands
                        .Where(c => c.RequiredPermission <= help.GetPermissions(msg.Author, msg.GetGuild().Id))
                        .Count(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)));
                    cmdCount += guildCustomCommands.Count(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)));

                    if (cmdCount > 10)
                    {
                        commands += "Bot Commands:\n";
                        for (int i = 0; i <= GetIntFromPerm(help.GetPermissions(msg.Author, msg.GetGuild().Id)); i++)
                        {
                            commands += $"\n{levels[i]}: ";
                            commands += GenericBot.Commands
                                .Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                                .Where(c => c.RequiredPermission == GetPermFromInt(i))
                                .Where(c => c.Name == "g" ? (guildConfig.Giveaway != null && guildConfig.Giveaway.Open) : true)
                                .OrderBy(c => c.RequiredPermission)
                                .ThenBy(c => c.Name)
                                .Select(c => $"`{c.Name}`")
                                .ToList().SumAnd();
                        }
                        if (guildCustomCommands.Count(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param))) > 0)
                        {
                            commands += "\nCustom Commands:\n";
                            commands += guildCustomCommands
                                .Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                                .Select(c => $"`{c.Name}`")
                                .OrderBy(c => c)
                                .ToList().SumAnd();
                        }
                    }
                    else
                    {
                        var cmds = GenericBot.Commands
                            .Where(c => c.RequiredPermission <= help.GetPermissions(msg.Author, msg.GetGuild().Id))
                            .Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                            .Where(c => c.Name == "g" ? (guildConfig.Giveaway != null && guildConfig.Giveaway.Open) : true)
                            .OrderBy(c => c.RequiredPermission)
                            .ThenBy(c => c.Name);
                        var ccmds = guildCustomCommands.Where(c => c.Name.ToLower().Contains(param) || c.Aliases.Any(a => a.ToLower().Contains(param)))
                            .OrderBy(c => c.Name);

                        foreach (var cmd in cmds)
                        {
                            commands += $"`{cmd.Name}`: {cmd.Description} (`{cmd.Usage}`)\n";
                            if (cmd.Aliases.Any())
                            {
                                commands += $"\tAliases: {cmd.Aliases.Select(c => $"`{c}`").ToList().SumAnd()}\n";
                            }
                        }
                        foreach (var cmd in ccmds)
                        {
                            commands += $"`{cmd.Name}`: Custom Command\n";
                            if (cmd.Aliases.Any())
                            {
                                commands += $"\tAliases: {cmd.Aliases.Select(c => $"`{c}`").ToList().SumAnd()}\n";
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(commands))
                {
                    await msg.ReplyAsync($"Could not find any commands matching `{paramList.reJoin()}`");
                    return;
                }

                foreach (var str in commands.SplitSafe())
                {
                    await msg.Channel.SendMessageAsync(str);
                }
            };
            helpCommands.Add(help);

            return helpCommands;
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
