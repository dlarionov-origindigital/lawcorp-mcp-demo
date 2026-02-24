# Claude Code Agent Workflow Rules

**Created:** 2026-02-24  
**Purpose:** Self-reference for consistent work practices across this project

These are the rules I (Claude Code Agent) follow when working with this MCP project management system.

---

## Core Principles

1. **File is truth** — The `.md` file is the canonical definition of work. Always read it first, implement it, verify it, then update it.

2. **Never break hierarchy** — All work items must live in `proj-mgmt/epics/NN-*/` folder structure. Never create top-level work files outside epics.

3. **One file, one concern** — Each `.md` file owns exactly one work item. Don't touch other files unless explicitly asked.

4. **Status before code** — Always check and understand the `**Status:**` field before starting work.

5. **Living history** — Every bug, decision, and change is documented and linked. Future developers should be able to read the docs and understand what was built and why.

---

## Pre-Work Checklist (Before Starting Any Implementation)

- [ ] **Read the item file completely** — Don't skim. Understand the full context.
- [ ] **Check status** — If already `IN PROGRESS` or `DONE`, ask before overwriting.
- [ ] **Verify hierarchy** — Confirm item is in correct epic/feature folder.
- [ ] **Check dependencies** — Read "Blocks" and "Depends on" fields. Are blockers resolved?
- [ ] **Review acceptance criteria** — These define "done". I must verify every one before marking `DONE`.
- [ ] **Check related files** — Read linked ADRs, parent feature file, epic file.
- [ ] **Understand the domain** — If unfamiliar with the domain concept, read related PRD sections.

---

## When Implementing a Story/Task

**Phase 1: Understanding**

1. Read the full story/task file
2. Read parent feature file (N.M-slug.md)
3. Read parent epic file (_epic.md)
4. Read any linked ADRs (decisions/)
5. Check the PRD for domain context
6. List acceptance criteria — these are my "definition of done"

**Phase 2: Implementation**

1. Implement code changes
2. Verify each acceptance criterion works
3. Check that no regressions occurred
4. Document any design choices that weren't obvious
5. Create or update unit tests
6. Verify the solution fits the architecture (re-read relevant ADRs)

**Phase 3: Completion**

1. ✓ Check off ALL acceptance criteria in the file
2. Update `**Status:**` to `DONE`
3. Add code references (file paths, functions) in the file
4. Update parent feature file's status table
5. If parent feature is now fully `DONE`, update epic file

**Phase 4: Documentation**

1. If I made a non-obvious choice, create or update a related ADR
2. If this resolves a PRD open question, close it
3. Link any related bugs or decisions in the "Notes" section

---

## When Triaging or Fixing a Bug

**Phase 1: Analysis**

1. Read the bug file completely
2. Check if it has a dedicated folder (complex bug)
3. Read the feature it's related to
4. Verify reproduction steps (can I recreate it?)
5. Document findings back into the bug file

**Phase 2: Root Cause**

1. Trace through the code path
2. Document root cause in the bug file (`## Root Cause` section)
3. Create or link an ADR if this reveals a design issue
4. Check if this is a regression (did it work before?)

**Phase 3: Fix**

1. Implement the fix
2. Add regression test
3. Verify the bug no longer reproduces
4. Check that no new bugs were introduced

**Phase 4: Resolution**

1. Update bug file:
   - Fill in `## Solution` section with changes made
   - List code file references
   - Check off all resolution acceptance criteria
   - Update `**Status:**` to `DONE`
2. Update parent feature/story file to reference this bug
3. If bug was in a folder structure, update bugs/ index

---

## When Starting New Work

**If given a specific item:** ("Implement 1.3.1")

1. Navigate to `proj-mgmt/epics/01-foundation/1.3.1-*.md`
2. Read the file (see Pre-Work Checklist)
3. Change `**Status:** BACKLOG` → `**Status:** IN PROGRESS`
4. Implement per "When Implementing" section above

**If given a vague request:** ("Add validation for..." without specific item)

1. **System thinking first** — Run through the [Systems Thinking Checklist](#systems-thinking-checklist) below
2. **Find or create the item file** — Determine which epic/feature should contain this
3. **Draft the item** — Write properly formatted story/task file
4. **Ask for approval** — Present the item to confirm it matches the intent
5. **Implement** — Once approved, proceed as normal

---

## When Creating New Work Items

### Story

```markdown
# N.M.P: Title

**Status:** BACKLOG
**Type:** Story
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`

---

As a [role],
I want [capability],
So that [value].

## Acceptance Criteria

- [ ] Criterion one
- [ ] Criterion two
```

### Task

```markdown
# N.M.P: Title

**Status:** BACKLOG
**Type:** Task
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`

---

[Description of technical step needed.]

- [ ] Step one
- [ ] Step two

## Acceptance Criteria

- [ ] Measurable outcome one
- [ ] Measurable outcome two
```

### Bug

```markdown
# N.M.P: Bug Title

**Status:** BACKLOG
**Type:** Bug
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`, `+severity:high`

---

## Problem

**Expected:** 
**Actual:** 
**Reproduces:** 

## Acceptance Criteria

- [ ] Bug no longer reproduces
- [ ] Regression test added
```

### New Feature

1. Add to parent feature file's items table
2. Create new `N.M.P-slug.md` file in parent epic
3. Link from parent epic `_epic.md` items table

### New Epic

1. Create folder: `NN-slug/`
2. Create `_epic.md` with goal and features index
3. Update `project-plan.md` with epic dependencies
4. Update parent epic's "Blocks" if this is part of a larger epic

---

## Systems Thinking Checklist

Use this before designing a solution to ensure it fits the whole system:

### 1. Domain Boundary

- [ ] What entities does this read/write?
- [ ] What other features touch these entities?
- [ ] Are there naming conflicts or redundancy?
- [ ] Does this violate DDD bounded contexts?

### 2. Authorization Impact

- [ ] Which roles need to execute this?
- [ ] What data is privileged? (requires role check)
- [ ] What data needs redaction for lower roles?
- [ ] Does the role matrix in PRD Section 4.2 cover this?

### 3. Data Access Pattern

- [ ] Which entities need row-level filtering?
- [ ] Does this require field-level redaction?
- [ ] What should be audit-logged?
- [ ] Can users export this data? (audit impact)

### 4. Protocol Surface

- [ ] Is this a tool, resource, prompt, or cross-cutting feature?
- [ ] Does it need pagination?
- [ ] Does it need progress reporting?
- [ ] Can it be cancelled?
- [ ] Are there rate limits?

### 5. Dependencies

- [ ] What must exist before building this?
- [ ] What will break if this changes?
- [ ] Are there data migrations needed?

### 6. Test Surface

- [ ] Unit tests: business logic
- [ ] Integration tests: auth checks
- [ ] Integration tests: happy path + error cases
- [ ] Manual test: end-to-end flow

### 7. Decisions & Reversibility

- [ ] Did I make any non-obvious choices?
- [ ] Would it be expensive to reverse?
- [ ] Should I write an ADR?

---

## Status Update Rules

**Never:**
- Set to `DONE` without verifying ALL acceptance criteria
- Set to `DONE` without linked code/tests
- Change status without explanation
- Set epic to `DONE` if any sub-item is not `DONE`

**Always:**
- Provide context when marking `BLOCKED` (what's the blocker?)
- Update parent feature/epic status tables after changing item status
- Link related items in "Notes" section
- Verify no regressions before closing bugs

---

## Code Implementation Rules

1. **No explicit IDs** — Let EF Core manage IDENTITY columns for operational data. Only use `ValueGeneratedNever()` for reference data (PracticeGroups, lookup tables, etc.).

2. **Foreign keys** — Reference data IDs (1-6, 1-5) are stable. Use them as needed. Operational data IDs are auto-generated; always save before querying related data.

3. **Null checks** — Verify navigation properties are populated before accessing. Load related entities explicitly in LINQ queries.

4. **Tests first** — Write regression tests before marking bug DONE. Test both the fix and that the bug doesn't reoccur.

5. **Config files** — Never hardcode values. Use `appsettings.Development.json` + `appsettings.Development.json.example` pattern.

6. **Migrations** — If modifying the schema, create migrations but don't run them in Program.cs unless explicitly needed. Use `EnsureCreatedAsync()` for dev/testing only.

---

## Git Workflow Rules

**CRITICAL:** I do NOT create git commits. Ever.

- Implement code changes
- Update `.md` files with completion status and code references
- Verify everything works
- Hand off to developer for commit

The developer decides when/how to commit based on logical units of work.

---

## When Uncertain

1. **Ask questions in the file** — If the item file is ambiguous, add a "Questions" section documenting what's unclear
2. **Reference PRD** — If something seems contradictory, link the PRD section and ask for clarification
3. **Create an ADR** — If I'm about to make a costly decision, draft an ADR and ask for approval before implementing
4. **Run systems thinking** — If I'm confused about scope, run through the checklist and document findings

---

## File Format Reminders

- Use markdown headings: `# Title`, `## Section`, `### Subsection`
- Use `**bold**` for field names: `**Status:**, **Type:**, **Feature:**`
- Use backticks for code: `` `AreAllAttorneyRolesNull()` ``
- Use `[link text](./path.md)` for internal links (always relative paths)
- Use checkboxes for acceptance criteria: `- [ ] Done when...` → `- [x] Done when...`
- Use tables for item indexes (epic features, feature items)
- Use `---` for section breaks

---

## Reminders About This System

- **Epic folder** = `NN-slug/_epic.md` (e.g., `01-foundation/_epic.md`)
- **Feature file** = `N.M-slug.md` (e.g., `1.3-authorization.md`)
- **Item file** = `N.M.P-slug.md` (e.g., `1.3.1-role-based-handler.md`)
- **Bug (simple)** = same convention, `Type: Bug`
- **Bug (complex)** = subfolder `N.M.P-slug/bugs/N.M.P.X-bug.md`
- **Research/examples** = subfolder `N.M.P-slug/research/`, `examples/`, etc.

---

## Success Looks Like

- ✅ Developer opens Epic 2 → sees all data model work in one place  
- ✅ Developer clicks Feature 2.1 → sees all people/org entity stories  
- ✅ Developer clicks Story 2.1.1 → sees spec, then clicks bug link → sees implementation issue + fix  
- ✅ New teammate reads 01-foundation → understands decisions, architecture, what was built and why  
- ✅ 6 months from now, we can audit "who fixed this bug and when?" by reading the bug file + git links

