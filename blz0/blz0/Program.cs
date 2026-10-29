using blz0.Components;
using Microsoft.EntityFrameworkCore;
using blz0.Data;
using blz0.Authorization;
using blz0.Operations;
using blz0.SharedServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var dotEnvPathCandidates = new[]
    {
        Path.Combine(builder.Environment.ContentRootPath, ".env"),
        Path.Combine(builder.Environment.ContentRootPath, "..", ".env"),
        Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".env")
    };

    var dotEnvValues = LoadDotEnv(dotEnvPathCandidates);
    if (dotEnvValues.Count > 0)
    {
        builder.Configuration.AddInMemoryCollection(dotEnvValues);
    }
}

builder.Services.AddSignalR();
builder.Services.AddHttpClient();

var azureAdTenantId = builder.Configuration["AzureAd:TenantId"];
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var azureAdClientSecret = builder.Configuration["AzureAd:ClientSecret"];
var apiTenantId = builder.Configuration["ApiCredentials:tenantId"];
var apiClientId = builder.Configuration["ApiCredentials:clientId"];
var apiClientSecret = builder.Configuration["ApiCredentials:clientSecret"];

if (string.IsNullOrWhiteSpace(azureAdTenantId) && !string.IsNullOrWhiteSpace(apiTenantId))
{
    builder.Configuration["AzureAd:TenantId"] = apiTenantId;
}

if (string.IsNullOrWhiteSpace(azureAdClientId) && !string.IsNullOrWhiteSpace(apiClientId))
{
    builder.Configuration["AzureAd:ClientId"] = apiClientId;
}

if (string.IsNullOrWhiteSpace(azureAdClientSecret) && !string.IsNullOrWhiteSpace(apiClientSecret))
{
    builder.Configuration["AzureAd:ClientSecret"] = apiClientSecret;
}

if (string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]))
{
    throw new InvalidOperationException("Missing Entra ID configuration: AzureAd:ClientId (or ApiCredentials:clientId in .env).");
}

builder.Services.AddDbContextFactory<blz0Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("blz0ContextSql") ?? throw new InvalidOperationException("Connection string 'blz0Context' not found.")), ServiceLifetime.Transient);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddBlazorBootstrap();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi(new[] { "https://analysis.windows.net/powerbi/api/.default" })
    .AddInMemoryTokenCaches();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<FabricAuth>();
builder.Services.AddScoped<GlobalData>();
builder.Services.AddScoped<FabricApiData>();
builder.Services.AddScoped<FabricSqlData>();
builder.Services.AddScoped<FabricKqlData>();
builder.Services.AddScoped<AiKqlService>();
builder.Services.AddScoped<WarehouseMetadataData>();
builder.Services.AddScoped<SharedFabricAuth>();

var app = builder.Build();
app.MapHub<NotificationHub>("/notificationHub").RequireAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // The default HSTS value is 30 days. Modify for production scenarios
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/account/change-user", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    var props = new OpenIdConnectChallengeProperties
    {
        RedirectUri = "/"
    };
    props.Prompt = "select_account";
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, props);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Register API endpoints from the separate file
app.MapApiEndpoints();
app.Run();

static Dictionary<string, string?> LoadDotEnv(IEnumerable<string> filePaths)
{
    foreach (var path in filePaths)
    {
        if (!File.Exists(path))
        {
            continue;
        }

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            key = key.Replace("__", ":");
            var value = line[(separatorIndex + 1)..].Trim();
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            result[key] = value;
        }

        return result;
    }

    return new Dictionary<string, string?>();
}
