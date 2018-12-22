using System;
using System.Collections.Generic;
using System.Linq;
using Discord.WebSocket;
using GenericBot.Entities;

namespace GenericBot.CommandModules
{
    class PointsCommands
    {
        public List<Command> GetPointsCommands()
        {
            var pointCommands = new List<Command>();

            Command points = new Command("points");
            points.Description = "Show the number of points the user has";
            points.ToExecute += async (client, msg, parameters) =>
            {
                if (!GenericBot.GuildConfigs[msg.GetGuild().Id].PointsEnabled)
                    return;

                var user = new DBGuild(msg.GetGuild().Id).GetUser(msg.Author.Id);
                await msg.ReplyAsync($"{msg.Author.Mention}, you have `{Math.Floor(user.PointsCount)}` {GenericBot.GuildConfigs[msg.GetGuild().Id].PointsName}s!");
            };
            pointCommands.Add(points);

            Command leaderboard = new Command(nameof(leaderboard));
            leaderboard.ToExecute += async (client, msg, parameters) =>
            {
                var db = new DBGuild(msg.GetGuild().Id);
                var topUsers = db.Users.OrderByDescending(u => u.PointsCount).Take(10);
                string result = $"Top 10 users in {msg.GetGuild().Name}\n";
                int i = 1;
                var config = GenericBot.GuildConfigs[msg.GetGuild().Id];
                foreach (var user in topUsers)
                {
                    if (msg.GetGuild().Users.HasElement(u => u.Id == user.ID, out SocketGuildUser sgu))
                    {
                        result += $"{i++}: {sgu.GetDisplayName().Escape()}: `{Math.Floor(user.PointsCount)}` {config.PointsName}s\n";
                    }
                    else
                    {
                        result += $"{i++}: Unknown User (ID: `{user.ID}`): `{Math.Floor(user.PointsCount)}` {config.PointsName}s\n";
                    }
                }
                await msg.ReplyAsync(result);
            };

            pointCommands.Add(leaderboard);

            Command award = new Command("award");
            award.Description = "Give a user one of your points";
            award.ToExecute += async (client, msg, parameters) =>
            {
                var config = GenericBot.GuildConfigs[msg.GetGuild().Id];
                if (!config.PointsEnabled)
                    return;

                var dbGuild = new DBGuild(msg.GetGuild().Id);
                var user = dbGuild.GetUser(msg.Author.Id);
                if (user.PointsCount < 1)
                {
                    await msg.ReplyAsync($"You don't have any {config.PointsName} to give!");
                }
                else
                {
                    if (msg.MentionedUsers.Any())
                    {
                        if (msg.MentionedUsers.Count > user.PointsCount)
                        {
                            await msg.ReplyAsync($"You don't have that many {GenericBot.GuildConfigs[msg.GetGuild().Id].PointsName}s to give!");
                        }
                        else
                        {
                            foreach (var u in msg.MentionedUsers)
                            {

                                if (u.Id == msg.Author.Id) continue;

                                user.PointsCount--;
                                dbGuild.GetUser(u.Id).PointsCount++;
                            }
                            dbGuild.Save();
                            await msg.ReplyAsync($"{msg.MentionedUsers.Select(us => us.Mention).ToList().SumAnd()} received a {GenericBot.GuildConfigs[msg.GetGuild().Id].PointsName} from {msg.Author.Mention}!");
                        }
                    }
                    else
                    {
                        await msg.ReplyAsync($"You have to select a user to give a {config.PointsName} to");
                    }
                }
            };
            pointCommands.Add(award);

            Command setPoints = new Command("setpoints");
            setPoints.RequiredPermission = Command.PermissionLevels.GlobalAdmin;
            setPoints.ToExecute += async (client, msg, parameters) =>
            {
                var dbGuild = new DBGuild(msg.GetGuild().Id);
                var user = dbGuild.GetUser(ulong.Parse(parameters[0]));
                user.PointsCount = decimal.Parse(parameters[1]);
                dbGuild.Save();
                await msg.ReplyAsync($"`{parameters[0]}` now has {(user.PointsCount)} points");
            };
            pointCommands.Add(setPoints);

            return pointCommands;
        }
    }
}
