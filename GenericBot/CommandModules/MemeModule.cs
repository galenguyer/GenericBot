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
            
            // i tried to add a kanye command but i'm a clueless bitch https://kanye.rest/
            //Command kanye = new Command("kanye");
            //kanye.WorksInDms = true;
            //kanye.Usage = "Posts a Kanye West quote";
            //kanye.ToExecute += async (context) =>
            //{
                //get https://api.kanye.rest/
              //}

            return commands;
        }
    }
}
