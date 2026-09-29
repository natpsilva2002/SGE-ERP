using PdfSharp.Fonts;

namespace SGE.Infrastructure.Pdf;

internal sealed class CrossPlatformFontResolver : IFontResolver
{
    private const string RegularFace = "Arial#Regular";
    private const string BoldFace = "Arial#Bold";

    public byte[] GetFont(string faceName)
    {
        var isBold = faceName == BoldFace;
        var candidates = OperatingSystem.IsWindows()
            ? new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), isBold ? "arialbd.ttf" : "arial.ttf")
            }
            : isBold
                ? new[]
                {
                    "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
                }
                : new[]
                {
                    "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
                };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path == null)
            throw new InvalidOperationException(
                "A fonte Arial/Liberation Sans necessaria para gerar o PDF nao foi encontrada.");

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
