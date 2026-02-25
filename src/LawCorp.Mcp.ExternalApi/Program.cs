using LawCorp.Mcp.ExternalApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DmsDb")
    ?? "Server=.\\SQLEXPRESS;Database=LawCorpDms;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("SeedDmsData"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    await DmsSeeder.SeedAsync(db);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
