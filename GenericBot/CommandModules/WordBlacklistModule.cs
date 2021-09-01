using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Text;

//module created by Metatheria#4741 with help from chef

namespace GenericBot.CommandModules 
{
    class WordBlacklistModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command wordBlacklist = new Command("wordblacklist");
            wordBlacklist.SendTyping = false;
            wordBlacklist.Description = "Add or remove words from the blacklist, and enable/disable auto-deletion of messages with blacklisted words";
            wordBlacklist.Usage = $"{wordBlacklist.Name} <add> <words> |  <remove> <word ids> | <view|on|off>";
	    wordBlacklist.RequiredPermission = Command.PermissionLevels.Moderator;
	    wordBlacklist.ToExecute += async (context) =>
            {
		bool wrongCommand = false;
	    	var gc = Core.GetGuildConfig(context.Guild.Id);
		if(context.Parameters.IsEmpty())
		{
			wrongCommand = true;	
		}   
		else
		{
			switch (context.Parameters[0])
			{
				case "add":
					context.Parameters.RemoveAt(0);
					if(context.Parameters.IsEmpty())
						wrongCommand = true;
					else
					{
						foreach(string word in context.Parameters)
						{
							var w = Core.AddWordToBlacklist(word, context.Guild.Id);
							await context.Message.ReplyAsync($"Added {w.ToString()} to word blacklist");
						}
					}
					break;
				case "remove":
					context.Parameters.RemoveAt(0);
					if(context.Parameters.IsEmpty())
						wrongCommand = true;
					else
					{
						foreach(string id in context.Parameters)
						{
							if(int.TryParse(id, out int wid))
							{
								if(Core.RemoveWordFromBlacklist(wid, context.Guild.Id))
									await context.Message.ReplyAsync($"Successfully removed word #{wid} from blacklist");
								else
									await context.Message.ReplyAsync($"{wid} is not a valid blacklisted word id");
							}
							else
							{
								await context.Message.ReplyAsync($"{id} is not a number");
							}
						}
					}
					break;
				case "view":
					var list = Core.GetWordBlacklist(context.Guild.Id);
					string reply = "";
					foreach(BlacklistedWord word in list)
					{
						reply += $"{word.Id} : {word.Word}\n";
					}
					await context.Message.ReplyAsync(reply.Trim());
					break;
				case "on":
					gc.WordBlacklistEnabled = true;
					await context.Message.ReplyAsync("Enabled word blacklist");
					break;
				case "off":
					gc.WordBlacklistEnabled = false;
					await context.Message.ReplyAsync("Disabled word blacklist");
					break;
				default:
					wrongCommand = true;
					break;
			}
		 }
	       	if(wrongCommand)
			await context.Message.ReplyAsync("Usage : " + wordBlacklist.Usage);
	    };
            commands.Add(wordBlacklist);
            return commands;
        }
    }
}
