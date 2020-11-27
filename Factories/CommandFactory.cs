using System;
using System.Collections.Generic;
using StackExchange.Redis.Extensions.Core.Abstractions;
using twitchBot.Commands;
using twitchBot.Interfaces;

namespace twitchBot.Factories
{
    public class CommandFactory
    {
        private readonly IRedisCacheClient _redisCacheClient;

        public CommandFactory(IRedisCacheClient redisCacheClient)
        {
            _redisCacheClient = redisCacheClient;
        }

        public ICommand Build(string key)
        {
            var dictionary = new Dictionary<string, ICommand>()
            {
                {"%lm", new LastMessage(_redisCacheClient)},
                {"%ff", new FirstFollower(_redisCacheClient)}
            };


            ICommand value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }

            return null;
        }
    }
}
