# Agentic Engineering Guide

**Created:** 2026-02-25
**Purpose:** How AI agents are set up and expected to operate on this project.

---

## Overview

This project uses AI coding assistants (Claude Code, Cursor, GitHub Copilot) as first-class contributors. To ensure every agent follows the same conventions — and that any developer can clone the repo and get consistent AI behavior immediately — all rules are stored in the repository itself.

---

## Rules Architecture

### Single source of truth: `RULES.md`

[`RULES.md`](../RULES.md) at the repo root contains **every** rule that governs AI agent behavior on this project — git workflow, code conventions, MCP-specific patterns, project management, and communication preferences.

Agent-specific config files are **exact copies** of `RULES.md`, not summaries or subsets. This eliminates drift between agents and reduces maintenance to a single file.

### Sync mechanism

A PowerShell script copies `RULES.md` to each agent's expected location:

```
RULES.md  (edit here)
  ├── .cursor/rules/project-rules.mdc   (Cursor — YAML frontmatter prepended)
  ├── .rules/CLAUDE.md                  (Claude Code)
  └── .github/copilot-instructions.md   (GitHub Copilot)
```

**To sync manually:**

```bash
powershell scripts/sync-agent-rules.ps1
```

**Automated sync:** A GitHub Action runs the script on every commit, so agent files never fall behind `RULES.md`. (The action also serves as a safety net — if a developer edits an agent file directly, the next commit overwrites it with the source of truth.)

### Where each agent reads rules

| Agent | File it reads | How it's loaded |
|---|---|---|
| Cursor | `.cursor/rules/project-rules.mdc` | Auto-loaded; `alwaysApply: true` frontmatter |
| Claude Code | `.rules/CLAUDE.md` | Auto-loaded from project root |
| GitHub Copilot | `.github/copilot-instructions.md` | Auto-loaded by Copilot Chat |

### Editing rules

1. Edit `RULES.md` at the repo root
2. Run `powershell scripts/sync-agent-rules.ps1`
3. Commit all changed files (the GitHub Action will also run the sync as a safeguard)

**Never edit the agent files directly** — changes will be overwritten on next sync.

---

## Global vs. Local Rules

**Default: local (project) rules.** When an AI session produces a useful convention, it goes into `RULES.md`, committed to the repo. Every developer who clones gets the same AI behavior immediately.

**Exception: global user rules.** Rules that apply across many projects (e.g., "always use TypeScript strict mode") go in the developer's personal AI config (`~/.claude/CLAUDE.md`, Cursor global settings, etc.). This must be explicitly requested — the default is always project scope.

---

## Setting Up AI Tools

### Claude Code

Automatically loads `.rules/CLAUDE.md` from the project. No setup needed after cloning.

**Verify:** Open the project in Claude Code — it shows "Loaded project instructions."

### Cursor

Automatically loads all `.mdc` files in `.cursor/rules/` with `alwaysApply: true`. No setup needed after cloning.

**Verify:** Open Cursor → Settings → Rules — project rules should appear.

### GitHub Copilot (VS Code)

Reads `.github/copilot-instructions.md` automatically. No setup needed after cloning.

**Verify:** Ask Copilot Chat "What are the project coding conventions?" — it should reference tool name constants and EF Core rules.

---

## Core Conventions (Summary)

All conventions are defined in `RULES.md`. The key ones for quick reference:

- **No git commits from AI** — implement, verify, hand off to developer
- **No magic strings** — all enumerable strings use dedicated constant classes (`McpToolName`, `WebRoutes`, `AppConfigKeys`, `AppClaimTypes`)
- **Project management as documentation** — work items in `proj-mgmt/epics/`, read before coding, update after
- **Ask, don't guess** — when SDK internals are opaque, ask the developer directly

---

## Project Context for Fresh Sessions

- **Domain:** Law firm operations (cases, clients, billing, documents, calendar, research, intake)
- **Auth model:** Entra ID OBO — Blazor Web App acquires token on behalf of user → passes to MCP Server → resolved to `IFirmIdentityContext`
- **Roles:** `Partner`, `Associate`, `OfCounsel`, `Paralegal`, `LegalAssistant`, `Intern`
- **RBAC:** `ToolPermissionMatrix` enforces per-role tool access via MCP request filter pipeline
- **Transport:** HTTP (authenticated, production) or stdio (anonymous, MCP Inspector / Claude Desktop)
- **SDK:** `ModelContextProtocol` 1.0.0-rc.1 (C# official SDK)
