# Manual ingestion scenarios

These fixtures use `rules-all-operators.json`. Expected counts assume the first run against an empty database. Re-running the same file should store zero additional readings, evaluations, or alerts because ingestion is idempotent.

## Important semantic note

The stateless expectations below describe the current implementation: `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual`, `Equal`, and `Between` are conditions that a reading must satisfy. `SustainedAbove` follows the task specification and becomes violated after the threshold has remained exceeded for the configured event-time duration.

If stateless operators are later redefined as violation predicates, update these expectations together with the implementation and tests.

## `readings-all-operators.jsonl`

| Line | Operator | Value | Expected classification | Reason |
|---:|---|---:|---|---|
| 1 | `GreaterThan 50` | 60 | Acceptable | 60 is greater than 50. |
| 2 | `GreaterThan 50` | 50 | Unacceptable | The boundary is exclusive. |
| 3 | `GreaterThanOrEqual 50` | 50 | Acceptable | The boundary is inclusive. |
| 4 | `GreaterThanOrEqual 50` | 49 | Unacceptable | 49 is below 50. |
| 5 | `LessThan 10` | 9 | Acceptable | 9 is less than 10. |
| 6 | `LessThan 10` | 10 | Unacceptable | The boundary is exclusive. |
| 7 | `LessThanOrEqual 10` | 10 | Acceptable | The boundary is inclusive. |
| 8 | `LessThanOrEqual 10` | 11 | Unacceptable | 11 is above 10. |
| 9 | `Equal 5` | 5 | Acceptable | The values are equal. |
| 10 | `Equal 5` | 4 | Unacceptable | The values are different. |
| 11 | `Between [2, 5]` | 2 | Acceptable | The lower boundary is inclusive. |
| 12 | `Between [2, 5]` | 5 | Acceptable | The upper boundary is inclusive. |
| 13 | `Between [2, 5]` | 6 | Unacceptable | 6 is outside the range. |
| 14 | `SustainedAbove 80 for 30s` | 81 | Acceptable | The episode has just started. |
| 15 | `SustainedAbove 80 for 30s` | 85 | Unacceptable | The duration boundary is reached. |
| 16 | `SustainedAbove 80 for 30s` | 80 | Acceptable | The episode closes at or below the threshold. |

Expected report:

```text
Total lines read: 16
Parsed readings: 16
Stored readings: 16
Duplicates removed: 0
Invalid records rejected: 0
Rules loaded: 7
Rule evaluations performed: 16
Acceptable readings: 9
Unacceptable readings: 7
Rule violations: 7
Alerts generated: 1
```

The alert is expected for `SA-01`, starting at `2025-06-01T09:10:00Z`, ending at `2025-06-01T09:10:40Z`, with peak value `85`.

## `readings-sustained-out-of-order.jsonl`

The file is deliberately shuffled. After event-time sorting it contains three confirmed episodes:

| Episode | Start | End | Peak | Alert result |
|---:|---|---|---:|---|
| 1 | `10:00:00Z` | `10:00:40Z` | 85 | Emitted |
| 2 | `10:02:00Z` | `10:02:40Z` | 86 | Suppressed because its start is less than five minutes after the previous emitted episode ended. |
| 3 | `10:06:00Z` | `10:06:40Z` | 88 | Emitted because the suppressed episode does not move the cooldown reference. |

Expected report:

```text
Total lines read: 9
Parsed readings: 9
Stored readings: 9
Duplicates removed: 0
Invalid records rejected: 0
Rules loaded: 7
Rule evaluations performed: 9
Acceptable readings: 6
Unacceptable readings: 3
Rule violations: 3
Alerts generated: 2
```

## `readings-invalid-and-duplicates.jsonl`

- Line 1 is retained and is acceptable.
- Line 2 is an exact duplicate.
- Line 3 has the same identity as line 1 but a conflicting value. The first reading still wins.
- Line 4 is malformed JSON.
- Line 5 is valid JSON but is missing `metric`.

Expected report:

```text
Total lines read: 5
Parsed readings: 4
Stored readings: 1
Duplicates removed: 2
Invalid records rejected: 2
Rules loaded: 7
Rule evaluations performed: 1
Acceptable readings: 1
Unacceptable readings: 0
Rule violations: 0
Alerts generated: 0
```

## `readings-no-applicable-rule.jsonl`

Neither reading has an applicable rule. Both are expected to be acceptable according to the documented no-applicable-rule policy.

```text
Total lines read: 2
Parsed readings: 2
Stored readings: 2
Duplicates removed: 0
Invalid records rejected: 0
Rules loaded: 7
Rule evaluations performed: 0
Acceptable readings: 2
Unacceptable readings: 0
Rule violations: 0
Alerts generated: 0
```
