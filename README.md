# Sensor Ingestion

This repository contains the implementation of the Sensor Ingestion, DanaTadbir technical task.

## AI usage disclosure

The AI tool used in this project is **OpenAI Codex**, a GPT-5-based AI coding agent. Codex was used to help prepare and refine project documentation, including this README, and as a discussion partner when considering some architectural decisions and trade-offs. No other AI tools have been used so far.

Codex has not been used to generate, edit, or apply changes to the application or test source code. All implementation work and final technical decisions are reviewed and carried out by the candidate.

### Duplicate readings

A reading is identified by the following composite key:

```text
(deviceId, metric, ts, seq)
```

When the same key appears more than once, the first valid occurrence wins. Later occurrences are treated as duplicates and do not reach rule evaluation, classification, aggregation, or alert generation.

The input contains a few duplicate keys whose values disagree. These still follow the first-wins rule, but they are logged as warnings because they indicate conflicting source data rather than a harmless repeated line. This policy is deterministic for the same input file and avoids silently replacing an already accepted reading.

Database uniqueness will enforce the same identity so that processing the file again cannot insert a second copy.

### Timestamp validation and event ordering

Timestamps must be valid ISO-8601 values with an explicit timezone offset. They are normalized to UTC before they are used as part of an identity, ordering decision, or query.

The input file is a bounded batch and is known to be out of order. After invalid records and duplicates have been removed, readings are grouped by `(deviceId, metric)` and each group is ordered by:

1. Event timestamp
2. Sequence number

Rule evaluation, sustained-duration measurement, episode boundaries, cooldowns, and aggregation all use event time rather than the original line order or processing time.

Because the complete file is available before evaluation begins, there is no separate concept of a late arrival in the current implementation: every valid reading participates in the sort. A future streaming input would need an explicit lateness allowance and watermark policy; that is intentionally outside the scope of this batch-oriented task.

### Readings with no applicable rules

A valid reading is considered acceptable when no enabled rule applies to it. This includes cases where there is no rule for the reading's metric, or where all matching rules are disabled or scoped to another device.

Invalid and duplicate records never reach this decision and are not counted as unacceptable readings.

### `SustainedAbove` behavior

A candidate episode starts when the first ordered reading is strictly greater than the configured threshold. The episode remains open while subsequent readings stay above the threshold and ends at the first reading whose value is less than or equal to it.

The candidate becomes a confirmed violation when:

```text
current event time >= episode start + durationSeconds
```

Short candidates that return to or below the threshold before reaching the required duration are discarded and do not produce alerts.

For a confirmed episode:

- `startTs` is the timestamp of the first above-threshold reading, not the later confirmation timestamp.
- `endTs` is the timestamp of the first reading at or below the threshold.
- If the episode is still open at the end of the observed stream, `endTs` is the timestamp of the final observed reading in that stream.
- Peak value is the highest value observed during the episode.

For per-reading classification, the current working policy is that readings become unacceptable from the reading that confirms the required duration onward. Earlier above-threshold readings in the same candidate episode remain acceptable unless another rule rejects them. This avoids retroactively changing previously classified readings and is compatible with a future incremental implementation.

> **Review before submission:** The brief clearly defines alert timestamps, but it is less explicit about which individual readings inside a sustained episode should be classified as unacceptable. Revisit this policy after the stateful evaluator and its tests are complete, and make sure the README, implementation, and test expectations all describe the same behavior.

### Open sustained episodes

The task requires an alert for a confirmed episode even when the observed data ends before the metric returns to or below the threshold. Such an alert is persisted with the final observed event timestamp as `endTs`.

This timestamp means "end of the available observation window," not proof that the real-world condition ended at that moment. If this were a live system, the alert would remain open and would be updated when a closing event arrived.

> **Review before submission:** Confirm that the persistence model makes this distinction understandable. If necessary, add an `IsOpen` field rather than relying only on the meaning of `endTs`.

### Alert cooldown

Cooldown is evaluated independently for each `(ruleId, deviceId, metric)` combination and uses event time. It is a domain-level deduplication policy: a sustained episode can create at most one alert, regardless of how many readings violate the rule.

The working policy measures the quiet gap between the end of the last emitted alert and the start of the next confirmed episode. A new alert is emitted only after the configured cooldown has elapsed; otherwise the new candidate is suppressed.

The default cooldown is five minutes, as requested by the brief. Suppression affects alert creation only; it does not erase the underlying reading classifications or rule-evaluation results.

> **Review before submission:** Finalize the exact boundary behavior in tests: whether a gap of exactly five minutes is accepted or only a gap greater than five minutes. The brief uses wording that can support either interpretation. Also confirm whether a suppressed nearby episode should remain a separate recorded episode internally or be merged into the previous alert. The initial implementation should prefer suppression without merging unless the resulting model becomes confusing.

### Aggregation behavior

Aggregation operates only on acceptable readings. Invalid records, duplicates, and unacceptable readings do not contribute to count, average, minimum, or maximum values.

The requested range uses half-open semantics:

```text
[from, to)
```

A reading at `from` is included, while a reading exactly at `to` is excluded. Buckets are anchored at `from` and use the same half-open behavior. Empty buckets are omitted from the response rather than returned with zero counts.

Requests with missing identifiers, non-UTC timestamps, `from >= to`, or a non-positive bucket size are rejected as validation errors.

### Rule configuration failures

Rules are loaded from the configured JSON file when the application starts. Changing the file and restarting the service must be enough to add, enable, disable, or modify rule instances that use supported operators.

The application fails startup with a clear error when:

- The configured rule file cannot be found or read.
- The JSON is malformed.
- A rule ID is missing or duplicated.
- Required operator parameters are missing or invalid.
- An operator name is unknown.

Invalid rules are not silently ignored because that could make the service appear healthy while important checks are missing.

Adding a new rule that uses an existing operator is a data-only change. Adding a completely new operator requires a new strategy implementation and registration, but must not require changes to the existing evaluation loop.

### Idempotency keys

Idempotency is enforced at the record level rather than relying only on a file-level "already processed" flag. The planned natural keys are:

- Reading: `(deviceId, metric, ts, seq)`
- Rule evaluation: `(readingId, ruleId)`
- Alert: `(ruleId, deviceId, metric, startTs)`

An input-file hash may also be stored with the ingestion run for auditing and quick repeat detection, but database uniqueness remains the final protection against duplicates.

> **Review before submission:** Update this section with the exact table constraints and conflict-handling behavior once persistence is implemented. In particular, confirm how an open alert is updated without creating a new alert identity.
