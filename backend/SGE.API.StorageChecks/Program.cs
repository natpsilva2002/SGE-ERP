using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using SGE.API.Controllers.Purchasing;
using SGE.API.Services.FileStorage;
using SGE.Application.DTOs.Payment;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

var temporaryRoot = Path.Combine(Path.GetTempPath(), $"sge-storage-check-{Guid.NewGuid():N}");
Directory.CreateDirectory(temporaryRoot);

try
{
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["UPLOADS_PATH"] = temporaryRoot })
        .Build();
    var storage = new LocalFileStorage(new TestWebHostEnvironment(temporaryRoot), configuration);
    var paymentService = new TestPaymentService();
    var controller = new PaymentController(paymentService, storage, new TestCurrentUserService());
    controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

    var expectedBytes = "anexo privado de teste"u8.ToArray();
    var upload = new FormFile(new MemoryStream(expectedBytes), 0, expectedBytes.Length, "files", "comprovante.pdf")
    {
        Headers = new HeaderDictionary { ["Content-Type"] = "application/pdf" }
    };

    var uploadResult = await controller.UploadAttachments(Guid.NewGuid(), [upload]);
    Require(uploadResult.Result is OkObjectResult, "Upload autenticado deve persistir metadados.");

    var persistedKey = paymentService.PersistedFilePath
        ?? throw new InvalidOperationException("O serviço de metadados não recebeu a chave do objeto.");
    Require(persistedKey.StartsWith("payment-attachments/payments/", StringComparison.Ordinal), "A chave gerada deve ser única e namespaced.");
    Require(await storage.ExistsAsync(persistedKey), "O objeto enviado deve existir no armazenamento.");

    var downloadResult = await controller.DownloadAttachment(Guid.NewGuid(), Guid.NewGuid());
    Require(downloadResult is FileStreamResult, "Download deve continuar sendo servido pelo endpoint autenticado.");
    await using (var downloaded = ((FileStreamResult)downloadResult).FileStream)
    {
        using var buffer = new MemoryStream();
        await downloaded.CopyToAsync(buffer);
        Require(buffer.ToArray().SequenceEqual(expectedBytes), "Download deve devolver o mesmo conteúdo enviado.");
    }

    var controllerAuthorization = typeof(PaymentController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();
    var uploadAuthorization = typeof(PaymentController).GetMethod(nameof(PaymentController.UploadAttachments))!
        .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();
    var deleteAuthorization = typeof(PaymentController).GetMethod(nameof(PaymentController.DeleteAttachment))!
        .GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
        .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single();
    Require(controllerAuthorization.Roles == AppRoles.PaymentReaders, "A permissão de leitura existente deve permanecer aplicada.");
    Require(uploadAuthorization.Roles == AppRoles.FinanceOrAdmin, "O upload continua restrito aos perfis financeiros/admin.");
    Require(deleteAuthorization.Roles == AppRoles.FinanceOrAdmin, "A exclusão continua restrita aos perfis financeiros/admin.");

    var deleteResult = await controller.DeleteAttachment(Guid.NewGuid(), Guid.NewGuid());
    Require(deleteResult is NoContentResult, "Exclusão permitida deve remover o metadado e o objeto.");
    Require(!await storage.ExistsAsync(persistedKey), "O objeto deve deixar de existir após exclusão autorizada.");

    var legacyDirectory = Path.Combine(temporaryRoot, "payment-attachments");
    Directory.CreateDirectory(legacyDirectory);
    await File.WriteAllBytesAsync(Path.Combine(legacyDirectory, "legacy.pdf"), expectedBytes);
    await using var legacyDownload = await storage.DownloadAsync("uploads/payment-attachments/legacy.pdf");
    Require(legacyDownload != null, "Caminhos históricos uploads/<categoria>/<arquivo> devem continuar legíveis em Development.");

    var traversalRejected = false;
    try { await storage.ExistsAsync("production/../outside.pdf"); }
    catch (InvalidOperationException) { traversalRejected = true; }
    Require(traversalRejected, "Chaves com path traversal devem ser recusadas.");

    Console.WriteLine("PASS: upload, metadado de chave, download, autorização, exclusão, compatibilidade local histórica e path traversal.");
}
finally
{
    if (Directory.Exists(temporaryRoot)) Directory.Delete(temporaryRoot, recursive: true);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class TestPaymentService : IPaymentService
{
    public string? PersistedFilePath { get; private set; }

    public Task<PaymentDto?> AddAttachmentAsync(Guid paymentId, string originalFileName, string filePath, string contentType, long fileSizeBytes, Guid uploadedByUserId)
    {
        PersistedFilePath = filePath;
        return Task.FromResult<PaymentDto?>(new PaymentDto { Id = paymentId });
    }

    public Task<(string FilePath, string FileName, string ContentType)?> GetAttachmentAsync(Guid paymentId, Guid attachmentId) =>
        Task.FromResult<(string FilePath, string FileName, string ContentType)?>(PersistedFilePath == null
            ? null
            : (PersistedFilePath, "comprovante.pdf", "application/pdf"));

    public Task<(bool Deleted, string? FilePath)> DeleteAttachmentAsync(Guid paymentId, Guid attachmentId)
    {
        var path = PersistedFilePath;
        PersistedFilePath = null;
        return Task.FromResult((path != null, path));
    }

    public Task<IEnumerable<PaymentDto>> GetAllAsync() => Task.FromResult<IEnumerable<PaymentDto>>([]);
    public Task<PaymentDto?> GetByIdAsync(Guid id) => Task.FromResult<PaymentDto?>(null);
    public Task<IEnumerable<PaymentDto>> GetByPurchaseOrderIdAsync(Guid purchaseOrderId) => Task.FromResult<IEnumerable<PaymentDto>>([]);
    public Task<PaymentDto?> PayAsync(Guid purchaseOrderId, PayPurchaseOrderDto dto) => Task.FromResult<PaymentDto?>(null);
}

sealed class TestCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; } = Guid.NewGuid();
    public string Email => "test@example.invalid";
    public string Name => "Storage check";
    public string Role => "Financeiro";
    public bool IsAuthenticated => true;
}

sealed class TestWebHostEnvironment(string contentRoot) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "StorageChecks";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = contentRoot;
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = contentRoot;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
