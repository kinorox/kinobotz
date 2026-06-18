# Deployment & Operations Runbook

How Kinobotz is deployed, how to operate it, and how to recover when something breaks.

## Apps & topology

| App | Role | Stack | Deploys from | How |
|-----|------|-------|--------------|-----|
| **kinobotz** | Backend: `web` (webapi REST + SignalR) + `kinobotz` (bot worker) | container | this monorepo, `main` | Heroku GitHub integration (auto-deploy on merge) |
| **kinobotz-ui** | Vue SPA (nginx static) | container | this monorepo, `frontend/**` | GitHub Actions `frontend-deploy.yml` |
| ~~kinobotz-api~~ | dormant (0 dynos), heroku-22 | heroku-22 | — | decommission candidate (see Cleanup) |

Both backend dynos run on **.NET 10** base images (`mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0`).
Datastore: Redis (free Redis Cloud add-on, `REDISCLOUD_URL`). See `docs/redis-key-schema.md`.

## Deploy

- **Backend:** merge to `main`. Heroku's GitHub integration builds the containers from
  `heroku.yml` (`webapi.Dockerfile` → `web`, `twitchBot.Dockerfile` → `kinobotz`) and cuts a
  new release (`vNNN`). No manual step.
- **Frontend:** merge a change under `frontend/**` to `main` (or run the **Frontend Deploy**
  workflow manually via *Actions → Run workflow*). It builds `frontend/Dockerfile` and
  `heroku container:push`/`release`s to `kinobotz-ui`. Requires the `HEROKU_API_KEY` repo secret.

### Watch a deploy
```
heroku releases -a kinobotz -n 5        # confirm a new vNNN
heroku ps -a kinobotz                   # both dynos "up"
heroku logs --tail -a kinobotz          # boot: "List of connections: ..." then "Should be connected!"
```

### Rollback (instant)
```
heroku rollback -a kinobotz             # reverts to the previous release
heroku rollback v214 -a kinobotz        # or a specific release
```

## CI

- `backend-ci.yml` — `dotnet build` + tests on .NET 10; coverage collected as an artifact
  (**report-only** for now — ratchet to a `coverlet.msbuild /p:Threshold=70` line gate as the
  suite grows; target per the modernization spec).
- `frontend-ci.yml` — `npm ci` + lint + build (Node 20). Vitest lands with Phase 6b.
- `frontend-deploy.yml` — UI CD (installs the Heroku CLI; runners no longer ship it).

## Operations

### Re-authorize a channel (stale refresh token)
Symptom in logs: `<channel> - Error occurred trying to refresh access token` /
`BadRequestException ... refresh token was invalid`. The channel's stored Twitch refresh
token is stale. Fix: that channel re-logs-in via the dashboard OAuth flow, which writes fresh
`AccessToken`/`RefreshToken` onto its `BotConnection` in Redis. (Global `twitch_client_id`/
`secret` are fine — only the per-channel token is stale.)

### GPT not answering
`%gpt` / `@kinobotz` returns "temporarily unavailable" (or nothing). Almost always the OpenAI
account is **out of quota** — add credits at platform.openai.com → Billing. The bot fails
gracefully (circuit-breaker) but won't generate replies until the account has quota.

### Common log signatures
| Log | Meaning |
|-----|---------|
| `List of connections: a,b,c…` | Healthy boot — connections deserialized from Redis |
| `Should be connected!` / `Finished channel joining queue` | IRC connected, channels joined |
| `Error on PubSub` + `OperationCanceledException` (throttler) | **Benign** reconnect noise (Phase 4/EventSub cleans this up) |
| `… Error occurred trying to refresh access token` | Per-channel stale token → re-auth that channel |
| `insufficient_quota` / `Error during GPT execution` | OpenAI billing |

### Dynos
Eco dynos **sleep** after inactivity (`heroku ps` shows `idle`); they wake on traffic/restart.
Eco hours are pooled per account.

## Cleanup / hygiene (run manually — these mutate/destroy prod)

1. **Remove the stray `PORT` config var** (Heroku injects `PORT` at runtime; a manual one is
   redundant and can conflict). Restarts the dynos:
   ```
   heroku config:unset PORT -a kinobotz
   ```
2. **Decommission `kinobotz-api`** (dormant, 0 dynos, deprecated heroku-22 stack; superseded by
   the consolidated `kinobotz` app). **Irreversible:**
   ```
   heroku apps:destroy -a kinobotz-api --confirm kinobotz-api
   ```
3. **Disconnect the old UI deploy source:** once a monorepo-sourced UI deploy is confirmed live,
   disconnect `kinobotz-ui`'s GitHub auto-deploy from the standalone `kinobotz-ui` repo (Heroku
   dashboard → Deploy) and archive that repo, so the monorepo is the single source.
4. **Unrelated EOL apps** `frightening-cemetery-98205` and `pixelizer-app` (heroku-20, EOL) are
   separate projects — upgrade their stack (`heroku stack:set heroku-24`) or remove them as you see fit.
5. *(Optional, low value)* rename the worker process type `kinobotz` → `worker` in `heroku.yml`.
   Disruptive: requires a coordinated `heroku ps:scale worker=1 kinobotz=0` with the deploy.
