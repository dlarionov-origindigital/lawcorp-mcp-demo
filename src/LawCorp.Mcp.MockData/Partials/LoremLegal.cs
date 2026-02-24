namespace LawCorp.Mcp.MockData.Partials;

/// <summary>Legal-flavored lorem ipsum paragraphs for document content generation.</summary>
public static class LoremLegal
{
    public static readonly string[] Paragraphs =
    [
        "Whereas the parties hereto desire to set forth the terms and conditions under which the proposed transaction shall be consummated, and in consideration of the mutual covenants and agreements contained herein, the parties agree as follows.",
        "The representations and warranties set forth herein shall be true and correct in all material respects as of the date of this Agreement and as of the Closing Date, as though made on and as of such date.",
        "Subject to the satisfaction or waiver of the conditions set forth in Article VII, the closing of the transactions contemplated by this Agreement shall take place at the offices of counsel at 10:00 a.m. on the third Business Day after all conditions have been satisfied.",
        "Each party shall use its reasonable best efforts to take, or cause to be taken, all actions and to do, or cause to be done, all things necessary, proper or advisable to consummate and make effective the transactions contemplated hereby.",
        "The Company shall conduct its business in the ordinary course consistent with past practice and shall use commercially reasonable efforts to preserve intact its business organization and relationships with third parties.",
        "In the event of any breach of the representations, warranties, or covenants contained herein, the non-breaching party shall be entitled to seek all remedies available at law or in equity, subject to the limitations set forth in Article IX.",
        "The aggregate liability of the Indemnifying Party for indemnification obligations under this Agreement shall not exceed the Indemnification Cap, except in the case of Fundamental Representations or fraud.",
        "Any dispute arising out of or relating to this Agreement, or the breach, termination, or validity thereof, shall be finally resolved by binding arbitration under the Commercial Arbitration Rules of the American Arbitration Association."
    ];

    public static string Generate(Random rng, int paragraphCount = 3)
    {
        var selected = Enumerable.Range(0, paragraphCount)
            .Select(_ => Paragraphs[rng.Next(Paragraphs.Length)]);
        return string.Join("\n\n", selected);
    }
}
