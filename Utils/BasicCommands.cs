using System.Collections.Generic;
using System.Linq;
using twitchBot.Entities;

namespace twitchBot.Utils
{
    public class BasicCommands
    {
        private string cmdInitials = "!";
        private Dictionary<string, string> data;
        private Pyramid currentPyramid;

        public string PyramidChecker(Dictionary<string, string> data)
        {
            if(currentPyramid == null)
                currentPyramid = new Pyramid();

            currentPyramid.Add(data[data.Keys.ElementAt(0)]);

            if (currentPyramid.Failed)
            {
                currentPyramid = null;

                return null;
            }

            if (currentPyramid.Finished)
            {
                //not a valid pyramid, min width = 3
                if (!currentPyramid.IsValid())
                {
                    currentPyramid = null;

                    return null;
                }

                var responseMessage =
                    $"O {data.Keys.ElementAt(0)} conseguiu completar uma pirâmide com largura {currentPyramid.Width} {currentPyramid.ContentKey} ! insano PogChamp";

                currentPyramid = null;

                return responseMessage;

            }

            return null;
        }

        public string CommandListener(Dictionary<string, string> data)
        {
            string response;

            if (data == null)
                return null;
            
            response = PyramidChecker(data);

            // dictionary decomposition 
            if (response == null && data[data.Keys.ElementAt(0)].Substring(0, cmdInitials.Length) == cmdInitials)
            {
                this.data = data;
                string user = data.Keys.ElementAt(0);
                string msg = data[data.Keys.ElementAt(0)];

                return CommandTree(user, msg);
            }

            return response;
        }

        // This function is the manager to address who should be responding to the command. [Temp solution]
        // we need to keep track of who placed the command
        // Function act as controller to address responses base on type of commands
        private string CommandTree(string user, string msg)
        {
            switch (msg.ToLower())
            {
                case "!cmd1":
                    return $"Hey {user}, this is the command 1";
                case "!cmd2":
                    return $"Hey {user}, this is the command 2";
                default:
                    return "I dont know this command, please teach me!";
            }
        }
    }
}