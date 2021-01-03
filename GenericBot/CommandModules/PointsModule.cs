using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericBot.CommandModules
{
    public class PointsModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command points = new Command("points");
            points.Description = "Find out how many points you have and your rank on the leaderboard";
            points.ToExecute += async (context) =>
            {
                var sortedUsers = Core.GetAllUsers(context.Guild.Id).Where(u => u.IsPresent).OrderByDescending(u => u.Points).ToList();
                var position = sortedUsers.FindIndex(u => u.Id == context.Author.Id);
                await context.Message.ReplyAsync($"{context.Author.Mention}, you are at rank {position + 1} with {sortedUsers[position].Points} points!");
            };
            commands.Add(points);

            Command leaderboard = new Command("leaderboard");
            leaderboard.Description = "Display the points leaderboard for the server";
            leaderboard.ToExecute += async (context) =>
            {
                var sortedUsers = Core.GetAllUsers(context.Guild.Id).Where(u => u.IsPresent).OrderByDescending(u => u.Points).ToList();
                var top = sortedUsers.Skip(10 * 0).Take(10).ToList();
                string reply = $"The top {10} members are:\n";

                for(int i = 0; i < 10; i++)
                {
                    reply += $"**{i+1}**: {context.Guild.GetUser(top[i].Id).GetDisplayName()} ({top[i].Points} points)\n";
                }

                await context.Message.ReplyAsync(reply);
            };
            commands.Add(leaderboard);

            return commands;
        }
    }
}
