# Sensor Ingestion

This repository implements the DanaTadbir Backend Engineer technical task: ingesting messy sensor readings, evaluating configurable stateless and stateful rules, persisting auditable outcomes, generating cooldown-aware alerts, and exposing acceptable-reading aggregates.

## AI usage disclosure

I used **OpenAI Codex**, a GPT-5-based AI coding agent, to draft and refine project documentation, discuss architectural decisions and trade-offs, and design and write parts of the automated test suite.

I reviewed the AI-assisted documentation and tests and implemented the corresponding production code using a workflow close to test-driven development (TDD). I ran the complete test suite, verified the supplied dataset manually, and remain responsible for the final design and implementation decisions.

## Prerequisites

- .NET 10 SDK
- The supplied `readings.jsonl` file
- A JSON rule file such as [`data/rules.json`](data/rules.json)

SQLite is embedded through EF Core, so no external database server is required.

## Build, test, and run

From the repository root:

```powershell
dotnet build .\SensorIngestion.slnx --configuration Release
dotnet test .\SensorIngestion.slnx --configuration Release
```

The Development configuration expects the supplied input at `D:\DanaTadbir\readings.jsonl`, processes it during startup, and copies `data/rules.json` beside the application output:

```powershell
dotnet run --project .\src\SensorIngestion.Api\SensorIngestion.Api.csproj
```

Every path can be overridden without changing source code. Relative paths are resolved against `AppContext.BaseDirectory`:

```powershell
dotnet run --project .\src\SensorIngestion.Api\SensorIngestion.Api.csproj -- `
  --Input:Path="D:\path\to\readings.jsonl" `
  --Input:ProcessOnStartup=true `
  --RuleConfiguration:Path="D:\path\to\rules.json" `
  --Persistence:DatabasePath="D:\path\to\sensor-ingestion.db"
```

With the default settings, the database is created at `src/SensorIngestion.Api/bin/<Configuration>/net10.0/data/sensor-ingestion.db`. The schema is created with `EnsureCreated` because this is a self-contained assessment project; a production service would use reviewed EF Core migrations.

## Architecture

The dependency direction is `Api -> Infrastructure/Application -> Domain`:

- `SensorIngestion.Domain` contains readings, rules, rule evaluations, alert models, identities, and invariants. It has no ASP.NET Core, EF Core, file, or JSON dependency.
- `SensorIngestion.Application` contains ingestion orchestration, preprocessing, rule strategies, stateful evaluation, cooldown, aggregation, and persistence/input ports.
- `SensorIngestion.Infrastructure` implements JSONL input, JSON rule loading, SQLite persistence, and aggregate queries.
- `SensorIngestion.Api` is the composition root and exposes the HTTP endpoint.
- The two test projects separate fast domain/application tests from file, SQLite, startup, and HTTP integration tests.

SQLite was selected because the supplied input is a bounded assessment dataset and the task explicitly permits it. Storage access is behind application interfaces, so PostgreSQL or a time-series store can replace SQLite without moving persistence concerns into the domain. A time-series database would become attractive for high-volume retention and analytical queries, while alerts, rule versions, and audit records could remain in relational storage.

`Metric` is an extensible value object rather than an enum. Rules use a stable `RuleKey`, while each persisted configuration has its own database-generated ID. If a rule's configuration hash changes, a new immutable version is inserted, allowing an evaluation to identify the exact rule version that produced it.

## Ingestion and messy-data policies

Each JSONL line is handled independently. JSON shape/type validation happens in the infrastructure parser; domain construction then enforces semantic invariants. A rejected line is logged with its line number, category, and reason, but it never reaches evaluation or counts as unacceptable.

Timestamps must be ISO-8601 UTC values ending in `Z`. Fractional seconds are accepted. Missing UTC markers, non-UTC offsets, invalid dates, negative sequences, missing identifiers, non-finite values, malformed JSON, and incorrect field types are rejected.

A reading's natural identity is:

```text
(deviceId, metric, timestamp, sequence)
```

Duplicates use deterministic first-wins behavior. A later occurrence with a different value is still discarded, but it emits a structured warning. Database uniqueness enforces the same identity across reruns.

The complete bounded input is collected, deduplicated, grouped by `(deviceId, metric)`, and sorted by event timestamp and then sequence. Evaluation, sustained duration, episode boundaries, cooldown, and aggregation therefore use event time rather than file order. In this batch model every valid row participates in the sort, so there is no late-arrival cutoff. A streaming version would need a watermark and explicit allowed-lateness policy.

The current batch implementation uses `O(n)` working memory and approximately `O(n log n)` sorting time. It is intentionally simple for the supplied dataset. A larger or continuous workload should use chunked/bulk persistence, database-side upserts, incremental per-stream state, and watermark-based event-time processing instead of loading the complete batch and existing identity sets into memory.

## Rule engine

An enabled rule applies when its metric matches and its optional `deviceId` is either absent or matches the reading. A global rule therefore applies to every device carrying that metric. If no enabled rule applies, the reading is acceptable.

An operator describes the condition a reading must satisfy. For example, `GreaterThan` passes only when the reading value is greater than its threshold; otherwise the rule is violated. Every applicable rule produces an auditable evaluation. A reading is acceptable only when all applicable rules pass, and one failed rule is enough to classify it as unacceptable.

The stateless evaluation loop resolves operator strategies through `RuleOperatorRegistry`; it does not contain an operator switch. To add another stateless operator:

1. Add its name and parameter names.
2. Implement `IRuleOperatorStrategy`.
3. Register the strategy in dependency injection.
4. Extend the JSON adapter's validation and parameter mapping for the new input shape.

The existing evaluation loop does not change. Adding or changing rule instances for supported operators only requires replacing `rules.json` and restarting the service. Missing files, malformed JSON, duplicate IDs, unknown operators, or missing/invalid parameters fail startup instead of silently disabling checks.

### `SustainedAbove`

Readings are scanned in event-time order per stream. A candidate episode starts at the first value strictly above the threshold and closes at the first value at or below it. It becomes confirmed when an observed reading reaches:

```text
reading timestamp >= episode start + durationSeconds
```

Readings before confirmation remain acceptable unless another rule rejects them. From the confirming reading onward, above-threshold readings are unacceptable. The closing reading passes the stateful rule. A candidate that closes before the duration is reached is discarded.

For a confirmed episode, `startTs` is the first above-threshold event, `endTs` is the first event at or below the threshold, and `peakValue` is the highest observed value. If the input ends while a confirmed episode is active, it is persisted as open with `endTs` equal to the last observed event timestamp and `IsOpen = true`; this marks the end of observation, not proof that the real-world condition ended.

### Alert cooldown

One confirmed episode produces one alert candidate. Cooldown is tracked independently per `(ruleId, deviceId, metric)` and defaults to five minutes. It measures the event-time gap from the previous emitted alert's end to the next episode's start:

```text
next start - previous emitted end >= 5 minutes
```

A gap of exactly five minutes is allowed. A candidate inside the cooldown is suppressed rather than merged, and a suppressed candidate does not move the cooldown reference point. Suppression affects alert creation only; reading classifications and rule evaluations remain available.

## Persistence and idempotency

A processing run is recorded with its SHA-256 input fingerprint, status, timestamps, counters, and failure message when applicable. Reading, evaluation, and alert writes are committed atomically; a failed write rolls the processed data back while the run is marked failed.

Record-level idempotency is enforced with unique constraints:

- Reading: `(deviceId, metric, timestamp, sequence)`
- Rule evaluation: `(sensorReadingId, ruleId)`
- Alert: `(ruleId, deviceId, metric, startTimestamp)`
- Rule version: `(ruleKey, configurationHash)`

The fingerprint is retained for audit and repeat detection, while the natural keys remain the final protection. Reprocessing the supplied file creates a new run record but inserts no duplicate readings, evaluations, or alerts.

## Aggregation API

The endpoint is:

```text
GET /api/aggregations?deviceId=...&metric=...&from=...&to=...&bucketSeconds=...
```

It includes only acceptable readings. The requested range and every bucket are half-open: `[from, to)`. Buckets are anchored at `from`; a reading exactly at `to` is excluded, and empty buckets are omitted. Missing identifiers, non-UTC timestamps, `from >= to`, or a non-positive bucket size return `400 Bad Request`.

Example:

```http
GET /api/aggregations?deviceId=PUMP-01&metric=vibration&from=2025-06-01T08:20:00Z&to=2025-06-01T08:40:00Z&bucketSeconds=300
```

```json
[
  { "start": "2025-06-01T08:20:00+00:00", "count": 30, "average": -1.0802333333333334, "minimum": -1.775, "maximum": -0.135 },
  { "start": "2025-06-01T08:25:00+00:00", "count": 30, "average": 0.6027000000000001, "minimum": -0.021, "maximum": 1.113 },
  { "start": "2025-06-01T08:30:00+00:00", "count": 30, "average": 0.5537666666666667, "minimum": -0.642, "maximum": 5.493 }
]
```

## Processing report

Counter meanings are:

- `TotalLinesRead`: every physical input line.
- `ParsedReadings`: JSON reading objects parsed far enough for field/semantic validation; these can still be rejected later. Malformed JSON and a non-object root are not counted here.
- `StoredReadings`: valid unique readings newly inserted during this run.
- `DuplicatesRemoved`: valid readings skipped because their natural identity was already seen in the batch.
- `InvalidRecordsRejected`: malformed lines and parsed objects rejected by validation.
- `RulesLoaded`: valid definitions loaded, including disabled rules.
- `RuleEvaluationsPerformed`: enabled and applicable rule evaluations.
- `AcceptableReadings` / `UnacceptableReadings`: classifications of valid unique readings.
- `RuleViolations`: individual failed rule results, which can exceed the number of unacceptable readings.
- `AlertsGenerated`: new alerts that pass cooldown and persistence idempotency.

Verified first-run report for the supplied `readings.jsonl` and repository `rules.json`:

```text
Total lines read: 2150
Parsed readings: 2149
Stored readings: 2103
Duplicates removed: 38
Invalid records rejected: 9
Rules loaded: 3
Rule evaluations performed: 1050
Acceptable readings: 1433
Unacceptable readings: 670
Rule violations: 670
Alerts generated: 0
```

The supplied data does not contain a confirmed `SustainedAbove` episode for the configured `PUMP-01` temperature rule, so zero alerts is expected. On an immediate rerun against the same database, `Stored readings` becomes `0`; persisted reading, evaluation, and alert counts remain unchanged.

## Verification

The final Release verification completed with:

- 210 passing unit tests
- 45 passing integration tests
- 0 failed or skipped tests
- 0 compiler or analyzer warnings
- 0 build errors

Integration coverage includes messy out-of-order ingestion, cancellation and failure propagation, rule-file replacement, SQLite rollback and uniqueness, idempotent reruns, structured logs, acceptable-only half-open aggregation, and HTTP validation.
