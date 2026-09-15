using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using SGE.API.Services;

using SGE.Persistence.Contexts;

using SGE.Application.Security;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Repositories.Purchasing;

using SGE.Persistence.Repositories.Administration;
using SGE.Persistence.Repositories.Catalog;
using SGE.Persistence.Repositories.Companies;
using SGE.Persistence.Repositories.Purchasing;
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
using SGE.Infrastructure.Authentication;
using SGE.Infrastructure.Pdf;




var builder = WebApplication.CreateBuilder(args);
const string LocalAngularCorsPolicy = "LocalAngularCorsPolicy";

// ==========================================
// Controllers
// ==========================================
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(LocalAngularCorsPolicy, policy =>
        {
            policy
                .WithOrigins("http://localhost:4200")
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type");
        });
    });
}

// ==========================================
// Database
// ==========================================
builder.Services.AddDbContext<SgeDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// Authentication
// ==========================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key nao configurado.");

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
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
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IItemService, ItemService>();

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
builder.Services.AddScoped<IServiceMeasurementRepository, ServiceMeasurementRepository>();
builder.Services.AddScoped<IServiceOrderPaymentRepository, ServiceOrderPaymentRepository>();
builder.Services.AddScoped<IServiceAdvancePaymentRequestRepository, ServiceAdvancePaymentRequestRepository>();
builder.Services.AddScoped<IServiceOrderAttachmentRepository, ServiceOrderAttachmentRepository>();

builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IApprovalHistoryService, ApprovalHistoryService>();
builder.Services.AddScoped<IPurchaseRequestItemService, PurchaseRequestItemService>();
builder.Services.AddScoped<IQuotationItemService, QuotationItemService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IServiceOrderService, ServiceOrderService>();
builder.Services.AddScoped<IServiceMeasurementService, ServiceMeasurementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPurchaseOrderPdfService, PurchaseOrderPdfService>();

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

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await SeedDevelopmentAdminAsync(scope.ServiceProvider);
}

// ==========================================
// Middleware
// ==========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(LocalAngularCorsPolicy);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedDevelopmentAdminAsync(IServiceProvider services)
{
    var context = services.GetRequiredService<SgeDbContext>();
    var passwordHasher = services.GetRequiredService<IPasswordHasher>();

    var roles = new[]
    {
        AppRoles.Requester,
        AppRoles.Approver,
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

    var adminRole = await context.Roles
        .FirstAsync(x => x.Name == AppRoles.Admin);
    const string adminEmail = "admin@sge.local";

    if (!await context.Users.AnyAsync(x => x.Email == adminEmail))
    {
        await context.Users.AddAsync(new User(
            "Admin",
            "SGE",
            adminEmail,
            passwordHasher.HashPassword("Admin123!"),
            adminRole.Id));

        await context.SaveChangesAsync();
    }
}
