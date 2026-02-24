# Project Management — Law-Corp MCP Server

This folder is the single source of truth for all product decisions, work items, and architectural reasoning. It is version-controlled alongside the code so that every decision and scope change is auditable.

**One file per work item.** This means status updates, concurrent implementation, and Claude operations never touch the same file unless they're working on the same item.

---

## Quick Navigation

- **[WORKFLOW.md](./WORKFLOW.md)** — How bugs, stories, and tasks are organized in the hierarchy
- **[CLAUDE-WORKFLOW.md](../CLAUDE-WORKFLOW.md)** — Rules for the Claude Code Agent
- **[prd.md](./prd.md)** — Product requirements, domain model, capability surface
- **[project-plan.md](./project-plan.md)** — WBS, detailed phase plan, epic dependency graph
- **[decisions/](./decisions/)** — Architecture Decision Records (ADRs)

---

## Folder Structure

```
proj-mgmt/
├── README.md                        ← This file
├── WORKFLOW.md                      ← How to organize work in the hierarchy
├── prd.md                           ← Product requirements, domain model
├── project-plan.md                  ← WBS, phase plan, epic dependencies
├── decisions/                       ← Architecture Decision Records (ADRs)
│   ├── README.md                    ← ADR format and index
│   └── NNN-title.md                 ← One file per decision
│
└── epics/
    └── NN-epic-name/
        ├── _epic.md                 ← Epic goal, features table with status
        ├── N.M-feature-name.md      ← Feature spec, items table with status
        ├── N.M.P-item-title.md      ← Story / Task / Bug: full detail
        │
        └── N.M.P-item-name/         ← Optional: folder for complex items
            ├── N.M.P-item-name.md   ← Main item file
            ├── bugs/
            │   ├── N.M.P.1-bug.md   ← Bug as sub-item (hierarchical numbering)
            │   └── N.M.P.2-bug.md
            ├── research/            ← Investigation files
            └── examples/            ← Code examples
```

### The Bug Hierarchy

**Key principle:** Bugs are stored in the same epic folder tree as the feature/item they relate to. This creates a **living history** where you can start at an epic, drill down to a feature, see all tasks and bugs in one logical place.

**Simple bug** (single issue):
```
epics/02-data-model/
  2.1-people-org-entities.md
  2.1.1-practice-group-entity.md
  2.1.1-bug-validation-edge-case.md      ← Standalone bug file
```

**Complex item with multiple bugs** (created folder structure):
```
epics/02-data-model/2.1.1-practice-group/
  2.1.1-practice-group-entity.md         ← Main feature file
  bugs/
    2.1.1.1-migration-seed-sql-failure.md  ← First bug (sub-item .1)
    2.1.1.2-validation-edge-case.md        ← Second bug (sub-item .2)
  research/
    schema-analysis.md                      ← Investigation notes
```

See [**WORKFLOW.md**](./WORKFLOW.md) for detailed organization rules and bug lifecycle.

---

## Naming Conventions

| Level | Pattern | Example | Sorted |
|---|---|---|---|
| Epic | `NN-slug/` | `01-foundation/` | Numerically |
| Epic index | `_epic.md` | `_epic.md` | *First (underscore sorts first) |
| Feature | `N.M-slug.md` | `1.2-auth.md` | Numerically |
| Story / Task / Bug | `N.M.P-slug.md` | `1.2.1-mfa-support.md` | Numerically |
| Bug sub-item | `N.M.P.X-slug.md` | `2.1.1.1-sql-error.md` | Numerically |

**Why this naming?**

- Numeric prefixes encode the hierarchy: epic N, feature M, item P, bug version X
- Alphabetical file sort = natural reading order (numerically, then underscore, then alpha)
- `_epic.md` sorts to folder top — epic overview always visible first

---

## Work Item Hierarchy

```
Epic (large body of work unified by one goal)
└── Feature (coherent capability, multiple user stories/tasks)
    ├── Story (unit of user value; "As a...")
    ├── Task (technical enabling work)
    └── Bug (defect in expected behavior; lives alongside stories/tasks)
```

### Definitions & Status

| Level | Scope | Status Options | Lives in |
|---|---|---|---|
| **Epic** | Large body of work. Spans multiple phases. | BACKLOG, IN PROGRESS, DONE | `_epic.md` |
| **Feature** | Coherent capability slice. Deliverable together. | BACKLOG, IN PROGRESS, DONE | `N.M-slug.md` |
| **Story** | Unit of user-facing value. "As a... I want... So that..." | BACKLOG, TODO, IN PROGRESS, DONE, BLOCKED | `N.M.P-slug.md` |
| **Task** | Technical enabling work. No direct user value. | BACKLOG, TODO, IN PROGRESS, DONE, BLOCKED | `N.M.P-slug.md` |
| **Bug** | Defect in working or expected behavior. Emerges during/after work. | BACKLOG, TODO, IN PROGRESS, DONE, BLOCKED | `N.M.P-slug.md` or `bugs/N.M.P.X-slug.md` |

**Status fields** go in the header block of every file. Update them to track progress. Epic/feature status is derived from child items but set manually after reviewing child statuses.

---

## Tracer Bullet: From Epic to Bug

Here's how to navigate the hierarchy:

**Start:** Epic folder `epics/01-foundation/`

1. Open `_epic.md` → See all features in a table  
2. Click feature `1.3-authorization.md` → See all stories/tasks as table  
3. Click story `1.3.1-role-based-handler.md` → See implementation detail & acceptance criteria  
4. If complex: folder `1.3.1-role-based-handler/` → see `bugs/` → find `1.3.1.1-null-ref-exception.md`  
5. Read bug: problem statement, root cause, solution applied, status  

**Result:** Clean trail from epic goal → feature → story → implementation → bug found + fix (all in one logical place)

---

## File Formats

### Epic index (`_epic.md`)

| Level | Scope | Lives in |
|---|---|---|
| **Epic** | A large body of work unified by a single goal. Spans multiple phases. | `_epic.md` |
| **Feature** | A coherent slice of an epic that delivers a testable capability. | `N.M-slug.md` |
| **Story** | A unit of user-facing value. Has acceptance criteria. Written from a role's perspective. | `N.M.P-slug.md` |
| **Task** | A pure technical step that enables stories but delivers no direct user value. | `N.M.P-slug.md` |
| **Bug** | A defect in behavior that was previously working or expected to work. | `N.M.P-slug.md` with type `Bug` |

---

## Status

Every file has a `**Status:**` field in its header block. Update this field — and only this field — to change status.

| Status | Meaning |
|---|---|
| `BACKLOG` | Defined and groomed, not yet prioritized |
| `TODO` | Committed for current work |
| `IN PROGRESS` | Actively being worked on |
| `DONE` | All acceptance criteria verified |
| `BLOCKED` | Cannot proceed — blocker noted in the file |

**Epic and feature status** is derived from their items but set manually in the file header after reviewing item statuses.

---

## File Formats

### Epic index (`_epic.md`)

```markdown
# Epic N: Title

**Status:** BACKLOG | IN PROGRESS | DONE
**Goal:** One-sentence description of what this epic delivers.

## Features

| ID | Feature | Status |
|---|---|---|
| [N.1](./N.1-slug.md) | Feature title | BACKLOG |
| [N.2](./N.2-slug.md) | Feature title | IN PROGRESS |

## Dependencies

Depends on: [Epic M](../NN-slug/_epic.md)
Blocks: [Epic P](../NN-slug/_epic.md)

## Success Criteria

- [ ] Criterion from PRD
- [ ] Criterion from PRD
```

---

### Feature file (`N.M-slug.md`)

```markdown
# N.M: Feature Title

**Status:** BACKLOG | IN PROGRESS | DONE
**Epic:** [Epic N: Title](./_epic.md)
**Goal:** One-sentence description of what this feature delivers.

## Items

| ID | Title | Type | Status |
|---|---|---|---|
| [N.M.1](./N.M.1-slug.md) | Item title | Story | DONE |
| [N.M.2](./N.M.2-slug.md) | Item title | Task | BACKLOG |

## Acceptance Criteria

- [ ] Feature-level outcome one
- [ ] Feature-level outcome two
```

---

### Story file (`N.M.P-slug.md`)

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
- [ ] Criterion three

## Notes

[Implementation notes, links to ADRs, open questions.]
```

---

### Task file (`N.M.P-slug.md`)

```markdown
# N.M.P: Title

**Status:** BACKLOG
**Type:** Task
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`

---

[Description of what needs to be done and why.]

- [ ] Implementation step one
- [ ] Implementation step two

## Acceptance Criteria

- [ ] Measurable outcome one
- [ ] Measurable outcome two

## Notes

[Implementation notes, links to ADRs, decisions made.]
```

---

### Bug file (`N.M.P-slug.md`)

```markdown
# N.M.P: Title

**Status:** BACKLOG
**Type:** Bug
**Feature:** [N.M: Feature Title](./N.M-slug.md)
**Tags:** `+domain-tag`

---

**Expected:** What should happen.
**Actual:** What happens instead.
**Reproduces:** Steps to reproduce.

**Root Cause:** Fill in when known.

## Fix

- [ ] Step one
- [ ] Step two

## Acceptance Criteria

- [ ] Bug no longer reproduces
- [ ] Regression test added
```

---

## Architecture Decision Records

Significant decisions live in `decisions/NNN-title.md`. Write an ADR when:
- You chose one technology or approach over a real alternative
- The decision would be expensive to reverse
- A future developer would reasonably ask "why did they do it this way?"
- A PRD open question is resolved

See [`decisions/README.md`](./decisions/README.md) for the format. ADRs are never deleted — if reversed, mark `Superseded by ADR-NNN`.

---

## Systems Thinking Checklist

Run through this before starting any new feature or story. It prevents building the right piece without understanding how it fits the whole.

**1. Domain boundary** — Which entities does this read or write? What other features touch those same entities?

**2. Authorization impact** — Which roles are affected? Does the role matrix in the PRD need updating? Does this touch privileged or redacted data?

**3. Protocol surface** — Is this a tool, resource, prompt, or cross-cutting protocol feature? Does it need pagination, progress reporting, or cancellation?

**4. Dependencies** — What must exist before this can be built? What will break if this changes?

**5. Test surface** — What unit tests cover the auth logic? What integration tests cover the happy path and failure modes?

**6. Decisions** — Did you make any non-obvious choices? Write an ADR. Did this resolve a PRD open question? Close it.

---

## Concurrent Work

Because every story, task, and bug is its own file, multiple people (or Claude + a developer) can work simultaneously without file conflicts:

- Developer implements `1.3.1-role-auth-handler.md` → edits only that file
- Claude implements `2.7.1-db-context.md` → edits only that file
- Developer updates status on `1.1.1-create-solution.md` → edits only that file

**To start work on an item:** change `**Status:** BACKLOG` → `**Status:** IN PROGRESS`.

**To finish:** change to `**Status:** DONE` and check off all acceptance criteria. Then update the parent feature file's items table to reflect the new status.

---

## Working with Claude Code

**To implement a specific item:**
> "Implement story `1.3.1` from Epic 1."

Claude will read the file, check acceptance criteria, implement the code, check off completed criteria, and set status to `DONE`.

**To plan a new feature:**
> "We need to add webhook support for case updates. Apply systems thinking and draft a feature + stories for it."

Claude will consult the PRD, check entity and authorization implications, run through the checklist, and write properly formatted feature and story files in the right epic folder.

**What Claude will not do:**
- Create git commits — that is always the developer's responsibility
- Set status to `DONE` without verifying all acceptance criteria are met
