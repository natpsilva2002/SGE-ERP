using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Infrastructure.Pdf;

public class PurchaseOrderPdfService : IPurchaseOrderPdfService
{
    private const double Margin = 42;
    private const double FooterHeight = 26;
    private static readonly object FontResolverLock = new();

    public byte[] Generate(PurchaseOrderDto purchaseOrder)
    {
        EnsureFontResolver();
        EnsureOfficialStatus(purchaseOrder.Status);

        using var document = new PdfDocument();
        document.Info.Title = $"Ordem de Compra {purchaseOrder.Number}";

        var regular = new XFont("Arial", 9.5, XFontStyleEx.Regular);
        var small = new XFont("Arial", 7.5, XFontStyleEx.Regular);
        var valueFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var bold = new XFont("Arial", 9.5, XFontStyleEx.Bold);
        var section = new XFont("Arial", 10, XFontStyleEx.Bold);
        var title = new XFont("Arial", 18, XFontStyleEx.Bold);
        var orderNumber = new XFont("Arial", 11, XFontStyleEx.Bold);
        var totalFont = new XFont("Arial", 15, XFontStyleEx.Bold);
        var logo = new XFont("Arial", 12, XFontStyleEx.Bold);

        var accent = XColor.FromArgb(28, 91, 184);
        var dark = XColor.FromArgb(25, 43, 68);
        var muted = XColor.FromArgb(91, 108, 128);
        var headerFill = XColor.FromArgb(232, 239, 248);
        var border = XColor.FromArgb(205, 214, 224);
        var white = XBrushes.White;

        PdfPage page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        var gfx = XGraphics.FromPdfPage(page);
        var y = Margin;
        var contentWidth = page.Width.Point - Margin * 2;
        var contentBottom = page.Height.Point - Margin - FooterHeight;

        void NewPage()
        {
            gfx.Dispose();
            page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            gfx = XGraphics.FromPdfPage(page);
            y = Margin;
            contentWidth = page.Width.Point - Margin * 2;
            contentBottom = page.Height.Point - Margin - FooterHeight;
        }

        void EnsureSpace(double height)
        {
            if (y + height <= contentBottom) return;
            NewPage();
        }

        List<string> WrapText(string value, XFont font, double width)
        {
            var words = Blank(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var current = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                if (gfx.MeasureString(candidate, font).Width <= width || string.IsNullOrEmpty(current))
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = word;
            }

            if (!string.IsNullOrEmpty(current)) lines.Add(current);
            return lines.Count == 0 ? new List<string> { "-" } : lines;
        }

        void DrawWrappedText(string value, XFont font, XBrush brush, double x, double top, double width, double lineHeight)
        {
            var lines = WrapText(value, font, width);
            for (var index = 0; index < lines.Count; index++)
            {
                gfx.DrawString(lines[index], font, brush,
                    new XRect(x, top + index * lineHeight, width, lineHeight), XStringFormats.TopLeft);
            }
        }

        void SectionTitle(string label)
        {
            EnsureSpace(38);
            gfx.DrawString(label.ToUpperInvariant(), section, new XSolidBrush(accent),
                new XRect(Margin, y, contentWidth, 15), XStringFormats.TopLeft);
            gfx.DrawLine(new XPen(border, 0.7), Margin, y + 22, Margin + contentWidth, y + 22);
            y += 31;
        }

        void InfoGrid(params (string Label, string? Value, double Width)[] fields)
        {
            var valueLines = fields
                .Select(field => WrapText(field.Value ?? string.Empty, valueFont, field.Width - 18))
                .ToArray();
            var rowHeight = Math.Max(42, valueLines.Max(lines => 25 + lines.Count * 13));

            EnsureSpace(rowHeight + 12);
            var x = Margin;
            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                gfx.DrawRectangle(white, x, y, field.Width, rowHeight);
                gfx.DrawRectangle(new XPen(border, 0.6), x, y, field.Width, rowHeight);
                gfx.DrawString(field.Label, small, new XSolidBrush(muted),
                    new XRect(x + 9, y + 7, field.Width - 18, 11), XStringFormats.TopLeft);
                DrawWrappedText(Blank(field.Value), valueFont, new XSolidBrush(dark),
                    x + 9, y + 21, field.Width - 18, 13);
                x += field.Width;
            }

            y += rowHeight + 12;
        }

        void DrawTableCells(string[] values, double[] widths, XFont font, XBrush background, XBrush textBrush, double height, bool alignRightValues = false)
        {
            var x = Margin;
            gfx.DrawRectangle(background, Margin, y, widths.Sum(), height);
            for (var index = 0; index < values.Length; index++)
            {
                var cellRect = new XRect(x, y, widths[index], height);
                gfx.DrawRectangle(new XPen(border, 0.55), cellRect);
                var format = alignRightValues && index >= 3 ? XStringFormats.CenterRight : XStringFormats.CenterLeft;
                gfx.DrawString(values[index], font, textBrush,
                    new XRect(x + 7, y + 5, widths[index] - 14, height - 10), format);
                x += widths[index];
            }

            y += height;
        }

        void DrawTableHeader()
        {
            var columns = new[] { 190d, 45d, 42d, 112d, 122d };
            var headers = new[] { "ITEM / MATERIAL", "QTD.", "UN.", "VALOR UNITÁRIO", "TOTAL" };
            DrawTableCells(headers, columns, bold, new XSolidBrush(accent), white, 27, true);
        }

        void DrawItemRow(PurchaseOrderItemDto item)
        {
            var columns = new[] { 190d, 45d, 42d, 112d, 122d };
            var itemLines = WrapText(item.ItemDescription, regular, columns[0] - 16);
            var observationLines = string.IsNullOrWhiteSpace(item.Observation)
                ? new List<string>()
                : WrapText($"Observação: {item.Observation}", small, columns[0] - 16);
            var rowHeight = Math.Max(31, 10 + itemLines.Count * 13 + observationLines.Count * 11);

            if (y + rowHeight > contentBottom)
            {
                NewPage();
                DrawTableHeader();
            }

            var x = Margin;
            var values = new[]
            {
                string.Empty,
                item.QuantityOrdered.ToString("0.####"),
                Blank(item.Unit),
                FormatMoney(item.UnitPrice),
                FormatMoney(item.TotalValue)
            };

            gfx.DrawRectangle(white, Margin, y, columns.Sum(), rowHeight);
            for (var index = 0; index < columns.Length; index++)
            {
                gfx.DrawRectangle(new XPen(border, 0.55), x, y, columns[index], rowHeight);
                if (index > 0)
                {
                    gfx.DrawString(values[index], regular, new XSolidBrush(dark),
                        new XRect(x + 7, y + 8, columns[index] - 14, rowHeight - 16),
                        index >= 3 ? XStringFormats.CenterRight : XStringFormats.CenterLeft);
                }

                x += columns[index];
            }

            DrawWrappedText(Blank(item.ItemDescription), regular, new XSolidBrush(dark),
                Margin + 8, y + 7, columns[0] - 16, 13);
            if (observationLines.Count > 0)
            {
                DrawWrappedText($"Observação: {item.Observation}", small, new XSolidBrush(muted),
                    Margin + 8, y + 9 + itemLines.Count * 13, columns[0] - 16, 11);
            }

            y += rowHeight;
        }

        gfx.DrawRectangle(new XSolidBrush(accent), Margin, y, contentWidth, 5);
        y += 19;
        gfx.DrawString("SGE-ERP", logo, new XSolidBrush(dark),
            new XRect(Margin, y, 150, 18), XStringFormats.TopLeft);
        gfx.DrawString("Estrutural", regular, new XSolidBrush(muted),
            new XRect(Margin, y + 19, 150, 15), XStringFormats.TopLeft);
        gfx.DrawString("ORDEM DE COMPRA", title, new XSolidBrush(dark),
            new XRect(Margin + 180, y - 1, contentWidth - 180, 24), XStringFormats.TopRight);
        gfx.DrawString($"Nº {purchaseOrder.Number}", orderNumber, new XSolidBrush(accent),
            new XRect(Margin + 180, y + 26, contentWidth - 180, 17), XStringFormats.TopRight);
        y += 60;

        InfoGrid(
            ("EMISSÃO", FormatDate(purchaseOrder.IssueDate), 150),
            ("STATUS", StatusLabel(purchaseOrder.Status), 150),
            ("SOLICITADO POR", purchaseOrder.RequestedByUserName, contentWidth - 300));

        SectionTitle("Fornecedor");
        InfoGrid(
            ("RAZÃO SOCIAL", purchaseOrder.SupplierName, 220),
            ("CNPJ", FormatDocument(purchaseOrder.SupplierDocument), 130),
            ("CONTATO", string.Join(" · ", new[] { purchaseOrder.SupplierEmail, purchaseOrder.SupplierPhone }.Where(x => !string.IsNullOrWhiteSpace(x))), contentWidth - 350));

        SectionTitle("Dados da compra");
        InfoGrid(
            ("OBRA", purchaseOrder.WorkName, 210),
            ("SOLICITAÇÃO", purchaseOrder.PurchaseRequestNumber, 140),
            ("COTAÇÃO", purchaseOrder.QuotationNumber, contentWidth - 350));
        InfoGrid(("DESCRIÇÃO", purchaseOrder.PurchaseRequestDescription, contentWidth));

        SectionTitle("Itens");
        DrawTableHeader();
        foreach (var item in purchaseOrder.Items)
        {
            DrawItemRow(item);
        }

        EnsureSpace(96);
        var itemsSubtotal = purchaseOrder.ItemsSubtotal > 0
            ? purchaseOrder.ItemsSubtotal
            : purchaseOrder.Items.Sum(item => item.TotalValue);
        gfx.DrawString("Subtotal dos itens", regular, new XSolidBrush(muted),
            new XRect(Margin + 12, y + 2, contentWidth - 210, 15), XStringFormats.TopLeft);
        gfx.DrawString(FormatMoney(itemsSubtotal), regular, new XSolidBrush(dark),
            new XRect(page.Width.Point - Margin - 190, y + 2, 176, 15), XStringFormats.TopRight);
        gfx.DrawString("Frete", regular, new XSolidBrush(muted),
            new XRect(Margin + 12, y + 21, contentWidth - 210, 15), XStringFormats.TopLeft);
        gfx.DrawString(FormatMoney(purchaseOrder.FreightValue), regular, new XSolidBrush(dark),
            new XRect(page.Width.Point - Margin - 190, y + 21, 176, 15), XStringFormats.TopRight);
        y += 42;
        gfx.DrawRectangle(new XSolidBrush(headerFill), Margin, y, contentWidth, 48);
        gfx.DrawRectangle(new XPen(accent, 1.2), Margin, y, contentWidth, 48);
        gfx.DrawString("TOTAL DA ORDEM", bold, new XSolidBrush(muted),
            new XRect(Margin + 14, y + 9, contentWidth - 210, 15), XStringFormats.TopLeft);
        gfx.DrawString(FormatMoney(purchaseOrder.TotalValue), totalFont, new XSolidBrush(dark),
            new XRect(page.Width.Point - Margin - 190, y + 8, 176, 24), XStringFormats.TopRight);
        y += 62;

        SectionTitle("Condições comerciais");
        InfoGrid(
            ("PRAZO DE ENTREGA", FormatDelivery(purchaseOrder.DeliveryDays), 160),
            ("CONDIÇÃO DE PAGAMENTO", purchaseOrder.PaymentCondition, 220),
            ("PARCELAS", FormatInstallments(purchaseOrder.InstallmentCount), contentWidth - 380));

        gfx.Dispose();
        var generatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        for (var index = 0; index < document.Pages.Count; index++)
        {
            var footerPage = document.Pages[index];
            using var footerGfx = XGraphics.FromPdfPage(footerPage, XGraphicsPdfPageOptions.Append);
            var footerY = footerPage.Height.Point - Margin - 12;
            footerGfx.DrawLine(new XPen(border, 0.6), Margin, footerY - 6, footerPage.Width.Point - Margin, footerY - 6);
            footerGfx.DrawString("SGE-ERP · Estrutural", small, new XSolidBrush(muted),
                new XRect(Margin, footerY, 200, 12), XStringFormats.TopLeft);
            footerGfx.DrawString($"Gerado em {generatedAt}", small, new XSolidBrush(muted),
                new XRect(Margin + 180, footerY, 150, 12), XStringFormats.TopCenter);
            footerGfx.DrawString($"Página {index + 1} de {document.Pages.Count}", small, new XSolidBrush(muted),
                new XRect(footerPage.Width.Point - Margin - 150, footerY, 150, 12), XStringFormats.TopRight);
        }

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static void EnsureOfficialStatus(PurchaseOrderStatus status)
    {
        if (status == PurchaseOrderStatus.Open) throw new InvalidOperationException("O PDF oficial da ordem de compra so pode ser gerado apos aprovacao.");
        if (status == PurchaseOrderStatus.WaitingSecondApproval) throw new InvalidOperationException("A ordem de compra ainda aguarda a segunda aprovacao.");
        if (status == PurchaseOrderStatus.Cancelled) throw new InvalidOperationException("Nao e possivel gerar PDF oficial de uma ordem de compra cancelada.");
    }

    private static string FormatDate(DateTime value) => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    private static string FormatMoney(decimal value) => value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    private static string FormatDelivery(int? days) => !days.HasValue ? "-" : days.Value == 1 ? "1 dia" : $"{days.Value} dias";
    private static string FormatInstallments(int? count) => count.HasValue ? $"{count.Value}x" : "-";
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    private static string FormatDocument(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 14 ? $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}" : Blank(value);
    }

    private static string StatusLabel(PurchaseOrderStatus status) => status switch
    {
        PurchaseOrderStatus.Open => "Em aberto",
        PurchaseOrderStatus.Approved => "Aprovada",
        PurchaseOrderStatus.Sent => "Enviada",
        PurchaseOrderStatus.PartiallyReceived => "Recebimento parcial",
        PurchaseOrderStatus.Received => "Recebida",
        PurchaseOrderStatus.PartiallyCompleted => "Parcialmente concluída",
        PurchaseOrderStatus.Completed => "Concluída",
        PurchaseOrderStatus.Cancelled => "Cancelada",
        PurchaseOrderStatus.WaitingSecondApproval => "Aguardando segunda aprovação",
        _ => status.ToString()
    };

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver != null) return;
        lock (FontResolverLock) GlobalFontSettings.FontResolver ??= new CrossPlatformFontResolver();
    }
}
