namespace LawCorp.Mcp.Core.Models;

public enum FirmRole { Partner, Associate, OfCounsel, Paralegal, LegalAssistant, Intern }

[Obsolete("Use FirmRole instead. Retained only for migration compatibility.")]
public enum AttorneyRole { Partner, Associate, OfCounsel }
public enum ClientType { Individual, Organization }
public enum ContactType { Witness, Expert, Judge, OpposingCounsel, Prosecutor }
public enum CaseStatus { Active, OnHold, Closed, Settled }
public enum AssignmentRole { Lead, Supporting, Reviewer }
public enum PartyType { Defendant, Plaintiff, Witness, Expert }
public enum CaseEventType { StatusChange, Assignment, Note, Filing, Hearing, Deadline, DocumentAdded, Other }
public enum DocumentType { Motion, Brief, Contract, Correspondence, Evidence }
public enum DocumentStatus { Draft, UnderReview, Final, Filed }
public enum TimeEntryStatus { Draft, Submitted, Approved, Billed }
public enum InvoiceStatus { Draft, Sent, Paid, Overdue }
public enum HearingType { Motion, Trial, Pretrial, Status }
public enum DeadlineUrgency { Critical, High, Normal }
public enum DeadlineType { Filing, Discovery, Response, Regulatory }
public enum IntakeStatus { Pending, ConflictCheck, Approved, Rejected }
public enum ConflictCheckStatus { Clear, PotentialConflict, Conflict }
