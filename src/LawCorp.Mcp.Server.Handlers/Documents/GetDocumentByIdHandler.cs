using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LawCorp.Mcp.Core.Auth;
using LawCorp.Mcp.Core.Queries;
using MediatR;

namespace LawCorp.Mcp.Server.Handlers.Documents;

/// <summary>
/// Retrieves a single document from the external DMS API via OBO.
/// </summary>
public class GetDocumentByIdHandler(
    IHttpClientFactory httpClientFactory,
    IDownstreamTokenProvider tokenProvider)
    : IRequestHandler<GetDocumentByIdQuery, GetDocumentResult>
{
    public async Task<GetDocumentResult> Handle(GetDocumentByIdQuery request, CancellationToken ct)
    {
        var client = await CreateAuthenticatedClient(ct);
        var response = await client.GetAsync($"/api/documents/{request.DocumentId}", ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new GetDocumentResult(null, $"Document {request.DocumentId} not found.");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return new GetDocumentResult(null, "Access denied: you do not have access to this document.");

        response.EnsureSuccessStatusCode();
        var doc = await response.Content.ReadFromJsonAsync<DocumentDetail>(ct);
        return new GetDocumentResult(doc);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ExternalApi");
        var token = await tokenProvider.AcquireTokenAsync(["api://external-api/data.read"], ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
