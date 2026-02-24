# Product Requirements Document

## Law-Corp MCP Server

**Version:** 0.1.0-draft
**Date:** 2026-02-23
**Status:** Brainstorm / Draft

---

## 1. Overview

Law-Corp is a fictitious corporate law firm specializing in **Corporate Law & Mergers and Acquisitions**. This project delivers an **MCP (Model Context Protocol) server** built as a **.NET Web API** solution that exposes the firm's internal systems — case management, document management, billing, research, and more — as MCP tools, resources, and prompts.

The server is designed as an **enterprise-grade reference architecture** demonstrating:

- Real-world MCP tool, resource, and prompt design for a regulated industry
- On-behalf-of (OBO) authentication via **Microsoft Entra ID** for secured downstream resources
- A **custom authorization layer** over a local SQL Server Express database to demonstrate access control where none natively exists
- Entity Framework Core as the ORM with seed/mock data generation
- Deployment to **Azure Foundry**

The firm name "Law-Corp" is intentionally generic to the point of comedy — a mega-corp of law that takes itself very seriously.

---

## 2. Firm Profile

| Attribute | Detail |
|---|---|
| **Full Name** | Law-Corp LLP |
| **Tagline** | *"Corporate law. Corporately."* |
| **Specialization** | Corporate Law, Mergers & Acquisitions |
| **Size** | ~80 attorneys, plus paralegals, assistants, and interns |
| **Structure** | Single HQ office, multiple practice groups under the M&A umbrella |
| **Practice Groups** | Mergers & Acquisitions, Corporate Governance, Securities & Compliance, Due Diligence & Investigations, Contract Law, Intellectual Property (transactional) |

### 2.1 Pop-Culture Flavor

To keep the mock data memorable and fun, entity names will borrow from famous legal cases, films, and TV:

- **Partners**: Names inspired by iconic fictional lawyers (e.g., Atticus Finch, Elle Woods, Saul Goodman, Annalise Keating, Harvey Specter)
- **Cases**: Named after famous fictional or real legal proceedings (e.g., *Kramer v. Kramer*, *Fyre Festival Holdings Acquisition*, *Initech-Intertrode Merger*, *Dunder Mifflin Hostile Takeover*)
- **Clients**: A mix of fictional corporations (Acme Corp, Stark Industries, Umbrella Corp, Wonka Enterprises, Soylent Corp, Cyberdyne Systems)
- **Judges**: Named after famous TV/film judges (e.g., Judge Judy Sheindlin, Judge Dredd, Judge Reinhold — yes, the actor)
- **Witnesses / Experts**: Pop-culture adjacent names assembled from the mock data generator

---

## 3. MCP Capabilities

### 3.1 Tools

Tools are actions the LLM can invoke on behalf of the authenticated user. Each tool enforces authorization based on the caller's identity and role.

#### 3.1.1 Case Management

| Tool | Description | Parameters |
|---|---|---|
| `cases_search` | Search cases by keyword, status, practice group, assigned attorney, date range | `query`, `status?`, `practiceGroup?`, `assignedTo?`, `dateFrom?`, `dateTo?`, `page?`, `pageSize?` |
| `cases_get` | Retrieve full case details by case ID | `caseId` |
| `cases_update_status` | Update case status (e.g., Active, On Hold, Closed, Settled) | `caseId`, `newStatus`, `reason?` |
| `cases_assign_attorney` | Assign or reassign an attorney to a case | `caseId`, `attorneyId`, `role` (Lead, Supporting, Reviewer) |
| `cases_get_timeline` | Retrieve chronological timeline of all case events | `caseId`, `eventType?` |
| `cases_add_note` | Add a note or comment to a case | `caseId`, `content`, `isPrivileged?` |

#### 3.1.2 Document Management

| Tool | Description | Parameters |
|---|---|---|
| `documents_search` | Search documents by keyword, type, case, author | `query`, `documentType?`, `caseId?`, `authorId?`, `page?`, `pageSize?` |
| `documents_get` | Retrieve document metadata and content | `documentId` |
| `documents_draft` | Generate a draft document from a template | `templateType`, `caseId`, `parameters` |
| `documents_update_status` | Change document status (Draft, Under Review, Final, Filed) | `documentId`, `newStatus` |
| `documents_list_by_case` | List all documents associated with a case | `caseId`, `documentType?`, `status?` |

#### 3.1.3 Client & Contact Management

| Tool | Description | Parameters |
|---|---|---|
| `clients_search` | Search clients by name, industry, type | `query`, `clientType?` (Individual, Organization), `industry?` |
| `clients_get` | Retrieve client profile and engagement history | `clientId` |
| `clients_conflict_check` | Run conflict-of-interest check for a potential client or matter | `clientName`, `opposingParties`, `relatedEntities?` |
| `contacts_search` | Search contacts (witnesses, experts, opposing counsel, judges) | `query`, `contactType?`, `jurisdiction?` |
| `contacts_get` | Retrieve contact details | `contactId` |

#### 3.1.4 Billing & Time Entry

| Tool | Description | Parameters |
|---|---|---|
| `time_entries_log` | Log billable or non-billable time | `attorneyId`, `caseId`, `hours`, `description`, `date`, `billable` |
| `time_entries_search` | Search time entries by attorney, case, date range | `attorneyId?`, `caseId?`, `dateFrom?`, `dateTo?`, `billable?` |
| `billing_get_summary` | Get billing summary for a case or client | `caseId?`, `clientId?`, `dateFrom?`, `dateTo?` |
| `invoices_search` | Search invoices by client, status, date | `clientId?`, `status?` (Draft, Sent, Paid, Overdue), `dateFrom?`, `dateTo?` |
| `invoices_get` | Retrieve invoice details with line items | `invoiceId` |

#### 3.1.5 Court Calendar & Deadlines

| Tool | Description | Parameters |
|---|---|---|
| `calendar_get_hearings` | Get upcoming hearings for a case or attorney | `caseId?`, `attorneyId?`, `dateFrom?`, `dateTo?` |
| `calendar_get_deadlines` | Get filing deadlines and statute of limitations | `caseId?`, `attorneyId?`, `urgency?` (Critical, High, Normal) |
| `calendar_add_event` | Add a hearing, deadline, or meeting to the calendar | `caseId`, `eventType`, `title`, `dateTime`, `attendees?`, `notes?` |
| `calendar_get_conflicts` | Check for scheduling conflicts for attorneys | `attorneyIds`, `proposedDateTime`, `duration` |

#### 3.1.6 Legal Research

| Tool | Description | Parameters |
|---|---|---|
| `research_search_precedents` | Search case law precedents by topic, jurisdiction, date | `query`, `jurisdiction?`, `dateFrom?`, `dateTo?`, `practiceArea?` |
| `research_get_statute` | Retrieve statute text and annotations | `statuteId`, `jurisdiction` |
| `research_get_memo` | Retrieve a research memo | `memoId` |
| `research_create_memo` | Create a new research memo linked to a case | `caseId`, `topic`, `findings`, `authorId` |
| `research_search_memos` | Search existing research memos | `query`, `caseId?`, `authorId?`, `page?`, `pageSize?` |

#### 3.1.7 Intake & Onboarding

| Tool | Description | Parameters |
|---|---|---|
| `intake_create_request` | Create a new client intake request | `clientName`, `contactInfo`, `matterDescription`, `practiceGroup`, `referralSource?` |
| `intake_get_request` | Retrieve intake request details and status | `requestId` |
| `intake_run_conflict_check` | Run conflict checks as part of intake | `requestId` |
| `intake_approve` | Approve an intake request (Partner only) | `requestId`, `assignedPartnerId`, `notes?` |
| `intake_generate_engagement_letter` | Generate engagement letter from intake data | `requestId`, `feeStructure`, `scope` |

---

### 3.2 Resources

Resources are data endpoints the LLM can read. They follow URI templates and provide contextual data without side effects.

#### 3.2.1 Static Resources

| URI | Description |
|---|---|
| `lawcorp://firm/profile` | Firm profile, practice groups, office information |
| `lawcorp://firm/attorneys` | Full attorney directory with roles and practice groups |
| `lawcorp://firm/rate-cards` | Standard billing rates by attorney level |
| `lawcorp://reference/document-templates` | List of available document templates and their parameters |
| `lawcorp://reference/case-statuses` | Enumeration of valid case statuses and transitions |
| `lawcorp://reference/practice-groups` | Practice group definitions and descriptions |
| `lawcorp://reference/jurisdictions` | Supported jurisdictions and their court hierarchies |

#### 3.2.2 Dynamic Resources (URI Templates)

| URI Template | Description |
|---|---|
| `lawcorp://cases/{caseId}` | Case details including team, status, key dates |
| `lawcorp://cases/{caseId}/documents` | All documents for a case |
| `lawcorp://cases/{caseId}/timeline` | Case event timeline |
| `lawcorp://cases/{caseId}/billing` | Billing summary for a case |
| `lawcorp://clients/{clientId}` | Client profile and matter history |
| `lawcorp://clients/{clientId}/cases` | All cases for a client |
| `lawcorp://attorneys/{attorneyId}` | Attorney profile, caseload, and availability |
| `lawcorp://attorneys/{attorneyId}/timesheet` | Current period timesheet for an attorney |
| `lawcorp://calendar/{attorneyId}/week` | Weekly calendar view for an attorney |
| `lawcorp://research/memos/{memoId}` | Full research memo content |

#### 3.2.3 Subscription Resources (Notifications)

| URI | Description | Trigger |
|---|---|---|
| `lawcorp://notifications/deadlines` | Upcoming deadline alerts | Deadline within 48 hours |
| `lawcorp://notifications/case-updates` | Case status changes on assigned cases | Status transition |
| `lawcorp://notifications/new-assignments` | New case assignments for the current user | Assignment created |
| `lawcorp://notifications/billing-alerts` | Overdue invoices, budget thresholds | Invoice overdue or budget > 80% |

---

### 3.3 Prompts

Prompts are reusable, parameterized prompt templates that the LLM can offer to users. They encode domain-specific workflows.

| Prompt | Description | Arguments |
|---|---|---|
| `draft_motion` | Guide the user through drafting a motion with appropriate legal structure | `caseId`, `motionType`, `arguments` |
| `summarize_case` | Generate a structured case summary for a given audience | `caseId`, `audience` (Partner, Client, Court) |
| `prepare_due_diligence_checklist` | Generate a due diligence checklist for an M&A transaction | `caseId`, `dealType` (Asset Purchase, Stock Purchase, Merger) |
| `draft_engagement_letter` | Generate an engagement letter from client intake data | `clientId`, `matterDescription`, `feeStructure` |
| `analyze_contract_risks` | Analyze a contract and identify key risks and unusual terms | `documentId`, `contractType` |
| `prepare_board_resolution` | Draft a board resolution for corporate action | `caseId`, `resolutionType`, `details` |
| `summarize_deposition` | Summarize deposition transcript with key testimony highlighted | `documentId`, `focusTopics?` |
| `generate_case_status_report` | Create a status report for a case suitable for client communication | `caseId`, `reportingPeriod` |
| `conflict_check_analysis` | Analyze conflict check results and recommend next steps | `intakeRequestId` |
| `research_brief` | Compile a research brief on a legal topic with relevant precedents | `topic`, `jurisdiction`, `caseId?` |
| `compare_deal_terms` | Compare terms across multiple deal documents | `documentIds`, `comparisonAreas` |
| `draft_closing_checklist` | Generate a closing checklist for a transaction | `caseId`, `closingDate`, `dealType` |

---

### 3.4 Sampling

The MCP server will support **sampling** to enable server-initiated LLM requests for:

| Use Case | Description |
|---|---|
| **Document Classification** | Automatically classify uploaded documents by type (motion, brief, contract, correspondence) |
| **Deadline Extraction** | Parse documents and extract key dates and deadlines for calendar integration |
| **Conflict Detection Enhancement** | Use LLM reasoning to identify non-obvious conflicts from entity relationships |
| **Time Entry Description Enhancement** | Improve time entry descriptions for billing compliance |

---

### 3.5 Roots

The server will declare the following roots to inform the client of relevant filesystem or workspace boundaries:

| Root | Description |
|---|---|
| `lawcorp://workspace` | The top-level root representing the user's current workspace scope |
| `lawcorp://cases` | The case management system boundary |
| `lawcorp://documents` | The document management system boundary |
| `lawcorp://research` | The legal research database boundary |

---

## 4. Authorization Model

### 4.1 Authentication Flow

All requests are authenticated via **Microsoft Entra ID** using the **On-Behalf-Of (OBO) flow**:

1. Client application obtains a user token from Entra ID
2. MCP server receives the token and validates it
3. For downstream secured resources: server exchanges the token via OBO flow
4. For the local SQL Express database: server extracts claims (roles, groups) from the token and enforces authorization via a **custom middleware layer**

### 4.2 Roles & Permissions

| Role | Cases | Documents | Billing | Research | Intake | Calendar |
|---|---|---|---|---|---|---|
| **Partner** | Full access (own practice group) | Full access | Full access (view & approve) | Full access | Approve / Reject | Full access |
| **Associate** | Read/Update (assigned cases only) | Read/Draft (assigned cases) | Log time (own entries only) | Full access | Create requests | Own calendar + assigned case events |
| **Of Counsel** | Read (own practice group) | Read (own practice group) | View own time entries | Full access | No access | Own calendar |
| **Paralegal** | Read (assigned cases) | Read/Draft (assigned cases) | No access | Read only | Create requests | Assigned case events |
| **Legal Assistant** | Read (assigned attorney's cases) | Read (assigned attorney's cases) | View (assigned attorney) | No access | Create requests | Assigned attorney's calendar |
| **Intern** | Read (assigned, redacted) | Read (assigned, redacted) | No access | Read/Create memos | No access | Own schedule only |

### 4.3 Custom Authorization Layer

For the local SQL Express database (which has no native identity-aware access control), the server implements:

- **`IAuthorizationHandler`** — Custom authorization handlers that evaluate Entra ID claims against entity ownership and role permissions
- **Row-level filtering** — EF Core global query filters scoped to the user's role and assignments
- **Field-level redaction** — Sensitive fields (SSN, financial details, privileged notes) redacted based on role
- **Audit logging** — All data access logged with user identity, action, entity, and timestamp

---

## 5. Data Model

### 5.1 Entity Relationship Overview

```
Firm
├── PracticeGroup
│   └── Attorney (Partner, Associate, OfCounsel)
├── Staff
│   ├── Paralegal
│   ├── LegalAssistant
│   └── Intern
├── Client
│   ├── IndividualClient
│   └── OrganizationClient
├── Case
│   ├── CaseAssignment (Attorney ↔ Case with Role)
│   ├── CaseParty
│   │   ├── Defendant
│   │   ├── Plaintiff
│   │   ├── Witness
│   │   └── Expert
│   ├── OpposingCounsel
│   ├── Judge
│   ├── Court
│   ├── Document
│   │   ├── Motion
│   │   ├── Brief
│   │   ├── Contract
│   │   ├── Correspondence
│   │   └── Evidence
│   ├── TimeEntry
│   ├── Invoice
│   │   └── InvoiceLineItem
│   ├── CaseEvent (Timeline)
│   ├── Deadline
│   └── Hearing
├── ResearchMemo
├── IntakeRequest
├── ConflictCheck
│   └── ConflictCheckResult
└── AuditLog
```

### 5.2 Core Entities

#### People & Organization

| Entity | Key Fields |
|---|---|
| **Attorney** | Id, FirstName, LastName, Email, BarNumber, Role (Partner/Associate/OfCounsel), PracticeGroupId, HourlyRate, HireDate, IsActive |
| **Paralegal** | Id, FirstName, LastName, Email, PracticeGroupId, AssignedAttorneyIds, HireDate |
| **LegalAssistant** | Id, FirstName, LastName, Email, AssignedAttorneyId, HireDate |
| **Intern** | Id, FirstName, LastName, Email, School, PracticeGroupId, StartDate, EndDate, SupervisorId |
| **Client** | Id, Name, Type (Individual/Organization), Industry, ContactEmail, ContactPhone, Address, EngagementDate, Status |
| **Contact** | Id, FirstName, LastName, Type (Witness/Expert/Judge/OpposingCounsel/Prosecutor), Organization, ContactInfo, Jurisdiction, Notes |

#### Case Management

| Entity | Key Fields |
|---|---|
| **Case** | Id, CaseNumber, Title, Description, Status, PracticeGroupId, ClientId, CourtId, JudgeId, OpenDate, CloseDate, EstimatedValue |
| **CaseAssignment** | Id, CaseId, AttorneyId, Role (Lead/Supporting/Reviewer), AssignedDate |
| **CaseParty** | Id, CaseId, Name, PartyType (Defendant/Plaintiff/Witness/Expert), ContactId, Representation |
| **CaseEvent** | Id, CaseId, EventType, Title, Description, EventDate, CreatedById |

#### Documents

| Entity | Key Fields |
|---|---|
| **Document** | Id, CaseId, Title, DocumentType (Motion/Brief/Contract/Correspondence/Evidence), Status (Draft/UnderReview/Final/Filed), Content, AuthorId, CreatedDate, ModifiedDate, IsPrivileged, IsRedacted |

#### Billing

| Entity | Key Fields |
|---|---|
| **TimeEntry** | Id, AttorneyId, CaseId, Date, Hours, Description, BillableRate, Billable, Status (Draft/Submitted/Approved/Billed) |
| **Invoice** | Id, ClientId, CaseId, InvoiceNumber, IssueDate, DueDate, TotalAmount, Status (Draft/Sent/Paid/Overdue), Notes |
| **InvoiceLineItem** | Id, InvoiceId, TimeEntryId, Description, Hours, Rate, Amount |

#### Calendar & Deadlines

| Entity | Key Fields |
|---|---|
| **Hearing** | Id, CaseId, CourtId, JudgeId, DateTime, Type (Motion/Trial/Pretrial/Status), Location, Notes |
| **Deadline** | Id, CaseId, Title, DueDate, Urgency (Critical/High/Normal), Type (Filing/Discovery/Response/Regulatory), CompletedDate, AssignedToId |

#### Research

| Entity | Key Fields |
|---|---|
| **ResearchMemo** | Id, CaseId, AuthorId, Topic, Findings, Jurisdiction, CreatedDate, Tags |

#### Intake & Conflicts

| Entity | Key Fields |
|---|---|
| **IntakeRequest** | Id, ProspectName, ContactInfo, MatterDescription, PracticeGroupId, ReferralSource, Status (Pending/ConflictCheck/Approved/Rejected), CreatedDate, ReviewedById |
| **ConflictCheck** | Id, IntakeRequestId, CheckedById, CheckDate, Status (Clear/PotentialConflict/Conflict), Notes |

#### Audit

| Entity | Key Fields |
|---|---|
| **AuditLog** | Id, UserId, UserRole, Action, EntityType, EntityId, Timestamp, Details, IpAddress |

---

## 6. Mock Data Generation

### 6.1 Strategy

A standalone **mock data generation utility** will produce realistic seed data by assembling names and details from curated partial lists. The generator will be deterministic (seeded random) so that data is reproducible across environments.

### 6.2 Generator Design

```
MockDataGenerator/
├── Partials/
│   ├── FirstNames.cs        // ~50 first names (mix of pop-culture legal + common)
│   ├── LastNames.cs         // ~50 last names (drawn from legal fiction)
│   ├── CompanyNames.cs      // ~30 fictional company names (pop-culture corps)
│   ├── CompanySuffixes.cs   // Inc, Corp, LLC, Holdings, Enterprises, Industries
│   ├── CaseTitles.cs        // Templates: "{Plaintiff} v. {Defendant}", "In re {Company} {Action}"
│   ├── LegalTopics.cs       // M&A topics, corporate governance terms
│   ├── Jurisdictions.cs     // State and federal courts
│   └── LoremLegal.cs        // Legal-flavored lorem ipsum for document content
├── Generators/
│   ├── AttorneyGenerator.cs
│   ├── ClientGenerator.cs
│   ├── CaseGenerator.cs
│   ├── DocumentGenerator.cs
│   ├── TimeEntryGenerator.cs
│   ├── CalendarGenerator.cs
│   └── ResearchGenerator.cs
├── Profiles/
│   ├── SmallFirmProfile.cs  // ~20 attorneys, ~30 cases
│   ├── MediumFirmProfile.cs // ~80 attorneys, ~150 cases (default)
│   └── LargeFirmProfile.cs  // ~200 attorneys, ~500 cases
└── MockDataSeeder.cs        // Orchestrates generation and EF Core seeding
```

### 6.3 Permutation Approach

The generator assembles entities from partial lists via combinatorial selection:

- **People**: `FirstName[i] + LastName[j]` with role-appropriate attributes (bar numbers for attorneys, schools for interns)
- **Companies**: `CompanyName[i] + CompanySuffix[j]` with random industry assignment
- **Cases**: `CaseTitle template` populated with generated client/party names, assigned to practice groups, with realistic date ranges
- **Documents**: Generated per case using type-appropriate templates with legal lorem ipsum content
- **Time Entries**: Distributed across attorneys and their assigned cases, following realistic billing patterns (associates bill more hours, partners bill fewer at higher rates)
- **Calendar Events**: Generated relative to case dates with realistic hearing schedules and filing deadlines

### 6.4 Pop-Culture Name Pools (Examples)

**Attorney First + Last (will be mixed-and-matched):**
> Atticus, Harvey, Saul, Annalise, Elle, Mitch, Erin, Denny, Alan, Vinny, Jake, Kim, Perry, Ally, Frank

> Finch, Specter, Goodman, Keating, Woods, McDeere, Brockovich, Crane, Shore, Gambini, Brigance, Wexler, Mason, McBeal, Galvin

**Fictional Companies:**
> Acme Corp, Stark Industries, Umbrella Corp, Wonka Enterprises, Soylent Corp, Initech, Globex Corp, Massive Dynamic, Cyberdyne Systems, Oscorp Industries, Wayne Enterprises, Dunder Mifflin, Prestige Worldwide, Vandelay Industries, Bluth Company

**Case Title Templates:**
> `{Company} Acquisition Review`, `{Company} v. {Company} — Breach of Merger Agreement`, `In re {Company} Securities Litigation`, `{Company}-{Company} Joint Venture`, `{Company} Board of Directors — Fiduciary Duty Review`

---

## 7. MCP Protocol Features Summary

| MCP Feature | Used | Implementation Notes |
|---|---|---|
| **Tools** | Yes | 35+ tools across 7 categories (see Section 3.1) |
| **Resources** | Yes | Static, dynamic (URI templates), and subscription-based (see Section 3.2) |
| **Prompts** | Yes | 12 domain-specific prompt templates (see Section 3.3) |
| **Sampling** | Yes | Server-initiated LLM calls for classification, extraction, and enrichment (see Section 3.4) |
| **Roots** | Yes | Workspace boundary declarations (see Section 3.5) |
| **Logging** | Yes | Structured logging for all tool invocations and authorization decisions |
| **Pagination** | Yes | All search/list tools support cursor-based pagination |
| **Progress** | Yes | Long-running tools (conflict checks, document generation) report progress |
| **Cancellation** | Yes | Long-running operations support cancellation |
| **Error Handling** | Yes | Structured error responses with MCP error codes, authorization failures return appropriate codes |

---

## 8. Success Criteria

- [ ] All tools enforceable via the custom authorization layer with Entra ID claims
- [ ] Mock data is generated deterministically and seeds the local SQL Express database
- [ ] Resources correctly resolve URI templates and enforce read-level authorization
- [ ] Prompts produce well-structured, domain-appropriate output
- [ ] Sampling use cases demonstrably enhance tool capabilities
- [ ] Audit log captures every data access with full identity context
- [ ] The solution compiles, runs locally with SQL Express, and deploys to Azure Foundry

---

## 9. Open Questions

- [ ] Should we model multi-office or keep it single-HQ for simplicity?
- [ ] Do we need a separate "matter" concept distinct from "case"?
- [ ] Should the mock data generator be a separate CLI tool or integrated into the EF migration seed?
- [ ] What level of document content generation — just metadata, or actual legal-flavored text bodies?
- [ ] Should we model external integrations (e-filing, legal research APIs like Westlaw) even as stubs?

---

*This document is a living draft. Sections will be refined as brainstorming continues.*
