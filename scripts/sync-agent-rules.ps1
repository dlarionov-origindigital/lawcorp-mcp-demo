<#
.SYNOPSIS
    Copies RULES.md to all agent-specific rule file locations.

.DESCRIPTION
    RULES.md at the repo root is the single source of truth.
    This script copies it to the locations each AI agent reads:
      .cursor/rules/project-rules.mdc   (Cursor - with YAML frontmatter)
      .rules/CLAUDE.md                  (Claude Code)
      .github/copilot-instructions.md   (GitHub Copilot)

    Run after editing RULES.md:   powershell scripts/sync-agent-rules.ps1
    Will also run as a GitHub Action on commit.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$rulesPath = Join-Path $repoRoot 'RULES.md'

if (-not (Test-Path $rulesPath)) {
    Write-Error "RULES.md not found at $rulesPath"
    exit 1
}

$utf8 = New-Object System.Text.UTF8Encoding($false)
$rules = [System.IO.File]::ReadAllText($rulesPath, $utf8)
$header = "<!-- AUTO-GENERATED from RULES.md -- do not edit. Run: powershell scripts/sync-agent-rules.ps1 -->"

# ── Ensure directories exist ─────────────────────────────────────────────────
$cursorDir = Join-Path (Join-Path $repoRoot '.cursor') 'rules'
$rulesDir  = Join-Path $repoRoot '.rules'
$ghDir     = Join-Path $repoRoot '.github'

foreach ($dir in @($cursorDir, $rulesDir, $ghDir)) {
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

# ── 1. Cursor: YAML frontmatter + RULES.md content ──────────────────────────
$cursorContent = "---`ndescription: `"Project rules - auto-synced from RULES.md`"`nalwaysApply: true`n---`n`n$header`n`n$rules"
$cursorPath = Join-Path $cursorDir 'project-rules.mdc'
[System.IO.File]::WriteAllText($cursorPath, $cursorContent, $utf8)
Write-Host "  [OK] .cursor/rules/project-rules.mdc" -ForegroundColor Green

# ── 2. Claude Code: straight copy ────────────────────────────────────────────
$claudePath = Join-Path $rulesDir 'CLAUDE.md'
[System.IO.File]::WriteAllText($claudePath, "$header`n`n$rules", $utf8)
Write-Host "  [OK] .rules/CLAUDE.md" -ForegroundColor Green

# ── 3. GitHub Copilot: straight copy ─────────────────────────────────────────
$copilotPath = Join-Path $ghDir 'copilot-instructions.md'
[System.IO.File]::WriteAllText($copilotPath, "$header`n`n$rules", $utf8)
Write-Host "  [OK] .github/copilot-instructions.md" -ForegroundColor Green

# ── 4. Clean up obsolete files ────────────────────────────────────────────────
$obsolete = @(
    (Join-Path $cursorDir 'project-workflow.mdc'),
    (Join-Path $cursorDir 'preserve-before-removing.mdc'),
    (Join-Path $rulesDir  'COPILOT-RULES.md'),
    (Join-Path $rulesDir  'CLAUDE-WORKFLOW.md')
)
foreach ($f in $obsolete) {
    if (Test-Path $f) {
        Remove-Item $f
        $rel = $f.Replace($repoRoot, '').TrimStart('\', '/')
        Write-Host "  [RM] $rel (obsolete)" -ForegroundColor Yellow
    }
}

Write-Host "`nDone. All agent rules synced from RULES.md." -ForegroundColor Cyan
