﻿using System.Reflection.Metadata.Ecma335;
using Entities;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Infrastructure.Repository
{
    public class GptRepository : IGptRepository
    {
        private readonly IRedisClient redisClient;

        public GptRepository(IRedisClient redisClient)
        {
            this.redisClient = redisClient;
        }

        public async Task<BehaviorDefinition[]> GetAllGptBehaviors(string? botConnectionId)
        {
            var key = string.IsNullOrEmpty(botConnectionId)
                ? $"{Commands.GPT_BEHAVIOR}:hist"
                : $"{botConnectionId}:{Commands.GPT_BEHAVIOR}:hist";

            var result = await redisClient.Db0.SetMembersAsync<BehaviorDefinition>(key);

            return result;
        }

        public async Task<string?> GetGptBehavior(string botConnectionId)
        {
            return await redisClient.Db0.GetAsync<string>($"{botConnectionId}:{Commands.GPT_BEHAVIOR}:current");
        }

        public async Task<string?> GetGptBehaviorDefinedBy(string botConnectionId)
        {
            return await redisClient.Db0.GetAsync<string>($"{botConnectionId}:{Commands.GPT_BEHAVIOR}:definedby");
        }

        public async Task SetGptBehavior(string botConnectionId, BehaviorDefinition behaviorDefinition)
        {
            await redisClient.Db0.AddAsync($"{botConnectionId}:{Commands.GPT_BEHAVIOR}:current", behaviorDefinition.Behavior);
            await redisClient.Db0.SetAddAsync($"{botConnectionId}:{Commands.GPT_BEHAVIOR}:hist", behaviorDefinition);
            await redisClient.Db0.SetAddAsync($"{Commands.GPT_BEHAVIOR}:hist", behaviorDefinition);
        }

        public async Task SetGptBehaviorDefinedBy(string botConnectionId, string username)
        {
            await redisClient.Db0.AddAsync($"{botConnectionId}:{Commands.GPT_BEHAVIOR}:definedby", username);
        }
    }

    public class BehaviorDefinition
    {
        public BehaviorDefinition(string behavior, string definedBy, DateTime createdAt, string channel)
        {
            Behavior = behavior;
            DefinedBy = definedBy;
            CreatedAt = createdAt;
            Channel = channel;
        }

        public string Behavior { get; set; }
        public string DefinedBy { get; set; }
        public string Channel { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface IGptRepository
    {
        Task<BehaviorDefinition[]> GetAllGptBehaviors(string? botConnectionId);
        Task<string?> GetGptBehavior(string botConnectionId);
        Task<string?> GetGptBehaviorDefinedBy(string botConnectionId);
        Task SetGptBehavior(string botConnectionId, BehaviorDefinition behaviorDefinition);
        Task SetGptBehaviorDefinedBy(string botConnectionId, string username);
    }
}
