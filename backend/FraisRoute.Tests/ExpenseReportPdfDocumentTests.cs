namespace FraisRoute.Tests;

using System.Text;
using FraisRoute.Api.Models;
using FraisRoute.Api.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Xunit;

public class ExpenseReportPdfDocumentTests
{
    public ExpenseReportPdfDocumentTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public void GeneratePdf_EmptyDraftReport_ProducesValidPdfBuffer()
    {
        var emptyReport = new ExpenseReport
        {
            Id = 1,
            Title = "Note vide - Janvier 2026",
            EmployeeName = string.Empty,
            CompanyName = string.Empty,
            PeriodStart = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = ReportStatus.Draft,
            ReimbursementRatePercentage = 100m
        };

        var document = new ExpenseReportPdfDocument(emptyReport);
        Assert.Same(emptyReport, document.Report);
        Assert.NotNull(document.GetMetadata());
        Assert.NotNull(document.GetSettings());

        byte[] pdfBytes = document.GeneratePdf();

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);
        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Theory]
    [InlineData(ReportStatus.Draft, 100)]
    [InlineData(ReportStatus.Submitted, 80)]
    [InlineData(ReportStatus.Approved, 100)]
    [InlineData(ReportStatus.Rejected, 75)]
    public void GeneratePdf_FullReportWithTripsReceiptsAndStatuses_ProducesValidPdfBuffer(
        ReportStatus status,
        decimal reimbursementRate)
    {
        var electricVehicle = new Vehicle
        {
            Id = 2,
            Name = "Tesla Model 3",
            LicensePlate = "EV-999-FR",
            Type = VehicleType.Car,
            FiscalHorsepower = 6,
            IsElectric = true
        };

        var report = new ExpenseReport
        {
            Id = 42,
            Title = "Frais de route - Mars 2026",
            EmployeeName = "Alexy Martin",
            EmployeeEmail = "alexy@entreprise.fr",
            CompanyName = "TechCorp SAS",
            PeriodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = status,
            ReimbursementRatePercentage = reimbursementRate,
            Trips =
            [
                new Trip
                {
                    Id = 1,
                    Date = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                    Purpose = "Rendez-vous client",
                    ClientOrLocation = "Client Lyon",
                    DepartureAddress = "Paris",
                    ArrivalAddress = "Lyon",
                    DistanceKm = 120m,
                    IsRoundTrip = true,
                    Vehicle = electricVehicle,
                    CalculationMethod = RateType.FiscalScale,
                    CalculatedAmount = 191.52m
                },
                new Trip
                {
                    Id = 2,
                    Date = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc),
                    Purpose = "Intervention site",
                    ClientOrLocation = string.Empty,
                    DepartureAddress = "Lyon",
                    ArrivalAddress = "Grenoble",
                    DistanceKm = 110m,
                    IsRoundTrip = false,
                    CalculationMethod = RateType.CustomFixedRate,
                    CustomRatePerKm = 0.45m,
                    CalculatedAmount = 49.50m
                }
            ],
            Receipts =
            [
                new ReceiptExpense
                {
                    Id = 1,
                    Date = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Toll,
                    Merchant = "APRR Autoroute A6",
                    AmountTtc = 36.40m,
                    VatRate = 20m,
                    VatAmount = 6.07m,
                    ReceiptImagePath = "/uploads/receipts/toll.jpg"
                },
                new ReceiptExpense
                {
                    Id = 2,
                    Date = new DateTime(2026, 3, 6, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Parking,
                    Merchant = "Indigo Part-Dieu",
                    AmountTtc = 18.00m,
                    VatRate = 20m,
                    VatAmount = 3.00m
                },
                new ReceiptExpense
                {
                    Id = 3,
                    Date = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Meal,
                    Merchant = "Brasserie Georges",
                    AmountTtc = 25.00m,
                    VatRate = 10m,
                    VatAmount = 2.27m
                },
                new ReceiptExpense
                {
                    Id = 4,
                    Date = new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Fuel,
                    Merchant = "TotalEnergies",
                    AmountTtc = 65.00m
                },
                new ReceiptExpense
                {
                    Id = 5,
                    Date = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Hotel,
                    Merchant = "Ibis Styles",
                    AmountTtc = 95.00m
                },
                new ReceiptExpense
                {
                    Id = 6,
                    Date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Other,
                    Merchant = "Fournitures Bureau",
                    AmountTtc = 12.50m
                }
            ]
        };

        var document = new ExpenseReportPdfDocument(report);
        byte[] pdfBytes = document.GeneratePdf();

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1500);
        string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    [InlineData("fr", "fr")]
    [InlineData("unknown", "fr")]
    public void GeneratePdf_WithLanguageAndDynamicLocaleRegistration_ProducesValidPdfBuffer(
        string requestedLang,
        string expectedResolvedLang)
    {
        var report = new ExpenseReport
        {
            Id = 77,
            Title = "March 2026 Travel Expenses",
            EmployeeName = "Jane Doe",
            CompanyName = "Global Corp",
            CountryScale = "UK_HMRC",
            Currency = "GBP",
            PeriodStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            PeriodEnd = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc),
            Status = ReportStatus.Approved,
            ReimbursementRatePercentage = 90m,
            Trips =
            [
                new Trip
                {
                    Id = 1,
                    Date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    Purpose = "Client Visit London",
                    DepartureAddress = "London",
                    ArrivalAddress = "Oxford",
                    DistanceKm = 90m,
                    IsRoundTrip = true,
                    CalculatedAmount = 81.00m
                }
            ],
            Receipts =
            [
                new ReceiptExpense
                {
                    Id = 1,
                    Date = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                    Category = ReceiptCategory.Toll,
                    Merchant = "M6 Toll",
                    AmountTtc = 15.00m
                }
            ]
        };

        var doc = new ExpenseReportPdfDocument(report, requestedLang);
        Assert.Equal(expectedResolvedLang, doc.Language);

        byte[] pdfBytes = doc.GeneratePdf();
        Assert.True(pdfBytes.Length > 1000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(pdfBytes, 0, 5));
    }
}
