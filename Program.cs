using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Asp.Versioning.ApiExplorer;
using ProjectService.Data;
using ProjectService.Services;
using DotNetEnv;
using FMN.Vault;

// Load secrets from Vault if enabled, otherwise fall back to .env file
var vaultEnabled = Environment.GetEnvironmentVariable("VAULT_ENABLED") ?? "false";
if (vaultEnabled.Equals("true", StringComparison.OrdinalIgnoreCase))
{
    var vaultClient = new FmnVaultClient("projectservice");
    await vaultClient.LoadSecretsIntoEnvironmentAsync();
}
else
{
    // Fall back to .env file for local dev without Vault
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Get connection string from configuration (appsettings.Development.json for local dev, or environment variables)
// In Docker, ASPNETCORE_ENVIRONMENT=Development is set, but we should use env variables instead of appsettings
// because appsettings has localhost which doesn't work in Docker containers
var connectionString = (Environment.GetEnvironmentVariable("ConnectionString__DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStringsDefaultConnection"))
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Normalize environment variables - convert double-underscore to single-name format
var keycloakAuthority = Environment.GetEnvironmentVariable("Keycloak__Authority")
    ?? Environment.GetEnvironmentVariable("KeycloakAuthority");
var keycloakAudience = Environment.GetEnvironmentVariable("Keycloak__Audience")
    ?? Environment.GetEnvironmentVariable("KeycloakAudience");
var keycloakOAuthClientId = Environment.GetEnvironmentVariable("Keycloak__OAuthClientId")
    ?? Environment.GetEnvironmentVariable("KeycloakOAuthClientId");
var keycloakOAuthClientSecret = Environment.GetEnvironmentVariable("Keycloak__AuthClientSecret")
    ?? Environment.GetEnvironmentVariable("KeycloakOAuthClientSecret");

if (!string.IsNullOrEmpty(keycloakAuthority))
    Environment.SetEnvironmentVariable("KeycloakAuthority", keycloakAuthority);
if (!string.IsNullOrEmpty(keycloakAudience))
    Environment.SetEnvironmentVariable("KeycloakAudience", keycloakAudience);
if (!string.IsNullOrEmpty(keycloakOAuthClientId))
    Environment.SetEnvironmentVariable("KeycloakOAuthClientId", keycloakOAuthClientId);
if (!string.IsNullOrEmpty(keycloakOAuthClientSecret))
    Environment.SetEnvironmentVariable("KeycloakOAuthClientSecret", keycloakOAuthClientSecret);
if (!string.IsNullOrEmpty(connectionString))
    Environment.SetEnvironmentVariable("ConnectionStringsDefaultConnection", connectionString);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = Environment.GetEnvironmentVariable("KeycloakAuthority");
        options.Audience = Environment.GetEnvironmentVariable("KeycloakAudience");
    });

// Add all controllers
builder.Services.AddControllers();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    // Use query string parameter for versioning, "api-version" is the default parameter
    options.ApiVersionReader = new QueryStringApiVersionReader();

    // Assume default version when version is not specified
    options.AssumeDefaultVersionWhenUnspecified = true;

    // Report the supported versions in the response header
    options.ReportApiVersions = true;

    // Define default API version
    options.DefaultApiVersion = new ApiVersion(1, 0);
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV"; // Version format
    options.SubstituteApiVersionInUrl = true; // Substitute version in URL
});

// Get the connection string for migrations
builder.Services.AddDbContext<ProjectContext>(options =>
    options.UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStringsDefaultConnection")));

// Configure HttpClients for microservice communication
var habitServiceUrl = Environment.GetEnvironmentVariable("HabitService__Url") ?? "http://localhost:82";
var taskServiceUrl = Environment.GetEnvironmentVariable("TaskService__Url") ?? "http://localhost:83";

builder.Services.AddHttpClient<IHabitServiceClient, HabitServiceClient>(client =>
{
    client.BaseAddress = new Uri(habitServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<ITaskServiceClient, TaskServiceClient>(client =>
{
    client.BaseAddress = new Uri(taskServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Generate swagger UI
builder.Services.AddSwaggerGen(options =>{
    options.AddServer(new OpenApiServer { Url = "https://localhost:7257", Description = "Local dotnet run Server (HTTPS)" });
    options.AddServer(new OpenApiServer { Url = "http://localhost:5153", Description = "Local dotnet run Server (HTTP)" });
    options.AddServer(new OpenApiServer { Url = "http://localhost:89", Description = "Local Docker Compose Server" });
    options.AddServer(new OpenApiServer { Url = "https://localhost/project", Description = "Local Kubernetes Server" });
    options.AddServer(new OpenApiServer { Url = "https://forgetmenotqa.uplifttech.org/project", Description = "Cloud QA Server" });
    options.AddServer(new OpenApiServer { Url = "https://forgetmenot.uplifttech.org/project", Description = "Cloud Prod Server" });

    var provider = builder.Services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(description.GroupName, new OpenApiInfo()
        {
            Title = $"Forget Me Not Project API {description.ApiVersion}",
            Version = description.ApiVersion.ToString(),
        });
    }

    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());


    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{Environment.GetEnvironmentVariable("KeycloakAuthority")}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{Environment.GetEnvironmentVariable("KeycloakAuthority")}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "Open ID" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid" } // Add other scopes if needed
        }
    });
});

// Build the app
var app = builder.Build();

// Build the swagger app if it's a development environment
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI(options => {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Localhost only: {description.GroupName.ToUpperInvariant()}");
            options.SwaggerEndpoint($"/project/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        options.OAuthClientId(Environment.GetEnvironmentVariable("KeycloakOAuthClientId"));
        options.OAuthClientSecret(Environment.GetEnvironmentVariable("KeycloakOAuthClientSecret"));
        options.OAuthAppName("FMN Project Swagger UI");
    });
//}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Try to apply migrations
        var context = services.GetRequiredService<ProjectContext>();
        context.Database.Migrate(); // This applies the migration
    }
    catch (Exception ex)
    {
        // Log any problems applying migrations
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        logger.LogError(ex, "Connection String: " + Environment.GetEnvironmentVariable("ConnectionStringsDefaultConnection"));
    }
}

app.Run();
