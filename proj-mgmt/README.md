# Project Management — Law-Corp MCP Server

This folder is the single source of truth for all product decisions, work items, and architectural reasoning. It is version-controlled alongside the code so that every decision and scope change is auditable.

**One file per work item.** This means status updates, concurrent implementation, and Claude operations never touch the same file unless they're working on the same item.

---

## Folder Structure

```
proj-mgmt/
├── README.md                        ← This file
├── prd.md                           ← Product requirements, domain model, capability surface
├── project-plan.md                  ← WBS, phase plan, epic dependency graph
├── decisions/                       ← Architecture Decision Records (ADRs)
│   ├── README.md                    ← ADR format and index
│   └── NNN-title.md                 ← One file per decision
└── epics/
    └── NN-epic-name/
        ├── _epic.md                 ← Epic goal, feature index with status
        ├── N.M-feature-name.md      ← Feature: goal, items index with status
        └── N.M.P-item-title.md      ← Story / Task / Bug: full detail
```

### Naming conventions

| Level | Pattern | Example |
|---|---|---|
| Epic folder | `NN-slug/` | `01-foundation/` |
| Epic index | `_epic.md` | sorts to top of folder |
| Feature file | `N.M-slug.md` | `1.2-authentication.md` |
| Story / Task / Bug | `N.M.P-slug.md` | `1.2.1-entra-id-middleware.md` |

The numeric prefix in `N.M.P` encodes the hierarchy: epic 1, feature 2, item 1. Alphabetical file sort gives natural reading order.

---

## Work Item Hierarchy

```
Epic
└── Feature
    ├── Story   — user-facing value; "As a... I want... So that..."
    ├── Task    — technical step; no direct user value on its own
    └── Bug     — defect in previously-working or expected behavior
```

### Definitions

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
