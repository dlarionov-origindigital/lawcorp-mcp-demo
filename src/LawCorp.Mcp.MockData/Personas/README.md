# Persona Fixture — Entra ID Seed Data

This folder contains the six canonical personas used for identity-passthrough testing. Each persona has fixed structural data (name, role, practice group) in `PersonaDefinitions.cs`, while tenant-specific Entra ID values (email, Object ID) are loaded from `persona-seed.json` at runtime.

**Your tenant values stay out of source control.** The `persona-seed.json` file is gitignored — only the `.example` template is tracked.

## Setup

1. Create six users in your Azure AD tenant (see [docs/auth-config.md](../../../docs/auth-config.md) Step 6)
2. Note each user's **Object ID** (found in Entra ID → Users → select user → Object ID)
3. Copy the example config and fill in your values:

```bash
cp src/LawCorp.Mcp.Server/persona-seed.json.example \
   src/LawCorp.Mcp.Server/persona-seed.json
```

4. Edit `persona-seed.json` — replace each persona's `Email` and `EntraObjectId` with the UPN and OID from your tenant
5. Rebuild and re-seed: set `SeedMockData=true` in appsettings and restart the server

> If `persona-seed.json` is missing, personas are seeded with empty emails and no `EntraObjectId`. This is fine for demo mode (`UseAuth=false`).

## Personas

| Name | Type | Role | Practice Group | Notes |
|---|---|---|---|---|
| Harvey Specter | Attorney | Partner | M&A | Full access; supervises Elle and Vinny |
| Kim Wexler | Attorney | Associate | Contract Law | Assigned cases only |
| Alan Shore | Attorney | OfCounsel | Securities | Own practice group (read-only) |
| Erin Brockovich | Paralegal | — | M&A | Assigned cases; no billing |
| Elle Woods | LegalAssistant | — | — | Assigned to Harvey; sees Harvey's cases |
| Vinny Gambini | Intern | — | M&A | Supervised by Harvey; redacted privileged content |

## Architecture

```
persona-seed.json          ← your tenant values (gitignored)
persona-seed.json.example  ← template with placeholders (tracked)
PersonaSeedConfig.cs       ← POCO model for the JSON structure
PersonaDefinitions.cs      ← fixed persona structure, takes PersonaIdentity
PersonaSeeder.cs           ← seeds personas into the database
```

The `PersonaSeeder` runs as the first step of `MockDataSeeder.SeedAsync()`:

1. Reads `PersonaSeedConfig` (bound from the `PersonaSeed` config section)
2. Seeds Harvey, Kim, and Alan as `Attorney` records
3. Seeds Erin as a `Paralegal`, Elle as a `LegalAssistant`, Vinny as an `Intern` — all linked to Harvey
4. Random attorneys fill out the rest of the firm after persona seeding

## Related

- [docs/auth-config.md](../../../docs/auth-config.md) — Full Azure setup guide
- [Story 1.2.4](../../../proj-mgmt/epics/01-foundation/1.2.4-downstream-resource-access/1.2.4-downstream-resource-access.md) — Downstream resource access via identity passthrough
- [Feature 7.2](../../../proj-mgmt/epics/07-testing/7.2-persona-fixture.md) — Persona fixture specification
