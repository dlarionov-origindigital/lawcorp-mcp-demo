// TODO: Implement MCP Resources for Law-Corp (Epic 4)
//
// Resources are read-only data endpoints exposed via URI templates.
// The MCP SDK resource API will be wired here once the data layer is complete.
//
// Planned static resources:
//   lawcorp://firm/profile            - Firm profile, practice groups, office info
//   lawcorp://firm/attorneys          - Full attorney directory
//   lawcorp://firm/rate-cards         - Standard billing rates by attorney level
//   lawcorp://reference/document-templates
//   lawcorp://reference/case-statuses
//   lawcorp://reference/practice-groups
//   lawcorp://reference/jurisdictions
//
// Planned dynamic resources (URI templates):
//   lawcorp://cases/{caseId}
//   lawcorp://cases/{caseId}/documents
//   lawcorp://cases/{caseId}/timeline
//   lawcorp://cases/{caseId}/billing
//   lawcorp://clients/{clientId}
//   lawcorp://clients/{clientId}/cases
//   lawcorp://attorneys/{attorneyId}
//   lawcorp://attorneys/{attorneyId}/timesheet
//   lawcorp://calendar/{attorneyId}/week
//   lawcorp://research/memos/{memoId}
//
// See: proj-mgmt/epics/04-mcp-resources.md

namespace LawCorp.Mcp.Server.Resources;
