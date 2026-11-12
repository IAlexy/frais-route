using FraisRoute.Api.Data;
using FraisRoute.Api.Endpoints;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// 1. Configuration de la licence QuestPDF (Communautaire gratuite)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 2. Services
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FraisRoute API - Gestion des Indemnités Kilométriques & Frais de route",
        Version = "v1",
        Description = "API backend pour la saisie, le calcul fiscal Urssaf et l'export PDF de notes de frais"
    });
});

// Base de données SQLite locale sans dépendance externe
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? "Data Source=fraisroute.db";
    options.UseSqlite(connectionString);
});

// Client HTTP pour les APIs de géocodage et de routage
builder.Services.AddHttpClient();

// Configuration CORS pour Vue 3 (Web local + Capacitor mobile)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173", "http://127.0.0.1:5173", "capacitor://localhost", "http://localhost"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 3. Initialisation automatique de la base de données SQLite
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// 4. Pipeline HTTP
app.UseCors("DefaultCors");
app.UseStaticFiles();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FraisRoute API v1");
        c.RoutePrefix = "swagger";
    });
}

// 5. Mapping des endpoints
app.MapGroup("/api/vehicles").MapVehicleEndpoints().WithTags("Véhicules");
app.MapGroup("/api/reports").MapExpenseReportEndpoints().WithTags("Notes de frais");
app.MapGroup("/api/reports").MapTripEndpoints().WithTags("Trajets");
app.MapGroup("/api/reports").MapReceiptEndpoints().WithTags("Justificatifs & Reçus");
app.MapGroup("/api/calculator").MapCalculatorEndpoints().WithTags("Calculateur IK");
app.MapGroup("/api/addresses").MapAddressEndpoints().WithTags("Adresses & Distances");

app.MapGet("/", () => Results.Redirect("/swagger"));

await app.RunAsync();

public partial class Program
{
    protected Program()
    {
    }
}
