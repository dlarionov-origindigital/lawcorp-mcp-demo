# Bug Research: SQL Command Failure During Migration/Seed

**Bug ID:** 001-migration-seed-sql-failure  
**Status:** INVESTIGATING  
**Severity:** CRITICAL (blocks startup)  
**Date Reported:** 2026-02-24

## Problem Statement

The solution fails to start because an SQL command is failing during either:
- Entity Framework Core database migration, OR
- Mock data seeding process

The application cannot initialize and the error prevents normal development workflow.

## Investigation Steps

### 1. Identify Failure Point

- [ ] Determine if error occurs during `Update-Database` migration or `MockDataSeeder.cs` execution
- [ ] Check `appsettings.Development.json` for database connection string
- [ ] Review `Program.cs` initialization logic
- [ ] Check `LawCorp.Mcp.Server.csproj` and `LawCorp.Mcp.Core.csproj` for DbContext configuration

### 2. Collect Error Information

- [ ] Capture full stack trace from application startup
- [ ] Check `bin/` and `obj/` directories for build artifacts
- [ ] Review recent migrations in `Migrations/` folder (if exists)
- [ ] Examine `MockDataSeeder.cs` for seed data generation logic
- [ ] Check SQL Server logs for query errors

### 3. Database Schema Analysis

- [ ] Verify database exists and is accessible
- [ ] Check if migrations have been applied
- [ ] Confirm all required tables exist
- [ ] Validate foreign key constraints and relationships
- [ ] Review any recent schema changes that might cause conflicts

### 4. EF Core Configuration Issues

- [ ] Check `DbContext` configuration in `Auth/` or core setup
- [ ] Verify entity mappings and conventions
- [ ] Review any `HasData()` seeding configuration
- [ ] Check for SQL-specific type mappings (e.g., `nvarchar`, `uniqueidentifier`)

### 5. Data Seeding Logic

- [ ] Review `MockDataSeeder.cs` implementation
- [ ] Check for null foreign key violations
- [ ] Verify seed data matches entity constraints
- [ ] Look for duplicate key violations
- [ ] Check for type mismatches in seed data

## Key Files to Examine

- `src/LawCorp.Mcp.Server/Program.cs` - Application startup and DbContext initialization
- `src/LawCorp.Mcp.MockData/MockDataSeeder.cs` - Seed data logic
- `src/appsettings.Development.json` - Database connection configuration
- `src/LawCorp.Mcp.Server/appsettings.Development.json`
- Migration files (location TBD)
- DbContext definition (location TBD)

## Possible Root Causes

1. **Connection String Issue** - Invalid or missing database connection
2. **Migration Failure** - Pending or broken migrations not applied
3. **Foreign Key Constraint Violation** - Seed data references non-existent entities
4. **Type Mismatch** - SQL type incompatibility with entity property types
5. **Duplicate Key** - Seed data attempting to insert duplicate primary/unique keys
6. **Null Constraint Violation** - Required fields receiving null values
7. **Incomplete Schema** - Expected tables or columns not present
8. **Concurrency Issue** - Multiple seed operations conflicting
9. **Authentication** - SQL Server authentication context not available
10. **Microsoft Entra ID Integration** - Token-based auth failing in dev environment

## Next Actions

1. **Extract Error Details** - Run the solution with detailed logging enabled
2. **Review Recent Changes** - Check git history for recent data model or auth changes
3. **Test Connectivity** - Verify SQL Server connection independently
4. **Isolate Seed Process** - Run migrations and seeding separately to identify which fails
5. **Review PRD** - Cross-reference PRD Section 4.2 (permissions matrix) if auth is involved

