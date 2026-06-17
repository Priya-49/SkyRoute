---
description: Implement a SkyRoute roadmap phase end-to-end using small, tested, committed increments
argument-hint: <phase-id, e.g. "1A" or "Phase 1A">

allowed-tools: Bash(git checkout:*), Bash(git checkout -b:*), Bash(git branch:*),Bash(git add:*),Bash(git diff:*),Bash(git status:*),Bash(git commit:*),Bash(git log:*),Bash(dotnet build:*),Bash(dotnet test:*),Bash(dotnet run:*)
---

# Implement Phase

The argument supplied in $ARGUMENTS identifies a phase from `Roadmap.md` (e.g. `1A`, `2C`, `Phase 3B`). Accept either form — strip the word "Phase" and surrounding whitespace before matching.

This skill takes a single roadmap phase from "documented" to "implemented, tested, committed, and pushed," in small verifiable steps, then returns to `main` so the next phase always starts clean.

Examples:

/implement-phase 1A
/implement-phase "Phase 2A"
/implement-phase 3D

---

## Step 1 — Load Phase Definition

`Roadmap.md` is the single source of truth for phase scope. Do not consult or invent any other plan document.

1. Open `Roadmap.md` and locate the heading matching the requested phase (e.g. `## Phase 1A — Domain Foundation`).
2. Extract exactly:
   * **Phase name** (the heading text)
   * **Deliverables** (the bullet list)
   * **Exit criteria** (the bullet list)
   * **Estimated effort** (informational only — does not gate anything)
3. If the phase ID doesn't match any heading, stop and report the available phase IDs found in `Roadmap.md`. Do not guess or substitute a nearby phase.
4. Note the phase's position in the `## Implementation Order` sequence (Domain → Application → Infrastructure → API → Frontend) so later layering checks in Step 4 know what's in scope for *this* phase versus a later one.

Also check, if present in the repo, and read for conventions only (skip silently if absent — they are optional, not required):
* `Architecture.md` — layer boundary rules
* `Api_Contracts.md` — request/response shapes
* `Code_Standards.md` / `Critical_Rules.md` — style and hard constraints

Do not begin implementation until the phase's Deliverables and Exit Criteria are both extracted and understood.

---

## Step 2 — Plan Micro-Steps From Deliverables

Roadmap.md phases are already scoped to ~1-2 hours and already enumerate concrete deliverables — treat each deliverable bullet as one micro-step by default. Do **not** re-decompose further unless a single bullet is clearly compound (e.g. "Both pricing strategies + unit tests" → split into strategy implementation and test-writing as two micro-steps).

List the micro-steps before writing any code, in implementation order (respecting Domain → Application → Infrastructure → API → Frontend from the roadmap's stated sequence). For each micro-step, name:
* What gets built (one clear responsibility)
* Which exit-criteria bullet(s) it contributes to

This list is your execution checklist for Step 4. If the phase has 3 deliverables, expect roughly 3 micro-steps, not 9 — over-decomposing a phase that's already small wastes commits and slows verification.

---

## Step 3 — Create or Switch Branch

Slugify the phase name from Roadmap.md into:

```
feature/phase-<id-lowercase>-<slugified-name>
```

Examples:
* `## Phase 1A — Domain Foundation` → `feature/phase-1a-domain-foundation`
* `## Phase 2C — Angular App Scaffold & Services` → `feature/phase-2c-angular-app-scaffold-services`

Logic:
```
git status                     # confirm clean working tree before switching
git branch --list <branch>     # check existence
```
If it exists: `git checkout <branch>`
Otherwise: `git checkout -b <branch>`

Confirm the active branch with `git branch --show-current` before continuing. If the working tree is not clean (uncommitted changes from an unrelated phase), stop and report this rather than switching — do not silently carry over uncommitted work from a different phase.

---

## Step 4 — Implement Each Micro-Step

For each micro-step in order:

### 4.1 — Build only this micro-step
Implement exactly the scope named in Step 2 — nothing from a later micro-step or a later phase. Respect Clean Architecture boundaries per the roadmap's layering (a Domain-layer micro-step must not reference Infrastructure, etc.) and any conventions found in `Architecture.md`/`Api_Contracts.md` if present.

### 4.2 — Build and verify compilation
```
dotnet build
```
Zero errors, zero unaddressed warnings. Fix immediately before proceeding — do not move to the next micro-step on a red build.

### 4.3 — Add tests for this micro-step
Match test type to what was built: unit tests for entities/value objects/use cases/pricing logic, integration tests for repositories/DB/cache, controller/API tests for endpoints. Arrange-act-assert, one behavior per test, cover the happy path plus the edge cases the roadmap calls out explicitly (e.g. Phase 1B names "zero, negative, large numbers" — write those as literal test cases, not just "edge cases" in the abstract).

### 4.4 — Run tests
```
dotnet test --filter "FullyQualifiedName~<TestClassForThisMicroStep>"
```
Then, before committing, run the full suite to catch regressions:
```
dotnet test
```

**Failure handling:** if a test fails, attempt a fix and re-run. If the same micro-step's tests are still failing after **2 fix attempts**, stop this skill run entirely — do not keep retrying, do not skip the test, and do not commit. Report: the micro-step, the failing test(s), what was tried, and the current error output, then hand control back rather than guessing further.

### 4.5 — Self-review against exit criteria
Check the micro-step's output against the specific Exit Criteria bullets it claims to satisfy (from Step 2), plus:
* No accidental cross-layer dependency
* No hardcoded logic where the roadmap specifies an interface/strategy pattern (e.g. Phase 1B: "No hardcoded provider selection logic")
* API responses match `Api_Contracts.md` if present
* No missing validation the deliverable bullet implies

Fix any gaps before committing.

### 4.6 — Commit
```
git status
git add -A
git diff --cached
```
Write a Conventional Commit message: `<type>(<scope>): <what was built>`, type one of `feat`, `test`, `fix`, `refactor`.

```
git commit -m "<message>"
git log -1 --oneline
```

### 4.7 — Next micro-step
Repeat 4.1–4.6 for the next micro-step in the Step 2 list.

---

## Step 5 — Phase-Level Verification

Once every micro-step is committed:

1. **Full suite:** `dotnet test` — all green, including everything from prior phases (no regressions).
2. **Exit criteria sweep:** go back to the full Exit Criteria list extracted in Step 1 and check off each bullet individually — not just the ones already touched in Step 4.5. Mark ✅ or 🚫 per bullet.
3. **Runtime check, if the phase is runnable** (e.g. it stood up an endpoint, a UI form, or a DB-backed flow):
   ```
   dotnet run --project SkyRoute.API
   ```
   Manually exercise the relevant endpoint/flow, confirm no runtime errors, then stop the process.
4. If any exit criterion is 🚫, do not push — return to Step 4 for the relevant micro-step. Pushing with unmet exit criteria defeats the purpose of phase gating.

---

## Step 6 — Push and Return to Main

Only after Step 5 shows all exit criteria ✅:

```
git push -u origin <branch>
git checkout main
git pull
```

Confirm `git branch --show-current` reports `main` before finishing. Leave the feature branch intact (already pushed) rather than deleting it — merging/PR review is outside this skill's scope.

---

## Step 7 — Completion Report

Output, in this structure:

**Phase:** `<phase id and name from Roadmap.md>`
**Branch:** `<branch-name>` (pushed)

**Micro-steps completed:**
✅ `<micro-step>` — `<one-line result>`
(repeat per micro-step)

**Files created / modified:**
* `<path>`

**Tests added:**
* `<TestClassName>` (`<n>` tests)

**Commits:**
* `<short-hash>` — `<message>`

**Exit criteria:**
✅ `<criterion>`
🚫 `<criterion>` — *(only if Step 5 forced a stop; should not appear if push happened)*

**Notes:** assumptions made, any deviation from `Architecture.md`/`Api_Contracts.md` (should be rare and justified), what the next phase in sequence is per the roadmap.

---

## Hard Rules

* `Roadmap.md` deliverables and exit criteria are authoritative — do not add scope, even "obviously useful" scope, from outside the named phase.
* Never commit a micro-step with a red build or a failing test.
* Never push a phase whose exit criteria aren't all ✅.
* On 2 consecutive failed fix attempts for the same failure, stop and ask — do not loop indefinitely.
* One commit per micro-step, not one giant commit per phase.
* Always return to `main` at the end of a successful run, so the next `/implement-phase` invocation starts from a clean base.