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

        public static List<Command> DefaultCommands = new()
        {
            new Command { Prefix = DISABLE, Description = "Disables a command. Usage: %disable <command>", Enabled = true, AccessLevel = UserAccessLevelEnum.Moderator},
            new Command { Prefix = ENABLE, Description = "Enables a command. Usage: %enable <command>", Enabled = true, AccessLevel = UserAccessLevelEnum.Moderator },
            new Command { Prefix = RANDOM_STREAM_TITLE, Description = "Generates a random stream title using the defined gptbehavior. Usage: %rtitle", Enabled = true, AccessLevel = UserAccessLevelEnum.Broadcaster},
            new Command { Prefix = UPDATE_STREAM_TITLE, Description = "Updates the stream title. Usage: %title <title>", Enabled = true, AccessLevel = UserAccessLevelEnum.Moderator },
            new Command { Prefix = COMMAND, Description = "Creates a custom command. Usage: %command <add/update/delete> <commandName> <content>", Enabled = true, AccessLevel = UserAccessLevelEnum.Moderator },
            new Command { Prefix = EXISTING_COMMANDS, Description = "Lists all existing commands. Usage: %commands", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = LAST_MESSAGE, Description = "Gets the last message of a user. Usage: %lm <username>", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = FIRST_FOLLOW, Description = "Gets the first follower of a user. Usage: %ff <username>", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = CREATE_CLIP, Description = "Creates a clip. Usage: %clip", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = GPT, Description = "Generates text using GPT-3 and the defined behavior. Usage: %gpt <text> or just mention @kinobotz", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = GPT_BEHAVIOR, Description = "Sets the behavior of GPT-3. Usage: %gptbehavior <behavior>", Enabled = true, AccessLevel = UserAccessLevelEnum.Default, GlobalCooldown = true, Cooldown = 10},
            new Command { Prefix = GPT_BEHAVIOR_DEFINITION, Description = "Gets the definition of a behavior of GPT-3. Usage: %gptbehaviordef", Enabled = true, AccessLevel = UserAccessLevelEnum.Default },
            new Command { Prefix = TTS, Description = "Text to speech. Usage: %tts <voiceName>: <text>. To check the list of available voiceNames just type %tts.", Enabled = true, AccessLevel = UserAccessLevelEnum.Default, Cooldown = 10, GlobalCooldown = true},
            new Command { Prefix = NOTIFY, Description = "Joins the stream up/down notification list. Usage: %notify", Enabled = true, AccessLevel = UserAccessLevelEnum.Default }
        };
    }
}
