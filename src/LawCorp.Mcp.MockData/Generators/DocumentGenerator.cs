using LawCorp.Mcp.Core.Models;
using LawCorp.Mcp.MockData.Partials;

namespace LawCorp.Mcp.MockData.Generators;

public class DocumentGenerator(Random rng)
{
    private static readonly DocumentType[] Types =
        [DocumentType.Motion, DocumentType.Brief, DocumentType.Contract, DocumentType.Correspondence, DocumentType.Evidence];

    private static readonly DocumentStatus[] Statuses =
        [DocumentStatus.Draft, DocumentStatus.UnderReview, DocumentStatus.Final, DocumentStatus.Filed];

    public Document Generate(Case @case, User author)
    {
        var type = Types[rng.Next(Types.Length)];
        var status = Statuses[rng.Next(Statuses.Length)];
        var created = @case.OpenDate.ToDateTime(TimeOnly.MinValue).AddDays(rng.Next(1, 180));

        return new Document
        {
            CaseId = @case.Id,
            Title = $"{type} — {@case.Title}",
            DocumentType = type,
            Status = status,
            Content = LoremLegal.Generate(rng, 3),
            AuthorId = author.Id,
            CreatedDate = created,
            ModifiedDate = created.AddDays(rng.Next(0, 30)),
            IsPrivileged = rng.NextDouble() < 0.2,
            IsRedacted = false
        };
    }

    public IEnumerable<Document> GenerateForCase(Case @case, IList<User> users, int count = 3)
    {
        for (var i = 0; i < count; i++)
        {
            var author = users[rng.Next(users.Count)];
            yield return Generate(@case, author);
        }
    }
}
