using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SGE.API.Services;
using SGE.API.Services.FileStorage;

using SGE.Persistence.Contexts;

using SGE.Application.Security;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;
using SGE.Application.Interfaces.Repositories.Dashboard;

using SGE.Persistence.Repositories.Administration;
using SGE.Persistence.Repositories.Catalog;
using SGE.Persistence.Repositories.Companies;
using SGE.Persistence.Repositories.Purchasing;
using SGE.Persistence.Repositories.Dashboard;
using SGE.Application.Interfaces.Services.Catalog;
using SGE.Application.Services.Catalog;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Application.Services.Companies;
using SGE.Application.Interfaces.Services.Administration;
using SGE.Application.Services.Administration;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Services.Purchasing;
using SGE.Application.Services.Authentication;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Entities.Companies;
using SGE.Infrastructure.Authentication;
using SGE.Infrastructure.Pdf;




var builder = WebApplication.CreateBuilder(args);
const string LocalAngularCorsPolicy = "LocalAngularCorsPolicy";
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var railwayPort) && railwayPort is > 0 and <= 65535)
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");

// ==========================================
// Controllers
// ==========================================
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:4200", "http://127.0.0.1:4200" }
    : (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToArray();

if (builder.Environment.IsProduction() && allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins precisa conter a origem publica do frontend em producao.");

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(LocalAngularCorsPolicy, policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .AllowAnyHeader();
        });
    });
}

// ==========================================
// Database
// ==========================================
builder.Services.AddDbContext<SgeDbContext>(options =>
    options.UseNpgsql(PostgresConnectionString.Resolve(builder.Configuration)));
var storageProvider = builder.Configuration["Storage:Provider"] ??
    (builder.Environment.IsDevelopment() ? "Local" : "S3");
if (builder.Environment.IsProduction() && !storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Storage:Provider deve ser S3 em produção.");

if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();
else if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IFileStorage, S3FileStorage>();
else
    throw new InvalidOperationException("Storage:Provider deve ser Local ou S3.");

// ==========================================
// Authentication
// ==========================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key nao configurado.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer nao configurado.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience nao configurado.");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key deve conter ao menos 32 bytes.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ==========================================
// Repositories
// ==========================================

// Administration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

// Catalog
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IUnitOfMeasureRepository, UnitOfMeasureRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();

// Companies
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IWorkRepository, WorkRepository>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IWorkService, WorkService>();

// Purchasing
builder.Services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IQuotationRepository, QuotationRepository>();
builder.Services.AddScoped<IApprovalRepository, ApprovalRepository>();
builder.Services.AddScoped<IApprovalHistoryRepository, ApprovalHistoryRepository>();
builder.Services.AddScoped<IPurchaseRequestItemRepository, PurchaseRequestItemRepository>();
builder.Services.AddScoped<IQuotationItemRepository, QuotationItemRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IReceiptItemRepository, ReceiptItemRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
builder.Services.AddScoped<IServiceOrderAmendmentRepository, ServiceOrderAmendmentRepository>();
builder.Services.AddScoped<IServiceMeasurementRepository, ServiceMeasurementRepository>();
builder.Services.AddScoped<IServiceOrderPaymentRepository, ServiceOrderPaymentRepository>();
builder.Services.AddScoped<IServiceAdvancePaymentRequestRepository, ServiceAdvancePaymentRequestRepository>();
builder.Services.AddScoped<IServiceOrderAttachmentRepository, ServiceOrderAttachmentRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IWorkCostsRepository, WorkCostsRepository>();

builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IFinanceQueueService, FinanceQueueService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IApprovalHistoryService, ApprovalHistoryService>();
builder.Services.AddScoped<IPurchaseRequestItemService, PurchaseRequestItemService>();
builder.Services.AddScoped<IQuotationItemService, QuotationItemService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
builder.Services.AddScoped<IServiceOrderAmendmentService, ServiceOrderAmendmentService>();
builder.Services.AddScoped<IServiceMeasurementService, ServiceMeasurementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPurchaseOrderPdfService, PurchaseOrderPdfService>();
builder.Services.AddScoped<IServiceOrderPdfService, ServiceOrderPdfService>();

// ==========================================
// Swagger
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

// ==========================================
// Build
// ==========================================
var app = builder.Build();
// Resolve eagerly so a Production S3 setup with missing credentials/endpoints fails at startup.
_ = app.Services.GetRequiredService<IFileStorage>();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        if (exceptionFeature?.Error != null)
            logger.LogError(exceptionFeature.Error, "Erro nao tratado em {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Nao foi possivel concluir a operacao."
        });
    });
});

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    await SeedStructuralDataAsync(scope.ServiceProvider, app.Configuration, app.Environment.IsProduction());
}

// ==========================================
// Middleware
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsProduction())
    app.UseHttpsRedirection();

if (allowedOrigins.Length > 0)
{
    app.UseCors(LocalAngularCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (SgeDbContext context, CancellationToken cancellationToken) =>
    await context.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.MapControllers();

app.Run();

static async Task SeedStructuralDataAsync(IServiceProvider services, IConfiguration configuration, bool isProduction)
{
    var context = services.GetRequiredService<SgeDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();
    await using var transaction = await context.Database.BeginTransactionAsync();
    await context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(74380925)");

    var roles = new[]
    {
        AppRoles.Buyer,
        AppRoles.Warehouse,
        AppRoles.Finance,
        AppRoles.Admin
    };

    foreach (var roleName in roles)
    {
        if (!await context.Roles.AnyAsync(x => x.Name == roleName))
        {
            await context.Roles.AddAsync(new Role(
                roleName,
                $"{roleName} profile"));
        }
    }

    await context.SaveChangesAsync();

    if (!await context.Companies.AnyAsync(x => x.TradeName == "Estrutural" || x.CorporateName == "Estrutural"))
    {
        await context.Companies.AddAsync(new Company("Estrutural", "Estrutural", "ESTRUTURAL-SGE", string.Empty, string.Empty));
        await context.SaveChangesAsync();
    }

    if (await context.Users.AnyAsync(x => x.Role.Name == AppRoles.Admin))
    {
        await transaction.CommitAsync();
        return;
    }

    var bootstrapEmail = configuration["BootstrapAdmin:Email"];
    var bootstrapPassword = configuration["BootstrapAdmin:Password"];
    if (string.IsNullOrWhiteSpace(bootstrapEmail) || string.IsNullOrWhiteSpace(bootstrapPassword))
    {
        if (isProduction)
            throw new InvalidOperationException("Configure BootstrapAdmin:Email e BootstrapAdmin:Password para criar o primeiro administrador.");
        await transaction.CommitAsync();
        return;
    }
    if (bootstrapPassword.Length < 16)
        throw new InvalidOperationException("BootstrapAdmin:Password deve conter ao menos 16 caracteres.");

    var normalizedEmail = bootstrapEmail.Trim();
    if (await context.Users.AnyAsync(x => x.Email == normalizedEmail))
        throw new InvalidOperationException("O email BootstrapAdmin:Email ja pertence a outro perfil.");

    var adminRole = await context.Roles.FirstAsync(x => x.Name == AppRoles.Admin);
    await context.Users.AddAsync(new User("Administrador", "SGE", normalizedEmail,
        passwordHasher.HashPassword(bootstrapPassword), adminRole.Id));
    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
