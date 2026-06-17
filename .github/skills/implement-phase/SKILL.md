---
name: implement-phase
description: Implement a SkyRoute roadmap phase end-to-end in small, tested, committed increments — branch, implement, verify, push, return to main. Use when the user asks to implement, build, or work on a specific roadmap phase (e.g. "implement phase 1A", "build out 2C", "do phase 3D").
---

# Implement Phase

The argument supplied after invoking this skill identifies a phase from [Roadmap.md](../../../Roadmap.md), in any of these forms: `1A`, `Phase 1A`, `2C`, `Phase 3D`. Strip the word "Phase" and surrounding whitespace before matching.

This skill takes a single roadmap phase from documented to implemented, tested, committed, and pushed, in small verifiable steps, then returns to `main` so the next phase always starts clean.

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

## Step 2 — Sequence the Deliverables

Execute the phase's Deliverables list from `Roadmap.md` in order, one bullet per build → test → commit cycle (Step 4). Each bullet is its own micro-step by default — split a single bullet into two micro-steps only when it bundles two distinct testable units (e.g. "Both pricing strategies + unit tests" → one micro-step for the strategy implementations, one for the tests). For each micro-step, note which Exit Criteria bullet(s) it's expected to satisfy, so Step 4.5's review has something concrete to check against.

---

## Step 3 — Create or Switch Branch (Once Per Phase, Not Per Micro-Step)

**Run this step exactly once, before Step 4 begins.** Every micro-step in Step 4's loop commits onto this same branch — do not re-run this step, re-check, or create a new branch when looping back to Step 4.1 for the next micro-step. One phase = one branch = many commits on it.

Build the branch name as: `feature/<id-lowercase>-<short-slug>`

* `<id-lowercase>` is the phase ID only (e.g. `1a`, `2c`) — do not include the word "phase"; the ID alone is unambiguous given this naming pattern.
* `<short-slug>` is the first 2-3 meaningful words of the phase name, lowercased and hyphenated, dropping connector words (`and`, `&`, `the`, etc.) and dropping anything past the third meaningful word. This keeps branch names scannable instead of mirroring the full heading verbatim.

Examples:
* `## Phase 1A — Domain Foundation` → `feature/1a-domain-foundation`
* `## Phase 2C — Angular App Scaffold & Services` → `feature/2c-angular-scaffold`
* `## Phase 3D — Create Booking API Controller` → `feature/3d-booking-api-controller`

```
git status                     # confirm clean working tree before switching
git branch --list <branch>     # check existence
```
If it exists: `git checkout <branch>`. Otherwise: `git checkout -b <branch>`.

Confirm the active branch with `git branch --show-current`. If the working tree is not clean from an unrelated phase, stop and report this rather than switching — do not silently carry over uncommitted work.

---

## Step 4 — Implement Each Micro-Step

For each micro-step in order, **on the single branch created in Step 3** — looping back here for the next micro-step never means going back to Step 3:

**4.1 — Build only this micro-step.** Implement exactly the scope named in Step 2 — nothing from a later micro-step or later phase. Respect Clean Architecture boundaries per the roadmap's layering and any conventions found in `Architecture.md`/`Api_Contracts.md` if present.

**4.2 — Build and verify compilation.**
```
dotnet build
```
Zero errors, zero unaddressed warnings. Fix immediately before proceeding.

**4.3 — Add tests for this micro-step.** Match test type to what was built: unit tests for entities/value objects/use cases/pricing logic, integration tests for repositories/DB/cache, controller/API tests for endpoints. Arrange-act-assert, one behavior per test, cover the happy path plus edge cases the roadmap calls out explicitly (e.g. Phase 1B names "zero, negative, large numbers" as literal test cases).

**4.4 — Run the scoped test.**
```
dotnet test --filter "FullyQualifiedName~<TestClassForThisMicroStep>"
```
Only this micro-step's own new/changed tests need to pass here — the full suite runs once at phase end (Step 5), not after every micro-step, to avoid re-running the growing cross-phase suite N times per phase.

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
Confirm `git branch --show-current` reports `main` before finishing. Leave the feature branch intact (already pushed) — merging/PR review is outside this skill's scope.

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