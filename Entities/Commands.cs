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

        public static Dictionary<string, bool> DefaultCommands = new()
        {
            { RANDOM_STREAM_TITLE, true },
            { UPDATE_STREAM_TITLE, true },
            { COMMAND, true },
            { EXISTING_COMMANDS, true },
            { LAST_MESSAGE, true },
            { FIRST_FOLLOW, true },
            { CREATE_CLIP, true },
            { GPT, true },
            { GPT_BEHAVIOR, true },
            { GPT_BEHAVIOR_DEFINITION, true },
            { TTS, false },
            { NOTIFY, true },
            { ENABLE, true },
            { DISABLE, true }
        };
    }
}
