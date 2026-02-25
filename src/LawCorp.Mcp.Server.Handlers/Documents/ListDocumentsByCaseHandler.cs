using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LawCorp.Mcp.Server.Handlers.Documents;

/// <summary>
/// Lists documents for a specific case from the external DMS API via OBO.
/// </summary>
public class ListDocumentsByCaseHandler(
    IHttpClientFactory httpClientFactory,
    IDownstreamTokenProvider tokenProvider,
    IConfiguration configuration)
    : IRequestHandler<ListDocumentsByCaseQuery, SearchDocumentsResult>
{
    public async Task<SearchDocumentsResult> Handle(ListDocumentsByCaseQuery request, CancellationToken ct)
    {
        var client = await CreateAuthenticatedClient(ct);

        var queryParams = new List<string> { $"caseId={request.CaseId}" };
        if (request.DocumentType is not null) queryParams.Add($"documentType={Uri.EscapeDataString(request.DocumentType)}");
        if (request.Status is not null) queryParams.Add($"status={Uri.EscapeDataString(request.Status)}");

        var response = await client.GetAsync($"/api/documents?{string.Join("&", queryParams)}", ct);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Access denied by the document management system.");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchDocumentsResult>(ct)
            ?? new SearchDocumentsResult([], 1, 100, 0, false);
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
