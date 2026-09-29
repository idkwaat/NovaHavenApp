using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Account;
using NovaHaven.Application.Features.Media;
using NovaHaven.Application.Features.Media.Repositories;
using NovaHaven.Application.Features.Media.Services;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Api.Endpoints;
using NovaHaven.Infrastructure.Data;
using NovaHaven.Infrastructure.Identity;
using NovaHaven.Infrastructure.Media;
using NovaHaven.Infrastructure.Persistence.Repositories.Wiki;
using NovaHaven.Infrastructure.Persistence.UnitOfWork;
using NovaHaven.Infrastructure.Notifications;
using WebPush;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("NovaDb")
    ?? throw new InvalidOperationException("ConnectionStrings:NovaDb must be set in environment configuration.");

builder.Services.AddProblemDetails();
builder.Services.AddControllersWithViews(); // Registers the built-in antiforgery filter used by Admin API actions.
builder.Services.AddDbContext<NovaDbContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<WikiReadService>();
builder.Services.AddScoped<IWikiReadRepository, EfWikiReadRepository>();
builder.Services.AddScoped<IWikiTagRepository, EfWikiTagRepository>();
builder.Services.AddScoped<IWikiCategoryRepository, EfWikiCategoryRepository>();
builder.Services.AddScoped<IWikiArticleRepository, EfWikiArticleRepository>();
builder.Services.AddScoped<IWikiMediaRepository, EfWikiMediaRepository>();
builder.Services.AddScoped<WikiTagService>();
builder.Services.AddScoped<WikiCategoryService>();
builder.Services.AddScoped<WikiArticleService>();
builder.Services.AddScoped<WikiMediaService>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddSingleton<IWikiMediaStorage, LocalWikiMediaStorage>();
builder.Services.AddSingleton<LocalOrSmtpUserEmailSender>();
builder.Services.AddSingleton<IUserEmailSender>(services => services.GetRequiredService<LocalOrSmtpUserEmailSender>());
builder.Services.AddSingleton<ILocalEmailOutbox>(services => services.GetRequiredService<LocalOrSmtpUserEmailSender>());
builder.Services.AddSingleton<IVapidKeyProvider, VapidKeyProvider>();
builder.Services.AddSingleton(new WebPushClient(new HttpClientHandler { AllowAutoRedirect = false }));
builder.Services.AddScoped<IWebPushGateway, WebPushGateway>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 12;
    options.Lockout.MaxFailedAccessAttempts = 5;
}) .AddEntityFrameworkStores<NovaDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();
app.MapAuthEndpoints();
app.MapNotificationEndpoints();
app.MapAuditEndpoints();
app.MapOperationsEndpoints();
app.MapNewsEndpoints();
app.MapCatalogEndpoints();
app.MapCatalogRecipeEndpoints();
app.MapKnowledgeEndpoints();
app.MapCommunityEndpoints();
app.MapIntegrationEndpoints();
app.MapRewardEndpoints();
app.MapCommerceEndpoints();
app.MapCommerceOrderEndpoints();

// Bootstrap is explicit, local-only, never performs schema changes or logs credentials.
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedAdmin:Enabled"))
{
    var email = builder.Configuration["SeedAdmin:Email"];
    var password = builder.Configuration["SeedAdmin:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
        throw new InvalidOperationException("Valid SeedAdmin:Email and SeedAdmin:Password are required for opt-in development bootstrap.");
    using var scope = app.Services.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    if (!await roles.RoleExistsAsync("Admin"))
    {
        var created = await roles.CreateAsync(new IdentityRole<Guid>("Admin"));
        if (!created.Succeeded) throw new InvalidOperationException("Failed to create development Admin role.");
    }
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var admin = await users.FindByEmailAsync(email);
    if (admin is null)
    {
        admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var created = await users.CreateAsync(admin, password);
        if (!created.Succeeded) throw new InvalidOperationException("Development Admin creation failed. Check password rules.");
    }
    else
    {
        var token = await users.GeneratePasswordResetTokenAsync(admin);
        var reset = await users.ResetPasswordAsync(admin, token, password);
        if (!reset.Succeeded) throw new InvalidOperationException("Development Admin password reset failed.");
    }
    if (!await users.IsInRoleAsync(admin, "Admin"))
    {
        var assigned = await users.AddToRoleAsync(admin, "Admin");
        if (!assigned.Succeeded) throw new InvalidOperationException("Development Admin role assignment failed.");
    }
}

app.Run();

public partial class Program { }
