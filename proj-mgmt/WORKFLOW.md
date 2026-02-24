# Project Management Workflow

**Last Updated:** 2026-02-24

This document describes how we organize and track work across epics, features, stories, tasks, and bugs in a hierarchical living history system.

---

## Organizational Hierarchy

```
proj-mgmt/
├── README.md                           ← Overview of the PM system
├── WORKFLOW.md                         ← This file
├── CLAUDE-WORKFLOW.md                  ← Rules for Claude Code Agent
├── prd.md                              ← Product requirements & domain model
├── project-plan.md                     ← WBS, phases, dependencies
├── decisions/                          ← Architecture Decision Records
│   ├── README.md
│   └── NNN-title.md
│
└── epics/
    ├── NN-epic-name/
    │   ├── _epic.md                    ← Epic goal, features index
    │   ├── N.M-feature-name.md         ← Feature spec, items index
    │   ├── N.M.P-story-or-task.md      ← Story, Task, or Bug file
    │   │
    │   ├── N.M.P-item-with-details/    ← FOLDER for complex items (optional)
    │   │   ├── N.M.P-item-name.md      ← Main item file
    │   │   ├── bugs/                   ← Bugs found during/after this item
    │   │   │   └── N.M.P.X-bug.md      ← Bug as sub-item (use .X numbering)
    │   │   ├── research/               ← Investigation docs
    │   │   ├── examples/               ← Example code or configs
    │   │   └── notes/                  ← Implementation notes
    │   │
    │   └── ...other items
    │
    └── ...other epics
```

### When to Use Item Folders

Create a folder for an item (`N.M.P-slug/`) when it has:

- **Multiple related bugs** that emerged during implementation (e.g., `N.M.P.1-bug.md`, `N.M.P.2-bug.md`)
- **Complex research** documented separately (e.g., performance analysis, vendor evaluation)
- **Generated code examples** or configuration templates
- **Implementation notes** that are too detailed for inline comments

Otherwise, keep it simple: single file at the feature level.

---

## Bug Lifecycle & Tracking

### Where Bugs Live

Bugs are **always** stored in the same epic folder tree as the feature/item they relate to. This creates a living history:

```
Example 1: Bug in a completed task
epics/02-data-model/
  2.1-people-org-entities.md
  2.1.1-practice-group-entity.md
  2.1.1-bug-migration-seed-sql-failure.md

Example 2: Bug found within a complex feature
epics/02-data-model/2.1.1-practice-group/
  2.1.1-practice-group-entity.md     ← Main feature file
  bugs/
    2.1.1.1-migration-seed-sql-failure.md    ← Bug as sub-item
    2.1.1.2-validation-edge-case.md          ← Another bug
  research/
    schema-analysis.md
```

### Bug File Format

```markdown
# N.M.P.X: Bug Title

**Status:** BACKLOG | IN PROGRESS | DONE | BLOCKED
**Type:** Bug
**Feature:** [N.M: Feature Title](../N.M-slug.md)
**Feature Item:** [N.M.P-slug](../N.M.P-slug.md)
**Related Epic:** [Epic N](../../../NN-slug/_epic.md)
**Tags:** `+domain-tag`, `+severity:critical`

---

## Problem

**Expected:** What should happen.
**Actual:** What happens instead.
**Reproduces:** Step-by-step reproduction (or "Consistently" if reliability not yet confirmed).
**Severity:** Critical | High | Medium | Low
**Impact:** [Who is affected and how.]

---

## Root Cause

[If known, describe the root cause. Otherwise: "TBD — requires investigation".]

---

## Solution (If Implemented)

### Changes Made

- File 1: What was changed and why
- File 2: What was changed and why

### Code References

- Commit: [hash or branch name]
- Diff: [link if available]

---

## Investigation

### Research

- [ ] Review error logs
- [ ] Check related code
- [ ] Verify data state
- [ ] Check if regression

### Fix Steps

- [ ] Step one
- [ ] Step two
- [ ] Step three

---

## Acceptance Criteria (Resolution)

- [ ] Bug no longer reproduces in [environment]
- [ ] All related error logs cleared
- [ ] Regression test added (file: [path])
- [ ] Feature-level acceptance criteria still all check ✓

---

## Notes

[Links to ADRs, related stories, blocked items, design documents.]
```

### Bug Status Tracking

**BACKLOG** → Bug identified, not yet prioritized  
**TODO** → Bug prioritized for current sprint  
**IN PROGRESS** → Developer actively fixing  
**DONE** → Fix verified, regression test added  
**BLOCKED** → Cannot fix until blocker is resolved (note the blocker)  

---

## Tracing Work from Epic to Bug

Starting point: **Epic folder**

```
Step 1: Epic folder (e.g., epics/01-foundation/)
  → Open _epic.md
  → See which Features are related to your area of interest

Step 2: Feature file (e.g., 1.3-authorization.md)
  → See which Stories/Tasks make up this feature
  → See status of each

Step 3: Story/Task file (e.g., 1.3.1-role-based-auth-handler.md)
  → See implementation detail
  → See acceptance criteria (what "done" means)
  → See if it has a folder (complex item)

Step 4: If complex item (folder):
  → Open 1.3.1-role-based-auth-handler/
  → Look in bugs/ subfolder
  → See any bugs that emerged during/after implementation
  → See research/ subfolder for investigation docs
  → See status of each bug

Step 5: Bug file (e.g., 1.3.1.1-null-reference-exception.md)
  → Read problem statement
  → Check if fixed or still open
  → See solution applied (if any)
  → Find related code changes
```

**Result:** One logical flow from epic goal → feature → story → implementation → bugs found. No scattered bug repos.

---

## Updating Status Across Hierarchy

When you close a bug or task, update the parent files:

1. **Bug file**: Change `**Status:** BACKLOG` → `**Status:** DONE`
2. **Feature file** (if applicable): Update `N.M.P-slug` row in the items table to show new status
3. **Epic file** (if applicable): Review all features and adjust epic status if needed

**Important:** Do NOT set an epic or feature to `DONE` unless all child items are `DONE`.

---

## Living History Principle

This structure creates a **readable record of what was built, when, and why**:

- Go back in time: Read commits linked from bug files
- Know the decisions: Read ADRs referenced in story files
- Understand the journey: See which bugs were found and fixed during feature dev
- Onboard faster: New team members read epic → features → stories in order
- Less context switching: Everything related to a feature is in one place

---

## File Naming Quick Reference

| Item Type | File Name Pattern | Example |
|-----------|-------------------|---------|
| Epic | `NN-slug/_epic.md` | `01-foundation/_epic.md` |
| Feature | `N.M-slug.md` | `1.3-authorization.md` |
| Story | `N.M.P-slug.md` | `1.3.1-role-based-handler.md` |
| Task | `N.M.P-slug.md` | `2.1.2-audit-persistence.md` |
| Bug (simple) | `N.M.P-bug-slug.md` | `2.1.1-bug-migration-sql-error.md` |
| Bug (in folder) | `bugs/N.M.P.X-bug.md` | `bugs/2.1.1.1-migration-sql-error.md` |

---

## Collaboration

- **Developer**: Updates `**Status:**` fields, marks items `DONE`, checks off acceptance criteria
- **Claude Code**: Reads requirements, implements code, updates acceptance criteria, suggests status changes (doesn't commit)
- **Product Owner**: Adds new features to backlog, writes PRD, reviews ADRs

**One file, one person at a time** — file-level lock prevents merge conflicts.

