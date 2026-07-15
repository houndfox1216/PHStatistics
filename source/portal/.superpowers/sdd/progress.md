# Progress Ledger — PH Aggregation Engine Migration

Plan: docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: 02d99cb (plan doc committed at 36170a8; two unrelated pre-existing bug fixes —
logout session clear, AS 新生/流失 onchange wiring — committed at 02d99cb just before Task 1
started, so they don't pollute Task 1's diff)

## Tasks

- [x] Task 1: complete (commit 55d1f27, review clean — approved; controller independently re-verified all 24 rows correct via fresh sqlcmd query after re-running apply script twice; sqlcmd's console message count is unreliable/lower than statement count on this box but final DB state and committed SQL file content both match the spec table exactly)
- [x] Task 2: complete (commits 55d1f27..3b608e8, review clean — approved after user resolved a mid-task blocker: test initially found 12 unexpected mismatches across 6/920 PH populations, root-caused to 2 genuine engine-vs-old-code behavior differences [course 67 總人數: new engine excludes stray cross-type dept-27 data, more correct; course 35/62 與上週相比: LastWeekNumber cache diverges from live query due to PH's EM1 per-student weekly Class.Id churn]. User decided: accept both as additional documented exceptions. ExpectedChangeCourseIds now {22,34,35,49,61,62,67}, design spec updated, 0 unexpected mismatches confirmed, 2 follow-up doc-consistency fixes applied)
- [x] Task 3: complete (commit 4a8db34, review clean — approved; both #if false blocks verified byte-identical to live pre-change code, single new aggregationEngine.Calculate(...) call reuses existing GEPT-shared instance, PS/PSJ/GEPT/AfterSchool untouched, build 0 errors, PhAggregationComparisonTests still 0 unexpected mismatches. Manual browser verification (plan Step 5) explicitly deferred — no browser tool in this environment — to a later consolidated verification pass, per established project pattern)

## Final whole-branch review (2026-07-15, opus)

Ready to merge: With fixes — the only "fix" is the disclosed non-code condition (manual browser verification not yet run, no browser tool available). No Critical/Important code defects found. Full review output not re-pasted here — see conversation.

Confirmed regression-safe: `aggregationEngine` instance shared with untouched GEPT branch is stateless (StudentPopulationController.cs:1356/1452/1475); no rule ordering dependency (all 24 PH rules read only raw non-IsSum items, never other summary values); post-loop block's 11 courses are all fully subsumed by the single in-loop engine call, empirically confirmed by 920-population 0-unexpected-mismatch test.

Minor findings (non-blocking, recorded for later):
1. Test's 7-ID `ExpectedChangeCourseIds` allowlist (`PhAggregationComparisonTests.cs`) masks ANY mismatch on those IDs, not just the documented one — fine for a one-time `[Explicit]` migration gate, but if ever promoted to a recurring/CI guard should assert the specific expected relationship instead of blanket-allowing the ID.
2. `catch (Exception ex) { }` around the classGroup loop (StudentPopulationController.cs:1517) now also swallows anything `AggregationEngine.Calculate` could throw (e.g. unsupported StatisticsType) with no log — pre-existing pattern (GEPT already runs through it), not introduced here, but the engine is now the most likely thing to throw inside this swallow.
3. Manual-input courses now get reloaded+resaved unchanged instead of hitting the old `continue` — behaviorally equivalent, matches how GEPT already works, just a real control-flow difference worth knowing about.

Recommendations for future work:
- Track "delete the #if false PH blocks" as an actual follow-up task (gated on browser verification) so ~260 dead lines don't linger indefinitely in a hot 600-line method — GEPT's equivalent migration deleted outright (0e8b4dc); PH's conservative choice was deliberate but still needs a follow-through step.
- File a data-cleanup task for the dept-27/Type=1 stray rows found polluting 4 PH populations' course-67 total (root cause behind one of the accepted exceptions) — `ReportExportService.ComputeIsumValue` and any other non-scoped consumer would still miscount from this dirty data.
- The EM1 per-student weekly `Class.Id` churn that broke course 35/62's `LastWeekNumber` cache sync likely also taints course 34/61's "fixed" values for the same underlying reason (their old values were dead-0, so this wasn't visible as a regression, but may not be fully correct either) — worth a future task keying last-week linkage on CourseId+ClassType instead of Class.Id (same fix already applied to the Excel-import LastWeekNumber algorithm per project memory).

---

# Archive — Aggregation Engine + GEPT Migration (completed 2026-07-15)

Plan: docs/superpowers/plans/2026-07-15-aggregation-engine-and-gept-migration.md
No worktree used (project convention: commit directly on develop/portal, no branching).

Task 1: complete (commits 76901d0..d8c9dba, review clean)
Task 2: complete (commits d8c9dba..b42835d, review clean)
Task 3: complete (commits b42835d..98d1dd9, review clean)
Task 4: complete (commits 98d1dd9..c74c38f, review clean)
Task 5: complete (commits c74c38f..ca3ab4d, review clean; minor notes: encoding-fix narrative unverified but end-state correct, apply script doesn't explicitly null unused columns)
Task 6: complete (commits 1ce1ca4..0793473, review clean after resolving evidence gap: independently re-ran sqlcmd count myself, confirmed 902 GEPT populations, 0 mismatches)
Task 7: complete (commits 0793473..0e8b4dc, review clean) — all 7 tasks done

## Final whole-branch review (2026-07-15)

Ready to merge: With fixes. Full review output not re-pasted here — see conversation.

Findings to address in a future session (user said: record now, handle later):

1. **[Important] Courses 91/92 (去年同期人數/去年同期比) last-year Type-scoping changed behavior, unverified.**
   Old GEPT branch code queried last-year StudentPopulationItem with NO `Type` filter (would sum across ALL population types for that school/week, not just GEPT — looks like a latent old bug). New AggregationEngine correctly scopes via SourceDepartmentIds to GEPT-only departments (14-21).
   Verified via SQL: across all 902 GEPT populations in the dev DB, ZERO have any last-year data at all (neither GEPT's own nor any other type's, for the corresponding school+week one year prior) — so the Task 6 comparison's "0 mismatches" never actually exercised this divergence for courses 91/92. Both old and new code trivially return 0 in every tested case.
   Decision needed: accept the new (more correct) GEPT-only-scoped behavior as an intentional fix-along-the-way (recommended), or construct a synthetic multi-type cross-year test case to confirm the divergence is understood before treating GEPT's migration as fully settled.

2. **[Important] Duplicate "適用班別" caption in Admin Course grid.**
   `Areas/Admin/Views/Course/Index.cshtml` — pre-existing `ClassType` column and the new `ApplicableClassType` column (Task 4) both display caption "適用班別". Only `ApplicableClassType` drives the AggregationEngine; `ClassType` does not. Real misconfiguration risk on a screen whose whole point is admin-editable rules. Fix: rename the new column, e.g. "加總適用班別"/"統計限定班別".

3. **[Important] Malformed SourceDepartmentIds/SourceCourseIds JSON silently computes 0.**
   `AggregationEngine.cs` `ParseIntArray` catches parse failure and returns an empty list → empty source-id list → Sum/Count over nothing → silently 0, no error, no log. Now that Task 4 lets admins hand-type JSON arrays into a free-text column, a typo silently zeroes a real report number. Suggested fix: throw instead of silently defaulting to empty (consistent with the engine's existing "fail loud on unsupported StatisticsType" philosophy), or at minimum log a warning.

4. **[Important] Revert SQL (Task 5) has an ordering hazard, undocumented.**
   `docs/superpowers/sql/2026-07-15-gept-aggregation-rules-revert.sql` — if run WITHOUT also reverting the Task 7 code commit (0e8b4dc), GEPT's `StatisticsType` goes back to NULL, `AggregationEngine.Calculate` no-ops on null type, and GEPT summary values would never recompute at all (worse than the original problem). Fix: add a header comment to the revert script stating it must be paired with (or run after) reverting the code commit.

5. **[Minor] Plan doc still has a few stale counts from the 12→13 fix** (commit 598e92c was incomplete): Task 5 Step 3 text, Step 4 commit-message example text, and "8 messages" (should be 7 — the apply script has 7 UPDATE statements). Doc-only, cosmetic.

6. **[Minor, optional]** `None`(0)/`ManualInput`(50) both no-op and both appear in the Admin dropdown — consider collapsing or clarifying. Sum-family enum values (SumByDepartment/SumBySourceDepartments/SumBySourceCourses) are functionally interchangeable (behavior is actually driven by which of SourceDepartmentIds/SourceCourseIds is populated) — a validation opportunity, not a bug.

None of these are Critical. Full plan (all 7 tasks) is implemented, committed, and individually+collectively reviewed. Nothing is blocking further work on PS/PSJ/AS/PH migrations except addressing (or consciously deferring) the above.
