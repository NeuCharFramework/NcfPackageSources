# DDD bounded-context and XNCF splitting rules

## Contents

- Boundary discovery
- Split scorecard
- Context-map patterns
- Dependency and consistency rules
- Migration from one XNCF to several
- Proposal format

## Boundary discovery

Start from business language and invariants. For each capability, answer:

1. Which commands change state?
2. Which rules must be true atomically?
3. Which aggregate owns those rules?
4. Who is authoritative for each fact?
5. Do two teams use the same word with different meanings?
6. Which actors and policies protect the capability?
7. Can it be installed, released, scaled, or disabled independently?
8. What happens when a dependency is unavailable?

Group capabilities that share language, invariants, transaction boundaries, and lifecycle. Separate capabilities that require translation between models or can tolerate asynchronous consistency.

## Split scorecard

Score each proposed boundary from 0 to 2. Scores guide discussion; they do not replace judgment.

| Signal | 0: keep together | 1: investigate | 2: split evidence |
| --- | --- | --- | --- |
| Language/model | Same terms and meanings | Some specialized terms | Conflicting models/ubiquitous language |
| Invariants | Same aggregate transaction | Coordinated but separable | Independent invariants/aggregates |
| Data ownership | Same authoritative data | Shared reads | Distinct write owners |
| Lifecycle | Always installed/released together | Sometimes optional | Independent install/version/release |
| Security/tenancy | Same policy | Additional roles | Distinct trust or tenant boundary |
| Availability/scale | Same profile | Hotspot only | Independent SLO/scale/failure needs |
| Integration | Internal calls | Stable boundary emerging | External/anti-corruption boundary |
| Dependency shape | Rich internal collaboration | A few contracts | Narrow event/API contract |

Interpretation:

- `0-4`: keep one XNCF unless a hard constraint exists.
- `5-9`: model both options and test dependency/data ownership.
- `10+`: multiple XNCFs are usually justified.

Hard constraints override the total: a required independent install, distinct data owner, security boundary, or incompatible model can justify a split by itself.

## Context-map patterns

Choose and name the relationship:

- **Customer/Supplier**: upstream XNCF owns a contract; downstream consumes it without internal access.
- **Published Language**: use versioned events/contracts in `.Abstractions`.
- **Anti-Corruption Layer**: translate an external or legacy model into the owning XNCF's language.
- **Conformist**: accept an upstream model only when the coupling is deliberate and documented.
- **Shared Kernel**: allow only a tiny, jointly governed set of stable value objects/contracts. Never use it as a dumping ground.
- **Open Host Service**: expose a stable application/API boundary for several consumers.
- **Separate Ways**: duplicate small read-only concepts when sharing would create stronger coupling than value.

Represent dependencies as a DAG. If the context map contains a cycle, move the orchestration to a higher-level process module, publish events, extract a neutral contract, or reconsider the boundary.

## Dependency and consistency rules

- A command has one owning XNCF.
- A query may compose read models but must not grant cross-module write access.
- Cross-XNCF workflows are eventually consistent unless a documented synchronous contract is unavoidable.
- Put compensation, timeout, retry, and idempotency behavior in the workflow design.
- Do not wrap repositories from two XNCFs in one ambient transaction to simulate one aggregate.
- Publish integration events after successful local state changes; use durable outbox/inbox patterns when loss or duplicate delivery is unacceptable.
- Version contracts compatibly. Add fields compatibly where possible; do not expose internal class layouts.

## Avoid false boundaries

Do not create an XNCF solely for:

- each table/entity or aggregate child;
- each menu/page/controller;
- CRUD versus reporting over the same owned model;
- each technical layer (`DomainXncf`, `ApiXncf`, `UiXncf`);
- a generic helper library with no business capability;
- a temporary organizational chart.

A bounded context may contain several aggregates and several UI/API adapters. A reusable technical library is usually a normal package, not an XNCF, unless it has an installable NCF module lifecycle and capability of its own.

## Migration from one XNCF to several

Use incremental extraction:

1. Record current routes, functions, tables, migrations, events, permissions, and consumers.
2. Define target owners and contracts before moving code.
3. Add characterization/contract tests around current behavior.
4. Introduce an application facade or abstraction at the future boundary.
5. Move one vertical slice and its owned data at a time.
6. Replace direct calls with contracts/events; keep temporary adapters explicit and time-bounded.
7. Migrate data with idempotent scripts and a rollback/cutover plan.
8. Move routes/UI only after the application contract works.
9. Remove old writes, then old reads, then compatibility adapters.
10. Verify install/update order, host discovery, authorization, migrations, runtime behavior, and packaging.

Never give two XNCFs concurrent authority over the same mutable table during the steady state. During migration, designate one writer and make replication direction explicit.

## Proposal format

Before implementation, produce:

| XNCF | Business purpose | Owned aggregates/data | Entry points | Publishes | Consumes | Depends on |
| --- | --- | --- | --- | --- | --- | --- |

Then document:

- decision to keep/split and scorecard evidence;
- dependency graph and context-map relationship;
- synchronous versus asynchronous interactions;
- transaction and consistency boundaries;
- contract project(s) and versioning;
- security/tenant ownership;
- migration/cutover order;
- validation and rollback plan.

