# Agentic Engineering Guide

**Created:** 2026-02-25
**Purpose:** How AI agents are set up and expected to operate on this project.

---

## Overview

This project uses AI coding assistants (Claude Code, Cursor, GitHub Copilot) as first-class contributors. To ensure every agent follows the same conventions — and that any developer can clone the repo and get consistent AI behavior immediately — all rules are stored in the repository itself.

**Single source of truth:** [`RULES.md`](../CLAUDE.md) at the repo root. All other agent config files reference or replicate this file's core content.

---

## Rule File Locations

| Agent | Config File | Notes |
|---|---|---|
| Claude Code / Claude Desktop | [`CLAUDE.md`](../CLAUDE.md) | Auto-loaded by Claude Code from project root |
| Cursor | [`.cursor/rules/project-workflow.mdc`](../.cursor/rules/project-workflow.mdc) | Loaded by Cursor automatically; `alwaysApply: true` |
| Cursor | [`.cursor/rules/preserve-before-removing.mdc`](../.cursor/rules/preserve-before-removing.mdc) | Rule: always write destination before deleting source |
| GitHub Copilot | [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) | Loaded by Copilot Chat automatically |
| Legacy (deprecated) | `.rules/CLAUDE.md`, `.rules/CLAUDE-WORKFLOW.md`, `.rules/COPILOT-RULES.md` | Superseded by `CLAUDE.md`; kept for historical reference |

### Rule of Thumb

- **`CLAUDE.md`** is authoritative. Edit this file when the team agrees on a new convention.
- Agent-specific files (`.cursor/rules/*.mdc`, `.github/copilot-instructions.md`) should stay lightweight — they point to `CLAUDE.md` and highlight only the most code-completion-relevant rules.
- When in doubt, put it in `CLAUDE.md`.

---

## Global vs. Local Rules

**Default: local (project) rules.** When an AI agent session produces a useful rule or convention, it goes into `CLAUDE.md` (or the appropriate agent-specific file), committed to the repository. This way every developer who clones the repo immediately gets the same AI behavior.

**Exception: global user rules.** If a rule applies across many projects (e.g., "always use TypeScript strict mode"), the developer can add it to their global AI config (`~/.claude/CLAUDE.md` for Claude Code, or equivalent). This should be explicitly requested — the default is always local/project scope.

---

## Setting Up AI Tools

### Claude Code

Claude Code automatically loads `CLAUDE.md` from the project root. No additional setup is needed after cloning.

**Verify:** Open the project in Claude Code — it will show "Loaded project instructions from CLAUDE.md."

### Cursor

Cursor automatically picks up all `.mdc` files inside `.cursor/rules/` when `alwaysApply: true` is set in the frontmatter. No additional setup is needed after cloning.

**Verify:** Open Cursor → Settings → Rules — you should see the project rules loaded.

### GitHub Copilot (VS Code)

GitHub Copilot Chat reads `.github/copilot-instructions.md` automatically when the file exists in the repository root. No additional setup is needed after cloning.

**Verify:** Open Copilot Chat and ask "What are the project coding conventions?" — it should reference the tool name and EF Core rules.

---

## Core Conventions All Agents Follow

### No AI Commits

AI assistants never create git commits. They implement and verify code, then hand off to the developer. The developer owns the commit message and decides the logical unit of work.

### Project Management as Documentation

All work is tracked in `proj-mgmt/epics/NN-slug/`. Agents read the item file before coding and update it after. This creates a living record of decisions that future developers (and agents) can follow.

### No Magic Strings for Tool Names

MCP tool names are defined once in `src/LawCorp.Mcp.Core/McpToolName.cs`. Every agent is expected to use `McpToolName.X.Y` constants rather than raw string literals. This prevents drift between the permission matrix and tool registration.

### Ask, Don't Guess

When SDK internals are opaque (DLLs, closed-source packages), agents ask the developer rather than using decompilers or other indirect tooling.

---

## Updating Rules

1. Agree on the new convention (usually by noticing a pattern or fixing an inconsistency)
2. Edit `CLAUDE.md` with the rule
3. Mirror it in the relevant agent-specific file if it affects code completion
4. Commit the rule change so all developers and future sessions benefit

---

## Project Context for Agents

For agents starting a fresh session, the key context points are:

- **Domain:** Law firm operations (cases, clients, billing, documents, calendar, research, intake)
- **Auth model:** Entra ID OBO — Blazor Web App acquires token on behalf of logged-in user → passes to MCP Server → resolved to `IFirmIdentityContext` (role: `Partner`, `Associate`, `OfCounsel`, `Paralegal`, `LegalAssistant`, `Intern`)
- **RBAC:** `ToolPermissionMatrix` enforces which tools each role can call via MCP request filter pipeline
- **Transport:** HTTP (authenticated, production) or stdio (anonymous, MCP Inspector / Claude Desktop)
- **SDK:** `ModelContextProtocol` 1.0.0-rc.1 (C# official SDK)
