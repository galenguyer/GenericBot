using GenericBot.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericBot.CommandModules
{
    class MemeModule : Module
    {
        public List<Command> Load()
        {
            List<Command> commands = new List<Command>();

            Command mock = new Command("mock");
            mock.WorksInDms = true;
            mock.Description = "MOcKinG sPoNgeBoB TeXt";
            mock.ToExecute += async (context) =>
            {
                string mockedMessage = "";
                double rand = new Random().NextDouble();
                foreach (var c in context.ParameterString.ToLower())
                {
                    rand += new Random().NextDouble();
                    if (rand >= 1.10)
                    {
                        rand = new Random().NextDouble();
                        mockedMessage += char.ToUpper(c);
                    }
                    else
                        mockedMessage += c;
                }
                await context.Message.ReplyAsync(mockedMessage);
            };
            commands.Add(mock);

            Command clap = new Command("clap");
            clap.WorksInDms = true;
            clap.Usage = "Put the clap emoji between each word";
            clap.ToExecute += async (context) =>
            {
                await context.Message.ReplyAsync(context.Parameters.Rejoin(" :clap: ") + " :clap:");
            };
            commands.Add(clap);
            
            Command uwu = new Command("uwu");
            uwu.WorksInDms = true;
            uwu.Usage = "Uwu-ify text";
            uwu.ToExecute += async (context) =>
            {
                string uwuified = context.ParameterString;
                uwuified = new Regex("(?:r|l)").Replace(uwuified, "w");
                uwuified = new Regex("(?:R|L)").Replace(uwuified, "W");
                uwuified = new Regex("n([aeiou])").Replace(uwuified, "ny$1");
                uwuified = new Regex("N([aeiou])").Replace(uwuified, "Ny$1");
                uwuified = new Regex("N([AEIOU])").Replace(uwuified, "Ny$1");
                uwuified = new Regex("ove").Replace(uwuified, "uv");
                uwuified = new Regex("th").Replace(uwuified, "f");
                uwuified = new Regex("Th").Replace(uwuified, "f");
            };
            commands.Add(uwu);

            return commands;
        }
    }
}
