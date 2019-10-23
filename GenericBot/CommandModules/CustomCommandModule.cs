using GenericBot.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenericBot.CommandModules
{
    class CustomCommandModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command command = new Command("command");
            command.Aliases = new List<string> { "commands" };
            command.RequiredPermission = Command.PermissionLevels.Admin;
            command.Description = "Modify custom commands for a server";
            command.Usage = "command <list|add|remove|toggleDelete> [name] [response]";
            command.ToExecute += async (context) =>
            {
                if (context.Parameters.IsEmpty())
                {
                    await context.Message.ReplyAsync("You have to input an option");
                    return;
                }
                if (context.Parameters[0].Equals("list"))
                {
                    if (!Core.GetCustomCommands(context.Guild.Id).Any())
                    {
                        await context.Message.ReplyAsync("This server has no custom commands");
                        return;
                    }
                    string rawResponse = "";
                    foreach (var cmd in Core.GetCustomCommands(context.Guild.Id))
                    {
                        rawResponse += $"`{cmd.Name}`: {cmd.Response.SafeSubstring(100)}\nDelete: `{cmd.Delete}`\n\n";
                    }
                    foreach (var resp in rawResponse.MessageSplit('\n'))
                    {
                        await context.Message.ReplyAsync(resp);
                    }
                }
                else if (context.Parameters[0].Equals("add"))
                {
                    if (Core.Commands.Any(c =>
                        c.Name.Equals(context.Parameters[1]) || c.Aliases.Any(a => a.Equals(context.Parameters[1]))) ||
                        Core.GetCustomCommands(context.Guild.Id).Any(c => c.Name.Equals(context.Parameters[1])))
                    {
                        await context.Message.ReplyAsync($"That's already a command, you can't add it.");
                        return;
                    }
                    string cName = context.Parameters[1];
                    string pref = Core.GetPrefix(context);

                    string message = context.Message.Content;
                    message = message.Remove(0, pref.Length).TrimStart(' ').Remove(0, "command".Length).TrimStart('s').TrimStart(' ').Remove(0, "add".Length).TrimStart(' ').Remove(0, cName.Length).Trim(' ');

                    CustomCommand newCommand = Core.SaveCustomCommand(new CustomCommand(cName.ToLower(), message), context.Guild.Id);
                    await context.Message.ReplyAsync($"New command created! \n```\n{JsonConvert.SerializeObject(newCommand, Formatting.Indented)}\n```");
                }
                else if (context.Parameters[0].Equals("remove") || context.Parameters[0].Equals("delete"))
                {
                    CustomCommand custom = new CustomCommand();
                    if (Core.GetCustomCommands(context.Guild.Id).HasElement(c => c.Name == context.Parameters[1], out custom))
                    {
                        Core.DeleteCustomCommand(custom.Name, context.Guild.Id);
                        await context.Message.ReplyAsync($"The command `{custom.Name}` has been deleted");
                    }
                    else
                    {
                        await context.Message.ReplyAsync($"No command matching `{context.Parameters[1]}` exists");
                    }
                }
                else if (context.Parameters[0].ToLower().Equals("toggledelete"))
                {
                    CustomCommand custom = new CustomCommand();
                    if (Core.GetCustomCommands(context.Guild.Id)
                        .HasElement(c => c.Name == context.Parameters[1], out custom))
                    {
                        custom.Delete = !custom.Delete;
                        Core.SaveCustomCommand(custom, context.Guild.Id);
                        if (!custom.Delete)
                        {
                            await context.Message.ReplyAsync($"The command `{custom.Name}` will **not** be deleted");
                        }
                        else
                        {
                            await context.Message.ReplyAsync($"The command `{custom.Name}` **will** be deleted");
                        }
                    }
                    else
                    {
                        await context.Message.ReplyAsync($"No command matching `{context.Parameters[1]}` exists");
                    }
                }
            };
            commands.Add(command);

            return commands;
        }
    }
}
