using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SGE.Application.DTOs.ServiceOrder;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Infrastructure.Pdf;

public class ServiceOrderPdfService : IServiceOrderPdfService
{
    private const double Margin = 42;
    private const double FooterHeight = 26;
    private static readonly object FontResolverLock = new();

    public byte[] Generate(ServiceOrderDto serviceOrder)
    {
        EnsureFontResolver();

        using var document = new PdfDocument();
        document.Info.Title = $"Ordem de Serviço {serviceOrder.Number}";

        var regular = new XFont("Arial", 9.5, XFontStyleEx.Regular);
        var small = new XFont("Arial", 7.5, XFontStyleEx.Regular);
        var valueFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var bold = new XFont("Arial", 9.5, XFontStyleEx.Bold);
        var section = new XFont("Arial", 10, XFontStyleEx.Bold);
        var title = new XFont("Arial", 18, XFontStyleEx.Bold);
        var orderNumber = new XFont("Arial", 11, XFontStyleEx.Bold);
        var totalFont = new XFont("Arial", 16, XFontStyleEx.Bold);
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
            if (y + height > contentBottom) NewPage();
        }

        List<string> WrapText(string? value, XFont font, double width)
        {
            var words = Blank(value).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var current = string.Empty;
            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
                if (gfx.MeasureString(candidate, font).Width <= width || string.IsNullOrEmpty(current))
                    current = candidate;
                else
                {
                    lines.Add(current);
                    current = word;
                }
            }

            if (!string.IsNullOrEmpty(current)) lines.Add(current);
            return lines.Count == 0 ? new List<string> { "-" } : lines;
        }

        void DrawWrappedText(string? value, XFont font, XBrush brush, double x, double top, double width, double lineHeight)
        {
            var lines = WrapText(value, font, width);
            for (var index = 0; index < lines.Count; index++)
                gfx.DrawString(lines[index], font, brush,
                    new XRect(x, top + index * lineHeight, width, lineHeight), XStringFormats.TopLeft);
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
            var lines = fields.Select(field => WrapText(field.Value, valueFont, field.Width - 18)).ToArray();
            var rowHeight = Math.Max(42, lines.Max(item => 25 + item.Count * 13));
            EnsureSpace(rowHeight + 12);
            var x = Margin;
            for (var index = 0; index < fields.Length; index++)
            {
                var field = fields[index];
                gfx.DrawRectangle(white, x, y, field.Width, rowHeight);
                gfx.DrawRectangle(new XPen(border, 0.6), x, y, field.Width, rowHeight);
                gfx.DrawString(field.Label, small, new XSolidBrush(muted),
                    new XRect(x + 9, y + 7, field.Width - 18, 11), XStringFormats.TopLeft);
                DrawWrappedText(field.Value, valueFont, new XSolidBrush(dark),
                    x + 9, y + 21, field.Width - 18, 13);
                x += field.Width;
            }

            y += rowHeight + 12;
        }

        gfx.DrawRectangle(new XSolidBrush(accent), Margin, y, contentWidth, 5);
        y += 19;
        gfx.DrawString("SGE-ERP", logo, new XSolidBrush(dark), new XRect(Margin, y, 150, 18), XStringFormats.TopLeft);
        gfx.DrawString("Estrutural", regular, new XSolidBrush(muted), new XRect(Margin, y + 19, 150, 15), XStringFormats.TopLeft);
        gfx.DrawString("ORDEM DE SERVIÇO", title, new XSolidBrush(dark),
            new XRect(Margin + 180, y - 1, contentWidth - 180, 24), XStringFormats.TopRight);
        gfx.DrawString($"Nº {serviceOrder.Number}", orderNumber, new XSolidBrush(accent),
            new XRect(Margin + 180, y + 26, contentWidth - 180, 17), XStringFormats.TopRight);
        y += 60;

        InfoGrid(
            ("EMISSÃO", serviceOrder.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), 150),
            ("STATUS DE EXECUÇÃO", ExecutionStatusLabel(serviceOrder.ExecutionStatus), 170),
            ("RESPONSÁVEL", serviceOrder.RequestedByUserName, contentWidth - 320));

        SectionTitle("Prestador");
        InfoGrid(
            ("RAZÃO SOCIAL", serviceOrder.SupplierName, 220),
            ("CNPJ", FormatDocument(serviceOrder.SupplierDocument), 130),
            ("CONTATO", string.Join(" · ", new[] { serviceOrder.SupplierEmail, serviceOrder.SupplierPhone }
                .Where(value => !string.IsNullOrWhiteSpace(value))), contentWidth - 350));

        SectionTitle("Dados do serviço");
        InfoGrid(
            ("OBRA", serviceOrder.WorkName, 250),
            ("SOLICITAÇÃO DE ORIGEM", serviceOrder.PurchaseRequestNumber, contentWidth - 250));
        InfoGrid(("DESCRIÇÃO", serviceOrder.ServiceDescription, contentWidth));
        if (!string.IsNullOrWhiteSpace(serviceOrder.ServiceSpecification))
            InfoGrid(("ESPECIFICAÇÃO", serviceOrder.ServiceSpecification, contentWidth));
        InfoGrid(
            ("QUANTIDADE", FormatQuantity(serviceOrder.EstimatedQuantity), contentWidth * 0.55),
            ("UNIDADE", serviceOrder.Unit, contentWidth * 0.45));

        SectionTitle("Condições comerciais");
        InfoGrid(
            ("CONDIÇÃO DE PAGAMENTO", serviceOrder.PaymentCondition, contentWidth * 0.62),
            ("PARCELAS", FormatInstallments(serviceOrder.InstallmentCount), contentWidth * 0.38));

        var approvedAmendments = serviceOrder.Amendments
            .Where(x => x.Status == ServiceOrderAmendmentStatus.Approved)
            .OrderBy(x => x.ApprovedAt)
            .ToList();
        if (approvedAmendments.Count > 0)
        {
            SectionTitle("Adendos contratuais");
            InfoGrid(
                ("VALOR ORIGINAL", FormatMoney(serviceOrder.ContractedValue), contentWidth * 0.5),
                ("VALOR CONTRATUAL ATUAL", FormatMoney(serviceOrder.CurrentContractedValue), contentWidth * 0.5));
            if (serviceOrder.EstimatedQuantity.HasValue || serviceOrder.CurrentContractedQuantity.HasValue)
                InfoGrid(
                    ("QUANTIDADE ORIGINAL", FormatQuantity(serviceOrder.EstimatedQuantity) + (string.IsNullOrWhiteSpace(serviceOrder.Unit) ? "" : $" {serviceOrder.Unit}"), contentWidth * 0.5),
                    ("QUANTIDADE ATUAL", FormatQuantity(serviceOrder.CurrentContractedQuantity) + (string.IsNullOrWhiteSpace(serviceOrder.Unit) ? "" : $" {serviceOrder.Unit}"), contentWidth * 0.5));

            foreach (var amendment in approvedAmendments)
            {
                var details = new List<string>();
                if (amendment.ValueAdjustment.HasValue)
                    details.Add($"Valor: {FormatSignedMoney(amendment.ValueAdjustment.Value)} · {FormatMoney(amendment.ValueBeforeApproval ?? 0m)} → {FormatMoney(amendment.ValueAfterApproval ?? 0m)}");
                if (amendment.QuantityAdjustment.HasValue)
                    details.Add($"Quantidade: {FormatSignedQuantity(amendment.QuantityAdjustment.Value)} {serviceOrder.Unit} · {FormatQuantity(amendment.QuantityBeforeApproval)} → {FormatQuantity(amendment.QuantityAfterApproval)} {serviceOrder.Unit}");
                details.Add($"Aprovado em {amendment.ApprovedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")} por {amendment.ApprovedByUserName ?? "-"}");
                InfoGrid(($"ADENDO — {amendment.Reason}", string.Join(" · ", details), contentWidth));
                if (!string.IsNullOrWhiteSpace(amendment.Observation))
                    InfoGrid(("OBSERVAÇÃO", amendment.Observation, contentWidth));
            }
        }

        if (!string.IsNullOrWhiteSpace(serviceOrder.ContractFileName))
        {
            SectionTitle("Contrato");
            var uploadedAt = serviceOrder.ContractUploadedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            var uploader = string.IsNullOrWhiteSpace(serviceOrder.ContractUploadedByUserName)
                ? null
                : $"Enviado por {serviceOrder.ContractUploadedByUserName}";
            InfoGrid(("DOCUMENTO", string.Join(" · ", new[] { serviceOrder.ContractFileName, uploadedAt, uploader }
                .Where(value => !string.IsNullOrWhiteSpace(value))), contentWidth));
        }

        EnsureSpace(72);
        gfx.DrawRectangle(new XSolidBrush(headerFill), Margin, y, contentWidth, 56);
        gfx.DrawRectangle(new XPen(accent, 1.2), Margin, y, contentWidth, 56);
        gfx.DrawString("VALOR CONTRATUAL ATUAL", bold, new XSolidBrush(muted),
            new XRect(Margin + 14, y + 18, contentWidth - 220, 16), XStringFormats.TopLeft);
        gfx.DrawString(FormatMoney(serviceOrder.CurrentContractedValue), totalFont, new XSolidBrush(dark),
            new XRect(page.Width.Point - Margin - 190, y + 13, 176, 27), XStringFormats.TopRight);

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

    private static string ExecutionStatusLabel(ServiceOrderExecutionStatus status) => status switch
    {
        ServiceOrderExecutionStatus.WaitingContract => "Aguardando contrato",
        ServiceOrderExecutionStatus.Released => "Liberada para execução",
        ServiceOrderExecutionStatus.InProgress => "Em execução",
        ServiceOrderExecutionStatus.Completed => "Concluída",
        ServiceOrderExecutionStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    private static string FormatMoney(decimal value) => value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
    private static string FormatSignedMoney(decimal value) => $"{(value >= 0 ? "+" : "−")} {FormatMoney(Math.Abs(value))}";
    private static string FormatSignedQuantity(decimal value) => $"{(value >= 0 ? "+" : "−")} {FormatQuantity(Math.Abs(value))}";
    private static string FormatInstallments(int? count) => count.HasValue ? $"{count.Value}x" : "-";
    private static string FormatQuantity(decimal? quantity) => quantity.HasValue
        ? quantity.Value.ToString("0.####", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
        : "-";
    private static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string FormatDocument(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == 14
            ? $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}"
            : Blank(value);
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver != null) return;
        lock (FontResolverLock) GlobalFontSettings.FontResolver ??= new CrossPlatformFontResolver();
    }
}
