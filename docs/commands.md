# Command & Feature Reference

Kinobotz is a Twitch chat bot. Chat commands start with `%`; mentioning **`@kinobotz`** triggers a GPT reply. This reference is derived from `Entities/Commands.cs` (the default command set) and the handlers in `twitchBot/Handlers/`.

## Access levels & cooldowns

A command runs when the caller's access level is **≥** the command's required level. Ordering (low→high): `Default < Vip < Subscriber < Moderator < Broadcaster < Premium < Admin`. The user `k1notv` is hard-coded as **Admin** (and bypasses cooldowns). Cooldowns are per-user unless marked **global** (shared across the channel). Per-channel command config (enabled/disabled, access, cooldown) is editable from the dashboard and stored in Redis.

## Commands

| Command | Access | Cooldown | What it does | Integrations |
|---------|--------|----------|--------------|--------------|
| `%gpt <text>` *(or `@kinobotz <text>`)* | Default | — | GPT reply, shaped by the channel's behavior prompt, capped at 500 chars | OpenAI via `Microsoft.Extensions.AI` |
| `%tts <voiceName>: <text>` *(or `%tts` to list voices)* | Default | 10 min (global) | Text-to-speech via ElevenLabs → played on the overlay; optionally posted to a Discord webhook. 1000 chars/user/day | ElevenLabs, SignalR overlay, Discord |
| `%clip` | Default | — | Creates a Twitch clip of the stream; posts the link to Discord if configured | Twitch Helix, Discord |
| `%lm <username>` | Default | — | Shows the last chat message stored for a user | Redis |
| `%ff <username>` | Default | — | Looks up the first channel a user followed (cached) | Twitch Helix, Redis |
| `%commands` | Default | — | Lists the available commands | — |
| `%gptbehavior <instructions>` | Default | 10 min (global) | Sets the GPT system prompt ("personality") for the channel; records who set it | Redis |
| `%gptbehaviordef` | Default | — | Shows the current GPT behavior and who defined it | Redis |
| `%notify` | Default | — | Joins the stream up/down notification list | Redis |
| `%command <add\|update\|delete> <name> <content>` | Moderator | — | Manages custom text commands (stored in Redis) | Redis |
| `%title <new title>` | Moderator | — | Updates the stream title (max 140 chars) | Twitch Helix |
| `%enable <command>` | Moderator | — | Re-enables a disabled command | Redis |
| `%disable <command>` | Moderator | — | Disables a command (can't disable `enable`/`disable`) | Redis |
| `%rtitle` | Broadcaster | — | Generates a 1–5 word random stream title via GPT and applies it | OpenAI, Twitch Helix |

> `%gptbehavior` with no argument behaves like `%gptbehaviordef` (returns the current behavior).

## Event-driven features

These fire automatically (no command), gated by per-channel settings on the dashboard:

- **`@kinobotz` mention** → routes to the GPT command.
- **Bits / Cheers** — if `UseTtsOnBits` and the amount ≥ `TtsMinimumBitAmount`, the cheer message is auto-read via TTS (Cheer prefixes stripped).
- **Subscriptions** — if `UseTtsOnSubscription` and months ≥ `TtsMinimumResubMonthsAmount`, the sub message is auto-read via TTS.
- **Stream up / down** — posts a message tagging everyone on the `%notify` list.
- **Pyramid detection** — the bot reacts to chat "pyramids".

## Audit log

Every command execution is logged to Redis (a global `auditlog` set and a per-channel
`{botConnectionId}:auditlog` set): command, user, access level, original chat message,
timestamp, channel, and the response. Admins can view it via `GET /commands/log` and the
dashboard **Stats** page.

## TTS notes

- Voices are resolved from ElevenLabs and cached per channel.
- A channel can set a default voice; bits/sub auto-TTS uses it (or a random voice if unset).
- ElevenLabs key: `Premium` channels use the global `ELEVEN_LABS_API_KEY`; others configure
  their own key on the dashboard. Per-user daily character limit: 1000.

See also: [architecture.md](architecture.md) · [deployment-and-ops.md](deployment-and-ops.md) · [redis-key-schema.md](redis-key-schema.md).
