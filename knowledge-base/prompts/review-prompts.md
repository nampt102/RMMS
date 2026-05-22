# Code Review Prompts

Ready-to-use prompts for reviewing code. The reviewing AI plays the role of senior dev checking the work.

---

## 1. Comprehensive PR review

```
Review this PR for module {M5 Attendance}.

Files changed:
{paste diff or list of files}

Acceptance criteria targeted: {AC-X, AC-Y}

Please check:

### Correctness
- [ ] Implements stated AC correctly
- [ ] Matches business rules in `06-business-rules.md`
- [ ] Handles edge cases listed in module doc
- [ ] No incorrect assumptions about domain

### Code Quality
- [ ] Follows `08-coding-standards.md` conventions
- [ ] No `TODO`/`FIXME` left without ticket
- [ ] No commented-out code
- [ ] No `console.log` / `Console.WriteLine` / `print` debugging
- [ ] Variable names meaningful
- [ ] Functions small (< 50 lines preferred)
- [ ] Cyclomatic complexity reasonable

### Architecture
- [ ] Layers respected (no DB access in controllers, etc.)
- [ ] No new dependencies added without justification
- [ ] Patterns consistent with rest of codebase

### Security
- [ ] Input validation present
- [ ] Authorization checks present
- [ ] No SQL injection vectors
- [ ] No leaked credentials/secrets
- [ ] PII handled per privacy rules (CR-4)

### Testing
- [ ] Unit tests added for new logic
- [ ] Tests cover edge cases
- [ ] Tests are not flaky
- [ ] Integration test if API endpoint
- [ ] Coverage hasn't decreased

### Performance
- [ ] No N+1 queries
- [ ] Indexes added if new query patterns
- [ ] No blocking I/O in async paths
- [ ] No unnecessary serialization

### i18n
- [ ] All user-visible strings in resource files
- [ ] Both `vi` and `en` versions present
- [ ] Date/number formatting locale-aware

### Audit & Logging
- [ ] Audit log entries for critical actions (CR-1)
- [ ] Errors logged with context
- [ ] No sensitive data in logs

### Documentation
- [ ] OpenAPI/Swagger updated
- [ ] Public methods have XML doc / JSDoc
- [ ] Knowledge base updated if business logic changed
- [ ] CHANGELOG entry (if maintained)

Highlight blockers (must fix), suggestions (nice to have), and praises (good practices to repeat).
```

---

## 2. Quick spot-check

```
Quick review of this code:

{paste code}

Just check:
1. Any obvious bugs?
2. Any security issues?
3. Any performance concerns?
4. Does it match the pattern in `08-coding-standards.md`?

Be terse. List issues only.
```

---

## 3. Security-focused review

```
Security review for this code:

{paste code}

Check for:
- [ ] SQL injection
- [ ] XSS
- [ ] Authorization bypass
- [ ] Authentication weaknesses
- [ ] Insecure direct object reference
- [ ] Sensitive data exposure (logs, errors, responses)
- [ ] Missing rate limiting
- [ ] Missing input validation
- [ ] CSRF (if applicable)
- [ ] Race conditions on critical resources
- [ ] Timing attacks on auth
- [ ] Insecure cryptography choices

For each finding:
- Severity (critical/high/medium/low)
- Impact
- Recommended fix
```

---

## 4. Test coverage review

```
Review tests for {feature/file}:

{paste test code + code under test}

Evaluate:
1. Does each test verify ONE concept?
2. Are tests independent (no shared state)?
3. Is the Arrange-Act-Assert structure clear?
4. Are edge cases covered? (boundary values, empty inputs, nulls, errors)
5. Are external dependencies properly mocked?
6. Will tests fail meaningfully if the bug they're testing for is reintroduced?
7. Is the mocked behavior realistic?
8. Are there missing tests for important behaviors?

Suggest specific missing test cases with names.
```

---

## 5. Architectural concern review

```
I'm not sure if this architectural choice is right:

{describe choice or paste code}

Context:
- Module: {M?}
- Stage: {sprint N}

Evaluate against our principles in `08-coding-standards.md` and `02-tech-stack.md`:
1. Does it fit our layered architecture?
2. Does it introduce hidden coupling?
3. Will it scale to expected load?
4. Will it cause issues for the other dev to maintain?
5. Are there simpler alternatives?
6. Long-term maintenance cost?

Provide a recommendation with reasoning.
```

---

## 6. DB schema change review

```
Review this migration:

{paste migration}

Check:
- [ ] Backward compatible (can roll back?)
- [ ] No data loss potential
- [ ] Index strategy correct
- [ ] Foreign keys correctly set up (cascade rules)
- [ ] Default values reasonable
- [ ] Nullable correctness
- [ ] Naming follows conventions in `08-coding-standards.md`
- [ ] Will it perform well on a large table (>1M rows)?
- [ ] Migration is idempotent or safely re-runnable?
- [ ] Down migration is correct?

Flag concerns and suggest alternatives if needed.
```

---

## 7. UX flow review

```
Review the UX flow for {check-in screen}:

{paste screenshots or describe flow}

Reference: `modules/M05-attendance-antifraud.md`

Check:
1. Does it satisfy the user story?
2. Are error states clear?
3. Loading states present?
4. Empty states present?
5. Network failure handled gracefully?
6. Permission denied (camera, GPS) handled?
7. Accessibility considered (font sizes, contrast)?
8. i18n correctness in VI and EN?
9. Consistent with rest of app?
10. Performance feels snappy?

Provide constructive feedback.
```

---

## 8. Documentation review

```
Review this knowledge base update:

{paste markdown}

Check:
- [ ] Consistent with existing docs
- [ ] No contradicting info elsewhere
- [ ] Cross-references valid (no broken links)
- [ ] Examples are complete and correct
- [ ] Terminology matches `01-glossary.md`
- [ ] No assumptions presented as facts
- [ ] Edge cases mentioned
- [ ] Easy for an AI to parse and use
```

---

## 9. Acceptance gate review

```
We're approaching Phase 1 acceptance. Review readiness for AC-{18}:

"{paste AC text}"

Implementation files:
{list}

Test files:
{list}

Check:
1. Is the behavior 100% per spec?
2. Are all edge cases from module doc handled?
3. Test coverage of this AC?
4. Demo-able to stakeholder?
5. Any known issues or workarounds?
6. Documentation accurate?

Pass/Fail with reasoning.
```

---

## 10. "Smell test" prompt for new code

```
Smell test this code:

{paste}

Look for:
- God classes (>500 lines)
- Long methods (>50 lines)
- Deep nesting (>3 levels)
- Magic numbers
- Boolean parameter explosion
- Premature optimization
- Speculative generality (YAGNI violations)
- Tight coupling
- Hidden side effects
- Mutable shared state
- Inconsistent error handling

For each smell found, suggest a refactor (small enough to do incrementally).
```
