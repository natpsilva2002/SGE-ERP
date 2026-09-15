using PdfSharp.Fonts;

namespace SGE.Infrastructure.Pdf;

internal sealed class WindowsFontResolver : IFontResolver
{
    private const string RegularFace = "Arial#Regular";
    private const string BoldFace = "Arial#Bold";

    public byte[] GetFont(string faceName)
    {
        var fileName = faceName == BoldFace
            ? "arialbd.ttf"
            : "arial.ttf";

        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts),
            fileName);

        if (!File.Exists(path))
            throw new InvalidOperationException(
                $"A fonte necessaria para gerar o PDF nao foi encontrada: {fileName}.");

        return File.ReadAllBytes(path);
    }

    public FontResolverInfo ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic)
    {
        return new FontResolverInfo(isBold ? BoldFace : RegularFace);
    }
}
