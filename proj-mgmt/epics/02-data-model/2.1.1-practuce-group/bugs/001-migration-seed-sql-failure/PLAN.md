# Bug Fix Plan: SQL Command Failure During Migration/Seed

**Bug ID:** 001-migration-seed-sql-failure  
**Created:** 2026-02-24  
**Target Resolution:** TBD (after investigation)

## Execution Plan

### Phase 1: Diagnosis (High Priority)

**Objective:** Identify exact failure point and root cause

#### Task 1.1: Enable Detailed Logging
- [ ] Add `LogLevel.Debug` to EF Core `DbContextOptionsBuilder`
- [ ] Enable SQL command logging in `Program.cs`
- [ ] Redirect stderr/stdout to capture migration output
- [ ] Run solution startup with full trace

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  
**Acceptance:** Full SQL query visible in logs leading to failure

#### Task 1.2: Isolate Migration vs. Seed
- [ ] Run `dotnet ef database update` independently
- [ ] Document success/failure
- [ ] If successful, run seeding separately
- [ ] If both fail, investigate DbContext configuration

**Owner:** [YOUR NAME]  
**Time Est:** 20 minutes  
**Acceptance:** Clear identification of failure source

#### Task 1.3: Examine Recent Changes
- [ ] Review git history on `LawCorp.Mcp.Core`, `LawCorp.Mcp.MockData`, `LawCorp.Mcp.Server`
- [ ] Check for uncommitted changes affecting migrations
- [ ] Review any data model changes from Epic 2
- [ ] Document relevant changes

**Owner:** [YOUR NAME]  
**Time Est:** 20 minutes  
**Acceptance:** List of recent changes that could impact database setup

### Phase 2: Root Cause Analysis

**Objective:** Determine specific SQL error and why it's occurring

#### Task 2.1: Parse SQL Error
- [ ] Extract full SQL command from logs
- [ ] Identify the problematic statement (CREATE TABLE, INSERT, ALTER, etc.)
- [ ] Note any parameter values that seem suspicious
- [ ] Check if error is syntax-related or constraint-related

**Owner:** [YOUR NAME]  
**Time Est:** 20 minutes  

#### Task 2.2: Validate Schema Assumptions
- [ ] Connect to target SQL Server directly
- [ ] Check existing table structure
- [ ] Verify data types match EF Core entity definitions
- [ ] Check for orphaned tables or columns from previous attempts

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  

#### Task 2.3: Review Entity Definitions
- [ ] Examine all entity classes in `LawCorp.Mcp.Core/Models/`
- [ ] Check for inconsistencies between attributes and EF Core fluent configuration
- [ ] Verify navigation property configurations
- [ ] Review foreign key relationships against database

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  

### Phase 3: Implementation (After diagnosis)

**Objective:** Fix the identified root cause

**Conditional Fixes (select based on Phase 2 findings):**

#### Fix A: Migration Error
- [ ] Drop and recreate migration if corrupted: `dotnet ef migrations remove --force`
- [ ] Create clean migration: `dotnet ef migrations add InitialCreate`
- [ ] Update database: `dotnet ef database update`
- [ ] Test solution startup

**Owner:** [YOUR NAME]  
**Time Est:** 20 minutes  

#### Fix B: Seed Data Issue
- [ ] Review `MockDataSeeder.cs` for constraint violations
- [ ] Validate foreign key references exist before insertion
- [ ] Check for duplicate primary key attempts
- [ ] Add null checks for required fields
- [ ] Add transactional wrapper with rollback on error
- [ ] Re-run seeding

**Owner:** [YOUR NAME]  
**Time Est:** 45 minutes  

#### Fix C: Configuration Issue
- [ ] Verify `appsettings.Development.json` connection string
- [ ] Test SQL Server connectivity from development machine
- [ ] Check authentication method (Windows vs. SQL auth)
- [ ] Validate database name and server address
- [ ] Create database if it doesn't exist

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  

#### Fix D: Entity Mapping Issue
- [ ] Create migration with `--verbose` flag to see SQL
- [ ] Compare generated SQL against entity definitions
- [ ] Add explicit column mappings via Fluent API if needed
- [ ] Adjust data types (e.g., `string` → `varchar(max)`, GUID handling)

**Owner:** [YOUR NAME]  
**Time Est:** 45 minutes  

### Phase 4: Validation

**Objective:** Ensure fix is complete and solution starts cleanly

#### Task 4.1: Clean Build & Run
- [ ] `dotnet clean`
- [ ] Delete `bin/` and `obj/` directories
- [ ] `dotnet build` - confirm no errors
- [ ] Run application - confirm startup without SQL errors
- [ ] Verify mock data is loaded (if seed phase)

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  
**Acceptance:** Application runs without error, listens on expected port

#### Task 4.2: Regression Testing
- [ ] Verify solution structure (Epic 1.1 success criteria still met)
- [ ] Test MCP server responds to basic protocol messages
- [ ] Confirm database is populated with mock data
- [ ] Check that no unintended schema changes occurred

**Owner:** [YOUR NAME]  
**Time Est:** 30 minutes  
**Acceptance:** All previous functionality intact

#### Task 4.3: Documentation
- [ ] Update this plan with solution applied
- [ ] Document any configuration changes needed for other developers
- [ ] Update README if connection string or setup steps changed
- [ ] Close bug and link to related commits

**Owner:** [YOUR NAME]  
**Time Est:** 20 minutes  

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Migration affects production database | Use isolated dev database only; verify connection string before running |
| Seed data is lost if not backed up | Commit `MockDataSeeder.cs` changes before rebuilding database |
| Incomplete fix causes repeated failures | Run validation phase completely before closing bug |
| Other developers blocked | Document solution in README immediately upon fix |

## Success Criteria

- [x] Bug is reproducible and tracked
- [ ] Root cause identified and documented
- [ ] Fix applied with test confirmation
- [ ] Solution starts without SQL errors
- [ ] Mock data loads successfully
- [ ] No regression in Epic 1.1 success criteria
- [ ] Team aware of cause and prevention

## Related Issues

- Relates to: Epic 2 - Data Model implementation
- May depend on: Epic 1.1 solution structure
- Potentially blocks: Feature development in Epic 2+

