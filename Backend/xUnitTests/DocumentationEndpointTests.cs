using System.Net;
using ClinicAppointmentBooking.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClinicAppointmentBooking.Tests;

[Collection(ClinicApiCollection.Name)]
public sealed class DocumentationEndpointTests : ApiTestBase
{
    public DocumentationEndpointTests(ClinicApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task OpenApi_DescribesEveryAppointmentRoute()
    {
        var response = await Client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("Clinic Appointment Booking API", document);
        Assert.Contains("\"version\": \"v1\"", document);
        Assert.Contains("/api/Appointments/doctors", document);
        Assert.Contains("/api/Appointments/dates", document);
        Assert.Contains("/api/Appointments/doctors/{doctorId}/schedule", document);
        Assert.Contains("/api/Appointments/book", document);
        Assert.Contains("/api/Appointments/patients/{patientId}/appointments", document);
        Assert.Contains("/api/Appointments/cancel", document);
        Assert.Contains("http://localhost:5246", document);
        Assert.Contains("https://localhost:7036", document);
        Assert.Contains("http://localhost:8080", document);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/docs")]
    public async Task SwaggerUi_ReturnsInteractivePage(string path)
    {
        var response = await Client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("swagger-ui", html);
        Assert.Contains("/openapi/v1.json", html);
        Assert.Contains("Clinic Appointment Booking API Documentation", html);
    }

    [Fact]
    public async Task LegacySwaggerJson_RedirectsToOpenApi()
    {
        using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/openapi/v1.json", response.Headers.Location?.OriginalString);
    }
}
