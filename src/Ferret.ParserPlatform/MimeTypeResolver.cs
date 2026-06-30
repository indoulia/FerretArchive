using Ferret.Core.Documents;

namespace Ferret.ParserPlatform;

/// <summary>
/// Resolves MIME type metadata from a file name by extension lookup.
/// Uses a static dictionary — no I/O, no external libraries, deterministic.
/// Unknown non-binary extensions default to text/plain with Confidence=0.5.
/// </summary>
public sealed class MimeTypeResolver : IMimeTypeResolver
{
    private static readonly Dictionary<string, MediaTypeInfo> Map =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".txt"] = Text("text/plain", DocumentKind.Unknown),
            [".md"] = Text("text/markdown", DocumentKind.Prose),
            [".markdown"] = Text("text/markdown", DocumentKind.Prose),
            [".json"] = Text("application/json", DocumentKind.Data),
            [".jsonc"] = Text("application/json", DocumentKind.Data),
            [".csv"] = Text("text/csv", DocumentKind.Data),
            [".tsv"] = Text("text/tab-separated-values", DocumentKind.Data),
            [".xml"] = Text("text/xml", DocumentKind.Data),
            [".yaml"] = Text("text/yaml", DocumentKind.Config),
            [".yml"] = Text("text/yaml", DocumentKind.Config),
            [".toml"] = Text("application/toml", DocumentKind.Config),
            [".proto"] = Text("text/x-protobuf", DocumentKind.Config),
            [".tf"] = Text("text/x-terraform", DocumentKind.Config),
            [".graphql"] = Text("text/x-graphql", DocumentKind.Config),
            [".html"] = Text("text/html", DocumentKind.Prose),
            [".htm"] = Text("text/html", DocumentKind.Prose),
            [".css"] = Text("text/css", DocumentKind.Code),
            [".js"] = Text("text/javascript", DocumentKind.Code),
            [".jsx"] = Text("text/javascript", DocumentKind.Code),
            [".ts"] = Text("text/typescript", DocumentKind.Code),
            [".tsx"] = Text("text/typescript", DocumentKind.Code),
            [".vue"] = Text("text/x-vue", DocumentKind.Code),
            [".razor"] = Text("text/x-razor", DocumentKind.Code),
            [".cshtml"] = Text("text/x-razor", DocumentKind.Code),
            [".cs"] = Text("text/x-csharp", DocumentKind.Code),
            [".java"] = Text("text/x-java", DocumentKind.Code),
            [".kt"] = Text("text/x-kotlin", DocumentKind.Code),
            [".py"] = Text("text/x-python", DocumentKind.Code),
            [".rb"] = Text("text/x-ruby", DocumentKind.Code),
            [".swift"] = Text("text/x-swift", DocumentKind.Code),
            [".go"] = Text("text/x-go", DocumentKind.Code),
            [".rs"] = Text("text/x-rust", DocumentKind.Code),
            [".c"] = Text("text/x-c", DocumentKind.Code),
            [".h"] = Text("text/x-c", DocumentKind.Code),
            [".cpp"] = Text("text/x-c++", DocumentKind.Code),
            [".hpp"] = Text("text/x-c++", DocumentKind.Code),
            [".sh"] = Text("text/x-sh", DocumentKind.Code),
            [".bash"] = Text("text/x-sh", DocumentKind.Code),
            [".ps1"] = Text("text/x-powershell", DocumentKind.Code),
            [".sql"] = Text("text/x-sql", DocumentKind.Code),
            [".dll"] = Binary(),
            [".exe"] = Binary(),
            [".pdb"] = Binary(),
            [".obj"] = Binary(),
            [".bin"] = Binary(),
            [".zip"] = Binary(),
            [".gz"] = Binary(),
            [".tar"] = Binary(),
            [".7z"] = Binary(),
            [".rar"] = Binary(),
            [".png"] = Binary(),
            [".jpg"] = Binary(),
            [".jpeg"] = Binary(),
            [".gif"] = Binary(),
            [".bmp"] = Binary(),
            [".ico"] = Binary(),
            [".svg"] = Binary(),
            [".pdf"] = Binary(),
            [".docx"] = Binary(),
            [".xlsx"] = Binary(),
            [".pptx"] = Binary(),
            [".mp3"] = Binary(),
            [".mp4"] = Binary(),
            [".avi"] = Binary(),
            [".mov"] = Binary(),
            [".ttf"] = Binary(),
            [".woff"] = Binary(),
            [".woff2"] = Binary(),
            [".eot"] = Binary(),
        };

    private static readonly MediaTypeInfo UnknownText = new()
    {
        MediaType = "text/plain",
        IsText = true,
        IsBinary = false,
        Confidence = 0.5,
    };

    /// <inheritdoc/>
    public MediaTypeInfo Resolve(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (fileName.Length == 0)
        {
            return UnknownText;
        }

        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(ext) || ext == ".")
        {
            return UnknownText;
        }

        return Map.TryGetValue(ext, out var info) ? info : UnknownText;
    }

    private static MediaTypeInfo Text(string mediaType, DocumentKind kind) => new()
    {
        MediaType = mediaType,
        IsText = true,
        IsBinary = false,
        SuggestedKind = kind,
        Confidence = 1.0,
    };

    private static MediaTypeInfo Binary() => new()
    {
        MediaType = "application/octet-stream",
        IsText = false,
        IsBinary = true,
        Confidence = 1.0,
    };
}
