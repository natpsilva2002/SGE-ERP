using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SGE.Application.DTOs.PurchaseOrder;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Domain.Enums;

namespace SGE.Infrastructure.Pdf;

public class PurchaseOrderPdfService : IPurchaseOrderPdfService
{
    private const double Margin = 40;
    private const double LineHeight = 18;
    private static readonly object FontResolverLock = new();

    public byte[] Generate(PurchaseOrderDto purchaseOrder)
    {
        EnsureFontResolver();

        if (purchaseOrder.Status == PurchaseOrderStatus.Open)
            throw new InvalidOperationException(
                "O PDF oficial da ordem de compra so pode ser gerado apos aprovacao.");

        if (purchaseOrder.Status == PurchaseOrderStatus.WaitingSecondApproval)
            throw new InvalidOperationException(
                "A ordem de compra ainda aguarda a segunda aprovação.");

        if (purchaseOrder.Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException(
                "Nao e possivel gerar PDF oficial de uma ordem de compra cancelada.");

        using var document = new PdfDocument();
        document.Info.Title = $"Ordem de Compra {purchaseOrder.Number}";

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var y = Margin;

        var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
        var sectionFont = new XFont("Arial", 12, XFontStyleEx.Bold);
        var regularFont = new XFont("Arial", 10, XFontStyleEx.Regular);
        var boldFont = new XFont("Arial", 10, XFontStyleEx.Bold);

        void EnsureSpace(double requiredHeight)
        {
            if (y + requiredHeight <= page.Height.Point - Margin)
                return;

            page = document.AddPage();
            gfx = XGraphics.FromPdfPage(page);
            y = Margin;
        }

        void Text(string value, XFont? font = null, double indent = 0)
        {
            EnsureSpace(LineHeight);
            gfx.DrawString(
                value,
                font ?? regularFont,
                XBrushes.Black,
                new XRect(Margin + indent, y, page.Width.Point - (Margin * 2) - indent, LineHeight),
                XStringFormats.TopLeft);
            y += LineHeight;
        }

        void Section(string title)
        {
            EnsureSpace(LineHeight * 2);
            y += 8;
            Text(title, sectionFont);
        }

        Text("ORDEM DE COMPRA", titleFont);
        Text($"Numero da OC: {purchaseOrder.Number}", boldFont);
        Text($"Data: {FormatDate(purchaseOrder.IssueDate)}");
        Text($"Status: {purchaseOrder.Status}");

        Section("Origem");
        Text($"Solicitacao: {Blank(purchaseOrder.PurchaseRequestNumber)}");
        Text($"Cotacao: {Blank(purchaseOrder.QuotationNumber)}");
        Text($"Obra: {Blank(purchaseOrder.WorkName)}");

        Section("Fornecedor");
        Text($"Nome/Razao social: {Blank(purchaseOrder.SupplierName)}");
        Text($"CNPJ: {FormatDocument(purchaseOrder.SupplierDocument)}");

        Section("Itens");
        foreach (var item in purchaseOrder.Items)
        {
            Text($"{Blank(item.ItemDescription)}", boldFont);
            Text($"Quantidade: {item.QuantityOrdered:0.####} {item.Unit}", indent: 12);
            Text($"Preco unitario: {FormatMoney(item.UnitPrice)}", indent: 12);
            Text($"Preco total: {FormatMoney(item.TotalValue)}", indent: 12);

            if (!string.IsNullOrWhiteSpace(item.Observation))
                Text($"Observacao: {item.Observation}", indent: 12);

            y += 4;
        }

        Text($"Total geral: {FormatMoney(purchaseOrder.TotalValue)}", boldFont);

        Section("Condicoes comerciais");
        Text($"Prazo de entrega: {FormatDelivery(purchaseOrder.DeliveryDays)}");
        Text($"Condicao de pagamento: {Blank(purchaseOrder.PaymentCondition)}");
        Text($"Parcelas: {FormatInstallments(purchaseOrder.InstallmentCount)}");

        using var stream = new MemoryStream();
        document.Save(stream, false);

        return stream.ToArray();
    }

    private static string FormatDate(DateTime value) =>
        value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    private static string FormatMoney(decimal value) =>
        value.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    private static string FormatDelivery(int? days) =>
        days.HasValue ? $"{days.Value} dia(s)" : "-";

    private static string FormatInstallments(int? count) =>
        count.HasValue ? $"{count.Value}x" : "-";

    private static string Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string FormatDocument(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 14)
            return Blank(value);

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }

    private static void EnsureFontResolver()
    {
        if (GlobalFontSettings.FontResolver != null)
            return;

        lock (FontResolverLock)
        {
            GlobalFontSettings.FontResolver ??= new WindowsFontResolver();
        }
    }
}
