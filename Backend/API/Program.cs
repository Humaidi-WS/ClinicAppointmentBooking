using ClinicAppointmentBooking.API.Data;
using ClinicAppointmentBooking.API.Interfaces;
using ClinicAppointmentBooking.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Configure OpenAPI 3.0 document generation
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Clinic Appointment Booking API",
            Version = "v1",
            Description = "A RESTful API for managing clinic doctor appointments, querying selectable dates and doctor schedules, booking 30-minute time slots, and looking up or cancelling appointments."
        };

        document.Servers = new List<OpenApiServer>
        {
            new() { Url = "http://localhost:5246", Description = "Local Development (HTTP)" },
            new() { Url = "https://localhost:7036", Description = "Local Development (HTTPS)" },
            new() { Url = "http://localhost:8080", Description = "Docker Container" }
        };

        return Task.CompletedTask;
    });
});

// Persist data in local SQLite file (clinic.db)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=clinic.db"));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAppointment, AppointmentService>();

// Allow Angular 21+ dev server (http://localhost:4200)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Create SQLite database & seed initial doctors if empty
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Doctors.Any())
    {
        db.Doctors.AddRange(
            new Doctor { FirstName = "James", LastName = "Anderson", Specialty = "Cardiology" },
            new Doctor { FirstName = "Sarah", LastName = "Mitchell", Specialty = "Dermatology" },
            new Doctor { FirstName = "Michael", LastName = "Roberts", Specialty = "Neurology" },
            new Doctor { FirstName = "Emily", LastName = "Carter", Specialty = "Pediatrics" },
            new Doctor { FirstName = "Daniel", LastName = "Wilson", Specialty = "Orthopedics" }
        );
        db.SaveChanges();
    }
}

app.UseCors("AngularClient");

// Map OpenAPI JSON endpoint (/openapi/v1.json)
app.MapOpenApi();

// Legacy swagger.json compatibility redirect
app.MapGet("/swagger/v1/swagger.json", () => Results.Redirect("/openapi/v1.json"));

// Interactive Swagger UI documentation endpoints
app.MapGet("/swagger", () => Results.Content(SwaggerUiHelper.Html, "text/html"));
app.MapGet("/swagger/index.html", () => Results.Content(SwaggerUiHelper.Html, "text/html"));
app.MapGet("/docs", () => Results.Content(SwaggerUiHelper.Html, "text/html"));

app.MapControllers();
app.Run();

public partial class Program { }

internal static class SwaggerUiHelper
{
    public const string Html = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <title>Clinic Appointment Booking API - Swagger UI</title>
          <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5.18.2/swagger-ui.css" />
          <link rel="icon" type="image/png" href="https://unpkg.com/swagger-ui-dist@5.18.2/favicon-32x32.png" sizes="32x32" />
          <style>
            html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }
            *, *:before, *:after { box-sizing: inherit; }
            body { margin: 0; background: #fafafa; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; }
            .topbar { display: none; }
            .header-banner {
              background: #1976d2;
              color: white;
              padding: 16px 24px;
              box-shadow: 0 2px 4px rgba(0,0,0,0.15);
            }
            .header-banner h1 { margin: 0; font-size: 20px; font-weight: 600; }
            .header-banner p { margin: 4px 0 0; font-size: 13px; opacity: 0.9; }
          </style>
        </head>
        <body>
          <div class="header-banner">
            <h1>Clinic Appointment Booking API Documentation</h1>
            <p>Interactive OpenAPI / Swagger UI interface</p>
          </div>
          <div id="swagger-ui"></div>
          <script src="https://unpkg.com/swagger-ui-dist@5.18.2/swagger-ui-bundle.js"></script>
          <script src="https://unpkg.com/swagger-ui-dist@5.18.2/swagger-ui-standalone-preset.js"></script>
          <script>
            window.onload = function() {
              SwaggerUIBundle({
                url: "/openapi/v1.json",
                dom_id: '#swagger-ui',
                deepLinking: true,
                presets: [
                  SwaggerUIBundle.presets.apis,
                  SwaggerUIStandalonePreset
                ],
                plugins: [
                  SwaggerUIBundle.plugins.DownloadUrl
                ],
                layout: "StandaloneLayout",
                displayRequestDuration: true,
                docExpansion: "list",
                filter: true
              });
            };
          </script>
        </body>
        </html>
        """;
}
