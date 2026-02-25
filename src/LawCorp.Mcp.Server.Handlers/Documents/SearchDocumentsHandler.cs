using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LawCorp.Mcp.Server.Handlers.Documents;

/// <summary>
/// Dispatches document search to the external DMS API via OBO token exchange.
/// The MCP tool calling this handler has no knowledge of the network call.
/// </summary>
public class SearchDocumentsHandler(
    IHttpClientFactory httpClientFactory,
    IDownstreamTokenProvider tokenProvider,
    IConfiguration configuration)
    : IRequestHandler<SearchDocumentsQuery, SearchDocumentsResult>
{
    public async Task<SearchDocumentsResult> Handle(SearchDocumentsQuery request, CancellationToken ct)
    {
        var client = await CreateAuthenticatedClient(ct);

        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Query))
            queryParams.Add($"query={Uri.EscapeDataString(request.Query)}");
        if (request.DocumentType is not null) queryParams.Add($"documentType={Uri.EscapeDataString(request.DocumentType)}");
        if (request.CaseId.HasValue) queryParams.Add($"caseId={request.CaseId.Value}");
        if (request.AuthorId.HasValue) queryParams.Add($"authorId={request.AuthorId.Value}");
        queryParams.Add($"page={request.Page}");
        queryParams.Add($"pageSize={request.PageSize}");

        var response = await client.GetAsync($"/api/documents?{string.Join("&", queryParams)}", ct);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Access denied by the document management system.");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchDocumentsResult>(ct)
            ?? new SearchDocumentsResult([], 1, 20, 0, false);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ExternalApi");
        var scopes = configuration.GetSection("DownstreamApis:ExternalApi:Scopes").Get<string[]>()
            ?? ["api://external-api/data.read"];
        var token = await tokenProvider.AcquireTokenAsync(scopes, ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
