using System.Globalization;

using Ferret.Core.Documents;

namespace Ferret.Benchmarks.Corpus;

/// <summary>
/// Builds realistic enterprise tabular documents — the artifacts that motivated Excel support.
/// Cells are typed (text/number/boolean/date) and row counts vary deterministically to give the
/// Excel parser realistic, non-uniform workloads. Deterministic given the RNG.
/// </summary>
public static class EnterpriseArchetypes
{
    // Non-uniform but deterministic row counts (index into this from the seeded RNG).
    private static readonly int[] RowCounts = [75, 120, 200, 350, 900, 1800, 4500];

    private static readonly string[] Terms =
        ["login", "export", "index", "search", "auth", "cache", "report", "sync", "upload", "filter"];

    /// <summary>Builds one document per archetype, each carrying a single typed <see cref="CorpusTable"/>.</summary>
    /// <param name="rng">Seeded RNG for row content and row-count selection.</param>
    /// <returns>The archetype documents.</returns>
    public static IReadOnlyList<CorpusDocument> Build(Random rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        return
        [
            Doc(
                rng,
                "Requirement Traceability Matrix",
                ["ID", "Requirement", "Priority", "Status", "Coverage", "Owner"],
                i => [CorpusCell.Text($"REQ-{i:D3}"), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "High", "Medium", "Low")), CorpusCell.Text(Pick(rng, "Open", "Done")), CorpusCell.Number(rng.Next(0, 101)), CorpusCell.Text(Pick(rng, "Alice", "Bob", "Chandra"))]),
            Doc(
                rng,
                "Bug Report Export",
                ["Key", "Summary", "Severity", "Resolved", "Assignee", "Created"],
                i => [CorpusCell.Text($"BUG-{i:D3}"), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "Blocker", "Major", "Minor")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Pick(rng, "Alice", "Bob")), CorpusCell.Date(new DateOnly(2026, 1, 1 + (i % 27)))]),
            Doc(
                rng,
                "Sprint Backlog",
                ["Story", "Points", "Sprint", "State", "Epic"],
                i => [CorpusCell.Text($"STORY-{i:D3}"), CorpusCell.Number(Pick(rng, 1, 2, 3, 5, 8)), CorpusCell.Text(Pick(rng, "S-12", "S-13")), CorpusCell.Text(Pick(rng, "To Do", "Doing", "Done")), CorpusCell.Text(Pick(rng, "Search", "Indexing"))]),
            Doc(
                rng,
                "Risk Register",
                ["Risk", "Likelihood", "Impact", "Mitigation", "Owner"],
                i => [CorpusCell.Text($"RISK-{i:D3}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Low", "Medium", "High")), CorpusCell.Text(Pick(rng, "Low", "Medium", "High")), CorpusCell.Text(Phrase(rng)), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
            Doc(
                rng,
                "Test Execution Report",
                ["Test", "Passed", "Duration", "Build", "Tester"],
                i => [CorpusCell.Text($"TC-{i:D3}"), CorpusCell.Boolean(rng.Next(3) != 0), CorpusCell.Number(rng.Next(1, 900)), CorpusCell.Text($"build-{rng.Next(100, 999)}"), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
            Doc(
                rng,
                "Release Checklist",
                ["Item", "Owner", "Status", "Due", "Notes"],
                i => [CorpusCell.Text($"Item {i}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Alice", "Bob", "Chandra")), CorpusCell.Text(Pick(rng, "Pending", "Done", "Blocked")), CorpusCell.Date(new DateOnly(2026, 2, 1 + (i % 27))), CorpusCell.Text(Phrase(rng))]),
            Doc(
                rng,
                "Deployment Plan",
                ["Step", "Environment", "Owner", "Rollback", "Status"],
                i => [CorpusCell.Text($"Step {i}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "Dev", "Staging", "Prod")), CorpusCell.Text(Pick(rng, "Alice", "Bob")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Pick(rng, "Planned", "Complete"))]),
            Doc(
                rng,
                "Production Incident",
                ["Incident", "Severity", "Detected", "Resolved", "Root Cause"],
                i => [CorpusCell.Text($"INC-{i:D3}: {Phrase(rng)}"), CorpusCell.Text(Pick(rng, "SEV1", "SEV2", "SEV3")), CorpusCell.Date(new DateOnly(2026, 1, 15)), CorpusCell.Boolean(true), CorpusCell.Text(Phrase(rng))]),
            Doc(
                rng,
                "Security Findings",
                ["Finding", "CVSS", "Component", "Fixed", "Remediation"],
                i => [CorpusCell.Text($"SEC-{i:D3}: {Phrase(rng)}"), CorpusCell.Number(PickD(rng, 3.1, 5.4, 7.8, 9.1)), CorpusCell.Text(Pick(rng, "auth", "index", "api")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Text(Phrase(rng))]),
            Doc(
                rng,
                "Database Schema",
                ["Table", "Column", "Type", "Nullable", "Indexed"],
                i => [CorpusCell.Text(Pick(rng, "documents", "assets", "chunks")), CorpusCell.Text($"col_{i}"), CorpusCell.Text(Pick(rng, "text", "integer", "boolean", "timestamp")), CorpusCell.Boolean(rng.Next(2) == 0), CorpusCell.Boolean(rng.Next(3) == 0)]),
            Doc(
                rng,
                "API Endpoint Inventory",
                ["Path", "Method", "Auth", "Deprecated", "Owner"],
                i => [CorpusCell.Text($"/api/v1/resource{i}"), CorpusCell.Text(Pick(rng, "GET", "POST", "PUT", "DELETE")), CorpusCell.Boolean(rng.Next(4) != 0), CorpusCell.Boolean(rng.Next(5) == 0), CorpusCell.Text(Pick(rng, "Alice", "Bob"))]),
        ];
    }

    private static CorpusDocument Doc(
        Random rng, string title, string[] headers, Func<int, IReadOnlyList<CorpusCell>> row)
    {
        var rows = RowCounts[rng.Next(RowCounts.Length)];
        var data = new List<IReadOnlyList<CorpusCell>>(rows);
        for (var i = 1; i <= rows; i++)
        {
            data.Add(row(i));
        }

        var metadata = Metadata(rng, title, "Data");
        return new CorpusDocument(title, metadata, [], [new CorpusTable(headers, data)]);
    }

    private static Dictionary<string, string> Metadata(Random rng, string subject, string category) =>
        new(StringComparer.Ordinal)
        {
            [DocumentMetadata.Author] = Pick(rng, "Alice", "Bob", "Chandra"),
            [DocumentMetadata.Subject] = subject,
            [DocumentMetadata.Category] = category,
        };

    private static string Phrase(Random rng) =>
        string.Create(CultureInfo.InvariantCulture, $"{Terms[rng.Next(Terms.Length)]} {Terms[rng.Next(Terms.Length)]}");

    private static string Pick(Random rng, params string[] options) => options[rng.Next(options.Length)];

    private static int Pick(Random rng, params int[] options) => options[rng.Next(options.Length)];

    private static double PickD(Random rng, params double[] options) => options[rng.Next(options.Length)];
}
