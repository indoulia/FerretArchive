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
            [".pdf"] = ParseableBinary("application/pdf", DocumentKind.Prose),
            [".docx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.wordprocessingml.document", DocumentKind.Prose),
            [".xlsx"] = ParseableBinary("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", DocumentKind.Data),
            [".pptx"] = Binary(),
            [".scss"] = Text("text/x-scss", DocumentKind.Code),
            [".less"] = Text("text/x-less", DocumentKind.Code),
            [".php"] = Text("text/x-php", DocumentKind.Code),
            [".scala"] = Text("text/x-scala", DocumentKind.Code),
            [".clj"] = Text("text/x-clojure", DocumentKind.Code),
            [".cljs"] = Text("text/x-clojure", DocumentKind.Code),
            [".dart"] = Text("text/x-dart", DocumentKind.Code),
            [".lua"] = Text("text/x-lua", DocumentKind.Code),
            [".r"] = Text("text/x-r", DocumentKind.Code),
            [".pl"] = Text("text/x-perl", DocumentKind.Code),
            [".groovy"] = Text("text/x-groovy", DocumentKind.Code),
            [".gradle"] = Text("text/x-groovy", DocumentKind.Config),
            [".bat"] = Text("text/x-bat", DocumentKind.Code),
            [".cmd"] = Text("text/x-bat", DocumentKind.Code),
            [".psm1"] = Text("text/x-powershell", DocumentKind.Code),
            [".psd1"] = Text("text/x-powershell", DocumentKind.Config),
            [".vb"] = Text("text/x-vb", DocumentKind.Code),
            [".fs"] = Text("text/x-fsharp", DocumentKind.Code),
            [".fsx"] = Text("text/x-fsharp", DocumentKind.Code),
            [".ini"] = Text("text/x-ini", DocumentKind.Config),
            [".cfg"] = Text("text/x-ini", DocumentKind.Config),
            [".conf"] = Text("text/x-ini", DocumentKind.Config),
            [".env"] = Text("text/x-dotenv", DocumentKind.Config),
            [".properties"] = Text("text/x-properties", DocumentKind.Config),
            [".csproj"] = Text("text/xml", DocumentKind.Config),
            [".vbproj"] = Text("text/xml", DocumentKind.Config),
            [".fsproj"] = Text("text/xml", DocumentKind.Config),
            [".props"] = Text("text/xml", DocumentKind.Config),
            [".targets"] = Text("text/xml", DocumentKind.Config),
            [".resx"] = Text("text/xml", DocumentKind.Data),
            [".xaml"] = Text("text/xml", DocumentKind.Code),
            [".rst"] = Text("text/x-rst", DocumentKind.Prose),
            [".adoc"] = Text("text/x-asciidoc", DocumentKind.Prose),
            [".tex"] = Text("text/x-tex", DocumentKind.Prose),
            [".gitignore"] = Text("text/plain", DocumentKind.Config),
            [".editorconfig"] = Text("text/plain", DocumentKind.Config),
            [".so"] = Binary(),
            [".dylib"] = Binary(),
            [".a"] = Binary(),
            [".o"] = Binary(),
            [".lib"] = Binary(),
            [".class"] = Binary(),
            [".pyc"] = Binary(),
            [".pyo"] = Binary(),
            [".wasm"] = Binary(),
            [".node"] = Binary(),
            [".nupkg"] = Binary(),
            [".snk"] = Binary(),
            [".pfx"] = Binary(),
            [".jar"] = Binary(),
            [".war"] = Binary(),
            [".ear"] = Binary(),
            [".db"] = Binary(),
            [".sqlite"] = Binary(),
            [".parquet"] = Binary(),
            [".dat"] = Binary(),
            [".keystore"] = Binary(),
            [".psd"] = Binary(),
            [".ai"] = Binary(),
            [".otf"] = Binary(),
            [".mp3"] = Binary(),
            [".mp4"] = Binary(),
            [".avi"] = Binary(),
            [".mov"] = Binary(),
            [".ttf"] = Binary(),
            [".woff"] = Binary(),
            [".woff2"] = Binary(),
            [".eot"] = Binary(),
        };

    private static readonly Dictionary<string, MediaTypeInfo> FileNameMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dockerfile"] = Text("text/x-dockerfile", DocumentKind.Config),
            ["Makefile"] = Text("text/x-makefile", DocumentKind.Config),
        };

    private static readonly MediaTypeInfo UnknownText = new()
    {
        MediaType = "text/plain",
        Category = MediaCategory.Text,
        Confidence = 0.5,
    };

    /// <summary>Gets the number of mapped extensions that resolve to text or parseable-binary content.</summary>
    public static int KnownExtensionCount => Map.Count(kv => kv.Value.Category != MediaCategory.BinaryOpaque);

    /// <inheritdoc/>
    public MediaTypeInfo Resolve(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        if (fileName.Length == 0)
        {
            return UnknownText;
        }

        var ext = Path.GetExtension(fileName);
        if (!string.IsNullOrEmpty(ext) && ext != "." && Map.TryGetValue(ext, out var byExtension))
        {
            return byExtension;
        }

        var name = Path.GetFileName(fileName);
        if (name.Length > 0 && FileNameMap.TryGetValue(name, out var byName))
        {
            return byName;
        }

        return UnknownText;
    }

    private static MediaTypeInfo Text(string mediaType, DocumentKind kind) => new()
    {
        MediaType = mediaType,
        Category = MediaCategory.Text,
        SuggestedKind = kind,
        Confidence = 1.0,
    };

    private static MediaTypeInfo Binary() => new()
    {
        MediaType = "application/octet-stream",
        Category = MediaCategory.BinaryOpaque,
        Confidence = 1.0,
    };

    private static MediaTypeInfo ParseableBinary(string mediaType, DocumentKind kind) => new()
    {
        MediaType = mediaType,
        Category = MediaCategory.BinaryParseable,
        SuggestedKind = kind,
        Confidence = 1.0,
    };
}
