# BUG FIX SUMMARY: SQL Command Failure During Migration/Seed

**Bug ID:** 001-migration-seed-sql-failure  
**Status:** ✅ RESOLVED  
**Severity:** CRITICAL (blocks startup)  
**Date Reported:** 2026-02-24  
**Date Fixed:** 2026-02-24  
**Time to Resolution:** ~2 hours

---

## Problem

The solution failed to start with the error:
```
Cannot insert explicit value for identity column in table '[TableName]' when IDENTITY_INSERT is set to OFF.
```

This occurred during mock data seeding when trying to insert data into multiple tables.

## Root Cause

Mock data generators were explicitly setting `Id` values on entities, but EF Core automatically configures integer primary keys as SQL Server IDENTITY columns (auto-increment). SQL Server doesn't allow explicit value insertion into IDENTITY columns by default.

**Affected Generators:**
- `AttorneyGenerator` - inserted Attorneys with explicit IDs
- `ClientGenerator` - inserted Clients with explicit IDs  
- `CaseGenerator` - inserted Cases with explicit IDs
- `CalendarGenerator` - inserted Hearings and Deadlines with explicit IDs
- `DocumentGenerator` - inserted Documents with explicit IDs
- `ResearchGenerator` - inserted ResearchMemos with explicit IDs
- `TimeEntryGenerator` - inserted TimeEntries with explicit IDs

## Solution

### Part 1: Entity Configurations for Reference Data

Created EF Core configurations allowing explicit ID values for reference tables:

**[PracticeGroupConfiguration.cs](../../src/LawCorp.Mcp.Data/Configurations/PracticeGroupConfiguration.cs)**
```csharp
builder.Property(e => e.Id)
    .ValueGeneratedNever();  // Allow explicit ID values
```

**[CourtConfiguration.cs](../../src/LawCorp.Mcp.Data/Configurations/CourtConfiguration.cs)**
```csharp
builder.Property(e => e.Id)
    .ValueGeneratedNever();  // Allow explicit ID values
```

These reference tables (`PracticeGroup`, `Court`) have stable IDs (1-6, 1-5) that other entities depend on.

### Part 2: Modified Generators for Operational Data

For operational entities (Attorneys, Clients, Cases, etc.), the generators were updated to **NOT set explicit IDs**. After `SaveChangesAsync()`, EF Core automatically populates the `Id` properties with database-generated values.

**Changes Made:**

1. **AttorneyGenerator.cs** 
   - Removed: `Id = id`
   - Changed method signature from `Generate(int id, ...)` to `Generate(int sequenceNumber, ...)`
   - `sequenceNumber` is now only used for role assignment logic, not ID

2. **ClientGenerator.cs**
   - Removed: `Id = id`
   - Changed method signature from `Generate(int id)` to `Generate()`

3. **CaseGenerator.cs**
   - Removed: `Id = id`
   - Replaced: `_nextId` counter for case numbers instead of IDs
   - Added: Internal `_caseNumberCounter` for `CaseNumber` field format

4. **CalendarGenerator.cs**
   - Removed: `Id = _nextHearingId++` from `GenerateHearing()`
   - Removed: `Id = _nextDeadlineId++` from `GenerateDeadline()`
   - Removed: Counter fields `_nextHearingId`, `_nextDeadlineId`

5. **DocumentGenerator.cs**
   - Removed: `Id = _nextId++`
   - Removed: Counter field `_nextId`

6. **ResearchGenerator.cs**
   - Removed: `Id = _nextId++`
   - Removed: Counter field `_nextId`

7. **TimeEntryGenerator.cs**
   - Removed: `Id = _nextId++`
   - Removed: Counter field `_nextId`

### Part 3: Database Recreation Logic

Updated [Program.cs](../../src/LawCorp.Mcp.Server/Program.cs) to ensure a clean database:

```csharp
// Ensure a clean database by deleting and recreating it
// This forces the schema to be rebuilt with the latest entity configurations
await db.Database.EnsureDeletedAsync();
await db.Database.EnsureCreatedAsync();
```

---

## Verification Results

✅ **Build:** Succeeded (0 warnings, 0 errors)
✅ **Database Creation:** Clean schema created with correct IDENTITY settings
✅ **Reference Data Seeding:** 6 PracticeGroups + 5 Courts inserted successfully
✅ **Operational Data Seeding:** All attorneys, clients, cases, documents, hearings, deadlines seeded without errors
✅ **Server Startup:** Application listens on MCP protocol successfully

---

## Technical Details

### Why This Works

1. **Reference Tables** (`PracticeGroup`, `Court`):
   - Configured with `ValueGeneratedNever()` 
   - Explicitly set IDs (1-6, 1-5)
   - Other entities' foreign keys depend on these IDs
   - IDs must be predictable and stable

2. **Operational Tables** (everything else):
   - Use standard IDENTITY columns (auto-increment)
   - Generators don't set IDs
   - After insert, EF Core populates the `Id` property
   - Relationships work correctly because IDs are available in memory after insert

### EF Core Behavior

When you call `SaveChangesAsync()`:
1. EF Core inserts the entities
2. SQL Server generates IDENTITY values  
3. EF Core fetches the generated values
4. Update entity objects with generated IDs
5. Return to application with valid IDs for relationships

---

## Files Changed

**Configuration Files (Created):**
- `src/LawCorp.Mcp.Data/Configurations/PracticeGroupConfiguration.cs`
- `src/LawCorp.Mcp.Data/Configurations/CourtConfiguration.cs`

**Application Files (Modified):**
- `src/LawCorp.Mcp.Server/Program.cs` - Added `EnsureDeletedAsync()`

**Generator Files (Modified):**
- `src/LawCorp.Mcp.MockData/Generators/AttorneyGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/ClientGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/CaseGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/CalendarGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/DocumentGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/ResearchGenerator.cs`
- `src/LawCorp.Mcp.MockData/Generators/TimeEntryGenerator.cs`

---

## Prevention

To prevent similar issues:

1. **Default Behavior:** Let EF Core manage IDENTITY columns for operational data
2. **Explicit IDs:** Only use `ValueGeneratedNever()` when:
   - Data is reference/lookup tables with stable IDs
   - IDs must be predictable (e.g., codes 1, 2, 3)
   - Foreign keys depend on specific ID values
3. **Testing:** Verify seeding runs successfully on clean database startup

---

## Impact

- ✅ Application now starts successfully
- ✅ Database schema created correctly
- ✅ Mock data seeded for development/testing
- ✅ No manual database migration steps needed
- ✅ Clean development environment setup

