namespace Entities
{
    public class Commands
    {
        public const string DISABLE = "disable";
        public const string RANDOM_STREAM_TITLE = "rtitle";
        public const string UPDATE_STREAM_TITLE = "title";
        public const string COMMAND = "command";
        public const string EXISTING_COMMANDS = "commands";
        public const string LAST_MESSAGE = "lm";
        public const string FIRST_FOLLOW = "ff";
        public const string CREATE_CLIP = "clip";
        public const string GPT = "gpt";
        public const string GPT_BEHAVIOR = "gptbehavior";
        public const string GPT_BEHAVIOR_DEFINITION = "gptbehaviordef";
        public const string TTS = "tts";
        public const string NOTIFY = "notify";
        public const string ENABLE = "enable";

        public static List<CommandInformation> DefaultCommands = new()
        {
            new CommandInformation { Prefix = DISABLE, Description = "Disables a command. Usage: %disable <command>", Enabled = true },
            new CommandInformation { Prefix = ENABLE, Description = "Enables a command. Usage: %enable <command>", Enabled = true },
            new CommandInformation { Prefix = RANDOM_STREAM_TITLE, Description = "Generates a random stream title using the defined gptbehavior. Usage: %rtitle", Enabled = true },
            new CommandInformation { Prefix = UPDATE_STREAM_TITLE, Description = "Updates the stream title. Usage: %title <title>", Enabled = true },
            new CommandInformation { Prefix = COMMAND, Description = "Creates a custom command. Usage: %command <add/update/delete> <commandName> <content>", Enabled = true },
            new CommandInformation { Prefix = EXISTING_COMMANDS, Description = "Lists all existing commands. Usage: %commands", Enabled = true },
            new CommandInformation { Prefix = LAST_MESSAGE, Description = "Gets the last message of a user. Usage: %lm <username>", Enabled = true },
            new CommandInformation { Prefix = FIRST_FOLLOW, Description = "Gets the first follower of a user. Usage: %ff <username>", Enabled = true },
            new CommandInformation { Prefix = CREATE_CLIP, Description = "Creates a clip. Usage: %clip", Enabled = true },
            new CommandInformation { Prefix = GPT, Description = "Generates text using GPT-3 and the defined behavior. Usage: %gpt <text> or just mention @kinobotz", Enabled = true },
            new CommandInformation { Prefix = GPT_BEHAVIOR, Description = "Sets the behavior of GPT-3. Usage: %gptbehavior <behavior>", Enabled = true },
            new CommandInformation { Prefix = GPT_BEHAVIOR_DEFINITION, Description = "Gets the definition of a behavior of GPT-3. Usage: %gptbehaviordef <behavior>", Enabled = true },
            new CommandInformation { Prefix = TTS, Description = "Text to speech. Usage: %tts <voiceName>: <text>. To check the list of available voiceNames just type %tts.", Enabled = true },
            new CommandInformation { Prefix = NOTIFY, Description = "Joins the stream up/down notification list. Usage: %notify", Enabled = true }
        };
    }

    public class CommandInformation
    {
        public string Prefix { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
    }
}
