using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Infrastructure.Pdf;

public class PurchaseOrderPdfService : IPurchaseOrderPdfService
{
    private const double Margin = 36;
    private const double FooterHeight = 24;
    private static readonly object FontResolverLock = new();

    public byte[] Generate(PurchaseOrderDto purchaseOrder)
    {
        EnsureFontResolver();
        EnsureOfficialStatus(purchaseOrder.Status);

        using var document = new PdfDocument();
        document.Info.Title = $"Ordem de Compra {purchaseOrder.Number}";
        var regular = new XFont("Arial", 9, XFontStyleEx.Regular);
        var small = new XFont("Arial", 8, XFontStyleEx.Regular);
        var bold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var section = new XFont("Arial", 10, XFontStyleEx.Bold);
        var title = new XFont("Arial", 19, XFontStyleEx.Bold);
        var accent = XColor.FromArgb(31, 78, 121);
        var light = XColor.FromArgb(235, 241, 247);
        var border = XColor.FromArgb(170, 180, 190);

        PdfPage page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        var y = Margin;

        void Footer()
        {
            var footerY = page.Height.Point - Margin + 4;
            gfx.DrawLine(new XPen(border, 0.6), Margin, footerY - 6, page.Width.Point - Margin, footerY - 6);
            gfx.DrawString($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm} · SGE-ERP", small, XBrushes.Gray,
                new XRect(Margin, footerY, page.Width.Point - Margin * 2, FooterHeight), XStringFormats.TopLeft);
        }

        void NewPage()
        {
            Footer();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            y = Margin;
        }

        void EnsureSpace(double height)
        {
            if (y + height <= page.Height.Point - Margin - FooterHeight) return;
            NewPage();
        }

        void Text(string value, XFont font, double x, double width, double height = 16)
        {
            EnsureSpace(height);
            gfx.DrawString(value, font, XBrushes.Black, new XRect(x, y, width, height), XStringFormats.TopLeft);
        }

        void SectionTitle(string label)
        {
            EnsureSpace(28);
            y += 10;
            gfx.DrawRectangle(new XSolidBrush(light), Margin, y, page.Width.Point - Margin * 2, 22);
            gfx.DrawString(label, section, XBrushes.DarkSlateGray,
                new XRect(Margin + 8, y + 4, page.Width.Point - Margin * 2 - 16, 16), XStringFormats.TopLeft);
            y += 28;
        }

        void KeyValue(string label, string? value, double x, double width)
        {
            Text(label, small, x, width, 12);
            y += 11;
            Text(Blank(value), bold, x, width, 16);
        }

        gfx.DrawRectangle(new XSolidBrush(accent), Margin, y, page.Width.Point - Margin * 2, 62);
        gfx.DrawString("SGE-ERP", new XFont("Arial", 11, XFontStyleEx.Bold), XBrushes.White,
            new XRect(Margin + 12, y + 9, 100, 18), XStringFormats.TopLeft);
        gfx.DrawString("Estrutural", regular, XBrushes.White,
            new XRect(Margin + 12, y + 31, 160, 16), XStringFormats.TopLeft);
        gfx.DrawString("ORDEM DE COMPRA", title, XBrushes.White,
            new XRect(Margin + 190, y + 9, page.Width.Point - Margin * 2 - 202, 26), XStringFormats.TopLeft);
        gfx.DrawString($"Nº {purchaseOrder.Number}", bold, XBrushes.White,
            new XRect(Margin + 190, y + 37, 200, 16), XStringFormats.TopLeft);
        y += 76;

        KeyValue("Emissão", FormatDate(purchaseOrder.IssueDate), Margin, 150);
        KeyValue("Status", purchaseOrder.Status.ToString(), Margin + 175, 150);
        KeyValue("Solicitado por", purchaseOrder.RequestedByUserName, Margin + 350, 180);
        y += 14;

        SectionTitle("Fornecedor");
        KeyValue("Razão / nome", purchaseOrder.SupplierName, Margin, 250);
        KeyValue("Documento", FormatDocument(purchaseOrder.SupplierDocument), Margin + 275, 170);
        KeyValue("Contato", string.Join(" · ", new[] { purchaseOrder.SupplierEmail, purchaseOrder.SupplierPhone }.Where(x => !string.IsNullOrWhiteSpace(x))), Margin + 460, page.Width.Point - Margin - (Margin + 460));
        y += 10;

        SectionTitle("Obra e solicitação de origem");
        KeyValue("Obra", purchaseOrder.WorkName, Margin, 250);
        KeyValue("Solicitação", purchaseOrder.PurchaseRequestNumber, Margin + 275, 170);
        KeyValue("Cotação", purchaseOrder.QuotationNumber, Margin + 460, page.Width.Point - Margin - (Margin + 460));
        y += 4;
        KeyValue("Descrição da solicitação", purchaseOrder.PurchaseRequestDescription, Margin, page.Width.Point - Margin * 2);
        y += 10;

        SectionTitle("Itens");
        var columns = new[] { 210d, 62d, 54d, 82d, 82d };
        var headers = new[] { "Item / material", "Qtd.", "Un.", "Unitário", "Total" };
        DrawRow(headers, columns, bold, new XSolidBrush(light), border, 24);
        foreach (var item in purchaseOrder.Items)
        {
            EnsureSpace(30);
            DrawRow(new[] { Truncate(item.ItemDescription), item.QuantityOrdered.ToString("0.####"), Blank(item.Unit), FormatMoney(item.UnitPrice), FormatMoney(item.TotalValue) }, columns, regular, XBrushes.White, border, 28);
            if (!string.IsNullOrWhiteSpace(item.Observation))
            {
                Text($"Obs.: {Truncate(item.Observation, 100)}", small, Margin + 8, page.Width.Point - Margin * 2 - 16, 14);
                y += 2;
            }
        }

        EnsureSpace(42);
        gfx.DrawRectangle(new XSolidBrush(light), Margin, y, page.Width.Point - Margin * 2, 34);
        gfx.DrawString("TOTAL DA ORDEM", bold, XBrushes.DarkSlateGray, new XRect(Margin + 10, y + 9, 220, 16), XStringFormats.TopLeft);
        gfx.DrawString(FormatMoney(purchaseOrder.TotalValue), new XFont("Arial", 13, XFontStyleEx.Bold), XBrushes.Black,
            new XRect(page.Width.Point - Margin - 170, y + 7, 160, 20), XStringFormats.TopRight);
        y += 46;

        SectionTitle("Condições comerciais");
        KeyValue("Prazo de entrega", FormatDelivery(purchaseOrder.DeliveryDays), Margin, 150);
        KeyValue("Condição de pagamento", purchaseOrder.PaymentCondition, Margin + 175, 220);
        KeyValue("Parcelas", FormatInstallments(purchaseOrder.InstallmentCount), Margin + 420, 100);

        Footer();
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();

        void DrawRow(string[] values, double[] widths, XFont font, XBrush background, XColor lineColor, double height)
        {
            var x = Margin;
            gfx.DrawRectangle(background, Margin, y, widths.Sum(), height);
            for (var i = 0; i < values.Length; i++)
            {
                gfx.DrawRectangle(new XPen(lineColor, 0.5), x, y, widths[i], height);
                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(x + 6, y + 6, widths[i] - 12, height - 8), XStringFormats.TopLeft);
                x += widths[i];
            }
            y += height;
        }
    }

    private static void EnsureOfficialStatus(PurchaseOrderStatus status)
    {
        if (status == PurchaseOrderStatus.Open) throw new InvalidOperationException("O PDF oficial da ordem de compra so pode ser gerado apos aprovacao.");
        if (status == PurchaseOrderStatus.WaitingSecondApproval) throw new InvalidOperationException("A ordem de compra ainda aguarda a segunda aprovacao.");
        if (status == PurchaseOrderStatus.Cancelled) throw new InvalidOperationException("Nao e possivel gerar PDF oficial de uma ordem de compra cancelada.");
    }

    private static string FormatDate(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    private static string FormatMoney(decimal value) => value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    private static string FormatDelivery(int? days) => days.HasValue ? $"{days.Value} dia(s)" : "-";
    private static string FormatInstallments(int? count) => count.HasValue ? $"{count.Value}x" : "-";
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
    private static string Truncate(string? value, int length = 42) => string.IsNullOrWhiteSpace(value) ? "-" : value.Length <= length ? value : value[..(length - 1)] + "…";
    private static string FormatDocument(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 14 ? $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}" : Blank(value);
    }
    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver != null) return;
        lock (FontResolverLock) GlobalFontSettings.FontResolver ??= new WindowsFontResolver();
    }
}
