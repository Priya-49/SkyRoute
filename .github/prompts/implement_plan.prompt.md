---
agent: agent
description: Implement a SkyRoute roadmap phase end-to-end in small, tested, committed increments — branch, implement, verify, push, return to main.
---

# Implement Phase

The argument supplied after this command identifies a phase from [Roadmap.md](../../Roadmap.md), in any of these forms: `1A`, `Phase 1A`, `2C`, `Phase 3D`. Strip the word "Phase" and surrounding whitespace before matching.

This prompt takes a single roadmap phase from documented to implemented, tested, committed, and pushed, in small verifiable steps, then returns to `main` so the next phase always starts clean.

---

## Step 1 — Load Phase Definition

`Roadmap.md` is the single source of truth for phase scope. Do not consult or invent any other plan document.

1. Open `Roadmap.md` and locate the heading matching the requested phase (e.g. `## Phase 1A — Domain Foundation`).
2. Extract exactly: **Phase name**, **Deliverables** (bullet list), **Exit criteria** (bullet list), **Estimated effort** (informational only, doesn't gate anything).
3. If the phase ID doesn't match any heading, stop and report the available phase IDs found in `Roadmap.md`. Do not guess or substitute a nearby phase.
4. Note the phase's position in the `## Implementation Order` sequence (Domain → Application → Infrastructure → API → Frontend) so later layering checks know what's in scope for *this* phase versus a later one.

Also check, if present in the repo, and read for conventions only (skip silently if absent): `Architecture.md`, `Api_Contracts.md`, `Code_Standards.md`, `Critical_Rules.md`.

Do not begin implementation until the phase's Deliverables and Exit Criteria are both extracted and understood.

---

## Step 2 — Plan Micro-Steps From Deliverables

Roadmap.md phases are already scoped to ~1-2 hours and already enumerate concrete deliverables — treat each deliverable bullet as one micro-step by default. Do **not** re-decompose further unless a single bullet is clearly compound (e.g. "Both pricing strategies + unit tests" → split into implementation and test-writing).

List the micro-steps before writing any code, in implementation order (Domain → Application → Infrastructure → API → Frontend). For each, name what gets built and which exit-criteria bullet(s) it contributes to. If the phase has 3 deliverables, expect roughly 3 micro-steps — don't over-decompose a phase that's already small.

---

## Step 3 — Create or Switch Branch

Slugify the phase name into: `feature/phase-<id-lowercase>-<slugified-name>`

Example: `## Phase 1A — Domain Foundation` → `feature/phase-1a-domain-foundation`

```
git status                     # confirm clean working tree before switching
git branch --list <branch>     # check existence
```
If it exists: `git checkout <branch>`. Otherwise: `git checkout -b <branch>`.

Confirm the active branch with `git branch --show-current`. If the working tree is not clean from an unrelated phase, stop and report this rather than switching — do not silently carry over uncommitted work.

---

## Step 4 — Implement Each Micro-Step

For each micro-step in order:

**4.1 — Build only this micro-step.** Implement exactly the scope named in Step 2 — nothing from a later micro-step or later phase. Respect Clean Architecture boundaries per the roadmap's layering and any conventions found in `Architecture.md`/`Api_Contracts.md` if present.

**4.2 — Build and verify compilation.**
```
dotnet build
```
Zero errors, zero unaddressed warnings. Fix immediately before proceeding.

**4.3 — Add tests for this micro-step.** Match test type to what was built: unit tests for entities/value objects/use cases/pricing logic, integration tests for repositories/DB/cache, controller/API tests for endpoints. Arrange-act-assert, one behavior per test, cover the happy path plus edge cases the roadmap calls out explicitly (e.g. Phase 1B names "zero, negative, large numbers" as literal test cases).

**4.4 — Run tests.**
```
dotnet test --filter "FullyQualifiedName~<TestClassForThisMicroStep>"
dotnet test
```
**Failure handling:** if a test fails, attempt a fix and re-run. If the same micro-step's tests are still failing after **2 fix attempts**, stop this run entirely — do not keep retrying, do not skip the test, do not commit. Report the micro-step, the failing test(s), what was tried, and the current error output, then hand control back.

**4.5 — Self-review against exit criteria.** Check the micro-step's output against the specific Exit Criteria bullets it claims to satisfy, plus: no accidental cross-layer dependency, no hardcoded logic where the roadmap specifies an interface/strategy pattern, API responses match `Api_Contracts.md` if present, no missing validation a deliverable bullet implies. Fix any gaps before committing.

**4.6 — Commit.**
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

**4.7 — Next micro-step.** Repeat 4.1–4.6 for the next micro-step.

---

## Step 5 — Phase-Level Verification

1. **Full suite:** `dotnet test` — all green, including everything from prior phases (no regressions).
2. **Exit criteria sweep:** go back to the full Exit Criteria list from Step 1 and check off each bullet individually, not just the ones already touched in 4.5. Mark ✅ or 🚫 per bullet.
3. **Runtime check, if the phase is runnable:**
   ```
   dotnet run --project SkyRoute.API
   ```
   Manually exercise the relevant endpoint/flow, confirm no runtime errors, then stop the process.
4. If any exit criterion is 🚫, do not push — return to Step 4 for the relevant micro-step.

---

## Step 6 — Push and Return to Main

Only after Step 5 shows all exit criteria ✅:
```
git push -u origin <branch>
git checkout main
git pull
```
Confirm `git branch --show-current` reports `main` before finishing. Leave the feature branch intact (already pushed) — merging/PR review is outside this prompt's scope.

---

## Step 7 — Completion Report

* **Phase implemented:** id and name
* **Branch:** name, and whether it was pushed
* **Micro-steps completed:** one-line result per step
* **Files created / modified**
* **Tests added:** test class names and counts
* **Commits:** short hash + message, one per micro-step
* **Exit criteria status:** ✅ / 🚫 per criterion
* **Recommended next phase:** per the roadmap's stated implementation order
* **Notes:** assumptions made, any deviation from `Architecture.md`/`Api_Contracts.md` (should be rare and justified)

If execution stopped early per a Step 4.4 or Step 5 condition, report exactly where and why instead of the full output above.

---

## Hard Rules

* `Roadmap.md` deliverables and exit criteria are authoritative — do not add scope, even "obviously useful" scope, from outside the named phase.
* Never commit a micro-step with a red build or a failing test.
* Never push a phase whose exit criteria aren't all ✅.
* On 2 consecutive failed fix attempts for the same failure, stop and ask — do not loop indefinitely.
* One commit per micro-step, not one giant commit per phase.
* Always return to `main` at the end of a successful run, so the next invocation starts from a clean base.