# Redis Key Schema

Kinobotz uses Redis (free Redis Cloud add-on, `REDISCLOUD_URL`) as its only datastore —
both cache and system-of-record. All access goes through `StackExchange.Redis.Extensions`
(`IRedisClient.Db0`). Values are JSON-serialized by the configured serializer
(**System.Text.Json** as of the modernization; previously Newtonsoft — the swap is
covered by `RedisSerializerCompatibilityTests`). Counters use raw `StringIncrement`
(not JSON). Sets use `SetAdd`/`SetMembers`.

> Derived from `Infrastructure/Repository/*` and `twitchBot` (`Bot`, `TextToSpeechCommandHandler`).
> `{id}`/`{botConnectionId}` = the BotConnection GUID; `{prefix}` = a command prefix (e.g. `gpt`, `tts`).

## Durable (system-of-record)

| Key | Redis type | Value | Written by |
|-----|-----------|-------|-----------|
| `botconnection:{id}:{channelId}:{login}` | string (JSON) | `BotConnection` (incl. OAuth access/refresh tokens, settings) | `BotConnectionRepository.SaveOrUpdate` |
| `{botConnectionId}:commands:{prefix}:definition` | string (JSON) | `Command` (per-channel command config) | `BotConnectionRepository.SetCommand(s)` |
| `{botConnectionId}:command:{name}` | string | custom command text | `CustomCommandRepository` |
| `{botConnectionId}:gptbehavior:current` | string | current GPT system prompt | `GptRepository` |
| `{botConnectionId}:gptbehavior:hist` | set (JSON) | `BehaviorDefinition` history (per channel) | `GptRepository` |
| `gptbehavior:hist` | set (JSON) | `BehaviorDefinition` history (global, all channels) | `GptRepository` |
| `{botConnectionId}:gptbehavior:definedby` | string | username who set the current behavior | `GptRepository` |
| `{botConnectionId}:notify` | string (JSON) | `NotifyUsers` (stream up/down notify list) | `NotifyCommandHandler` |
| `auditlog` | set (JSON) | `AuditLog` (global, every command execution) | `AuditLogRepository` |
| `{botConnectionId}:auditlog` | set (JSON) | `AuditLog` (per channel) | `AuditLogRepository` |

## Ephemeral / derived (safe to lose; rebuilt at runtime)

| Key | Redis type | Value | Written by |
|-----|-----------|-------|-----------|
| `lm:{username}` | string (JSON) | `SimplifiedChatMessage` (last message seen) | `Bot.StoreMessage` |
| `{prefix}:counter` | string (long) | global command execution counter | `BotConnectionRepository.IncrementExecutionCounter` |
| `{botConnectionId}:{prefix}:counter` | string (long) | per-channel execution counter | same |
| `{botConnectionId}:{prefix}:lastexecution` | string (JSON `DateTime`) | global cooldown timestamp | `BotConnectionRepository.SetLastExecutionTime` |
| `{botConnectionId}:{prefix}:lastexecution:{username}` | string (JSON `DateTime`) | per-user cooldown timestamp | same |
| `{channel}:{prefix}:{voiceName}` | string | cached ElevenLabs voice id | `TextToSpeechCommandHandler` |
| `{botConnectionId}:{prefix}:{date}:{username}:characters` | string (int) | per-user daily TTS character count | `TextToSpeechCommandHandler` |

## Durability

Redis is the system-of-record, so durability matters for the **Durable** keys above.

- **Enable persistence/backups** on the Redis Cloud plan (AOF/snapshot) so a restart doesn't
  lose bot connections, tokens, behaviors, or audit history.
- **Periodic export** (future): a scheduled job dumping the durable keys to Blob/object storage
  is the cheapest off-Redis backup. Tracked as a follow-up; not yet implemented.
- If relational queries on audit/stats ever become valuable, a **free external Postgres**
  (Neon/Supabase) can sit behind the repository interfaces — out of scope for now (cost).
