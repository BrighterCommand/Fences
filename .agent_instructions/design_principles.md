# Design Principles

These are the invariants that make Fences what it is. They are inherited from Polly's version 8
redesign and they are worth defending: a change that breaks one of them is a change to the shape of
the library, not a detail, and it needs an ADR.

## The strategy taxonomy

**Every resilience strategy is either reactive or proactive.**

- **Reactive** — it responds to a failure that has already happened: retry, circuit breaker,
  fallback, hedging.
- **Proactive** — it prevents overload before a failure happens: timeout, rate limiter.

A new strategy should be clearly one or the other. If it is neither, or both, that is a signal the
responsibility has not been factored properly, and it is the first thing to settle in the ADR.

## Every strategy has an `*Options` class

Configuration lives in a matching options type — `RetryStrategyOptions`,
`CircuitBreakerStrategyOptions`, `TimeoutStrategyOptions` — and **the options class is what the
public surface exposes**. The strategy itself is an implementation detail.

Consequences:

- New configuration is a property on the options class, not a new builder overload or a constructor
  parameter.
- The options class is where validation lives, and where defaults are documented.
- Because it is public, it is subject to [public_api.md](public_api.md).

## Predicates go through `PredicateBuilder`

Which outcomes a strategy handles is declared through the `PredicateBuilder` fluent API. Do not add
loose predicate parameters passed around by hand, and do not add a second way to express the same
thing — the one obvious way is the builder.

## `ResilienceContext` is pooled

`ResilienceContext` is rented and returned, not allocated per execution. Therefore:

- **Do not hold a reference to a context beyond the execution it belongs to.** Capturing one in a
  closure, a field, or a task that outlives the execution is a use-after-return bug that will
  present as impossible-looking cross-talk between unrelated executions.
- Do not store anything in a context expecting it to survive.
- If you need something to outlive an execution, copy it out.

## The legacy package is frozen

`src/Paramore.Fences` is the pre-v8 API, kept so that existing code keeps working.

- **It must not gain features.** New work goes in `src/Paramore.Fences.Core`.
- It is the one project that deliberately does not enable nullable reference types. Do not "fix"
  that; it would be a breaking change to its consumers' warnings.
- Bug fixes are fine. Anything else needs a very good reason and an ADR.

## Allocation and the hot path

The engine sits inside other people's request paths. Allocation and indirection on the execution
path are costs paid by every caller on every call, which is why the context is pooled and why
`bench/` exists. If you change something on the execution path, be able to say what it costs. The
BenchmarkDotNet projects in `bench/` are how you find out.

## Roles, responsibilities and collaborators

When you design something new — and when you write the `Key Components` table in an ADR — describe
it in terms of **responsibilities** before structure.

- A **role** is one phrase saying what a type does. If it takes more than one phrase, the type has
  too many responsibilities. This is the single responsibility principle restated as a single
  *role* principle.
- Responsibilities come in three kinds: **knowing**, **doing** and **deciding**. One type may carry
  more than one.
- **Collaborators** are the types a type works with to meet its responsibilities. A responsibility
  with no collaborator is either self-contained or in the wrong object; naming a role without its
  collaborators is half the picture.

The taxonomy above is what this looks like in Fences: a strategy *decides* whether an outcome is
handled (through `PredicateBuilder`) and *does* the thing it exists to do; the options class
*knows* the configuration; the pipeline *coordinates* the strategies; the context *knows* the state
of one execution.

`.claude/commands/spec/design.md` turns this into a table in every ADR. That table is where the
principle stops being an aspiration.

Fences does **not** use Brighter's `IAmA*` interface naming, and does not add an interface merely
to express a role. An abstract base class, a sealed type, or a delegate is often the better answer;
Polly's design is built that way and we have kept it.

## General

- Keep methods small and focused on a single responsibility.
- Prefer intention-revealing names over comments.
- Do not add a type without necessity, and do not add a second way to do something that already has
  one.
- If the implementation is hard to explain, it is a bad idea.
- Reveal intention; be explicit for the benefit of future readers.

## Tidy First

Separate structural changes from behavioural ones, and never mix them in one commit. Make the
structural change first, and prove it changed nothing by running the tests before and after. Use
`/tidy-first <change>` ([docs](../.claude/commands/refactor/README.md)).

See [code_style.md](code_style.md).

## Where design decisions are recorded

`docs/adr/`. Write the record before the code where you can. Reviewer-facing rationale —
alternatives considered, previous behaviour, why not the obvious thing — belongs there, not in an
inline comment. See [adr_frontmatter.md](adr_frontmatter.md).
