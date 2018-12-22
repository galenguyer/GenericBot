using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GenericBot.Entities;
using Newtonsoft.Json;

namespace GenericBot
{
    public static class MessageEventHandler
    {
        public static async Task MessageRecieved(SocketMessage parameterMessage, bool edited = false)
        {
            // Don't handle the command if it is a system message
            var message = parameterMessage;

            GenericBot.MessageCounter++;
            GenericBot.Latency = (int)Math.Round((DateTimeOffset.UtcNow - parameterMessage.Timestamp).TotalMilliseconds);

            if (GenericBot.GlobalConfiguration.BlacklistedIds.Contains(message.Author.Id))
            {
                return;
            }

            if (parameterMessage.Author.Id == GenericBot.DiscordClient.CurrentUser.Id)
                return;

            if (parameterMessage.Channel.GetType().FullName.ToLower().Contains("dmchannel"))
            {
                var msg = GenericBot.DiscordClient.GetApplicationInfoAsync().Result.Owner.GetOrCreateDMChannelAsync().Result
                    .SendMessageAsync($"```\nDM from: {message.Author}({message.Author.Id})\nContent: {message.Content}\n```").Result;
                if (parameterMessage.Content.Trim().Split().Length == 1)
                {
                    var guild = VerificationEngine.GetGuildFromCode(parameterMessage.Content, message.Author.Id);
                    if (guild == null)
                    {
                        await message.ReplyAsync("Invalid verification code");
                    }
                    else
                    {
                        await guild.GetUser(message.Author.Id)
                            .AddRoleAsync(guild.GetRole(GenericBot.GuildConfigs[guild.Id].VerifiedRole));
                        if (guild.TextChannels.HasElement(c => c.Id == (GenericBot.GuildConfigs[guild.Id].UserLogChannelId), out SocketTextChannel logChannel))
                        {
                            logChannel.SendMessageAsync($"`{DateTime.UtcNow.ToString(@"yyyy-MM-dd HH:mm tt")}`:  `{message.Author}` (`{message.Author.Id}`) just verified");
                        }
                        await message.ReplyAsync($"You've been verified on **{guild.Name}**!");
                        await msg.ModifyAsync(m =>
                            m.Content = $"```\nDM from: {message.Author}({message.Author.Id})\nContent: {message.Content.SafeSubstring(1900)}\nVerified on {guild.Name}\n```");
                    }
                }
            }

            var guildDb = new DBGuild(message.GetGuild().Id);
            if (guildDb.Users.Any(u => u.ID.Equals(message.Author.Id))) // if already exists
            {
                guildDb.Users.Find(u => u.ID.Equals(message.Author.Id)).AddUsername(message.Author.Username);
                guildDb.Users.Find(u => u.ID.Equals(message.Author.Id)).AddNickname(message.Author as SocketGuildUser);
            }
            else
            {
                guildDb.Users.Add(new DBUser(message.Author as SocketGuildUser));
            }
            guildDb.Save();

            if (!edited)
            {
                new GuildMessageStats(parameterMessage.GetGuild().Id).AddMessage(parameterMessage.Author.Id).Save();
            }

            if (parameterMessage.Author.Id != GenericBot.DiscordClient.CurrentUser.Id &&
                GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].FourChannelId == parameterMessage.Channel.Id)
            {

                await parameterMessage.DeleteAsync();
                await parameterMessage.ReplyAsync(
                    $"**[Anonymous]** {string.Format("{0:yyyy-MM-dd HH\\:mm\\:ss}", DateTimeOffset.UtcNow)}\n{parameterMessage.Content}");
            }

            System.Threading.Thread pointThread = new System.Threading.Thread(() =>
            {
                try
                {
                    lock (message.GetGuild().Id.ToString())
                    {
                        DBGuild db = new DBGuild(message.GetGuild().Id);
                        if (db.Users == null) return;
                        if (!edited)
                        {
                            db.GetUser(message.Author.Id).PointsCount += (decimal)(.01);
                            if (GenericBot.GuildConfigs[message.GetGuild().Id].Levels.Any(kvp => kvp.Key <= db.GetUser(message.Author.Id).PointsCount))
                            {
                                foreach (var level in GenericBot.GuildConfigs[message.GetGuild().Id].Levels
                                .Where(kvp => kvp.Key <= db.GetUser(message.Author.Id).PointsCount)
                                .Where(kvp => !(message.Author as SocketGuildUser).Roles.Any(r => r.Id.Equals(kvp.Value))))
                                {
                                    (message.Author as SocketGuildUser).AddRoleAsync(message.GetGuild().GetRole(level.Value));
                                }
                            }
                        }
                        var thanksRegex = new Regex(@"(\b)((thanks?)|(thx)|(ty))(\b)", RegexOptions.IgnoreCase);
                        if (thanksRegex.IsMatch(message.Content) && GenericBot.GuildConfigs[message.GetGuild().Id].PointsEnabled && message.MentionedUsers.Any())
                        {
                            if (new DBGuild(message.GetGuild().Id).GetUser(message.Author.Id).LastThanks.AddMinutes(1) < DateTimeOffset.UtcNow)
                            {
                                List<IUser> givenUsers = new List<IUser>();
                                foreach (var user in message.MentionedUsers)
                                {
                                    if (user.Id == message.Author.Id) continue;

                                    db.GetUser(user.Id).PointsCount++;
                                    givenUsers.Add(user);
                                }
                                if (givenUsers.Any())
                                {
                                    message.ReplyAsync($"{givenUsers.Select(us => $"**{(us as SocketGuildUser).GetDisplayName()}**").ToList().SumAnd()} received a {GenericBot.GuildConfigs[message.GetGuild().Id].PointsName} of thanks from **{(message.Author as SocketGuildUser).GetDisplayName()}**");
                                    db.GetUser(message.Author.Id).LastThanks = DateTimeOffset.UtcNow;
                                }
                                else message.ReplyAsync("You can't give yourself a point!");
                            }
                        }
                        db.Save();
                    }
                }
                catch (Exception ex)
                {
                    //GenericBot.Logger.LogErrorMessage($"{ex.Message}\nGuild:{message.GetGuild().Name} | {message.GetGuild().Id}\nChannel:{message.Channel.Name} | {message.Channel.Id}\nUser:{message.Author} | {message.Author.Id}\n{message.Content}");
                }

            });
            pointThread.IsBackground = true;
            pointThread.Start();


            GenericBot.QuickWatch.Restart();
            try
            {
                var commandInfo = CommandHandler.ParseMessage(parameterMessage);

                CustomCommand custom = new CustomCommand();

                if (parameterMessage.Channel is IDMChannel) goto DMChannel;

                if(parameterMessage.MentionedUsers.Contains(GenericBot.DiscordClient.CurrentUser) && parameterMessage.Content.ToLower().Contains("prefix"))
                {
                    string pref = GenericBot.GlobalConfiguration.DefaultPrefix;

                    if (!String.IsNullOrEmpty(GenericBot.GuildConfigs[(parameterMessage.Channel as SocketGuildChannel).Guild.Id].Prefix))
                        pref = GenericBot.GuildConfigs[(parameterMessage.Channel as SocketGuildChannel).Guild.Id].Prefix;

                    await parameterMessage.ReplyAsync($"Try `{pref}`!");
                }

                if (GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].CustomCommands
                        .HasElement(c => c.Name == commandInfo.Name, out custom) ||
                    GenericBot.GuildConfigs[parameterMessage.GetGuild().Id].CustomCommands
                        .HasElement(c => c.Aliases.Any(a => a.Equals(commandInfo.Name)), out custom))
                {
                    if (custom.Delete)
                    {
                        await parameterMessage.DeleteAsync();
                    }
                    new GuildMessageStats(parameterMessage.GetGuild().Id).AddCommand(parameterMessage.Author.Id, custom.Name).Save();
                    await parameterMessage.ReplyAsync(custom.Response);
                }

            DMChannel:
                GenericBot.LastCommand = commandInfo;
                commandInfo.Command.ExecuteCommand(GenericBot.DiscordClient, message, commandInfo.Parameters).FireAndForget();
                GenericBot.Logger.LogGenericMessage($"Guild: {parameterMessage.GetGuild().Name} ({parameterMessage.GetGuild().Id}) Channel: {parameterMessage.Channel.Name} ({parameterMessage.Channel.Id}) User: {parameterMessage.Author} ({parameterMessage.Author.Id}) Command: {commandInfo.Command.Name} Parameters {JsonConvert.SerializeObject(commandInfo.Parameters)}");
                new GuildMessageStats(parameterMessage.GetGuild().Id).AddCommand(parameterMessage.Author.Id, commandInfo.Command.Name).Save();
                GenericBot.CommandCounter++;
            }
            catch (NullReferenceException nullRefEx)
            {

            }
            catch (Exception ex)
            {
                if (parameterMessage.Author.Id == GenericBot.GlobalConfiguration.OwnerId)
                {
                    await parameterMessage.ReplyAsync("```\n" + $"{ex.Message}\n{ex.StackTrace}".SafeSubstring(1000) +
                                                      "\n```");
                }
                await GenericBot.Logger.LogErrorMessage(ex.Message);
                Console.WriteLine($"{ex.StackTrace}");
            }
        }

        public static async Task MessageDeleted(Cacheable<IMessage, ulong> arg, ISocketMessageChannel channel)
        {
            if (!arg.HasValue) return;

            var guildConfig = GenericBot.GuildConfigs[(arg.Value as SocketMessage).GetGuild().Id];

            if (guildConfig.UserLogChannelId == 0 || guildConfig.MessageLoggingIgnoreChannels.Contains(channel.Id)) return;

            EmbedBuilder log = new EmbedBuilder()
                .WithTitle("Message Deleted")
                .WithColor(139, 0, 0)
                .WithCurrentTimestamp();

            if (string.IsNullOrEmpty(arg.Value.Author.GetAvatarUrl()))
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg.Value.Author} ({arg.Value.Author.Id})"));
            }
            else
            {
                log = log.WithAuthor(new EmbedAuthorBuilder().WithName($"{arg.Value.Author} ({arg.Value.Author.Id})")
                    .WithIconUrl(arg.Value.Author.GetAvatarUrl() + " "));
            }

            log.AddField(new EmbedFieldBuilder().WithName("Channel").WithValue("#" + arg.Value.Channel.Name).WithIsInline(true));
            log.AddField(new EmbedFieldBuilder().WithName("Sent At").WithValue(arg.Value.Timestamp.ToString(@"yyyy-MM-dd HH:mm.ss") + "GMT").WithIsInline(true));


            if (!string.IsNullOrEmpty(arg.Value.Content))
            {
                log.WithDescription("**Message:** " + arg.Value.Content);
            }

            if (arg.Value.Attachments.Any())
            {
                log.AddField(new EmbedFieldBuilder().WithName("Attachments").WithValue(arg.Value.Attachments.Select(a =>
                    $"File: {a.Filename}").Aggregate((a, b) => a + "\n" + b)));
                log.WithImageUrl(arg.Value.Attachments.First().ProxyUrl);
            }

            if(string.IsNullOrEmpty(arg.Value.Content) && !arg.Value.Attachments.Any() && arg.Value.Embeds.Any())
            {
                log.WithDescription("**Embed:**\n```json\n" + JsonConvert.SerializeObject(arg.Value.Embeds.First(), Formatting.Indented) + "\n```");
            }

            log.Footer = new EmbedFooterBuilder().WithText(arg.Value.Id.ToString());

            await (arg.Value as SocketMessage).GetGuild().GetTextChannel(guildConfig.UserLogChannelId).SendMessageAsync("", embed: log.Build());
        }

        public static async Task MessageRecieved(SocketMessage arg)
        {
            MessageEventHandler.MessageRecieved(arg, false);
        }
    }
}
