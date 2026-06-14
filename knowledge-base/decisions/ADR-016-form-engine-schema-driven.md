# ADR-016 — Form Engine: schema-driven JSONB + registry validator + factory renderer

- **Status:** Accepted
- **Date:** 2026-06-14
- **Deciders:** Tech lead, PM
- **Context module:** M10 Form Engine (Phase 1B, Sprints 11–14)

## Context

M10 lets Admin create and deploy forms (stock/market/photo reports, PC checklists,
surveys, knowledge tests, training, visit reports) **without a developer or a schema
migration per form**. We need one architecture that the web Builder writes, the mobile
app renders dynamically, and the backend validates — covering 12+ input types, per-form
rules, conditional logic, versioning (BR-505), offline draft (BR-504), and scoring.

The data model already stores the form definition as `form_versions.schema` (JSONB) with
`fields[]` + `rules{}` (see `04-data-model.md`). The open questions are: how to **define
the field contract**, how to **validate** submissions server-side, and how the **mobile
client renders** an arbitrary schema.

## Decision

A **schema-driven** engine with three parts sharing one contract (see
`modules/M10-form-engine-design.md`):

1. **Definition = JSONB, not relational columns.** Each field is
   `{id, type, label_vi/en, required, visible_if, validators[], …type-specific}`. Field
   naming/structure follows proven **SurveyJS / JSON-Schema conventions** (`type`, `title`,
   `validators[]`, `visibleIf`) but is our own minimal contract — no external form library
   is added (web is AntD Pro, mobile is Flutter; both fixed by prior ADRs).
2. **Server validation by an input-type registry (Strategy pattern), NOT generic JSON
   Schema.** One validator per `type`, plus form-rule checks and `visible_if` evaluation.
   Reason: our fields reference DB entities (`product_selector`, `store_selector`) and need
   i18n error messages and a small boolean expression evaluator — a generic JSON-Schema
   validator (e.g. NJsonSchema, which the spec floated) cannot check referential validity or
   produce localized messages. The server is the source of truth; client validation is UX only.
3. **Mobile renderer = factory pattern**, one widget per input type, driven by the schema;
   unknown types fall back to a safe "unsupported" widget. Offline drafts use Hive with a
   client-generated idempotency key reconciled by the existing `IdempotencyMiddleware`.

Submissions snapshot `form_version_id` and are **immutable to version edits** (BR-505/AC-21).

## Consequences

- **+** New form types/fields are pure data (registry entry + one widget + one validator) —
  no DB migration, no breaking existing submissions.
- **+** Single contract shared by Builder, renderer, and validator → no duplicated field
  definitions; web and mobile stay in sync.
- **+** Registry validator handles reference integrity + i18n that generic JSON-Schema can't.
- **−** We own the schema contract and its evolution (must keep registry, renderer, validator
  in lockstep; a typed shared spec/doc is the guardrail).
- **−** Conditional logic (`visible_if`) needs a small, **safe** expression evaluator on both
  client and server (no `eval`); Phase 1 limits operators to `== != > < >= <= && || in`.
- **−** JSONB answers are flexible but not relationally queryable; reporting (Sprint 16) reads
  JSONB with GIN indexes where needed.

## Alternatives considered

- **Relational columns per form** — type-safe but every new form = migration; defeats the
  "Admin self-serve, no dev" purpose. Rejected.
- **Adopt a JS form library (SurveyJS / form.io / RJSF) end-to-end** — mature, but they are
  React/JS runtimes that don't render in Flutter, and pull UI deps outside the agreed stack
  (would need to overturn AntD/Material decisions). We borrow their **schema conventions**
  only. Rejected as a dependency.
- **Generic JSON-Schema validation (NJsonSchema)** — clean for primitive constraints but can't
  validate `product_id`/`store_id` references or emit vi/en messages, and maps awkwardly to our
  rules. Rejected in favour of the registry validator.
