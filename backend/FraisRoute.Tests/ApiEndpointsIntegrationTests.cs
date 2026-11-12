namespace FraisRoute.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FraisRoute.Api.Data;
using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

public sealed class ApiEndpointsIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly string _tempWebRoot;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiEndpointsIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _tempWebRoot = Path.Combine(Path.GetTempPath(), $"fraisroute_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWebRoot);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseWebRoot(_tempWebRoot);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_tempWebRoot))
        {
            Directory.Delete(_tempWebRoot, recursive: true);
        }
    }

    [Fact]
    public async Task RootEndpoint_RedirectsToSwagger()
    {
        var response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/swagger", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Vehicles_Crud_DefaultSwitch_AndValidation()
    {
        // 1. GET initial seeded vehicles
        var initialVehicles = await _client.GetFromJsonAsync<List<Vehicle>>("/api/vehicles");
        Assert.NotNull(initialVehicles);
        Assert.Equal(2, initialVehicles.Count);
        Assert.Contains(initialVehicles, v => v.Name == "Peugeot 208" && v.IsDefault);

        // 2. GET by ID (existing and 404)
        var v1 = await _client.GetFromJsonAsync<Vehicle>("/api/vehicles/1");
        Assert.NotNull(v1);
        Assert.Equal("Peugeot 208", v1.Name);

        var notFoundGet = await _client.GetAsync("/api/vehicles/999");
        Assert.Equal(HttpStatusCode.NotFound, notFoundGet.StatusCode);

        // 3. POST invalid vehicle -> 400 BadRequest
        var badVehicleDto = new CreateVehicleDto("", "AA-111-BB", VehicleType.Car, 0, false, null, false);
        var badPostResponse = await _client.PostAsJsonAsync("/api/vehicles", badVehicleDto);
        Assert.Equal(HttpStatusCode.BadRequest, badPostResponse.StatusCode);

        // 4. POST new default vehicle -> switches previous default off
        var newVehicleDto = new CreateVehicleDto(
            Name: "Renault Mégane E-Tech",
            LicensePlate: "ab-123-cd",
            Type: VehicleType.Car,
            FiscalHorsepower: 5,
            IsElectric: true,
            DefaultCustomRatePerKm: null,
            IsDefault: true
        );

        var createRes = await _client.PostAsJsonAsync("/api/vehicles", newVehicleDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdVehicle = await createRes.Content.ReadFromJsonAsync<Vehicle>();
        Assert.NotNull(createdVehicle);
        Assert.Equal("AB-123-CD", createdVehicle.LicensePlate);
        Assert.True(createdVehicle.IsDefault);

        var afterCreateList = await _client.GetFromJsonAsync<List<Vehicle>>("/api/vehicles");
        Assert.NotNull(afterCreateList);
        Assert.Single(afterCreateList.Where(v => v.IsDefault));
        Assert.Equal(createdVehicle.Id, afterCreateList.First(v => v.IsDefault).Id);

        // 5. PUT update vehicle (make vehicle 2 default, check 404 & 400)
        var badPutRes = await _client.PutAsJsonAsync($"/api/vehicles/{createdVehicle.Id}", badVehicleDto);
        Assert.Equal(HttpStatusCode.BadRequest, badPutRes.StatusCode);

        var validUpdateDto = new UpdateVehicleDto("Tesla Model 3 LR", "EV-999-FR", VehicleType.Car, 7, true, null, true);
        var notFoundPut = await _client.PutAsJsonAsync("/api/vehicles/999", validUpdateDto);
        Assert.Equal(HttpStatusCode.NotFound, notFoundPut.StatusCode);

        var putRes = await _client.PutAsJsonAsync("/api/vehicles/2", validUpdateDto);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);
        var updatedV2 = await putRes.Content.ReadFromJsonAsync<Vehicle>();
        Assert.NotNull(updatedV2);
        Assert.Equal("Tesla Model 3 LR", updatedV2.Name);
        Assert.True(updatedV2.IsDefault);

        // 6. DELETE default vehicle -> automatically promotes another remaining vehicle as default
        var deleteDefaultRes = await _client.DeleteAsync("/api/vehicles/2");
        Assert.Equal(HttpStatusCode.NoContent, deleteDefaultRes.StatusCode);

        var remainingVehicles = await _client.GetFromJsonAsync<List<Vehicle>>("/api/vehicles");
        Assert.NotNull(remainingVehicles);
        Assert.Equal(2, remainingVehicles.Count);
        Assert.Single(remainingVehicles.Where(v => v.IsDefault));

        var notFoundDelete = await _client.DeleteAsync("/api/vehicles/999");
        Assert.Equal(HttpStatusCode.NotFound, notFoundDelete.StatusCode);
    }

    [Fact]
    public async Task ExpenseReports_Crud_Conflict409_StatusPatch_AndPdfExport()
    {
        // 1. POST invalid report -> 400
        var invalidReport = new CreateExpenseReportDto(
            Title: "",
            EmployeeName: "Alexy",
            EmployeeEmail: null,
            CompanyName: "Acme",
            PeriodStart: new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            ReimbursementRatePercentage: 100m
        );
        var badRes = await _client.PostAsJsonAsync("/api/reports", invalidReport);
        Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);

        // 2. POST valid report
        var validDto = new CreateExpenseReportDto(
            Title: "Frais Mai 2026",
            EmployeeName: "Alexy Martin",
            EmployeeEmail: "alexy@acme.fr",
            CompanyName: "Acme Corp",
            PeriodStart: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 5, 31, 0, 0, 0, DateTimeKind.Utc),
            ReimbursementRatePercentage: 80m
        );
        var createRes = await _client.PostAsJsonAsync("/api/reports", validDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await createRes.Content.ReadFromJsonAsync<ExpenseReport>();
        Assert.NotNull(created);
        Assert.Equal(ReportStatus.Draft, created.Status);

        // 3. POST duplicate month/year for same employee -> 409 Conflict
        var duplicateRes = await _client.PostAsJsonAsync("/api/reports", validDto);
        Assert.Equal(HttpStatusCode.Conflict, duplicateRes.StatusCode);

        // 4. GET /api/reports & GET /api/reports/{id}
        var list = await _client.GetFromJsonAsync<List<ExpenseReportSummaryDto>>("/api/reports");
        Assert.NotNull(list);
        Assert.Single(list);

        var detail = await _client.GetFromJsonAsync<ExpenseReportDetailDto>($"/api/reports/{created.Id}");
        Assert.NotNull(detail);
        Assert.Equal("Frais Mai 2026", detail.Title);

        var notFoundDetail = await _client.GetAsync("/api/reports/999");
        Assert.Equal(HttpStatusCode.NotFound, notFoundDetail.StatusCode);

        // 5. PUT /api/reports/{id} & PATCH /api/reports/{id}/status
        var updateDto = new UpdateExpenseReportDto(
            Title: "Frais Mai 2026 (Mis à jour)",
            EmployeeName: "Alexy Martin",
            EmployeeEmail: "alexy@acme.fr",
            CompanyName: "Acme Corp",
            PeriodStart: validDto.PeriodStart,
            PeriodEnd: validDto.PeriodEnd,
            Status: ReportStatus.Submitted,
            ReimbursementRatePercentage: 100m
        );
        var putRes = await _client.PutAsJsonAsync($"/api/reports/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, putRes.StatusCode);

        var patchRes = await _client.PatchAsJsonAsync(
            $"/api/reports/{created.Id}/status",
            new UpdateReportStatusDto(ReportStatus.Approved));
        Assert.Equal(HttpStatusCode.OK, patchRes.StatusCode);
        var patched = await patchRes.Content.ReadFromJsonAsync<ExpenseReport>();
        Assert.NotNull(patched);
        Assert.Equal(ReportStatus.Approved, patched.Status);

        var notFoundPatch = await _client.PatchAsJsonAsync(
            "/api/reports/999/status",
            new UpdateReportStatusDto(ReportStatus.Approved));
        Assert.Equal(HttpStatusCode.NotFound, notFoundPatch.StatusCode);

        // 6. GET /api/reports/{id}/pdf
        var pdfRes = await _client.GetAsync($"/api/reports/{created.Id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfRes.StatusCode);
        Assert.Equal("application/pdf", pdfRes.Content.Headers.ContentType?.MediaType);
        byte[] pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        Assert.True(pdfBytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));

        var notFoundPdf = await _client.GetAsync("/api/reports/999/pdf");
        Assert.Equal(HttpStatusCode.NotFound, notFoundPdf.StatusCode);

        // 7. DELETE /api/reports/{id}
        var delRes = await _client.DeleteAsync($"/api/reports/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        var delNotFound = await _client.DeleteAsync($"/api/reports/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, delNotFound.StatusCode);
    }

    [Fact]
    public async Task Trips_AddOneWayAndRoundTrip_RecalculatesReportTotalsAndAnnualCumulative()
    {
        // Create report
        var reportRes = await _client.PostAsJsonAsync("/api/reports", new CreateExpenseReportDto(
            Title: "Frais Juin 2026",
            EmployeeName: "Alexy Martin",
            EmployeeEmail: null,
            CompanyName: "Acme",
            PeriodStart: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc),
            ReimbursementRatePercentage: 100m
        ));
        var report = await reportRes.Content.ReadFromJsonAsync<ExpenseReport>();
        Assert.NotNull(report);

        // Invalid trip -> 400
        var invalidTrip = new CreateTripDto(
            Date: new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            Purpose: "",
            ClientOrLocation: "Lyon",
            DepartureAddress: "Paris",
            ArrivalAddress: "Lyon",
            DistanceKm: 0m,
            IsRoundTrip: false,
            VehicleId: 1,
            CalculationMethod: RateType.FiscalScale,
            CustomRatePerKm: null,
            Notes: null
        );
        var badTripRes = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/trips", invalidTrip);
        Assert.Equal(HttpStatusCode.BadRequest, badTripRes.StatusCode);

        // 1. Add first trip: 2475 km round-trip (= 4950 km) with Vehicle 1 (5 CV thermal, 0.636/km)
        // Amount = 4950 * 0.636 = 3148.20 €
        var trip1Dto = new CreateTripDto(
            Date: new DateTime(2026, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            Purpose: "Tournée nationale",
            ClientOrLocation: "Clients Sud",
            DepartureAddress: "Paris",
            ArrivalAddress: "Marseille",
            DistanceKm: 2475m,
            IsRoundTrip: true,
            VehicleId: 1,
            CalculationMethod: RateType.FiscalScale,
            CustomRatePerKm: null,
            Notes: "Premier grand déplacement"
        );
        var t1Res = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/trips", trip1Dto);
        Assert.Equal(HttpStatusCode.Created, t1Res.StatusCode);
        var t1 = await t1Res.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(t1);
        Assert.Equal(4950m, t1.TotalDistanceKm);
        Assert.Equal(3148.20m, t1.CalculatedAmount);

        // 2. Add second trip in same year: 100 km one-way with Vehicle 1
        // Cumulative goes from 4950 km to 5050 km (crosses 5000 km bracket!) -> differential amount = 49.65 €
        var trip2Dto = new CreateTripDto(
            Date: new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            Purpose: "Visite client",
            ClientOrLocation: "Orléans",
            DepartureAddress: "Paris",
            ArrivalAddress: "Orléans",
            DistanceKm: 100m,
            IsRoundTrip: false,
            VehicleId: 1,
            CalculationMethod: RateType.FiscalScale,
            CustomRatePerKm: null,
            Notes: null
        );
        var t2Res = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/trips", trip2Dto);
        Assert.Equal(HttpStatusCode.Created, t2Res.StatusCode);
        var t2 = await t2Res.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(t2);
        Assert.Equal(100m, t2.TotalDistanceKm);
        Assert.Equal(49.65m, t2.CalculatedAmount);

        // 3. Verify report totals updated automatically
        var updatedReport = await _client.GetFromJsonAsync<ExpenseReportDetailDto>($"/api/reports/{report.Id}");
        Assert.NotNull(updatedReport);
        Assert.Equal(5050m, updatedReport.TotalDistanceKm);
        Assert.Equal(3197.85m, updatedReport.TotalMileageAmount);

        // 4. Update trip 2 with CustomFixedRate (0.50 €/km * 100 km = 50.00 €)
        var updateTripDto = new UpdateTripDto(
            Date: trip2Dto.Date,
            Purpose: "Visite client forfait",
            ClientOrLocation: "Orléans",
            DepartureAddress: "Paris",
            ArrivalAddress: "Orléans",
            DistanceKm: 100m,
            IsRoundTrip: false,
            VehicleId: 1,
            CalculationMethod: RateType.CustomFixedRate,
            CustomRatePerKm: 0.50m,
            Notes: "Forfait"
        );
        var putTripRes = await _client.PutAsJsonAsync($"/api/reports/{report.Id}/trips/{t2.Id}", updateTripDto);
        Assert.Equal(HttpStatusCode.OK, putTripRes.StatusCode);
        var updatedT2 = await putTripRes.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(updatedT2);
        Assert.Equal(50.00m, updatedT2.CalculatedAmount);

        // 5. Delete trip 2 and check report total
        var delRes = await _client.DeleteAsync($"/api/reports/{report.Id}/trips/{t2.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        var afterDeleteReport = await _client.GetFromJsonAsync<ExpenseReportDetailDto>($"/api/reports/{report.Id}");
        Assert.NotNull(afterDeleteReport);
        Assert.Equal(4950m, afterDeleteReport.TotalDistanceKm);
        Assert.Equal(3148.20m, afterDeleteReport.TotalMileageAmount);
    }

    [Fact]
    public async Task Receipts_AutoVatCalculation_UploadImage_AndDelete()
    {
        // Create report with 80% reimbursement rate
        var reportRes = await _client.PostAsJsonAsync("/api/reports", new CreateExpenseReportDto(
            Title: "Frais Juillet 2026",
            EmployeeName: "Alexy Martin",
            EmployeeEmail: null,
            CompanyName: "Acme",
            PeriodStart: new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 7, 31, 0, 0, 0, DateTimeKind.Utc),
            ReimbursementRatePercentage: 80m
        ));
        var report = await reportRes.Content.ReadFromJsonAsync<ExpenseReport>();
        Assert.NotNull(report);

        // Invalid receipt -> 400
        var badReceipt = new CreateReceiptExpenseDto(DateTime.UtcNow, ReceiptCategory.Toll, "", 0m, 20m, null, null);
        var badRes = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/receipts", badReceipt);
        Assert.Equal(HttpStatusCode.BadRequest, badRes.StatusCode);

        // Add receipt with TTC = 120.00 € and VatRate = 20% (VatAmount null -> auto-calculated to 20.00 €)
        var receiptDto = new CreateReceiptExpenseDto(
            Date: new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            Category: ReceiptCategory.Hotel,
            Merchant: "Hôtel Mercure",
            AmountTtc: 120.00m,
            VatRate: 20.0m,
            VatAmount: null,
            Notes: "Nuitée déplacement"
        );
        var addRes = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/receipts", receiptDto);
        Assert.Equal(HttpStatusCode.Created, addRes.StatusCode);
        var createdReceipt = await addRes.Content.ReadFromJsonAsync<ReceiptExpense>();
        Assert.NotNull(createdReceipt);
        Assert.Equal(20.00m, createdReceipt.VatAmount);

        // Upload receipt image
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xE0]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(fileContent, "file", "ticket.jpg");

        var uploadRes = await _client.PostAsync($"/api/reports/{report.Id}/receipts/{createdReceipt.Id}/upload", form);
        Assert.Equal(HttpStatusCode.OK, uploadRes.StatusCode);

        // Verify report totals updated (120 € gross -> 96 € reimbursable at 80%)
        var detail = await _client.GetFromJsonAsync<ExpenseReportDetailDto>($"/api/reports/{report.Id}");
        Assert.NotNull(detail);
        Assert.Equal(120.00m, detail.TotalReceiptsAmount);
        Assert.Equal(120.00m, detail.TotalGrossAmount);
        Assert.Equal(96.00m, detail.TotalReimbursableAmount);

        // Delete receipt
        var delRes = await _client.DeleteAsync($"/api/reports/{report.Id}/receipts/{createdReceipt.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        var detailAfter = await _client.GetFromJsonAsync<ExpenseReportDetailDto>($"/api/reports/{report.Id}");
        Assert.NotNull(detailAfter);
        Assert.Equal(0m, detailAfter.TotalReceiptsAmount);
    }

    [Fact]
    public async Task CalculatorEndpoints_GetIk_PostSimulate_AndGetRates()
    {
        // GET /api/calculator/ik
        var ikRes = await _client.GetFromJsonAsync<CalculateMileageResponse>(
            "/api/calculator/ik?distanceKm=50&isRoundTrip=true&vehicleType=Car&fiscalHorsepower=5&isElectric=true");
        Assert.NotNull(ikRes);
        Assert.Equal(100m, ikRes.TotalDistanceKm);
        Assert.Equal(76.32m, ikRes.CalculatedAmount);

        // GET /api/calculator/ik invalid -> 400
        var badIkRes = await _client.GetAsync("/api/calculator/ik?distanceKm=0");
        Assert.Equal(HttpStatusCode.BadRequest, badIkRes.StatusCode);

        // POST /api/calculator/simulate
        var simReq = new CalculateMileageRequest(85m, true, VehicleType.Car, 5, false, null);
        var simRes = await _client.PostAsJsonAsync("/api/calculator/simulate", simReq);
        Assert.Equal(HttpStatusCode.OK, simRes.StatusCode);
        var simData = await simRes.Content.ReadFromJsonAsync<CalculateMileageResponse>();
        Assert.NotNull(simData);
        Assert.Equal(170m, simData.TotalDistanceKm);
        Assert.Equal(108.12m, simData.CalculatedAmount);

        // GET /api/calculator/rates
        var ratesRes = await _client.GetAsync("/api/calculator/rates");
        Assert.Equal(HttpStatusCode.OK, ratesRes.StatusCode);

        // GET /api/calculator/presets
        var presets = await _client.GetFromJsonAsync<List<MileagePresetDto>>("/api/calculator/presets");
        Assert.NotNull(presets);
        Assert.Equal(7, presets.Count);
        Assert.Contains(presets, p => p.Code == "CH_TCS" && p.Currency == "CHF");

        // Simulate international scale (Switzerland CH_TCS, 100 km -> 70.00 CHF)
        var chSimRes = await _client.PostAsJsonAsync(
            "/api/calculator/simulate",
            new CalculateMileageRequest(100m, false, VehicleType.Car, 5, false, null, 0m, "CH_TCS", "CHF"));
        Assert.Equal(HttpStatusCode.OK, chSimRes.StatusCode);
        var chSimData = await chSimRes.Content.ReadFromJsonAsync<CalculateMileageResponse>();
        Assert.NotNull(chSimData);
        Assert.Equal(70.00m, chSimData.CalculatedAmount);
        Assert.Equal("CHF", chSimData.Currency);
    }

    [Fact]
    public async Task InternationalExpenseReport_WithCanadianScale_CalculatesTripsAndPdfInCad()
    {
        var reportRes = await _client.PostAsJsonAsync("/api/reports", new CreateExpenseReportDto(
            Title: "Frais Août 2026 - Montréal",
            EmployeeName: "Jean Tremblay",
            EmployeeEmail: "jean@montreal.ca",
            CompanyName: "Québec Tech Inc.",
            PeriodStart: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd: new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc),
            ReimbursementRatePercentage: 100m,
            CountryScale: "CA_CRA",
            Currency: "CAD"
        ));
        Assert.Equal(HttpStatusCode.Created, reportRes.StatusCode);
        var report = await reportRes.Content.ReadFromJsonAsync<ExpenseReport>();
        Assert.NotNull(report);
        Assert.Equal("CA_CRA", report.CountryScale);
        Assert.Equal("CAD", report.Currency);

        var tripRes = await _client.PostAsJsonAsync($"/api/reports/{report.Id}/trips", new CreateTripDto(
            Date: new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
            Purpose: "Visite client Québec",
            ClientOrLocation: "Québec",
            DepartureAddress: "Montréal, QC",
            ArrivalAddress: "Québec, QC",
            DistanceKm: 250m,
            IsRoundTrip: true, // 500 km * 0.70 CAD = 350.00 CAD
            VehicleId: 1,
            CalculationMethod: RateType.FiscalScale,
            CustomRatePerKm: null,
            Notes: null
        ));
        Assert.Equal(HttpStatusCode.Created, tripRes.StatusCode);
        var trip = await tripRes.Content.ReadFromJsonAsync<Trip>();
        Assert.NotNull(trip);
        Assert.Equal(350.00m, trip.CalculatedAmount);

        var pdfRes = await _client.GetAsync($"/api/reports/{report.Id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfRes.StatusCode);
        byte[] pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));

        var pdfEnRes = await _client.GetAsync($"/api/reports/{report.Id}/pdf?lang=en");
        Assert.Equal(HttpStatusCode.OK, pdfEnRes.StatusCode);
        Assert.Contains("Expense_Report", pdfEnRes.Content.Headers.ContentDisposition?.FileName ?? string.Empty);
        byte[] pdfEnBytes = await pdfEnRes.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfEnBytes, 0, 5));
    }

    [Fact]
    public async Task AddressEndpoints_Search_Autocomplete_AndDistanceValidation()
    {
        // GET /api/addresses/search validation -> 400 when query too short
        var badSearch = await _client.GetAsync("/api/addresses/search?q=a");
        Assert.Equal(HttpStatusCode.BadRequest, badSearch.StatusCode);

        // GET /api/addresses/autocomplete -> empty array when query too short
        var shortAuto = await _client.GetFromJsonAsync<List<object>>("/api/addresses/autocomplete?q=ab");
        Assert.NotNull(shortAuto);
        Assert.Empty(shortAuto);

        // GET /api/addresses/distance validation -> 400 when coordinates out of bounds or missing
        var missingCoords = await _client.GetAsync("/api/addresses/distance?fromLon=2.35");
        Assert.Equal(HttpStatusCode.BadRequest, missingCoords.StatusCode);

        var outOfBoundsCoords = await _client.GetAsync("/api/addresses/distance?fromLon=200&fromLat=95&toLon=2.35&toLat=48.85");
        Assert.Equal(HttpStatusCode.BadRequest, outOfBoundsCoords.StatusCode);

        // GET /api/addresses/distance valid coordinates (Paris -> Lyon)
        var validDistanceRes = await _client.GetAsync("/api/addresses/distance?fromLon=2.3522&fromLat=48.8566&toLon=4.8357&toLat=45.7640");
        Assert.Equal(HttpStatusCode.OK, validDistanceRes.StatusCode);
    }
}
