using System.Collections.Generic;

namespace twitchBot.Entities
{
    public class Pyramid
    {
        public string ContentKey { get; set; }

        public int LastTileWidth { get; set; }

        public int Width { get; set; }

        public bool Failed { get; set; }

        public bool Finished { get; set; }

        public List<string> Lines { get; set; }

        public void Add(string content)
        {
            if(string.IsNullOrEmpty(content))
                return;

            var keys = content.Split(" ");

            // prevents a 2 tiles start pyramid
            if (keys.Length > 1 && Width == 0)
                return;

            if (Width == 0)
                ContentKey = keys[0].Trim();

            if(Lines == null)
                Lines = new List<string>();

            //if the current message is different from the contentKey, it means that someone broke the pyramid
            if (IsPyramidBroken(keys))
                Failed = true;

            CheckIfIsFinished(keys.Length);

            if (!IsFinishingPyramid(keys.Length))
                Width++;

            LastTileWidth = keys.Length;

            Lines.Add(content);
        }

        private void CheckIfIsFinished(int currentTileWidth)
        {
            var differenceBetweenTiles = LastTileWidth - currentTileWidth;

            Finished = currentTileWidth == 1 && differenceBetweenTiles == 1;
        }

        public bool IsFinishingPyramid(int currentTileWidth)
        {
            return currentTileWidth < LastTileWidth;
        }

        public bool IsValid()
        {
            return Width > 2;
        }

        private bool IsPyramidBroken(string[] content)
        {
            foreach (var c in content)
            {
                if (!ContentKey.Equals(c))
                    return true;
            }

            return false;
        }
    }
}
