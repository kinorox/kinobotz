Both confirmed: the EF Core 7.0.8 ref is in **Entities.csproj line 11** (not Infrastructure, as the .NET-10 draft wrongly stated), and `RewardCommands` is an empty dictionary (line 31, `new()`), so the redemption path is dead. The reviewer corrections are accurate. I have enough verified ground truth to synthesize the master plan with the fixes incorporated.

```markdown
# Kinobotz Modernization â€” Master Implementation Plan

**Date:** 2026-06-18
**Source spec:** `docs/superpowers/specs/2026-06-18-kinobotz-modernization-design.md` (locked decisions Â§3)
**Repos consolidated:** backend `C:/Users/Gabriel/Documents/Git/kinobotz` (twitchBot, webapi, Infrastructure, Entities, kinobotz.Tests) + frontend `C:/Users/Gabriel/Documents/Git/kinobotz-ui` (Vue 3.2 / Vue CLI / Vuex)

This is the synthesized, review-hardened plan across 9 phases. Reviewer-flagged **blockers/majors are incorporated inline** and called out with **[FIX]** notes so the change from the draft is auditable. Testing and documentation are cross-cutting; each phase lists what lands.

---

## Milestones & Sequencing (overview)

| Phase | Title | Ships when | Hard gate before next |
|---|---|---|---|
| **0** | Consolidate + safety net | Monorepo on net7.0 builds+tests green in CI; characterization seed | CI green on PR; both Dockerfiles build from repo root |
| **1** | .NET 10 framework upgrade | All 5 projects net10.0, packages aligned, images pull, behavior preserved | Characterization suite unchanged-green on .NET 10 |
| **2** | AI swap + GPT hardening | `Microsoft.Extensions.AI` `IChatClient`; quota circuit-breaker | GPT tests green; no chat error-dump |
| **3** | Architecture cleanup | MediatRâ†’dispatcher, AutoMapperâ†’explicit mapping, async fixes, idioms, TimeProvider | Characterization suite re-green post-refactor |
| **4** | Twitch EventSub | PubSubâ†’EventSub; resilient token refresh; log cleanup | Live k1notv + 2nd channel validation green |
| **5** | Data layer | System.Text.Json, typed repos, key schema, backup job, Testcontainers | Live-data corpus deserializes; integration tests green |
| **6** | Frontend modernization | Vite+Pinia+ESLint9+TS(incremental)+Vitest | FE build/lint/type-check/tests green; overlay live-smoke |
| **7** | Deployment & CI/CD | web+worker on .NET 10, UI app, CD workflow, coverage gate | Deploy + post-deploy smoke green; old `kinobotz` dyno scaled to 0 |
| **8** | Documentation | docs/ complete; UI Commands page wired to single source | Docs render; prefix-parity + link checks green in CI |

**Critical sequencing constraints (cross-workstream):**
1. **Phase 0 is a prerequisite for everything** â€” produces `backend/` + `frontend/` layout, `backend/global.json`, CI, and the characterization seed.
2. **Characterization tests precede every risky refactor** â€” written/green on *current* code before MediatR/AutoMapper removal (P3), serializer swap (P5), and EventSub (P4).
3. **.NET 10 green (P1) before P2/P3/P4/P5** â€” `TwitchLib.EventSub.Websockets 0.8.0` hard-pins `System.Text.Json 10.0.0` + `Microsoft.Extensions.Logging.Abstractions 10.0.0`; adding it on net7.0 corrupts the dependency graph. P1 is a strict gate for P4.
4. **P3 (dispatcher) before/with P4** â€” EventSub-sourced commands route through the new dispatcher; the audit-log null-safety fix (below) must land before redemptions/bits/subs flow through it.
5. **P1 NRT before P2** â€” `GptResult` nullable members and `ChatResponse.Text?` guards depend on `Nullable` enabled in `twitchBot.csproj` and `Entities.csproj`.
6. **P6 frontend toolchain** is largely independent but its npm scripts (`build`/`lint`/`type-check`/`test`) are consumed by P7 CD; the CORS allow-list change (add Vite dev origin) couples P6â†”backend.

---

## Per-phase task checklist (trackable)

> Each unchecked item is a discrete, assignable task. `[FIX]` = reviewer-driven change from the draft.

### Phase 0 â€” Consolidate + safety net
- [ ] Branch `chore/phase0-monorepo-consolidation`; tag `pre-monorepo-phase0`; confirm both repos clean
- [ ] `git subtree add --prefix=frontend <ui> main` (no `--squash`, history preserved)
- [ ] Remove UI cruft (`frontend/.vs`, `frontend/obj`, `vueapp.esproj*`, `nuget.config`); **keep** `vue.config.js`, `babel.config.js`, `jsconfig.json`, `package-lock.json`, `default.conf.template`, `generate-config.sh`, `Dockerfile`, `heroku.yml`, `.env.example`, `README.md` **[FIX: enumerate kept files]**
- [ ] `git mv` 5 projects + `twitchBot.sln` under `backend/`
- [ ] `git mv` Dockerfiles â†’ `backend.web.Dockerfile` / `backend.worker.Dockerfile`; prefix COPY paths with `backend/`; keep FROM on `7.0`
- [ ] **Add root `.dockerignore`** (`frontend/`, `docs/`, `.git`, `**/bin`, `**/obj`, `**/node_modules`, `**/.env`, `*.md`) **[FIX: blocker â€” repo-root build context]**
- [ ] Rewrite root `heroku.yml`: `web: backend.web.Dockerfile`, `kinobotz: backend.worker.Dockerfile` (keep legacy `kinobotz` process name; rename is P7)
- [ ] Add `backend/global.json` (`{ "sdk": { "version": "7.0.100", "rollForward": "latestFeature" } }`) **[FIX: floor band to avoid setup-dotnet feature-band error]**
- [ ] Verify `dotnet restore/build/test backend/twitchBot.sln -c Release` green on net7.0
- [ ] Verify `docker build -f backend.web.Dockerfile .` and `-f backend.worker.Dockerfile .` build runnable images
- [ ] Add `<ProjectReference Include="..\twitchBot\twitchBot.csproj" />` (transitively Entities) to `backend/kinobotz.Tests/kinobotz.Tests.csproj` **[FIX: blocker â€” tests can't see prod code]**
- [ ] Replace `UnitTest1.cs` with a **pure** characterization test: `CommandFactory.GetChatCommandNames()` / access-level precedence â€” **NOT** `SimplifiedChatMessage` (that DTO is never consumed by `CommandFactory.Build(ChatMessage)`) **[FIX: blocker â€” wrong type/non-pure]**
- [ ] Frontend test stub: add `vitest`, `@vue/test-utils`, `jsdom`, **`@vitejs/plugin-vue`**, and `frontend/vitest.config.js` (`plugins:[vue()]`, `resolve.alias '@'â†’/src`, `environment:'jsdom'`); pin compatible versions (avoid Vitest v4 + jsdom incompat). Remove unused `jest`/`jest-editor-support` from **dependencies** **[FIX: blocker â€” SFC mount needs plugin+config]**
- [ ] Add path-aware `.github/workflows/ci.yml`: backend job (`global-json-file: backend/global.json`), frontend job (Node 22), `dorny/paths-filter@v3` (pinned), `ci-status` aggregation job **[FIX: pin paths-filter; single source SDK]**
- [ ] Capture build-warning/NuGet-advisory baseline in PR (47 warnings incl. CS4014, NU1903 AutoMapper HIGH); keep `TreatWarningsAsErrors=false`, NuGetAudit non-blocking for P0 **[FIX]**
- [ ] Root `README.md`: monorepo layout + local build for each side
- [ ] Open PR; confirm CI green

### Phase 1 â€” .NET 10 framework upgrade
- [ ] Bump `backend/global.json` â†’ `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }`
- [ ] Retarget all 5 csproj to `net10.0`
- [ ] Add `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>` to **`twitchBot.csproj` ONLY** (Entities/Infrastructure/webapi/Tests already have both) **[FIX: major â€” mis-scoped NRT]**
- [ ] Remove `FrameworkReference Microsoft.AspNetCore.App 3.1.0` from `twitchBot.csproj`; replace with versionless `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
- [ ] Remove `Microsoft.EntityFrameworkCore 7.0.8` from **`Entities.csproj` line 11** (NOT Infrastructure) **[FIX: blocker â€” wrong file]**
- [ ] Remove deprecated `Microsoft.AspNetCore.SignalR 1.1.0` from all projects; add versionless `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `Infrastructure.csproj` (it hosts `OverlayHub : Hub`) and `twitchBot.csproj` **[FIX: correct rationale â€” KinobotzService uses HttpClient, not SignalR]**
- [ ] Evaluate/prune dead `services.AddSignalR()` in `twitchBot/Program.cs` (worker has no own hub usage)
- [ ] Realign packages to **10.0.x**: `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.OpenApi`, all `Microsoft.Extensions.*`; **add `AspNet.Security.OAuth.Twitch 7.0.2 â†’ 10.0.0`** **[FIX: major â€” missing auth-critical package]**
- [ ] Pin **all four** Wilson packages to one recent 8.x (â‰¥8.15): `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.JsonWebTokens`, `Microsoft.IdentityModel.Protocols.OpenIdConnect` **[FIX: major â€” only 2 named]**
- [ ] **Remove** `Microsoft.Identity.Web 2.13.1` (zero code usage) and `AspNetCore.Proxy 4.4.0` (dead) **[FIX]**
- [ ] Bump test packages: `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.5`, `Microsoft.NET.Test.Sdk 17.12+`, `coverlet.collector 6.x` **[FIX: current versions]**
- [ ] Bump Serilog/Swashbuckle/Quartz to net10-compatible; remove obsolete `q.UseMicrosoftDependencyInjectionJobFactory()`
- [ ] Resolve restore conflicts to zero NU1605/NU1107
- [ ] Update Dockerfiles FROM â†’ `mcr.microsoft.com/dotnet/{sdk,aspnet}:10.0-noble`; ensure `backend/global.json` is COPYed before `dotnet restore` **[FIX: honor SDK pin in-container]**
- [ ] CI step: `docker build` both Dockerfiles on PR to prove 10.0 tags pull **before** any deploy
- [ ] Run JWT round-trip + JwtBearer pipeline characterization (IdentityModel 8.x: `JsonWebToken` vs `JwtSecurityToken`, case-sensitive `ClaimsIdentity`)
- [ ] Green build + characterization suite unchanged on .NET 10

### Phase 2 â€” AI swap + GPT hardening
- [ ] Add `Microsoft.Extensions.AI 10.7.0` + abstractions + `System.ClientModel 1.8.0` to **`Infrastructure.csproj`** (where `GptChatService` lives) **[FIX: major â€” packages only added to twitchBot in draft]**
- [ ] Add `Microsoft.Extensions.AI.OpenAI 10.7.0` + `OpenAI 2.11.0` to **`twitchBot.csproj`** (where `IChatClient` is constructed) **[FIX: align to 10.7.0]**
- [ ] Remove OkGoDoIt `OpenAI 1.7.2` + all `using OpenAI_API;` / `IOpenAIAPI`
- [ ] Add `OpenAiOptions` (ApiKey, Model=`gpt-4o-mini`, CircuitBreakerCooldownSeconds=60); **bind via `services.Configure<OpenAiOptions>(...)`** and inject `IOptions<OpenAiOptions>` **[FIX: major â€” options never registered]**
- [ ] `IGptChatService`/`GptChatService` (Infrastructure): build messages `[System(behavior if non-empty), System(short-answer pt prompt), User(message){AuthorName=username}]` â€” **use `ChatMessage.AuthorName`, NOT `"{name}: {msg}"` content prefix** **[FIX: blocker â€” wrong parity; OkGoDoIt set the `name` field]**
- [ ] Write the parity characterization against the **OLD** library output (content=message, name=username) before swap **[FIX]**
- [ ] Circuit breaker (singleton state) with injected `TimeProvider`: PERMANENT = 401 OR 429 with body `error.code` **or** `error.type` == `insufficient_quota`; TRANSIENT = 429 rate_limit / 5xx / network; empty/unparseable body â†’ TRANSIENT **[FIX: parse code AND type]**
- [ ] Register `services.AddSingleton(TimeProvider.System)` unconditionally; register `GptChatService` as singleton (breaker state lives in service, not `IChatClient`)
- [ ] Rewrite `GptCommandHandler` + `GenerateRandomStreamTitleCommandHandler`: Success (500/140-char cap), PermanentlyUnavailable+announce-once (friendly pt message), PermanentlyUnavailable+suppress (`WasExecuted=false`/empty), Transient (log only) â€” **never** put technical detail in `Response.Message`
- [ ] Add `Microsoft.Extensions.TimeProvider.Testing` to test project; write breaker/handler tests
- [ ] Document `OPENAI_API_KEY`, `OPENAI_MODEL`, cooldown in `.env.example` + runbook

### Phase 3 â€” Architecture cleanup + idiomatic refactor
- [ ] Inventory MediatR/AutoMapper coupling (record in PR)
- [ ] **Characterization first (green on current code):**
  - [ ] `CommandFactory.Build`: prefix routing, unknownâ†’null, `@kinobotz`â†’GptCommand stripped, access-level **precedence** (Moderator>Broadcaster>Subscriber>Vip>Default; cover multi-role)
  - [ ] `RewardRedeemed` path currently returns **null** (empty `RewardCommands`) â€” lock as-is **[FIX]**
  - [ ] Pipeline: disabledâ†’message+not-executed; access-denied; k1notvâ†’Admin + Admin cooldown bypass; global vs per-user cooldown; success counter+lastExec; `WasExecuted=false` skips counter; audit written **only on non-throwing paths**
  - [ ] **Early-throw paths (null botConnection / null command / null token) propagate out of `Handle()` with NO audit written** â€” assert `IAuditLogRepository.Create` NOT called **[FIX: blocker/major â€” draft wrongly said "audit attempted then rethrow"]**
  - [ ] `LastMessageCommandHandler` empty-`TargetUsername` returns null â†’ base NPEs at line 85 â€” lock or consciously fix **[FIX]**
  - [ ] Nested dispatch: `GptBehaviorCommandHandler` â†’ `GptBehaviorDefinitionCommand` runs the **full** `HandleAsync` pipeline (2nd audit for `gptbehaviordef`, inner cooldown/access), outer forces `WasExecuted=false` **[FIX: major â€” lock full inner pipeline]**
  - [ ] AutoMapper `BotConnectionâ†’BotConnectionDto` field snapshot (assert sensitive fields excluded)
- [ ] `ICommandHandler<TCommand>` (drop cosmetic `in`; DI resolves invariant closed types) + `ICommandDispatcher`
- [ ] `CommandDispatcher` (resolve by `command.GetType()` via cached compiled delegate); **DispatchAsync invokes `HandleAsync` (full base pipeline), never `InternalHandle`** **[FIX]**; note: root provider, safe (no scoped services)
- [ ] Port `BaseCommandHandler` â†’ `CommandHandlerBase<T> : ICommandHandler<T>` verbatim ordering; **make audit log null-safe**: `request.ChatMessage?.Message` + source label (EventSub-sourced commands have null `ChatMessage`) **[FIX: major â€” NRE on redemption/bits/sub path]**
- [ ] Update 13 handlers `: CommandHandlerBase<X>`; `GptBehaviorCommandHandler` uses `ICommandDispatcher`; migrate dead `FirstFollowCommandHandler` to compile but leave unwired
- [ ] Replace `AddMediatR` with explicit `AddTransient<ICommandHandler<X>, XHandler>()` (Ã—13) + `AddSingleton<ICommandDispatcher, CommandDispatcher>()`
- [ ] Replace 4 `IMediator.Send` in `Bot.cs` with `DispatchAsync`; **preserve fire-and-forget (unawaited) semantics at lines 90/120** (subs/bits) â€” flag as latent, do not await here
- [ ] Remove MediatR packages (twitchBot); grep-clean
- [ ] AutoMapper removal: `ToDto()` extension co-located; `ChannelCommands` nullable decision (`List<Command>?` or coalesce to `[]`) reconciled with NRT **[FIX]**; remove from webapi AND unused twitchBot ref; delete `MappingProfiles.cs`; remove `AddAutoMapper`
- [ ] Async fixes: `Bot.RefreshAccessToken` (async void+`.Result`â†’`async Task`+await); `Bot.Connect` `.Result`â†’await; event handlers keep `async void` **with** top-level try/catch (add to StreamUp/Down/ChannelPoints); the two **unawaited `_mediator.Send`** in sub/bits handlers flagged (revenue paths) **[FIX]**; `FirstFollowCommandHandler` `.Result`â†’await; `Orchestrator.Connect` await per-channel
- [ ] Idioms: primary constructors, file-scoped namespaces (solution-wide), twitchBot minimal hosting, records for DTOs, collection expressions, `CommandFactory` switch-expression **preserving exact precedence** **[FIX]**, `IHttpClientFactory` for TTS handler **and** `KinobotzService` (delete unused `new HttpClient` line 29) **[FIX]**, STJ for in-request TTS payload only
- [ ] `TimeProvider` for cooldowns + TTS daily limit (gate behind cooldown decision-path tests); decide warnings-as-errors policy (resolve NRT warnings if on)
- [ ] Post-refactor: dispatcher/base/mapping unit tests; characterization suite unchanged-green

### Phase 4 â€” Twitch EventSub
- [ ] **Drop the abandoned `TwitchLib 3.5.3` metapackage** (latest IS 3.5.3, no bump exists); reference individuals directly: `TwitchLib.Client`, `TwitchLib.Api 3.10.2`, `TwitchLib.EventSub.Websockets 0.8.0` â€” eliminates transitive `TwitchLib.PubSub 3.2.6` **[FIX: blocker â€” bogus "bump metapackage"; 3.10.3 stable doesn't exist]**
- [ ] CI: restore + dump effective package versions before pinning **[FIX]**
- [ ] Characterization (pure, extracted): cheer bits regex `Cheer\d+` strip + `TtsMinimumBitAmount` gate; resub `cumulative_months â‰¥ TtsMinimumResubMonthsAmount` **[FIX: map legacy `Months`â†’`cumulative_months`, documented]**; stream-up exact string incl. `BloodTrail` token; stream-down `'stream ended. Notifying users:'` **[FIX: verbatim strings]**
- [ ] **Decision record: WebSocket vs Webhook transport** â€” EventSub WS is single-broadcaster by design; document deliberate deviation if staying on one shared WS session (multi-broadcaster) **[FIX: blocker â€” understated risk]**
- [ ] Abstractions `ITwitchEventHandler` / `ITwitchAuth`; move event business logic out of socket wiring; dispatch via P3 `CommandDispatcher`
- [ ] `services.AddTwitchLibEventSubWebsockets()` â€” `EventSubWebsocketClient` is **singleton** (`TryAddSingleton`); one shared client owned by `EventSubService`; remove "one per Bot" option **[FIX: blocker â€” wrong lifetime claim]**
- [ ] Subscriptions on `WebsocketConnected && !IsRequestedReconnect`, per active+authorized channel, for: `channel.cheer` v1, `channel.subscribe` v1, `channel.subscription.message` v1, `stream.online` v1, `stream.offline` v1, `channel.channel_points_custom_reward_redemption.add` v1 â€” using **`botConnection.AccessToken`** (broadcaster token), condition `broadcaster_user_id == botConnection.ChannelId` (never the shared bot token) **[FIX: assert token+condition]**; per-channel try/catch isolation; idempotent ensure-subscriptions
- [ ] Route events by `BroadcasterUserId` â†’ connection lookup (shared client)
- [ ] **Redemption is NEW functionality, not "restore parity"** â€” wire `RewardCommands` (currently empty) / EventSub mapping so `'TTS V2'` â†’ `TextToSpeechCommand` (port the real logic from `TextToSpeechCommand.InternalBuild(RewardRedeemed)`); test against desired behavior **[FIX: blocker â€” non-existent baseline]**
- [ ] Add `NeedsReauth`/`LastRefreshError`/`LastRefreshAttemptUtc` to `BotConnection`; resilient refresh moved into Quartz job (no per-connection `System.Timers.Timer`); on failure set `NeedsReauth=true`, log once at Warning, never throw; skip EventSub subs for `NeedsReauth` channels
- [ ] Surface `NeedsReauth` via `BotConnectionDto` + controller for UI re-auth banner
- [ ] Remove `OnPubSubServiceError` "Error on PubSub" handler; downgrade benign `OperationCanceledException`; replace `OnLog` firehose with Debug/Serilog filter
- [ ] Flag `FirstFollowCommandHandler` uses decommissioned `GetUsersFollowsAsync` â€” remove or reimplement against Get Channel Followers **[FIX]**
- [ ] Live validation: k1notv re-auth + 6 subs created (no 401/403); live-fire cheer/resub/stream/redemption; **re-auth a 2nd channel** and confirm both receive events on the shared session **[FIX: multi-channel validation]**; confirm log noise gone; tag prior image for rollback

### Phase 5 â€” Data layer
- [ ] **Re-verify versions on nuget.org** â€” target aligned `12.2.0` for `StackExchange.Redis.Extensions.{Core,AspNetCore,System.Text.Json}`; remove `Newtonsoft`. **No version skew exists** (draft's 11.0.0/12.1.0 numbers are stale) **[FIX: blocker â€” central decision-gate was bogus]**
- [ ] Verify Core 12.2.0 `SearchKeysAsync` (db-number fix) on existing patterns (`botconnection:*:*:*`, `*:*:counter`, `{id}:commands:*:definition`) **[FIX]**
- [ ] Spike: how custom `JsonSerializerOptions` thread through (generic `AddStackExchangeRedisExtensions<SystemTextJsonSerializer>` uses **parameterless ctor + Flexible defaults**) â€” either accept Flexible (already `PropertyNameCaseInsensitive=true`, numeric enums) or register the serializer instance manually **[FIX: major â€” wiring unproven]**
- [ ] Characterization (Testcontainers + current Newtonsoft): golden round-trips for every entity; **deserialization-equivalence** (not byte-identical â€” STJ Flexible omits nulls via `WhenWritingNull` vs Newtonsoft writes nulls) **[FIX: reframed risk]**
- [ ] Capture **live keyspace corpus** (SCAN export) committed as fixture; assert STJ reads real Newtonsoft values
- [ ] `[JsonIgnore]` on `Response.Exception` (round-trips differently, doesn't throw) **[FIX: softened claim]**; STJ ctor-param matching test for `NotifyUsers`/`BehaviorDefinition` (non-parameterless ctors) before record conversion **[FIX]**
- [ ] Counter raw-write (`StringIncrement`) / serialized-read (`GetAllAsync<long>`) asymmetry test **[FIX]**
- [ ] Swap serializer at both `Program.cs` DI points; align/remove Extensions packages (or drop wrapper for plain `StackExchange.Redis` as an idiomatic preference, not a skew escape hatch)
- [ ] Extract repo interfaces to `Abstractions/`; `GetLastExecutionTime` â†’ `Task<DateTime?>` **with** the cooldown decision-path characterization captured first + the `BaseCommandHandler` lines 77â€“94 consumer edit (preserve "absent â‡’ runs now") **[FIX: major â€” behavior-change unprotected; coordinate with P3]**; `IncrementExecutionCounter` â†’ awaited `Task`
- [ ] Centralize keys in `RedisKeys`; move `BehaviorDefinition` to `Entities`
- [ ] `docs/data/redis-key-schema.md` (every family: pattern/type/serializer/TTL/writer-reader/system-of-record)
- [ ] Backup/export `BackgroundService` (worker-only, schedule via `TimeProvider`, SCAN-throttled, **reuse existing `IConnectionMultiplexer`** â€” don't open a new connection against the 30-conn cap) **[FIX]**; restore CLI; document RPO + free-tier no-native-backup gap
- [ ] Testcontainers integration tests for all 4 repos + backup round-trip; wire into CI (Docker job)

### Phase 6 â€” Frontend modernization
- [ ] Characterization snapshot: 8 routes + guards; dead-dep grep confirmation; API endpoint inventory
- [ ] Vite scaffold: `vite ^8`, `@vitejs/plugin-vue ^6`, **`vue ^3.5.x`** (Pinia 3 peer is `vue ^3.5.11`, not 3.4) **[FIX: blocker]**; `vite.config` (alias `@`â†’`/src`, port 8080, `outDir dist`, sourcemap false)
- [ ] Move `public/index.html`â†’`frontend/index.html`; `<%= BASE_URL %>favicon.ico`â†’`/favicon.ico`; **rewrite `href="styles.css"`â†’`/styles.css`** (relative breaks on deep routes after refresh) **[FIX: major]**; add module script; keep `/config.js` in `<head>`
- [ ] Replace `process.env.NODE_ENV`â†’`import.meta.env.DEV` in `main.js`/`CallbackApp.vue`/`TwitchLogin.vue`
- [ ] Centralize config (`config.ts`): `apiUrl`/`twitchClientId`/`twitchRedirectUri` from `window.__ENV__` w/ fallbacks; **fix `main.js` hardcoded SignalR URL â†’ `config.apiUrl + '/overlayHub'`**
- [ ] Remove dead deps: `@aspnet/signalr`, `@dreamonkey/vue-signalr`, `@latelier/vue-signalr`, `bootstrap-vue`, `vue-moment`, `argon-design-system-free`, `core-js`, `jest`, `jest-editor-support`, FA regular/solid, `vue3-easy-data-table` (+ dead `EasyDataTable` registration); keep `@microsoft/signalr` (â†’10.0.0)
- [ ] Vuexâ†’Pinia (`stores/auth.ts`, thin/cookie-backed); fix `CallbackApp` `mapActions`â†’`useAuthStore`
- [ ] ESLint 9 flat config: `eslint ^9`, **`eslint-plugin-vue ^10`** (v14 TS config peer), `@vue/eslint-config-typescript@14.8`, `typescript-eslint ^8`, `typescript ^5`, `vue-tsc`, `@vue/tsconfig`; **scope `vueTsConfigs` to `.ts`/typed SFCs only (use `vueTsConfigs.recommended`, not type-checked) so all-JS SFCs lint without TS** **[FIX: major â€” sequencing/parser conflict]**
- [ ] Dockerfile: `node:22-alpine` build; **CMD `/bin/bash`â†’`/bin/sh`** (alpine has no bash) or use nginx template entrypoint **[FIX: major â€” alpine breaks bash CMD]**; keep `generate-config.sh` + `default.conf.template`; add `wss://*.herokuapp.com` to CSP `connect-src` pre-emptively **[FIX]**; `npm install`â†’`npm ci`
- [ ] Pin `engines.node >=22.12 <23`; `.nvmrc` `22`; regenerate `package-lock.json` on Node 22
- [ ] Incremental TS: `tsconfig` extends `@vue/tsconfig` (`allowJs:true`, `checkJs:false`); new files TS; page-by-page order (leaf utils â†’ simple pages â†’ list pages â†’ **DashboardApp last**); page SFCs live at `src/*App.vue` (only NavBar/TwitchLogin/LoadingSpinner under `src/components/`) **[FIX: path accuracy]**
- [ ] Vitest setup (`environment:'happy-dom'`, `coverage: v8`; fall back to istanbul if SFC lcov BRDA bug bites); `vitest`/`@vitest/coverage-v8` â†’ `^4`; tests for axios interceptor, config, Pinia auth, NavBar guards, Callback, Dashboard, Commands, GptBehavior, Stats, **Overlay (mocked `@microsoft/signalr`)**, TwitchLogin
- [ ] Verify built bundle has **no inline scripts** (CSP `script-src 'self'`) and `/config.js` loads before entry on every route **[FIX]**
- [ ] Rewrite `frontend/README.md`

### Phase 7 â€” Deployment & CI/CD
- [ ] `backend.web.Dockerfile` (.NET 10 `sdk:10.0-noble`â†’`aspnet:10.0-noble`, backend/-prefixed COPY, non-root, no `EXPOSE $PORT`)
- [ ] `backend.worker.Dockerfile` (aspnet base â€” justify via P1 FrameworkReference decision; or prune `AddSignalR` to drop to `runtime` base) **[FIX: pin the cross-phase decision]**
- [ ] **Add root `.dockerignore`** if not already (P0) + remove `<None Update=".env" CopyToOutputDirectory=Always>` so secrets never bake into images **[FIX: major â€” secret-leak]**
- [ ] Root `heroku.yml`: `web` + `worker`; document one-time `heroku ps:scale kinobotz=0 worker=1 -a kinobotz` cutover (avoid duplicate live bot) **[FIX: rename risk]**
- [ ] `ci.yml`: backend build/test, **single coverage gate mechanism** â€” add `coverlet.msbuild` + `/p:Threshold=70 /p:ThresholdType=line` OR ReportGenerator+check; **drop the contradictory `--collect` + `-p:Threshold` pairing** **[FIX: major]**; frontend lint/vitest w/ thresholds; `actionlint`/`hadolint` as explicit jobs
- [ ] `deploy.yml` (push to main): backend via **heroku.yml git-push path** (`akhileshns/heroku-deploy@v3.15.15`, `usedocker=false`); UI via `container:push`/`release` with **pinned exact invocation** (rename to `Dockerfile.web` or `cd frontend && container:push`) **[FIX: ambiguous path]**; path-filtered; concurrency; post-deploy smoke
- [ ] Add `app.MapHealthChecks('/health')` to `webapi/Program.cs`; CD curls `/health` (NOT `/swagger` â€” dev-only) following https redirect **[FIX]**
- [ ] GitHub secrets `HEROKU_API_KEY` (from `heroku authorizations:create`) + `HEROKU_EMAIL`; document config-var names; reconcile existing `frontend/.env.example` (don't clobber) **[FIX]**
- [ ] `heroku config:unset PORT -a kinobotz`
- [ ] Optional Review Apps (`app.json`, container) â€” **scope worker=0** (no second live bot on k1no.tv)
- [ ] Decommission `frightening-cemetery-98205`, `kinobotz-api` (after confirming nothing routes); flag `pixelizer-app` to owner (out of scope, EOL)
- [ ] Bump action versions: `setup-dotnet@v5`, `setup-node@v6` (`node-version-file: frontend/.nvmrc`), `codecov-action@v5` **[FIX]**
- [ ] Pin Docker tags to noble-qualified; deterministic test logger/artifacts

### Phase 8 â€” Documentation
- [ ] `docs/` skeleton + index, each stamped "verified against commit/phase"
- [ ] `architecture.md` w/ 2 Mermaid blocks (flowchart + chat-command sequenceDiagram); describes **modernized** system; **GET /Commands is `[Authorize]`-gated, NOT public** **[FIX: major]**
- [ ] `commands.md` (all 14 + `@kinobotz` mention + event-driven features), per-command "source files" line; flag `%ff` as **unreachable AND backed by removed `GetUsersFollowsAsync`** **[FIX]**; document dead `'TTS V2'` reward path **[FIX]**; k1notv Admin + cooldown bypass + 1000-char/day cap
- [ ] `redis-schema.md` (finalized from P5; STJ note + SCAN/30MB caveat)
- [ ] `runbook.md`: deploy/rollback/re-auth/rotate-keys/OpenAI-billing/monitoring/backup; log-signatures table incl. **both** refresh-token strings + `OnPubSubServiceErrorâ†’RefreshAccessToken` correlation **[FIX]**
- [ ] `contributing.md` both stacks; note **CORS allow-list must add `http://localhost:5173`** (Vite dev origin) **[FIX]**
- [ ] Wire UI Commands page to single source (`commands-reference.json` + enriched columns); fix prefix casing bug; prefix-parity CI test vs `Entities/Commands.cs`
- [ ] Pin verified Mermaid CI validation (markdown-input `mmdc` on temp copy or `render-md-mermaid` action â€” **no in-place `--parse` exists**) **[FIX]**; link-check; cross-links

---

## Phase Details

### Phase 0 â€” Repo consolidation + safety net

**Objective.** Subtree-merge `kinobotz-ui` into `frontend/` preserving its 67-commit history; move the .NET solution + 5 projects under `backend/`; fix every broken path (Dockerfiles, `heroku.yml`, `.gitignore`/`.gitattributes`); keep the solution building on the **current net7.0** TFM; stand up path-aware GitHub Actions CI that builds+tests both stacks. `main` stays shippable â€” Heroku container builds must still resolve their moved Dockerfiles. No CD, no framework bump.

**Ordered steps (with paths).**
1. Pre-flight: `git checkout -b chore/phase0-monorepo-consolidation`; `git tag pre-monorepo-phase0`.
2. `git remote add ui-local <kinobotz-ui>` â†’ `git fetch ui-local main` â†’ `git subtree add --prefix=frontend ui-local main` (no `--squash`) â†’ `git remote remove ui-local`. Verify `git log --follow -- frontend/src/main.js` shows pre-merge history.
3. Remove cruft (`frontend/.vs`, `frontend/obj`, `vueapp.esproj*`, `nuget.config`). **[FIX]** Explicitly preserve `vue.config.js`, `babel.config.js`, `jsconfig.json`, `package-lock.json`, `default.conf.template`, `generate-config.sh`, `Dockerfile`, `heroku.yml`, `.env.example`, `README.md`.
4. `git mv` the 5 projects + `twitchBot.sln` into `backend/`.
5. `git mv webapi.Dockerfile backend.web.Dockerfile`, `git mv twitchBot.Dockerfile backend.worker.Dockerfile`; prefix COPY paths with `backend/`, set `WORKDIR /src/backend/{webapi|twitchBot}`; **keep FROM on 7.0**.
6. **[FIX â€” blocker]** Create root `C:/Users/.../kinobotz/.dockerignore` excluding `frontend/`, `docs/`, `.git`, `**/bin`, `**/obj`, `**/node_modules`, `**/.env`, `*.md`, `.github/` â€” required because both Dockerfiles do `COPY . .` with the build context now at repo root.
7. Rewrite root `heroku.yml`: `web: backend.web.Dockerfile`, `kinobotz: backend.worker.Dockerfile` (legacy process name kept).
8. **[FIX]** Add `backend/global.json` pinned to `7.0.100` + `rollForward: latestFeature`.
9. Verify `dotnet restore/build/test backend/twitchBot.sln -c Release` green; verify `docker build -f backend.web.Dockerfile .` and worker build runnable images (and **do not contain** `frontend/`/`node_modules` â€” exit criterion).
10. **[FIX â€” blocker]** `backend/kinobotz.Tests/kinobotz.Tests.csproj`: add `<ProjectReference Include="..\twitchBot\twitchBot.csproj" />`. Replace `UnitTest1.cs` with a **pure** characterization (`CommandFactory.GetChatCommandNames()` returning the 13 prefixes, or access-level precedence) â€” remove all `SimplifiedChatMessage` mentions (never consumed by `Build(ChatMessage)`).
11. **[FIX â€” blocker]** Frontend: add `vitest`, `@vue/test-utils`, `jsdom`, `@vitejs/plugin-vue` + `frontend/vitest.config.js` (`plugins:[vue()]`, alias `@`â†’`/src`, `environment:'jsdom'`, `globals:true`); pin a known-good matrix (avoid Vitest v4 + jsdom incompat); one smoke test (trivial assertion + simple JS import, defer SFC mount if friction). Remove unused `jest`/`jest-editor-support` from `dependencies`.
12. `.github/workflows/ci.yml`: backend job uses `actions/setup-dotnet@v4` with `global-json-file: backend/global.json` **[FIX â€” single SDK source]**; frontend job Node 22; `dorny/paths-filter@v3` **[FIX â€” pinned]**; `ci-status` aggregation job (`needs: [backend, frontend]`, `if: always()`).
13. **[FIX]** Capture warning/advisory baseline in PR; keep `TreatWarningsAsErrors=false`, NuGetAudit non-blocking; note AutoMapper NU1903 HIGH as extra justification for P3 removal.
14. Root `README.md`; open PR; confirm CI green.

**Package changes.** Frontend add `vitest`/`@vue/test-utils`/`jsdom`/`@vitejs/plugin-vue` (dev); remove `jest`/`jest-editor-support`. New `backend/global.json` (toolchain pin, not NuGet).

**Tests added.** `CommandFactoryTests.cs` (pure characterization); `frontend/src/__tests__/smoke.test.js`. Testcontainers/EventSub/endpoint tests explicitly deferred.

**Risks + mitigations.** `no common commits` warning is harmless (document in PR; tag enables rollback). Dockerfile path edits are the top footgun â†’ `docker build` is an exit criterion, not just `dotnet build`. Worker process-type rename deferred to P7 (avoids re-scale outage). Path-filtered CI false-pass â†’ `ci-status` aggregation required check.

**Exit criteria.** History preserved (log --follow); layout exact (backend/, frontend/, docs/, .github/, root Dockerfiles+heroku.yml+.dockerignore); `dotnet` build+test green on net7.0; both images build with repo-root context and exclude `frontend/`/`node_modules`; CI green on PR; one real backend characterization + â‰¥1 FE test pass; `git status` clean after full build; `main` shippable (TFM unchanged, Heroku paths resolve).

---

### Phase 1 â€” .NET 10 framework upgrade

**Objective.** Retarget all 5 projects net7.0â†’net10.0; align every `Microsoft.Extensions.*`/AspNetCore package to 10.x and the four Wilson/IdentityModel packages to a single 8.x; remove dead refs (bogus `AspNetCore.App 3.1.0`, unused EF Core, deprecated standalone SignalR, unused `Microsoft.Identity.Web`/`AspNetCore.Proxy`); keep behavior preserved on .NET 10 base images. Isolated from MediatR/AutoMapper/OpenAI/TwitchLib (those stay; removed in later phases).

**Ordered steps.**
1. Bump `backend/global.json` â†’ `10.0.100` / `latestFeature`.
2. Retarget 5 csproj to `net10.0`.
3. **[FIX â€” major]** Add `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>` to **`twitchBot.csproj` only**. State that Entities/Infrastructure/webapi/Tests already have both; `BotConnection`'s CS8618 (`ProfileImageUrl`/`Email`) is a **pre-existing** latent warning.
4. Remove `FrameworkReference Microsoft.AspNetCore.App 3.1.0` (twitchBot) â†’ versionless `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
5. **[FIX â€” blocker]** Remove `Microsoft.EntityFrameworkCore 7.0.8` from **`backend/Entities/Entities.csproj` line 11** (verified â€” NOT Infrastructure, whose line 11 is `StackExchange.Redis.Extensions.Core`).
6. Remove `Microsoft.AspNetCore.SignalR 1.1.0` everywhere; add versionless `FrameworkReference` to `Infrastructure.csproj` (hosts `OverlayHub : Hub` / `IHubContext`) and `twitchBot.csproj` (transitive + `AddSignalR()`). Evaluate pruning the worker's `AddSignalR()` as dead. **[FIX â€” correct rationale: `KinobotzService` uses `HttpClient`, not SignalR.]**
7. **[FIX]** Realign to 10.0.x: `JwtBearer`, `OpenApi`, all `Microsoft.Extensions.*`, and **add `AspNet.Security.OAuth.Twitch 7.0.2 â†’ 10.0.0`** (auth/OAuth login package, lockstep with ASP.NET major).
8. **[FIX â€” major]** Pin all four Wilson packages to one recent 8.x (â‰¥8.15): `Microsoft.IdentityModel.Tokens`, `System.IdentityModel.Tokens.Jwt`, `Microsoft.IdentityModel.JsonWebTokens`, `Microsoft.IdentityModel.Protocols.OpenIdConnect` (transitive JwtBearer 10.x can silently resolve a too-low IdentityModel â€” dotnet/aspnetcore #57940/#59210).
9. **[FIX]** Remove unused `Microsoft.Identity.Web 2.13.1` and `AspNetCore.Proxy 4.4.0`.
10. Bump test packages (`xunit 2.9.3`, runner `3.1.5`, Test.Sdk 17.12+, coverlet 6.x); Serilog/Swashbuckle/Quartz net10-compatible; remove obsolete `q.UseMicrosoftDependencyInjectionJobFactory()`.
11. Resolve restore to zero NU1605/NU1107.
12. Dockerfiles FROM â†’ `:10.0-noble`; **[FIX]** COPY `backend/global.json` before `dotnet restore`. CI `docker build` both on PR.
13. JWT round-trip + JwtBearer-pipeline characterization (`JsonWebToken` vs `JwtSecurityToken`, case-sensitive `ClaimsIdentity`; `JwtService` uses `JwtSecurityTokenHandler` directly while pipeline uses `JsonWebToken` â€” test both).
14. Green build + characterization suite unchanged.

**Tests added.** JWT generateâ†’validate round-trip + WebApplicationFactory JwtBearer validation; cooldown unit tests (deferred to P3 TimeProvider); pure helper tests (`StringExtensions`, `ConvertTimestampToDateTime` â€” note it lives in `StringExtensions.cs`).

**Risks + mitigations.** IdentityModel 6â†’8 is highest risk â†’ round-trip test first; grep confirms no `JwtSecurityToken` down-cast in handlers. `Microsoft.Identity.Web` conflict â†’ removed outright. NRT warning wave on twitchBot â†’ stage, use `required`/`?` truthfully. `FirstFollowCommandHandler.GetUsersFollowsAsync` is already broken vs live Twitch (endpoint removed) â€” await refactor still valid, real fix is P4.

**Exit criteria.** 5 projects net10.0; `global.json` 10.0.100; zero NU1605/NU1107; dead refs gone; all four Wilson packages on one 8.x; both images build green on `:10.0-noble`; characterization unchanged-green.

---

### Phase 2 â€” AI swap + GPT hardening

**Objective.** Replace OkGoDoIt `OpenAI 1.7.2` with `Microsoft.Extensions.AI` `IChatClient` (OpenAI provider) behind an `IGptChatService`; preserve `%gpt` (per-channel behavior + pt short-answer prompt + 500-char cap) and `%rtitle` (distinct prompt + 140-char cap + `ModifyChannelInformationAsync`); add the Â§5.2 hardening (permanent vs transient classification + announce-once friendly message + circuit breaker) so a dead key/quota neither spams chat nor burns calls.

**Ordered steps.**
1. **[FIX â€” major]** Add `Microsoft.Extensions.AI 10.7.0` + abstractions + `System.ClientModel 1.8.0` to **`Infrastructure.csproj`** (where `GptChatService` lives). Add `Microsoft.Extensions.AI.OpenAI 10.7.0` + `OpenAI 2.11.0` to **`twitchBot.csproj`** (where the `IChatClient` is constructed). Remove OkGoDoIt + `using OpenAI_API;`.
2. `OpenAiOptions`; **[FIX â€” major]** `services.Configure<OpenAiOptions>(...)`, inject `IOptions<OpenAiOptions>`. New `OPENAI_MODEL` key (default `gpt-4o-mini`).
3. **[FIX â€” blocker]** `IGptChatService`/`GptChatService`: build `[System(behavior if non-empty), System(pt short-answer), User(message){AuthorName=username}]`. Use `ChatMessage.AuthorName` (maps to OpenAI `name` field) â€” **not** a `"{name}: {msg}"` content prefix. Write the parity characterization against the OLD library's actual output (content unchanged, `name`=username) before swapping.
4. Circuit breaker in singleton with injected `TimeProvider`: PERMANENT = 401 OR 429 where body `error.code` **or** `error.type` == `insufficient_quota`; TRANSIENT = 429 rate_limit / 5xx / network / empty-or-unparseable body; `OperationCanceledException` rethrown. **[FIX]** While open, return immediately without calling OpenAI; announce once.
5. DI: `AddChatClient(_ => new OpenAI.Chat.ChatClient(model, key).AsIChatClient())` (singleton by default); `AddSingleton<IGptChatService, GptChatService>()`; `AddSingleton(TimeProvider.System)` unconditionally. webapi needs no AI registration.
6. Rewrite both handlers mapping outcomes (Success cap; PermanentlyUnavailable+announce friendly pt message once; +suppress `WasExecuted=false`/empty so `Bot.ExecuteCommand`'s `!IsNullOrEmpty` guard sends nothing; Transient logs only). Title handler skips `ModifyChannelInformationAsync` on PermanentlyUnavailable.
7. Tests with `Microsoft.Extensions.TimeProvider.Testing` + hand-rolled `FakeChatClient` (mock the **interface** method `GetResponseAsync(IEnumerable<ChatMessage>, ChatOptions?, CancellationToken)`, not the string extension). **[FIX]**
8. Docs/`.env.example`/runbook updates.

**Tests added.** Success+caps; behavior-prompt-only-when-non-empty; `AuthorName` parity; 401 + 429/insufficient_quota â†’ permanent+breaker-open+announce-once; 429/rate_limit â†’ transient; breaker short-circuit (client invoked once across N); cooldown expiry via FakeTimeProvider; title handler skips Twitch API when unavailable; technical detail never in `Response.Message`.

**Risks + mitigations.** Package-id collision (official `OpenAI` â‰  OkGoDoIt) â†’ delete old ref + fix usings. 429-vs-429 misclassification â†’ parse code AND type + tests. Wrong lifetime â†’ singleton + invoked-once test. NRT precondition (`twitchBot.csproj`/`Entities.csproj` `Nullable` enabled) confirmed in P1 â€” exit-criterion check.

**Exit criteria.** OkGoDoIt gone; solution builds on the new AI stack; both handlers route through one `IChatClient` via `IGptChatService` preserving prompts+caps; permanent failures announce â‰¤1Ã—/window and make no API call while open; transients don't trip the breaker; auto-reset after cooldown; tests green.

---

### Phase 3 â€” Architecture cleanup + idiomatic refactor

**Objective.** Replace MediatR with `ICommandHandler<TCommand>` + thin DI `CommandDispatcher`; replace AutoMapper with explicit mapping; fix async anti-patterns; apply modern-C# idioms + `TimeProvider` â€” all behavior-preserving, with characterization green **before** touching production code.

**Ordered steps.**
1. Inventory coupling (PR note).
2. **Characterization (green on current code):** as in the checklist, with these corrected expectations:
   - **[FIX â€” blocker/major]** Early-throw paths (null botConnection L45â€“48 / null command L52â€“55 / null token L57â€“60) **propagate out of `Handle()` with NO audit written** (verified: `Run()` rethrows at L104; `Handle()` awaits before the audit try at L112). Assert `IAuditLogRepository.Create` is **not** called and the exception reaches `Bot.ExecuteCommand`'s catch.
   - Audit-always-written applies **only** to non-throwing paths (disabled/access-denied/cooldown/success).
   - **[FIX â€” major]** Nested dispatch runs the **full** `GptBehaviorDefinitionCommandHandler.HandleAsync` pipeline (2nd audit, inner cooldown/access), outer forces `WasExecuted=false`.
   - **[FIX]** `RewardRedeemed` currently returns null (empty `RewardCommands`) â€” lock as-is.
   - **[FIX]** `LastMessageCommandHandler` empty-`TargetUsername` returns null â†’ base NPE at L85 â€” lock or consciously fix.
3. `ICommandHandler<TCommand>` (drop cosmetic `in`; MS DI resolves invariant closed types) + `ICommandDispatcher`.
4. `CommandDispatcher` resolves by `command.GetType()` via cached compiled delegate; **DispatchAsync â†’ `HandleAsync` (full pipeline)**, never `InternalHandle`; exceptions propagate unwrapped. Note: resolves from root provider (safe â€” no scoped services).
5. Port base â†’ `CommandHandlerBase<T>` verbatim ordering (existsâ†’tokenâ†’enabledâ†’accessâ†’cooldownâ†’executeâ†’counter; audit wraps in `HandleAsync`). **[FIX â€” major]** Make audit null-safe: `request.ChatMessage?.Message` + synthetic source label for EventSub-sourced commands (null `ChatMessage`); add test.
6. Update 13 handlers; `GptBehaviorCommandHandler` â†’ `ICommandDispatcher`; dead `FirstFollowCommandHandler` compiles, stays unwired.
7. DI: explicit `AddTransient<ICommandHandler<X>, XHandler>()` (Ã—13) + `AddSingleton<ICommandDispatcher, CommandDispatcher>()`; remove `AddMediatR`.
8. `Bot.cs`: 4 `Send`â†’`DispatchAsync`; **preserve unawaited fire-and-forget at L90/L120** (subs/bits) â€” flag the two unawaited revenue-path dispatches as latent (P4 owns).
9. Remove MediatR packages; grep-clean (`MediatR|IMediator|IRequest|IRequestHandler|.Send(`).
10. AutoMapper removal: `ToDto()` extension; **[FIX]** reconcile `ChannelCommands` NRT (`List<Command>?` or coalesce `[]`) â€” decide and lock with snapshot; remove from webapi + unused twitchBot ref; delete `MappingProfiles.cs`; remove `AddAutoMapper`.
11. Async fixes (verified line refs): `Bot.RefreshAccessToken` (~L358/366 async void+`.Result`â†’async Task+await); `Bot.Connect` (~L147/154 `.Result`â†’await + safe timer); add try/catch to StreamUp/Down/ChannelPoints handlers; **[FIX]** flag the two unawaited `_mediator.Send` (sub L90 TTS, bits L120 TTS); `FirstFollowCommandHandler` L38/54 `.Result`â†’await; `Orchestrator.Connect` await per-channel.
12. Idioms: primary constructors; file-scoped namespaces solution-wide; twitchBot minimal hosting; records for DTOs; collection expressions; **[FIX]** `CommandFactory` access-level switch-expression **preserving exact if/else precedence** (covered by multi-role characterization); **[FIX]** `IHttpClientFactory` for TTS handler **and** `KinobotzService` (delete unused `new HttpClient` L29); STJ for the in-request TTS payload **only** (Redis serializer stays Newtonsoft until P5).
13. `TimeProvider` for cooldowns + TTS daily limit (gate behind cooldown decision-path tests). Decide warnings-as-errors policy; if on, resolve NRT warnings this phase.
14. Post-refactor unit tests; characterization unchanged-green.

**Tests added.** Characterization (corrected) + `CommandDispatcherTests` (resolution/missing-throws/CancellationToken/exception-propagation) + `CommandHandlerBaseTests` + `ToDto` field-by-field vs snapshot + EventSub-sourced (null ChatMessage) audit no-throw + every handler `InternalHandle`.

**Risks + mitigations.** Cooldown/access ordering drift â†’ verbatim port + characterization first. Nested-dispatch full-pipeline â†’ explicit test. Fire-and-forget byte-for-byte preserved. Sensitive-field exclusion is a security property â†’ snapshot asserts it. Per-call reflection â†’ cached delegate.

**Exit criteria.** Zero MediatR/AutoMapper references; dispatcher+13 handlers+nested dispatch run through the new abstraction with identical ordering/messages; explicit `ToDto()` field-identical (sensitive fields excluded); all characterization green before and after; zero `async void` except event handlers (each with try/catch); zero blocking `.Result`/`.Wait()` in `Bot.cs`/`FirstFollowCommandHandler`/Orchestrator connect path.

---

### Phase 4 â€” Twitch modernization (EventSub)

**Objective.** Restore the bits/subs/stream/redemption event path (dead since Twitch decommissioned PubSub 2025-04-14) by migrating to EventSub over WebSocket on the current TwitchLib stack; make per-channel token refresh resilient (`NeedsReauth`); remove the mislabeled "Error on PubSub" + throttler-cancellation log noise. This is a **functional restore**, validated live on k1notv.

**Ordered steps.**
1. **[FIX â€” blocker]** Drop the `TwitchLib 3.5.3` metapackage (no newer version exists; it transitively pins PubSub). Reference individuals: `TwitchLib.Client`, `TwitchLib.Api 3.10.2` (latest **stable** â€” 3.10.3 stable does not exist), `TwitchLib.EventSub.Websockets 0.8.0`. CI restore-and-dump effective versions before pinning.
2. **[FIX]** Note hard deps: `Microsoft.Extensions.Logging.Abstractions 10.0.0`, `System.Text.Json 10.0.0`, `TwitchLib.EventSub.Core 3.0.0` â€” MUST be after P1 (net10.0). Strict gate.
3. Characterization (pure, extracted into `TwitchEventHandler`): bits regex strip + amount gate; resub `cumulative_months` gate (legacy `Months`â†’`cumulative_months`, documented); stream-up exact string incl. `BloodTrail`; stream-down `'stream ended. Notifying users:'`. **[FIX â€” verbatim strings]**
4. **[FIX â€” blocker]** Transport decision record: WS is single-broadcaster by design; document deliberate one-shared-session multi-broadcaster deviation (or choose Webhook). Validate with a 2nd channel before declaring done.
5. Abstractions `ITwitchEventHandler`/`ITwitchAuth`; logic out of socket wiring; dispatch via P3 `CommandDispatcher`.
6. `services.AddTwitchLibEventSubWebsockets()` â€” **`EventSubWebsocketClient` is singleton** (`TryAddSingleton`); one shared client in `EventSubService`. **[FIX â€” blocker: remove "one per Bot" claim.]**
7. Subscriptions on `WebsocketConnected && !IsRequestedReconnect`, per active+authorized channel, 6 types, each with **`botConnection.AccessToken`** and `broadcaster_user_id == botConnection.ChannelId` (never the shared bot token) **[FIX]**; per-channel try/catch isolation; idempotent ensure.
8. Route by `BroadcasterUserId`â†’connection.
9. **[FIX â€” blocker]** Redemption is **new** functionality: wire `RewardCommands`/EventSub mapping so `'TTS V2'`â†’`TextToSpeechCommand` (port `TextToSpeechCommand.InternalBuild(RewardRedeemed)` logic); test desired behavior, not "parity".
10. Resilient refresh: add `NeedsReauth`/`LastRefreshError`/`LastRefreshAttemptUtc` to `BotConnection`; refresh in Quartz job (no per-connection `System.Timers.Timer`); failure â†’ `NeedsReauth=true`, Warning-once, no throw; skip subs for `NeedsReauth`.
11. Surface `NeedsReauth` via DTO/controller.
12. Remove `OnPubSubServiceError`; downgrade benign cancellation; replace `OnLog` firehose.
13. **[FIX]** Flag `FirstFollowCommandHandler.GetUsersFollowsAsync` (removed endpoint) â€” remove or reimplement against Get Channel Followers.
14. Live validation: k1notv re-auth â†’ 6 subs (no 401/403); live-fire cheer/resub/stream/redemption; **2nd-channel multi-broadcaster check**; log noise gone; rollback image tagged.

**Tests added.** Eventâ†’command mapping parity; redemption (new) mapping; token-refresh isolation + `NeedsReauth`; subscription isolation + idempotency + token/condition assertion; orchestrator per-channel resilience; log-noise (type removed).

**Risks + mitigations.** EventSub WS open-beta â†’ thin abstraction. 0.8.0 namespace move â†’ IntelliSense, not stale samples. Session-scoped subs â†’ idempotent ensure on connect + after refresh-job pass. Multi-broadcaster on one WS session is the documented-against path â†’ explicit decision + 2nd-channel validation. Stale-token channels surface as `NeedsReauth` (operator action). Live cutover on production k1notv â†’ green suite + rollback + staged checklist.

**Exit criteria.** Builds on .NET 10; zero `TwitchLib.PubSub` (package excluded, not just `using`-free); all P4 tests green; k1notv shows WebsocketConnected + 6 subs; live-fire restores bits/resub/stream/redemption; `NeedsReauth` isolates bad channels (k1notv keeps working); `NeedsReauth` exposed to UI; no async void/`.Result` in `Bot.RefreshAccessToken`/`Connect`; logs clean; 2nd channel validated.

---

### Phase 5 â€” Data layer (Redis)

**Objective.** Move Redis serialization Newtonsoftâ†’System.Text.Json without corrupting existing data; typed repository interfaces; documented key schema; application-level periodic backup (free 30 MB tier has **no** native backups â€” the export job IS durability); Testcontainers integration tests. Stay Redis-only.

**Ordered steps.**
1. **[FIX â€” blocker]** Re-verify on nuget.org: target aligned `12.2.0` for `StackExchange.Redis.Extensions.{Core,AspNetCore,System.Text.Json}`; remove Newtonsoft. **No version skew** (draft's 11.0.0/12.1.0 was stale). Keep "drop the wrapper for plain `StackExchange.Redis`" only as an idiomatic preference, not a skew escape hatch.
2. **[FIX]** Verify Core 12.2.0 `SearchKeysAsync` (db-number fix) on existing patterns.
3. **[FIX â€” major]** Spike the custom-`JsonSerializerOptions` wiring: the generic `AddStackExchangeRedisExtensions<SystemTextJsonSerializer>` uses the parameterless ctor + library `Flexible` defaults (already `PropertyNameCaseInsensitive=true`, numeric enums). Either accept Flexible or register the serializer instance manually.
4. **[FIX â€” reframed]** Characterization (Testcontainers + current Newtonsoft): golden round-trips for every entity; assert **deserialization-equivalence** (Flexible `WhenWritingNull` omits nulls vs Newtonsoft writes them â€” not byte-identical).
5. Capture **live keyspace corpus** (SCAN export, committed fixture); assert STJ reads real Newtonsoft values.
6. `[JsonIgnore]` on `Response.Exception` (STJ doesn't round-trip it; doesn't throw) **[FIX â€” softened]**; STJ ctor-param-matching test for `NotifyUsers`/`BehaviorDefinition` (non-parameterless ctors) before any record conversion **[FIX]**.
7. **[FIX]** Counter raw-write/serialized-read asymmetry test (`StringIncrement` â†’ `GetAllAsync<long>`).
8. Swap serializer at both `Program.cs`; align/remove Extensions packages.
9. Extract interfaces to `Abstractions/`; **[FIX â€” major]** `GetLastExecutionTime`â†’`Task<DateTime?>` **with** the cooldown decision-path characterization captured first + the `BaseCommandHandler` L77â€“94 consumer edit (preserve "absent â‡’ runs now") â€” coordinate with P3 (defer to P3 if BaseCommandHandler already open); `IncrementExecutionCounter`â†’awaited `Task`.
10. `RedisKeys` builders; move `BehaviorDefinition`â†’Entities.
11. `docs/data/redis-key-schema.md`.
12. **[FIX]** Backup/export `BackgroundService` (worker-only, `TimeProvider`-scheduled, SCAN-throttled, **reuse existing `IConnectionMultiplexer`**, forbid `KEYS`); restore CLI; document RPO + free-tier gap; note `allowAdmin=true` footgun.
13. Testcontainers integration tests (all 4 repos + backup round-trip); CI Docker job; coverage contributes.

**Tests added.** Golden + live-corpus deserialization; AuditLog+Exception round-trip; per-repo integration (lookups, grabCommands, counters global vs scoped, lastexecution global/per-user, custom commands, dual audit-set writes, GPT current/hist scoped+global); counter asymmetry; backup round-trip (strings/sets/TTLs); `TimeProvider`-driven backup schedule; `GetLastExecutionTime` null when absent.

**Risks + mitigations.** Serializer data corruption â†’ golden + live-corpus before cutover; Flexible already case-insensitive + numeric enums. Free tier no native backups â†’ export job WITH alerting + documented RPO + paid-plan path. Unscoped global keys (`lm:`, `ff:`, `*:counter`, `auditlog`) are a multi-tenant reality the schema doc surfaces â€” do NOT change key shapes (would orphan data). 30-conn/100-ops cap â†’ reuse multiplexer + SCAN throttle. Testcontainers Docker dependency â†’ shared container per collection, pinned image.

**Exit criteria.** All Redis serialization via STJ (no Newtonsoft registered); live-corpus deserializes with no field loss; interfaces in `Abstractions/`; NRT-clean with `Task<DateTime?>` + awaited counter; key-schema doc matches `RedisKeys`; worker-only backup uploads restorable snapshot + tested restore + documented RPO; integration tests green in CI; bot+webapi read pre-existing Redis data correctly.

---

### Phase 6 â€” Frontend modernization

**Objective.** Vue CLI/webpackâ†’Vite; Vuexâ†’Pinia; ESLint 7â†’9 flat config; incremental TypeScript; collapse SignalR libs to `@microsoft/signalr`; strip dead deps; pin Node 22; centralize API base URL + fix the hardcoded SignalR URL; update nginx/Dockerfile for Vite `dist/`; Vitest coverage. Stays a separate static nginx Heroku app, behavior-preserving.

**Ordered steps.** (full list in checklist; key corrections:)
- **[FIX â€” blocker]** Bump `vue ^3.4`â†’**`^3.5.x`** (Pinia 3 peer `vue ^3.5.11`), same PR as Vite + Pinia; verify the peer graph (vue 3.5 / pinia 3 / plugin-vue 6 / vue-tsc 3 / @vue/test-utils 2).
- **[FIX â€” major]** `index.html`: rewrite `href="styles.css"`â†’`/styles.css` (relative 404s on deep routes); preview-check styles on `/overlay/<id>` + `/dashboard` after hard refresh.
- **[FIX â€” major]** ESLint flat config: use `vueTsConfigs.recommended` (non type-checked) scoped to `.ts`/typed SFCs only so all-JS SFCs lint cleanly; layer type-aware config with/after the TS migration. `eslint-plugin-vue ^10` (v14 TS config peer).
- **[FIX â€” major]** Dockerfile CMD `/bin/bash`â†’`/bin/sh` (alpine) or nginx template entrypoint; pre-emptively add `wss://*.herokuapp.com` to CSP `connect-src`; `npm install`â†’`npm ci`; `node:22-alpine`.
- **[FIX]** Pin `vitest`/`@vitest/coverage-v8` `^4`; `@microsoft/signalr` `10.0.0`.
- **[FIX]** Page SFCs live at `src/*App.vue` (only NavBar/TwitchLogin/LoadingSpinner under `src/components/`).
- **[FIX]** Verify built bundle has no inline scripts (CSP `script-src 'self'`) + `/config.js` loads before entry on every route.
- Centralize config; fix `main.js` hardcoded SignalR URLâ†’`config.apiUrl + '/overlayHub'`; `process.env.NODE_ENV`â†’`import.meta.env.DEV` in all three files.

**Tests added.** axios interceptor; config; Pinia auth store; NavBar guards; CallbackApp OAuth flow; DashboardApp (dynamic form/getFieldType/readonly/PUT/channelCommands clamp); CommandsApp; GptBehaviorApp (pt-BR formatting/filter); StatsApp; OverlayApp (mocked `@microsoft/signalr` decode/queue/reconnect); TwitchLogin URL build. Optional Playwright OAuthâ†’dashboard smoke (deferred to P7, not in gate).

**Risks + mitigations.** Vue/Pinia peer mismatch â†’ bump to 3.5 + verify graph. `process.env` under Vite undefined â†’ convert all three. SignalR overlay is the only realtime path validated live â†’ OverlayApp test + manual live smoke before merge. Vite doesn't type-check on build â†’ CI runs `vue-tsc --noEmit` separately. Lockfile peer conflicts â†’ align to `@vue/eslint-config-typescript` expectations. Incremental `checkJs:false` false safety â†’ require new/migrated files be TS.

**Exit criteria.** Clean `npm ci` on Node 22 with dead deps removed + new toolchain; `vite build` emits `dist/` + hashed assets; `preview` serves all 8 routes behavior-identical (incl. styles on deep refresh); `lint` + `vue-tsc --noEmit` clean; Vuex gone, Pinia auth works; only `@microsoft/signalr` (â‰¥10) for realtime, overlay works against live `/overlayHub` via centralized config; Dockerfile pins `node:22-alpine`, builds from `frontend/`, container smoke passes (routes + CSP + wss); `engines.node`+`.nvmrc`; Vitest coverage meets agreed FE threshold; TS incremental (tsconfig `allowJs`/`checkJs:false`, new files TS, DashboardApp last).

---

### Phase 7 â€” Deployment & CI/CD

**Objective.** One Heroku container app `kinobotz` running `web`+`worker` from `heroku.yml` on .NET 10 base images with repo-root Dockerfiles; rename legacy `kinobotz` process type â†’ `worker`; UI as separate static app from `frontend/`; GitHub Actions CI (build+test+coverage gate) + CD (deploy on merge); secrets via `HEROKU_API_KEY`; decommission dormant/EOL apps; remove stray `PORT` config var.

**Ordered steps.** (full list in checklist; key corrections:)
- `backend.web.Dockerfile`/`backend.worker.Dockerfile` (`:10.0-noble`, backend/-prefixed, non-root). **[FIX]** Pin the worker's ASP.NET dependency decision from P1 (versionless FrameworkReference or prune `AddSignalR` â†’ smaller `runtime` base).
- **[FIX â€” major]** Root `.dockerignore` (if not from P0) + remove `<None Update=".env" CopyToOutputDirectory=Always>` so secrets never bake into images.
- Root `heroku.yml` web+worker; document one-time `heroku ps:scale kinobotz=0 worker=1 -a kinobotz` cutover (avoid double live bot).
- **[FIX â€” major]** `ci.yml`: ONE coverage mechanism â€” add `coverlet.msbuild` + `/p:CollectCoverage=true /p:Threshold=70 /p:ThresholdType=line /p:ThresholdStat=total` OR ReportGenerator+check; **drop the `--collect` + `-p:Threshold` contradiction** (the collector path can't gate; current csproj only has `coverlet.collector`). `actionlint`/`hadolint` as explicit jobs.
- `deploy.yml`: backend via **heroku.yml git-push path** (`akhileshns/heroku-deploy@v3.15.15`, `usedocker=false`); UI via `container:push`/`release` with **pinned exact invocation** (`Dockerfile.web` naming or `cd frontend && heroku container:push web`); path-filtered; concurrency; post-deploy smoke.
- **[FIX]** Add `app.MapHealthChecks('/health')` to `webapi/Program.cs`; CD curls `/health` (Swagger is dev-only) following the https redirect.
- **[FIX]** Action versions: `setup-dotnet@v5`, `setup-node@v6` (`node-version-file: frontend/.nvmrc`), `codecov-action@v5`.
- **[FIX]** Reconcile existing `frontend/.env.example` (don't clobber).
- `heroku config:unset PORT -a kinobotz`; optional Review Apps scoped `worker=0`; decommission `frightening-cemetery-98205` + `kinobotz-api` (after confirming routing); flag `pixelizer-app` to owner.

**Tests added.** CI `docker build` smoke (both Dockerfiles, proves 10.0 tags pull); CD `/health` 200 smoke; worker liveness (`heroku ps` up); coverlet.msbuild gate; `actionlint`; optional `hadolint`.

**Risks + mitigations.** Build-context COPY paths (repo root) â†’ CI docker-build smoke. Two deploy paths â†’ backend MUST use git-push (`usedocker=false`). Worker rename â†’ mandatory one-time scale-down (else duplicate live bot). Live k1no.tv bot â†’ Review Apps `worker=0`. Redis single free add-on â†’ Testcontainers in CI, never point review/parallel deploys at prod. `HEROKU_API_KEY` long-lived â†’ dedicated revocable token, never echoed. Chiseled has no shell â†’ default `aspnet:10.0-noble`, chiseled only after smoke.

**Exit criteria.** Both Dockerfiles build green in CI from repo root; `heroku.yml` web+worker; deploy brings up both dynos, `/health` 200, old `kinobotz` dyno scaled to 0; UI deploys separately from `frontend/` with runtime `window.__ENV__`; `ci.yml` fails PRs below 70% backend + FE threshold, actionlint passes; `deploy.yml` path-filtered + concurrency + smoke; secrets configured + config-var names documented + `.env.example` per app; `PORT` unset; dormant apps destroyed; runtimes pinned; Review Apps decision recorded; .NET 10 FROM-flip lands in/after P1.

---

### Phase 8 â€” Documentation + UI Commands wiring

**Objective.** Complete `docs/` (architecture w/ Mermaid, full feature/command reference from actual handlers, Redis key schema, runbook, contributing) reflecting the **post-modernization** state; wire the feature reference into the UI Commands page from a single source of truth.

**Ordered steps.** (full list in checklist; key corrections:)
- `docs/` skeleton stamped with verified commit/phase.
- `architecture.md`: 2 Mermaid blocks (component flowchart + chat-command sequenceDiagram); modernized topology. **[FIX â€” major]** API surface: **GET /Commands is `[Authorize]`-gated, NOT public** (verified: class-level `[Authorize]`; UI redirects when no jwt cookie). The public surface is `/twitch/login` + `PublicController`.
- `commands.md`: all 14 + `@kinobotz` mention + event-driven features, each with a "source files" line; **[FIX]** `%ff` flagged as **unreachable from chat AND backed by removed `GetUsersFollowsAsync`**; **[FIX]** document the dead `'TTS V2'` reward path (empty `RewardCommands`); k1notv Admin + cooldown bypass + 1000-char/day cap; cooldown in minutes + global-vs-per-user.
- `redis-schema.md` (finalized from P5; STJ note + SCAN/30MB caveat).
- `runbook.md`: deploy/rollback/re-auth/rotate-keys/OpenAI-billing/monitoring/backup; **[FIX]** log-signatures table lists **both** refresh-token strings (`'Error when trying to refresh access token'` Connect L168 + `'{Login} - Error occurred trying to refresh access token.'` RefreshAccessToken L379) and the `OnPubSubServiceError â†’ RefreshAccessToken` correlation.
- `contributing.md` both stacks; **[FIX]** note backend CORS allow-list must add `http://localhost:5173` (Vite dev origin; current allows only `https://k1no.tv` + `http://localhost:8080`).
- Wire Commands page to `commands-reference.json` (enriched columns: Prefix/Syntax/Access/Cooldown/Description/Example); fix prefix-casing bug; CI prefix-parity test vs `Entities/Commands.cs DefaultCommands`.
- **[FIX]** Mermaid CI validation via markdown-input `mmdc` on a throwaway copy or `render-md-mermaid` action (no in-place `--parse` exists); markdown link-check; reciprocal cross-links.

**Tests added.** Docs link-check; prefix-parity (xUnit or Vitest); Mermaid parse smoke; Vitest CommandsApp render (merges live GET /Commands + static manifest); WebApplicationFactory test asserting GET /Commands returns enriched shape and **requires JWT (401 without token, 200 with)**.

**Risks + mitigations.** Docs describe a moving target â†’ P8 depends on 0â€“7; re-read modernized handlers before finalizing; stamp commit/phase. `%ff`/reward discrepancies could be silently fixed earlier â†’ verify `CommandFactory`/`RewardCommands` at finalization. Drift â†’ single manifest + prefix-parity CI test. Mermaid silent-fail â†’ CI parse check + branch preview. Secrets in docs â†’ names/placeholders only.

**Exit criteria.** All 5 docs render on GitHub; architecture has working Mermaid flowchart + sequenceDiagram and describes the modernized system (correct auth on GET /Commands); commands.md complete with source-file lines + the `%ff` + reward discrepancies flagged; redis-schema enumerated; runbook complete incl. both log signatures; contributing covers both stacks + CORS note; Commands page renders enriched reference from the shared source with the casing bug fixed; link-check + prefix-parity + Mermaid checks green in CI.

---

## Open Risks / Things to Verify During Execution

Distilled from the reviews, weighted toward low-confidence and live-only items:

1. **EventSub WebSocket multi-broadcaster (P4, highest)** â€” TwitchLib/Twitch steer multi-broadcaster to **Webhook**; the plan uses one shared WS session keyed by broadcaster. The single-channel k1notv cutover **masks** this. **Must validate â‰¥2 re-authed channels on the shared session before sign-off**, and keep Webhook as the fallback decision. Per-(client_id,user_id) limits (max_total_cost 10, 3 sessions, 300 subs) need watching as channels re-auth.
2. **Custom `JsonSerializerOptions` wiring (P5)** â€” unproven that the generic `AddStackExchangeRedisExtensions<SystemTextJsonSerializer>` threads custom options (it uses parameterless ctor + `Flexible`). **Spike before committing**; the live-data corpus deserialization test is the real safety gate, not byte-equality.
3. **IdentityModel 6â†’8 auth behavior (P1)** â€” case-sensitive `ClaimsIdentity` + `JsonWebToken` vs `JwtSecurityToken`. `JwtService` uses `JwtSecurityTokenHandler` directly while the pipeline uses `JsonWebToken`. Verify with both a round-trip and a WebApplicationFactory pipeline test; confirm all four Wilson packages resolve to one 8.x with zero NU1107.
4. **TwitchLib effective versions (P4)** â€” pin only after a CI restore-and-dump; `TwitchLib.Api` latest **stable is 3.10.2** (3.10.3 is preview). Confirm `Helix.EventSub.CreateEventSubSubscriptionAsync` honors the per-channel `accessToken` arg in 3.10.2.
5. **Redemption is new, not a restore (P4)** â€” `RewardCommands` is empty today and `CreateClipCommand.InternalBuild(RewardRedeemed)` throws `NotImplementedException`; the only real mapping lives (unreachable) in `TextToSpeechCommand.InternalBuild(RewardRedeemed)`. Build against desired behavior; do not characterize "returns null".
6. **Cooldown semantics change (P5â†”P3)** â€” `GetLastExecutionTime`â†’`Task<DateTime?>` changes `BaseCommandHandler` L77â€“94 (today MinValue â‡’ always runs). Capture the cooldown **decision-path** characterization first and preserve "absent â‡’ runs now"; sequence the consumer edit with whichever phase owns BaseCommandHandler.
7. **Audit-log behavior on early throw (P3)** â€” confirmed: early failures throw out of `Handle()` with **no audit written** (not "audit then rethrow"). Characterize the actual behavior; the line-121 `request.ChatMessage.Message` NRE for EventSub-sourced commands must be null-safed before redemptions/bits/subs route through the dispatcher.
8. **Coverage gate mechanism (P7)** â€” the collector (`--collect`) cannot fail-under; `-p:Threshold` needs `coverlet.msbuild` (current csproj has only `coverlet.collector`). Pick one and bump coverlet to a .NET 10-compatible 6.x. **Confirm the 70% backend starting threshold with Gabriel (spec Open Question Â§11).**
9. **Vitest v8 SFC lcov BRDA NaN bug (P6)** â€” may break Codecov; fall back to istanbul provider. Verify the vue 3.5 / pinia 3 / plugin-vue 6 / vue-tsc 3 / @vue/test-utils 2 peer graph resolves on the regenerated Node 22 lockfile.
10. **CSP for SignalR over wss (P6)** â€” pre-emptively add `wss://*.herokuapp.com`; the overlay is the only realtime path and is validated against the **live** API â€” make the OverlayApp live-smoke a required pre-merge gate.
11. **Secret leakage into images (P7)** â€” no `.dockerignore` exists today and both csproj copy `.env` to output; with the repo-root build context this is a larger blast radius. Land `.dockerignore` + remove the `.env` copy directives.
12. **Worker process-type rename (P7)** â€” first `heroku.yml` deploy leaves the old `kinobotz` dyno running â†’ a **second live bot on k1no.tv** (duplicate chat responses). The one-time `ps:scale kinobotz=0 worker=1` is mandatory and must be in the runbook.
13. **alpine shell (P6)** â€” `node:22-alpine`/nginx-alpine have no bash; the UI Dockerfile's `/bin/bash` CMD must become `/bin/sh` or the container fails to boot.
14. **`FirstFollowCommandHandler` (P1/P4)** â€” unreachable from chat AND backed by Twitch's removed Get User Follows endpoint; decide remove vs reimplement.
15. **Microsoft.Extensions.AI mockability (P2)** â€” `GetResponseAsync(string)` is an extension; mocks must target the interface method `GetResponseAsync(IEnumerable<ChatMessage>, ChatOptions?, CancellationToken)`. Prefer a hand-rolled `FakeChatClient`.
```
