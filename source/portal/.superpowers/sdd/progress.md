# Progress Ledger — 管理員輸入介面調整

Plan: docs/superpowers/plans/2026-07-16-population-admin-edit.md
Spec: docs/superpowers/specs/2026-07-16-population-admin-edit-design.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: 3a3db3d (plan doc committed at 3a3db3d, spec at 57d8b0c, just before Task 1 starts)
User pre-authorized: during execution, do not stop to ask for confirmation — always proceed with the recommended/default choice at any decision point.

## Tasks

- [x] Task 1: complete (commit eca1118, review clean — approved; StudentPopulationItemLog schema fix (LastWeekNumber/IsNew un-NotMapped, +ChangeLastWeekNumber/IsDeleted/MemberId+Member), migration applied to dev DB NewPAS0716, sqlcmd-verified. Minor repo-hygiene items surfaced, not defects: stray `Data - Backup.csproj` confuses `dotnet ef` cwd resolution; `Portal/appsettings.json` has a pre-existing uncommitted dev/prod divergence between disk and HEAD — both correctly left untouched)
- [x] Task 2: complete (commit a5eecb1, review clean — approved; AddNewClass/RemoveClassItem/UpdateClassDetail/UpdateClassDetail2 all bypass the Documented-lock for PopulationWeekSwitch users, ViewBag.CanEditLastWeek correctly set only on the two PartialView-returning methods, ConfirmPopulation/UpdateClassItem confirmed untouched. Submitted-week test record noted for later manual verification: StudentPopulationId 2344, Year 115 Week 1)
- [x] Task 3: complete (commit 9669a11, review clean — approved; UpdateClassItem gained lastWeekNumber param, reviewer specifically verified the security-critical `lastWeekApplied = lastWeekNumber.HasValue && canEditLocked` gate correctly prevents non-PopulationWeekSwitch users from setting item.LastWeekNumber or triggering SumPHPopulation via direct POST, normal-user path algebraically unchanged)
- [x] Task 4: complete (commit e515789, review clean — approved; 8 files (controller 5x ViewBag.CanEditLastWeek + PopulationPartialView.cshtml x2 + ASPopulationPartialView.cshtml x1 + 5x CreateXXXPopulation.cshtml lastWeekValueChange JS), reviewer independently verified all 8 ViewBag.CanEditLastWeek sites against the live file (correctly skipped Index action's 6th SelectedYear occurrence), all 3 view conditionals retain else-fallback to plain text for non-privileged users, all 5 JS functions POST correct lastWeekNumber field with matching area="" convention)
- [x] Task 5: complete (commit 2230894, review clean — approved; WriteItemLog helper + wired into UpdateClassItem/AddNewClass/RemoveClassItem/UpdateClassDetail/UpdateClassDetail2, reviewer independently re-derived argument order at all 5 call sites against the helper's declared signature and found zero transposition (the single highest-risk failure mode for this task — a swapped positional arg would silently corrupt the audit trail with no compile error), confirmed no Task 2/3 permission logic was altered. Minor note: UpdateClassDetail/2 log unconditionally on any save (no diff-check), which is plan-mandated behavior not an implementation defect. All 5 tasks of this plan now complete.)

## Final whole-branch review (2026-07-17, opus, base 3a3db3d..2230894, 5 commits)

Ready to merge: With fixes — one Important operational gap, no Critical/code-defect findings. End-to-end 6-action user journey (edit 本週/上週人數, edit class name/type, add class, remove class) traced clean across lock-bypass + ViewBag + audit-log + recalc for all 5 unlocked actions. Non-privileged-user regression check is clean: every gate algebraically reduces to original behavior when `canEditLocked=false`, so already-submitted weeks stay fully locked and 上週人數 stays plain text for everyone without `PopulationWeekSwitch` — this was the single most important property and it holds. Migration's 5 new columns match exactly what `WriteItemLog` assigns (no unused column, no unbacked property). `ConfirmPopulation` confirmed untouched across all 5 commits.

Important (operational, not a code defect):
1. **`AddStudentPopulationItemLogAuditFields` migration must be applied to production before this ships.** Because `WriteItemLog` now runs on *every* edit by *every* user (not just the new privileged bypass path), an unapplied migration would break all 5 editing actions for ALL users in production — the real data change would commit, then the log `SaveChanges()` would throw on the missing column, leaving the user with a blank partial (UpdateClassItem/AddNewClass/RemoveClassItem's catch returns `new StudentPopulation()`) or a false error alert (UpdateClassDetail/2's catch/return returns `success=false` on a change that actually persisted). This requirement was not documented anywhere in the plan/spec — add it to the deploy checklist, same as every prior aggregation-engine migration in this project.

Minor (non-blocking, already triaged at task level, nothing new found):
2. Stray `Data - Backup.csproj` confuses `dotnet ef` cwd resolution (Task 1).
3. Pre-existing uncommitted `Portal/appsettings.json` dev/prod connection-string divergence between disk and HEAD (Task 1) — unrelated to this work, correctly left untouched.
4. `UpdateClassDetail`/`UpdateClassDetail2` log unconditionally on every call with no diff-check — plan-mandated, not an implementation defect (Task 5).
5. Pre-existing, unrelated to this feature: `sumValueChange` JS in the Create views posts to a `UpdateSumClassItem` action that doesn't exist in the controller — present before this plan's base commit, out of scope.

**Test coverage reality check (honest, not papered over):** nothing in this feature has been exercised against a running app yet — all verification so far is `dotnet build` + static code/schema inspection + read-only sqlcmd checks. No browser tool available in this environment. Deferred to the user: after applying the migration to whatever DB is used for manual testing, verify at minimum (a) a `PopulationWeekSwitch` user's edit on an already-submitted week recalculates and produces a real audit row with a populated `MemberId`, and (b) a normal user still sees a fully locked already-submitted week with 上週人數 as plain text.

Recommendation: merge with the production-migration-deployment note added to the deploy checklist; run the two-point manual verification above once a browser/environment is available.

---

# Archive — PSJ+AS Aggregation Migration (completed 2026-07-16)

Plan: docs/superpowers/plans/2026-07-16-psj-as-aggregation-migration.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: d65fdec (plan doc committed at d65fdec, spec at 6b03de9, just before Task 1 starts)

## Tasks

- [x] Task 1: complete (commit af3e1c4, review clean — approved; 76 rows configured, all 24 individual Id-14 SourceCourseIds mappings verified arithmetically correct with no transcription errors, GroupByClassType 1/0 split applied to exact correct rows, revert script exact inverse scope. Controller independently re-verified via fresh sqlcmd query. Minor: 158/208's GroupByClassType=0 is implicit/default rather than explicit — inherited from plan text, no risk under current conditions)
- [x] Task 2: complete (commit 8d3002a, review clean — approved; test found 3 unexpected mismatches on first run against 114 real PSJ populations (pop 2277 course207, pop 2299 courses165/166), root-caused as stale stored aggregates — PSJPopulationImporter only writes raw items and never recomputes IsSum aggregates, so source rows imported/edited after a population's last UI save leave the stored aggregate stale; legacy formula and new engine config confirmed formula-identical. **User decision (2026-07-16): recompute the 2 stale populations** (AggregationEngine.CalculateAll + SaveChanges on pop 2277/2299, equivalent to a UI open+save) rather than add a documented exception. Recompute done via throwaway script (not committed), independently verified via fresh sqlcmd — pop2277 course207 0→3, pop2299 course165 -3→-5, course166 -1→0, all confirmed. Final test: 114 checked, 0/0 mismatches. Controller further independently verified the specific row-level data and confirmed the "2 rows per course" pattern seen during spot-check is NOT duplicate/bad data — it's the correct GroupByClassType=true per-ClassType breakdown (one IsSum item per real ClassType present), differing UpdatedTime explained by EF only writing rows whose computed value actually changed. Reviewer's only Minor note: the recompute has no durable git record — noted here in the ledger as the durable record)
- [x] Task 3: complete (commit 87ab206, review clean — approved; 4-line diff, old logic verified byte-identical inside #if false by character-by-character comparison, only PSJ branch touched, aggregationEngine.Calculate reuses shared instance, PsjAggregationComparisonTests re-confirmed 0 unexpected mismatches. PSJ side of the migration fully done.)
- [x] Task 4: complete (commit ebf2859, review clean — approved; 76 rows configured, all 24 Id-14 SourceCourseIds mappings verified correct, GroupByClassType confirmed 0/false everywhere (0 rows with =1, the key constraint distinguishing this from PSJ's Task 1) both by reviewer diff-search and controller's independent sqlcmd query. Known sqlcmd -i flakiness struck again (5/30 statements silently skipped first pass) — caught via independent verification, fixed, re-verified twice by implementer + once more by controller)
- [x] Task 5: complete (commit ced4665, review clean — approved; clean first-try pass, 45 AS populations checked, 0/0 mismatches, no stale-aggregate issue like Task 2 hit. Implementer went beyond brief: confirmed all 76 AS course IDs appear in real data (28 engine-computed ones genuinely exercised) and confirmed 257/258+307/308 pairs show identical stored values wherever both exist, proving GroupByClassType=false faithfully reproduces the old undifferentiated-total quirk. Reviewer independently confirmed AfterSchool is a real distinct enum value, ruling out a silently-empty-set false-clean-pass. Minor: missing an explanatory comment present in the PSJ sibling file, cosmetic only)
- [x] Task 6: complete (commit fbb03cd, review clean — approved; 4-line diff, old logic verified byte-identical inside #if false, only AS branch touched, aggregationEngine.Calculate reuses shared instance. Task 6's implementer subagent repeatedly failed to complete a full-suite test run (ended its turn twice while "waiting" on its own background process) — controller took over directly: build 0 errors, focused AsAggregationComparisonTests 45/0/0, then diagnosed the full-suite hang as a pre-existing unrelated Selenium `Home` test (Test/Page.cs) that launches a real ChromeDriver and hangs with no browser available — recurring across multiple tasks this whole session. With user approval, marked it [Explicit] (separate commit 0b34f57, out-of-plan hygiene fix, matches existing *AggregationComparisonTests convention). Full suite then ran clean in 2s: 25 passed, 0 failed, 7 correctly Explicit-skipped. All 6 tasks of this plan now complete — PSJ and AS both fully migrated to AggregationEngine.)

Note: pending from the prior plan (PSJ CKC import rewrite) — `git push` to origin is still outstanding.
Will push both plans' commits together.

## Final whole-branch review (2026-07-16, opus, base d65fdec..0b34f57, 7 commits)

Ready to merge: With fixes — the only blocker is operational (SQL apply scripts must run on production), not a code defect. No Critical issues. Cross-checked all 3 artifacts at once (PSJ apply script + AS apply script + design spec's rule tables + disabled old code) — every course ID and GroupByClassType value agrees exactly, including the deliberate PSJ-vs-AS asymmetry (PSJ 157/207 split by classtype, AS 257/307 do not). SumPHPopulation confirmed internally consistent across all 5 migrated branches (PH/PSJ/PS/AS same #if-false+single-call shape, GEPT is the lone bare call since its old code was deleted per that migration's own precedent). Both comparison tests confirmed genuinely read-only (three-phase compute/compare/restore, never SaveChanges) and hit real live data.

Important (operational, not a code fix):
1. **Both SQL apply scripts (PSJ, AS) are dev-DB-only** — must be run against production before/with this code shipping, same requirement as every prior GEPT/PH/PS migration, but never explicitly stated in any of these scripts' headers (pre-existing documentation gap, not new). Add to deploy checklist.

Minor (non-blocking, recorded for later):
2. Migration doesn't fix the underlying stale-aggregate root cause (importers never trigger recompute after writing raw items) — only symptoms (2 populations) were cleared. Comparison test does catch staleness at test time (0 mismatches = proven fresh), but new staleness can recur until a UI save. Pre-existing behavior, not introduced here. Recommend a future ticket: importers call AggregationEngine.CalculateAll after writing raw items.
3. Recompute of populations 2277/2299 has no durable git record (out-of-band script, not committed) — acceptable since it's equivalent to an ordinary UI save, but optionally worth one committed sentence for durability.
4. AsAggregationComparisonTests.cs omits the three-phase-rationale comment its PSJ sibling carries — cosmetic only.
5. Test/Page.cs [Explicit] fix confirmed safe/minimal/out-of-plan, not scope creep.
6. Pre-existing empty catch in SumPHPopulation's classGroup loop now routes more logic through a swallow that would hide a NotSupportedException if a course were ever misconfigured — not triggerable today, pre-existing pattern.

---

# Archive — PSJ CKC Import Rewrite (completed 2026-07-16)

Plan: docs/superpowers/plans/2026-07-16-psj-ckc-import-rewrite.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: 2e53483 (plan doc committed at 2e53483, just before Task 1 starts)

## Final whole-branch review (2026-07-16, opus, base 2e53483..83d12c4, 7 commits)

Ready to merge: With fixes — the only true blocker is a decision, not a code change. No Critical or Important *code* defects. Full review output not re-pasted here — see conversation.

Confirmed cross-task coherence: PSJPopulationImporter.cs reads as one class post Task4+5 (no seams), Scan/Import skip exactly the same unresolvable schools, GradeOrder/PsjGradeOrder dual-array design traced end-to-end with no cross-indexing possible, Course IDs 345-377 agree across SQL script/CkcCourseIds/Import consumption, out-of-scope boundaries (aggregate courses 157-244, AS importer, browser tests) all held, CorrectLastWeekNumbers confirmed to work correctly for PSJ's first-time-ever populations (groups by CourseId+ClassType, the right key for PSJ's 一對一/小組班 model, no-ops safely with no prior-week data).

Important (decision needed, not a code defect):
1. **HomeController.ImportAll cannot batch-import PSJ after this change** — it calls importService.Import(db, PSJ, fs, fn) with no year/week, which now hard-errors ("PSJ 匯入必須指定學年度與週次"). Non-fatal (caught, logged, loop continues) and not a regression (PSJ never wrote real rows via any path before), but means the batch backfill tool that historically bulk-processes the other 4 formats week-by-week from 人數表匯入A\<week>\ folders structurally cannot backfill historical PSJ/CKC data — arguably the whole point of this feature. The plan never mentions ImportAll (only scoped admin-UI changes); this is a genuine scope gap the plan didn't consider, not an implementation bug. **User decision (2026-07-16): admin-page-only manual week-by-week upload IS the intended flow.** `ImportAll` intentionally does not support PSJ and does not need year/week wiring — accepted as a deliberate scope boundary, not a gap to fix.
2. **SQL apply script (Task 3) is dev-DB-only** — confirmed applied+verified on dev, but is a separate ops step (same as prior GEPT/PH/PS migrations) that must run against production before/with this code shipping, or every PSJ CKC import silently writes zero CKC rows in prod. Not a code fix — an operational reminder for the deploy checklist.

Minor (non-blocking, recorded for later):
3. Task 7's CKC-all-zero assertion (carry-forward from task review) queries db.Class globally, not scoped to this run's populations — reviewer says acceptable to ship as-is (currently correct, non-vacuous, [Explicit]-only), optionally scope to result.PopulationIds later.
4. Scan doesn't validate SchoolYear exists (Import does) — asymmetric but inert since the admin dropdown only posts real SchoolYear rows.
5. Zero-resolvable-schools upload silently succeeds with schoolCount=0 — minor UX only.
6. View's `<option value="@sy.Id">` is dead (JS reads data-year/data-week instead) — harmless but confusing for future maintainers.

## Tasks

- [x] Task 1: complete (commit cd26d2c, review clean — approved; exact match to brief, 7 files, no body-logic changes to PH/GEPT/PS/AS, PSJ signature-only. Controller independently verified no other call site bypasses PopulationImportService. Test suite: 19 passed, 1 pre-existing unrelated Selenium failure (no browser in this environment), 3 Explicit fixtures correctly skipped)
- [x] Task 2: complete (commit 9e4fcc4, review clean — approved; PsjGradeOrder/CkcCourseIds/PsjSchoolNameAliases verified byte/number-identical to brief, zero pre-existing CourseMapping members touched, 3/3 tests pass with real RED/GREEN evidence. Reviewer's ⚠️ on DB-side correctness of 高美→高美館 already independently confirmed by controller via live sqlcmd query during spec investigation)
- [x] Task 3: complete (commit 5fbd93b, review clean — approved; implementer found a real gap in the plan's literal SQL — Course.Id/CourseDepartment.Id are IDENTITY columns on the live schema, so the brief's SQL as written fails with error 544 and inserts nothing — fixed by adding SET IDENTITY_INSERT ON/OFF wrapping around the INSERTs only, no row values changed, reviewer mechanically diffed all 33 rows byte-identical to brief otherwise. Controller independently re-verified live DB via fresh sqlcmd query: CourseCount=33, DeptCount=1, all Id/Name/Ordinal/ClassType/IsSum/Published/DataMode values correct)
- [x] Task 4: complete (commit 8b43ce0, review clean — approved; ColumnLayout/FindSchoolBlocks/ResolveSchool verified byte-identical to brief, old GetSheetAt(0) implementation fully removed with no dead code, Import correctly left as scope-boundary stub, 3/3 PsjBlockDetectionTests pass against the real fixture file. Two disclosed test-file deviations (added using NPOI.SS.UserModel, omitted redundant using NUnit.Framework, force-add for gitignored Test/ path) independently re-verified by reviewer as genuine necessities matching Task 2's precedent, not assertion changes. Minor: 2 now-unused usings + unused ColumnLayout fields + +20 magic number are pre-existing rough edges inherited verbatim from the plan's own code — expected to resolve naturally once Task 5 lands, no action needed now)
- [x] Task 5: complete (commit edb431c, review clean — approved; single-file diff (75+/1-), ColumnLayout/FindSchoolBlocks/ResolveSchool/Scan confirmed byte-identical/untouched. Reviewer traced the highest-risk correctness point in detail: GradeOrder(12-item,含一年級)+PsjCourseIds vs PsjGradeOrder(11-item CKC-only)+CkcCourseIds pairing is correct, no cross-wiring. WriteIfPositive centralizes read/lookup/write/no-op uniformly across all 10 call sites. Minor (non-blocking): per-cell Course requery pattern matches pre-existing codebase convention (AS/PS/PHSheetReader), not a regression. Real runtime correctness against live Excel/DB deferred to Task 7 per plan)
- [x] Task 6: complete (commit d7b1aca, review clean — approved; SchoolYear query pattern verified byte-identical to existing StudentPopulationController.cs precedent, Import action's year/week params correctly forward to PopulationImportService's Task-1 signature, Razor/JS traced line-by-line with no encoding/null-reference issues found. Minor: theoretical SchoolYear.Week==null edge case inherited from schema nullability, inert in practice, not a task defect)
- [x] Task 7: complete (commit 83d12c4, review clean — approved; test passed on FIRST real run against live file + dev DB, "Schools imported: 11, items: 41". One using-directive fix (ImportSupport namespace), no logic/assertion changes. Controller independently spot-checked via sqlcmd: 南京 SubGroup=1 confirmed, 高美館's two same-named-but-different-CourseId "國二" courses (math vs 理化) correctly isolated by the MS-course-scoped query (Number=3 as expected), 12 total PSJ populations for Year114/Week50 = 11 touched by this run + 1 pre-existing unrelated (向上, not in this file) + 莊敬 correctly reused its pre-existing population Id via deleteExisting:true rather than duplicating. Reviewer flagged one Important (non-blocking) finding: CKC-all-zero assertion queries db.Class globally, not scoped to this run — matches plan's literal wording, currently correct, but could false-fail if any other CKC data is ever written elsewhere in the dev DB; deferred to final whole-branch review, not fixed now. Minor: .First() vs .Single(), dead .Include(), 內湖 split-class assertion checks count not individual values — all non-blocking)

---

# Archive — PS Aggregation Engine Migration (completed 2026-07-15)

Plan: docs/superpowers/plans/2026-07-15-ps-aggregation-migration.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: 0a29bb1 (plan doc committed at 0a29bb1; spec committed at f6ba9ad, just before Task 1 starts)

## Tasks

- [x] Task 1: complete (commit dcd98c2, review clean — approved; Average case + GetSourceItems restructure verified byte-identical to brief, TDD RED/GREEN evidence credible, regression-lock test genuinely falsifiable, no new enum members added, SourceDepartmentIds/DepartmentId paths still exclude IsSum items)
- [x] Task 2: complete (commit 036071f, review clean — approved; controller independently re-verified all 16 rows correct via fresh sqlcmd query; sqlcmd -i again silently skipped 3 statements first pass (known flaky behavior, third time seen this session), fixed via individual -Q statements, final DB state and committed SQL files both match the spec table exactly)
- [x] Task 3: complete (commits b5cd039..40dd6b6, review clean — approved after fixing a review-flagged Important defect: original per-item restore erased course 132's freshly-computed value before course 144 could read it, making the Ordinal ordering functionally inert despite 0 mismatches either way; restructured to calculate-all/compare-all/restore-all passes so 144 now genuinely observes 132's live value, matching what Task 4's production classGroup loop will do. Final: 98 PS populations checked, 0 expected changes, 0 unexpected mismatches)
- [x] Task 4: complete (commit 63b8b25, review clean — approved; classGroup gained OrderBy(Course.Ordinal) as its only change, PS in-loop branch #if false verified byte-identical to live pre-change code, single new aggregationEngine.Calculate(...) call reuses existing instance, PSJ/AfterSchool/GEPT/PH branches (in-loop and post-loop) confirmed untouched, build 0 errors, PsAggregationComparisonTests + AggregationEngineTests both still green. Manual browser verification (plan Step 5) explicitly deferred — no browser tool in this environment)

## Final whole-branch review (2026-07-15, opus)

Ready to merge: Yes (with one disclosed pre-merge condition — manual browser verification not yet run, no browser tool available). No Critical/Important code defects found. Full review output not re-pasted here — see conversation.

Confirmed regression-safe with concrete reasoning (not just "theoretical, no mechanism found" like the task-level review left it): traced every non-PS branch (PSJ, AfterSchool inline code; GEPT/PH via engine with SourceDepartmentIds) and confirmed none of them ever reads another summary item's freshly-computed value — GetSourceItems excludes IsSum items on every path except SourceCourseIds, and only PS's courses 142/144 use SourceCourseIds. So classGroup's new OrderBy(Ordinal) is provably inert for GEPT/PH, not just assumed safe. Also confirmed via the SQL apply scripts that neither GEPT nor PH configures SourceCourseIds at all, so the GetSourceItems restructuring is dead code for them.

Minor findings (non-blocking, recorded for later):
1. `AggregationEngine.CalculateAll` sorts by `Ordinal ?? 0` while the controller's `classGroup` sorts by `Ordinal` (no coalesce) — both deterministic today, but the divergence is a latent trap; consider aligning.
2. No cycle/self-reference guard on `SourceCourseIds` now that it can reference summary courses (e.g. A→B, B→A) — would silently produce order-dependent wrong numbers, not a crash/hang. Not exploitable by current config, but worth a validation or documented warning given Admin UI lets staff hand-configure this field.
3. The empty `ExpectedChangeCourseIds` set proves "engine == stored data" (which was produced by import, not by the old buggy save path) — NOT "engine == old save code." Two real behavior changes are intentionally hidden inside that "0": course 144's bug fix (old code wiped 百倍速 out of 144; engine correctly includes it) and courses 138/142 transitioning from manual-editable to auto-computed. Both correct and documented, but worth remembering when reading "0 mismatches" as more than data-parity.
4. Course 144's fix (144=132+135) is validated by reasoning more than by non-trivial observed data — for the test to show 0 mismatches, course 135 must be ~0 or already-consistent across essentially all 98 populations. Recommend specifically confirming a nonzero-135 case during the manual browser verification step.
5. Course 142's DiffWithLastWeek depends on LastWeekNumber being correctly maintained on the *summary* items 132/135 (not just raw items) — holds for current data, flagged as an ongoing data-maintenance dependency.
6. Pre-existing `catch (Exception ex) { }` swallow around the classGroup loop now has two silent-staleness failure modes for PS (NULL StatisticsType no-ops the engine; any engine exception gets silently swallowed too) — tech debt, not introduced here.
7. `CourseDepartment.Id=13` cross-type reuse (already noted in the design spec) — manual browser verification should also confirm no stray cross-type Class/Course rows landed in any PS population's Items, analogous to PH's course-67 finding (comparison test's 0-mismatch result implies none did, but wasn't independently audited row-by-row).
8. Accumulating `#if false` debt across PH and PS branches (GEPT was deleted outright) — worth a single follow-up cleanup task once all three are browser-verified, already anticipated in the spec's Out of scope section.

Recommendation: SQL apply script must be run against production together with/before this code ships (same operational constraint as GEPT/PH — code-without-SQL freezes all 16 PS summary courses at whatever value they last held).

---

# Archive — PH Aggregation Engine Migration (completed 2026-07-15)

Plan: docs/superpowers/plans/2026-07-15-ph-aggregation-migration.md
No worktree used (project convention: commit directly on develop/portal, no branching).
Base commit: 02d99cb

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
