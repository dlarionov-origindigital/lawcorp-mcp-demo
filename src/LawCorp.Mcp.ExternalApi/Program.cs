using LawCorp.Mcp.ExternalApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

if (!builder.Environment.IsProduction())
    builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DmsDb")
    ?? "Server=.\\SQLEXPRESS;Database=LawCorpDms;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseSqlServer(connectionString));

var useAuth = builder.Configuration.GetValue<bool>("UseAuth");
if (useAuth)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization();
}
else
{
    builder.Services.AddAuthentication("NoOp")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
            LawCorp.Mcp.ExternalApi.Auth.NoOpAuthHandler>("NoOp", null);
    builder.Services.AddAuthorization();
}

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("SeedDmsData"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    await DmsSeeder.SeedAsync(db);
}

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
