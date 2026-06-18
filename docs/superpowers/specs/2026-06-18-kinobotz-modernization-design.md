# Kinobotz Modernization — Design Spec

**Date:** 2026-06-18
**Status:** Draft for review
**Author:** Gabriel + Claude

## 1. Goal

Modernize the Kinobotz Twitch bot platform: consolidate the backend (`kinobotz`) and
frontend (`kinobotz-ui`) into a single repository, upgrade to current, supported
technology (.NET 10 LTS, modern Vue tooling), remove abandoned/commercial dependencies,
modernize the Heroku deployment pipeline, dramatically improve test coverage, and
document all existing functionality. Retargeting to .NET 10 also means **refactoring the
code to idiomatic .NET 10 / modern C#**, not just bumping the framework. The work is a
**deep modernization** done **incrementally and behavior-preserving** — `main` stays
shippable after every phase.

## 2. Current State (summary)

### Backend — `kinobotz` (.NET 7, EOL)
- **Solution** `twitchBot.sln` with 5 projects: `twitchBot` (worker dyno), `webapi`
  (web dyno: REST + SignalR `/overlayHub`), `Infrastructure` (Redis repos + services),
  `Entities` (domain models), `kinobotz.Tests` (effectively empty).
- **Datastore:** Redis only (StackExchange.Redis + Extensions, Newtonsoft serialization),
  via Redis Cloud free 30 MB add-on (`REDISCLOUD_URL`). EF Core 7 is referenced but **unused**.
- **Features (~14 commands):** `%lm`, `%gpt` / `@kinobotz` mention, `%tts`, `%clip`,
  `%command` (custom), `%commands`, `%gptbehavior`, `%gptbehaviordef`, `%title`,
  `%rtitle`, `%ff`, `%enable`, `%disable`, `%notify`; plus auto-TTS on bits/subs,
  stream up/down notifications, audit log, per-command access levels + cooldowns.
- **Integrations:** Twitch (TwitchLib 3.5.3, IRC + PubSub), OpenAI (OkGoDoIt `OpenAI` 1.7.2),
  ElevenLabs TTS, Discord webhooks, SignalR overlay audio.
- **Dispatch:** `Bot.OnMessageReceived` → `CommandFactory` → MediatR → `BaseCommandHandler<T>`
  (access/cooldown/audit) → `InternalHandle`.
- **Auth (webapi):** Twitch OAuth → JWT (HS256, 30-day). CORS for `https://k1no.tv` + localhost.

### Frontend — `kinobotz-ui` (Vue 3, but legacy tooling)
- **Vue 3.2.13** on **Vue CLI 5 + webpack** (deprecated), **Vuex**, **ESLint 7** (EOL),
  **no TypeScript**, three overlapping SignalR libs (`@microsoft/signalr` is the live one).
- 8 routes/pages: Home, Dashboard (main config), Commands, GptBehavior, Users (admin),
  Stats (admin), Callback (OAuth), Overlay (SignalR TTS audio).
- axios + JWT-in-cookie; API base from runtime `window.__ENV__.API_URL`.
- Deployed as its own nginx-static Heroku container app.

### Deployment
- Heroku **container** stack. App `kinobotz` runs `web` (webapi) + `kinobotz` (worker bot)
  from `heroku.yml`. `kinobotz-ui` is a separate static app. `kinobotz-api` (heroku-22) is
  dormant. Base images pinned to **.NET 7 (EOL)** — will break the next rebuild.

### Known live issues (observed in logs, 2026-06-18)
- **GPT broken by OpenAI `insufficient_quota`** (HTTP 429) — account out of credits; the
  handler dumps the raw error into chat on every mention. (Operational fix = billing; code
  fix = hardening, see §5.2.)
- **Stale Twitch refresh tokens** for some channels (`vilelandre`, `xandghost`) — token
  refresh throws `BadRequestException`.
- **Log noise:** continuous `OperationCanceledException` from TwitchLib sender throttlers,
  logged under a mislabeled "Error on PubSub" handler.

## 3. Decisions (locked)

| # | Decision | Choice | Rationale |
|---|----------|--------|-----------|
| 1 | Monorepo | Subtree-merge `kinobotz-ui` **preserving git history**; restructure to `backend/` + `frontend/` | Single source of truth; keeps UI history. |
| 2 | Depth | **Deep modernization** | Framework + libs + architecture + tests + docs. |
| 3 | AI library | **`Microsoft.Extensions.AI`** (OpenAI provider) | Modern abstraction; swap providers later without rewrite. |
| 4 | Deploy topology | One app: `web` (API) + `worker` (bot); **UI stays a separate static app**; GitHub Actions CI/CD | Clean separation, modern pipeline. |
| 5 | Datastore | **Redis-only** (keep free Redis Cloud 30 MB) | Cheapest ($0). Postgres on Heroku is $5/mo Essential-0; data is tiny. Free external Postgres (Neon/Supabase) noted as optional future (§10). |
| 6 | MediatR / AutoMapper | **Remove both** | Both went commercial in 2025. Replace MediatR with a thin DI-based dispatcher (~14 handlers); replace AutoMapper with explicit mapping. Net simplification. |
| 7 | Testing | **First-class workstream** | Comprehensive backend (xUnit) + frontend (Vitest) coverage with targets, woven through every phase. |
| 8 | Code style | **Refactor to idiomatic .NET 10 / modern C#** | Not just a framework bump — adopt current language/runtime idioms and fix legacy anti-patterns as files are touched. |

### Verified facts
- **.NET 10** is LTS, released 2025-11-11, supported to 2028-11-10. Target framework.
- **MediatR / AutoMapper** moved to commercial dual-license in 2025 (free Community tier
  only for orgs < $5M revenue / non-profit / educational). We remove them to avoid the friction.
- **Heroku Postgres** cheapest = Essential-0 **$5/mo**; **Heroku Redis** Mini = $3/mo;
  **Redis Cloud** add-on has a **free 30 MB** tier (current setup).

## 4. Target Architecture

```
kinobotz/                       single repo (UI subtree-merged with history)
├─ backend/                     .NET 10 solution
│  ├─ twitchBot/                worker dyno — Twitch chat + EventSub + jobs
│  ├─ webapi/                   web dyno — REST API + SignalR /overlayHub
│  ├─ Infrastructure/           Redis repos, AI client, Twitch, services
│  ├─ Entities/                 domain models + mapping extensions
│  └─ tests/                    xUnit unit + integration (Testcontainers Redis)
├─ frontend/                    Vue 3 + Vite + Pinia + TypeScript (separate static app)
├─ docs/                        architecture, full feature/command reference, runbook
├─ .github/workflows/           CI (build+test+coverage) and CD (deploy)
├─ backend.web.Dockerfile       .NET 10 aspnet base
├─ backend.worker.Dockerfile    .NET 10 aspnet base
└─ heroku.yml                   web + worker process types
```

- **AI:** `Microsoft.Extensions.AI` `IChatClient` backed by OpenAI. GPT handler hardened
  (friendly chat message + circuit-breaker on `insufficient_quota`/quota errors; no raw
  error spam).
- **Twitch:** TwitchLib upgraded to current; **PubSub → EventSub** (WebSocket) migration for
  bits/subs/stream-up-down; removes the dead-PubSub reconnect noise and the mislabeled
  "Error on PubSub" log storm. Token-refresh path made resilient (per-channel failure
  isolation, clear "needs re-auth" signal).
- **Data:** Redis-only, modernized: System.Text.Json serialization, typed repository
  interfaces, documented key schema, periodic backup/export for durability.
- **Deploy:** `web` + `worker` process types in one app on .NET 10 base images; UI as a
  separate static app; GitHub Actions builds, tests (gate on coverage), and deploys;
  Heroku review apps for PRs (optional).

## 5. Detailed Architecture Changes

### 5.1 Remove MediatR — thin command dispatch
- Define `ICommandHandler<TCommand>` with `Task<Response> HandleAsync(TCommand, CancellationToken)`.
- A `CommandDispatcher` resolves the handler for the concrete command type from DI and invokes it.
- Cross-cutting concerns currently in `BaseCommandHandler<T>` (command-exists/enabled,
  access level, cooldown, audit logging) move into either an abstract base handler or a small
  ordered set of explicit decorators (no MediatR pipeline behaviors).
- `CommandFactory` is retained (it builds the typed command from the chat message).

### 5.2 GPT path — `Microsoft.Extensions.AI` + hardening
- Register an `IChatClient` (OpenAI provider) in DI; inject into the GPT handler.
- Behavior preserved: system behavior prompt + 500-char cap.
- **Hardening:** catch quota/auth failures distinctly; reply with a friendly one-time message
  (e.g., "GPT is temporarily unavailable") and a short circuit-breaker so a dead key/quota
  doesn't spam chat or burn API calls on every mention. Log the technical detail, not chat-dump it.

### 5.3 Remove AutoMapper — explicit mapping
- Replace AutoMapper profiles with hand-written mapping extension methods
  (`ToDto()` / `ToEntity()` / `Apply(UpdateBotConnection)`), co-located with the DTOs/entities.

### 5.4 Twitch — TwitchLib upgrade + EventSub
- Upgrade TwitchLib to current major; migrate PubSub subscriptions (bits, subs, stream
  up/down, redemptions) to **EventSub over WebSocket**.
- Isolate per-channel token refresh failures; surface a clear "re-authorize" state instead
  of repeated exceptions; reduce/relabel the throttler-cancellation logging.

### 5.5 Frontend modernization
- **Vue CLI/webpack → Vite**; **Vuex → Pinia**; **ESLint 7 → 9 (flat config)**;
  **add TypeScript** (incrementally — config + new code typed, pages migrated over time).
- Remove dead SignalR libs (`@aspnet/signalr`, redundant Vue wrappers); keep `@microsoft/signalr`.
- Pin Node engine (LTS). Centralize the API base URL + runtime config injection.

### 5.6 Idiomatic .NET 10 / modern C# refactor
Applied opportunistically as each file is touched (not a separate big-bang pass):
- **Nullable reference types** enabled solution-wide; `ImplicitUsings` consistent across projects.
- **File-scoped namespaces**, top-level minimal hosting (`Program.cs`), and current ASP.NET
  Core configuration patterns.
- **Primary constructors** for DI-heavy classes; **records** for DTOs and immutable value types;
  **collection expressions**, `required` members, target-typed `new`, and pattern matching.
- **Fix async anti-patterns:** `Bot.RefreshAccessToken` is `async void` and calls `.Result`
  (blocking + exception-swallowing) — convert to proper `async Task` with awaited calls and
  correct cancellation; audit the codebase for other `.Result`/`.Wait()`/`async void`.
- **`System.Text.Json`** replacing Newtonsoft where practical (Redis serialization, APIs).
- **`TimeProvider`** for time-based logic (cooldowns, daily TTS limits) to make it testable.
- Nullable/required-driven cleanup of the many nullable `BotConnection` fields.

### 5.7 Configuration & secrets
- Consolidate config var naming; document every key; provide `.env.example` for each app.
- Remove the bogus `Microsoft.AspNetCore.App 3.1.0` `FrameworkReference` from the worker;
  remove unused EF Core reference.

## 6. Testing Strategy (first-class)

- **Backend (xUnit):**
  - Unit tests for every command handler (access level, cooldown, enable/disable, audit,
    error paths), `CommandFactory` parsing (prefix + `@mention`), the new dispatcher, mapping
    extensions, and the AI/Twitch/TTS service wrappers (mocked clients).
  - Integration tests for repositories against a real Redis via **Testcontainers**; webapi
    endpoint tests via `WebApplicationFactory` (auth, CORS, controllers).
  - Characterization tests written **before** refactors (MediatR/AutoMapper removal, EventSub)
    to lock current behavior.
- **Frontend (Vitest + Vue Test Utils):** store/Pinia logic, axios interceptor/auth, route
  guards, and component tests for Dashboard/Commands/Overlay; optionally a Playwright smoke
  for the OAuth+dashboard happy path.
- **Coverage:** CI reports coverage and **gates** merges below an agreed threshold
  (proposed: 70% backend lines to start, ratcheting up; meaningful component coverage on FE).
- **CI integration:** tests + coverage run on every PR in GitHub Actions.

## 7. Documentation Deliverables (`docs/`)
- **Architecture overview** (services, data flow, deployment) with a diagram.
- **Feature/command reference** — every command: syntax, access level, cooldown, behavior,
  external service, examples (also powers the UI Commands page).
- **Data/key schema** reference for Redis.
- **Runbook** — deploy, rollback, re-auth a channel, rotate keys, OpenAI billing, common log
  signatures, monitoring.
- **Contributing/dev setup** — local run for backend + frontend.

## 8. Phased Plan (each phase shippable)

- **Phase 0 — Consolidate & safety net:** subtree-merge UI (preserve history) into
  `frontend/`, move .NET solution to `backend/`; fix Dockerfile/heroku paths; stand up
  GitHub Actions CI (build + test); add characterization tests for current behavior.
- **Phase 1 — Backend framework upgrade:** .NET 7 → **.NET 10**; enable NRT solution-wide;
  bump/realign all MS packages; remove bogus 3.1.0 ref + unused EF Core; minimal hosting +
  file-scoped namespaces; green build + tests on .NET 10 base images.
- **Phase 2 — AI swap + hardening:** OkGoDoIt → `Microsoft.Extensions.AI`; harden GPT
  handler (quota/error circuit-breaker). Resolves the live GPT incident in code.
- **Phase 3 — Architecture cleanup + idiomatic refactor:** remove MediatR (thin dispatcher)
  and AutoMapper (explicit mapping); apply modern C# idioms (primary constructors, records,
  pattern matching, collection expressions); fix async anti-patterns (`async void`/`.Result`),
  adopt `TimeProvider`; refactor oversized files; expand unit coverage.
- **Phase 4 — Twitch modernization:** TwitchLib upgrade; PubSub → EventSub; resilient token
  refresh; log-noise cleanup.
- **Phase 5 — Data layer:** modernize Redis repositories (System.Text.Json, typed
  interfaces), document key schema, add backup/export; integration tests via Testcontainers.
- **Phase 6 — Frontend modernization:** Vite + Pinia + ESLint 9 + TypeScript; SignalR
  cleanup; Node pinning; Vitest coverage.
- **Phase 7 — Deployment & CI/CD:** consolidate API+bot app on .NET 10 base images; UI
  pipeline; GitHub Actions CD; coverage gates; (optional) review apps.
- **Phase 8 — Documentation:** complete `docs/` deliverables; wire the feature reference to
  the UI Commands page.

Testing and documentation are **cross-cutting** — each phase ships with its tests and doc updates; Phases 0/8 bookend with the safety net and the consolidated docs.

## 9. Risks & Mitigations
- **EventSub migration** is the highest-risk change → characterization tests first; do behind
  the safety net; validate bits/subs/stream events against a test channel.
- **MediatR/AutoMapper removal** could change dispatch/mapping behavior → lock with
  characterization tests before refactor.
- **.NET 10 base image availability on Heroku container stack** → verify tags pull in CI before cutover.
- **Redis as system-of-record durability** → enable Redis Cloud persistence/backups + periodic export.
- **Scope is large** → strictly phased, each independently shippable; no long no-ship window.

## 10. Out of Scope / Future Options
- **Postgres**: not adopted now (cost). If relational stats/audit become valuable, a **free
  external Postgres (Neon/Supabase)** can be introduced later behind the modernized repository
  interfaces with minimal churn.
- Platform migration off Heroku (not requested).
- Full TypeScript rewrite of all UI pages in one pass (done incrementally instead).

## 11. Open Questions
- Coverage threshold to gate CI on (proposed 70% backend to start) — confirm/adjust.

**Resolved:** EventSub migration will be validated against the **live `kinobotz`/`k1notv`
channel** (behind the characterization safety net, staged carefully) — no separate test channel.
