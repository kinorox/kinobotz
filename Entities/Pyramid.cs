using System.Collections.Generic;
using TwitchLib.Client.Models;

namespace twitchBot.Entities
{
    public class Pyramid
    {
        private static readonly object Lock = new object();

        private static Pyramid _currentPyramid;

        public string ContentKey { get; set; }

        public int LastTileWidth { get; set; }

        public int Width { get; set; }

        public bool Failed { get; set; }

        public bool Finished { get; set; }

        public List<string> Lines { get; set; }

        public static string Check(ChatMessage message)
        {
            lock (Lock)
            {
                _currentPyramid ??= new Pyramid();
            }

            lock (Lock)
            {
                _currentPyramid.Add(message.Message);
            }

            if (_currentPyramid.Finished)
            {
                //not a valid pyramid, min width = 3
                if (!_currentPyramid.IsValid())
                {
                    _currentPyramid = null;

                    return null;
                }

                var responseMessage =
                    $"O {message.Username} conseguiu completar uma piramide com largura {_currentPyramid.Width} {_currentPyramid.ContentKey} ! Belo trabalho amigão BloodTrail";

                _currentPyramid = null;

                return responseMessage;

            }

            return null;
        }

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

            Lines ??= new List<string>();

            //if the current message is different from the contentKey, it means that someone broke the pyramid
            if (IsPyramidBroken(keys))
            {
                ResetPyramid(content);

                return;
            }

            CheckIfIsFinished(keys.Length);

            if (!IsFinishingPyramid(keys.Length))
                Width++;

            LastTileWidth = keys.Length;
            
            lock (Lock)
            {
                Lines.Add(content);
            }
        }

        public void ResetPyramid(string content)
        {
            lock (Lock)
            {
                Width = 0;
                Lines = null;
            }

            Add(content);
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
            
            if(content.Length == 1 && Lines.Count == 1)
                return true;

            return false;
        }
    }
}
