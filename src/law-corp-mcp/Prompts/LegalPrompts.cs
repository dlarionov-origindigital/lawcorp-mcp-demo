// TODO: Implement MCP Prompts for Law-Corp (Epic 5)
//
// Prompts are reusable, parameterized templates that encode domain-specific workflows.
// The MCP SDK prompt API will be wired here.
//
// Planned prompts:
//   draft_motion               - Guide drafting a motion with legal structure
//   summarize_case             - Structured case summary for Partner, Client, or Court
//   prepare_due_diligence_checklist - Due diligence checklist for M&A transactions
//   draft_engagement_letter    - Engagement letter from client intake data
//   analyze_contract_risks     - Identify key risks and unusual terms in a contract
//   prepare_board_resolution   - Draft a board resolution for corporate action
//   summarize_deposition       - Summarize deposition transcript
//   generate_case_status_report - Status report suitable for client communication
//   conflict_check_analysis    - Analyze conflict check results and recommend next steps
//   research_brief             - Research brief on a legal topic with precedents
//   compare_deal_terms         - Compare terms across multiple deal documents
//   draft_closing_checklist    - Closing checklist for a transaction
//
// See: proj-mgmt/epics/05-mcp-prompts-sampling.md

namespace LawCorp.Mcp.Server.Prompts;
