namespace FraisRoute.Tests;

using FraisRoute.Api.Models;
using FraisRoute.Api.Services;
using Xunit;

public class FrenchFiscalScaleCalculatorTests
{
    [Theory]
    [InlineData(2, 1000, false, 529.00)]
    [InlineData(3, 1000, false, 529.00)]
    [InlineData(4, 1000, false, 606.00)]
    [InlineData(5, 1000, false, 636.00)]
    [InlineData(6, 1000, false, 665.00)]
    [InlineData(7, 1000, false, 697.00)]
    [InlineData(9, 1000, false, 697.00)]
    public void CalculateCumulative_Car_Bracket1_AllHorsepowers(int cv, decimal km, bool isElectric, decimal expected)
    {
        Assert.True(km <= 5000m);
        decimal result = FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(
            totalAnnualKm: km,
            vehicleType: VehicleType.Car,
            fiscalCv: cv,
            isElectric: isElectric
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(3, 10000, false, 4225.00)] // (10000 * 0.316) + 1065
    [InlineData(4, 10000, false, 4730.00)] // (10000 * 0.340) + 1330
    [InlineData(5, 10000, false, 4965.00)] // (10000 * 0.357) + 1395
    [InlineData(6, 10000, false, 5197.00)] // (10000 * 0.374) + 1457
    [InlineData(7, 10000, false, 5455.00)] // (10000 * 0.394) + 1515
    public void CalculateCumulative_Car_Bracket2_5001To20000Km(int cv, decimal km, bool isElectric, decimal expected)
    {
        Assert.InRange(km, 5001m, 20000m);
        decimal result = FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(
            totalAnnualKm: km,
            vehicleType: VehicleType.Car,
            fiscalCv: cv,
            isElectric: isElectric
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(3, 25000, false, 9250.00)]  // 25000 * 0.370
    [InlineData(4, 25000, false, 10175.00)] // 25000 * 0.407
    [InlineData(5, 25000, false, 10675.00)] // 25000 * 0.427
    [InlineData(6, 25000, false, 11175.00)] // 25000 * 0.447
    [InlineData(7, 25000, false, 11750.00)] // 25000 * 0.470
    public void CalculateCumulative_Car_Bracket3_Above20000Km(int cv, decimal km, bool isElectric, decimal expected)
    {
        Assert.True(km > 20000m);
        decimal result = FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(
            totalAnnualKm: km,
            vehicleType: VehicleType.Car,
            fiscalCv: cv,
            isElectric: isElectric
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(3, 100, 63.48)]  // 52.90 * 1.20
    [InlineData(4, 100, 72.72)]  // 60.60 * 1.20
    [InlineData(5, 100, 76.32)]  // 63.60 * 1.20
    [InlineData(6, 100, 79.80)]  // 66.50 * 1.20
    [InlineData(7, 100, 83.64)]  // 69.70 * 1.20
    public void CalculateTrip_Car_Electric_Applies20PercentBonus(int cv, decimal km, decimal expected)
    {
        decimal amount = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: km,
            vehicleType: VehicleType.Car,
            fiscalCv: cv,
            isElectric: true
        );

        Assert.Equal(expected, amount);
    }

    [Theory]
    [InlineData(2, 2000, 790.00)]  // <= 2 CV, <= 3000 km: 2000 * 0.395
    [InlineData(2, 4000, 1287.00)] // <= 2 CV, <= 6000 km: 4000 * 0.099 + 891
    [InlineData(2, 7000, 1736.00)] // <= 2 CV, > 6000 km: 7000 * 0.248
    [InlineData(4, 2000, 936.00)]  // <= 5 CV, <= 3000 km: 2000 * 0.468
    [InlineData(4, 4000, 1486.00)] // <= 5 CV, <= 6000 km: 4000 * 0.082 + 1158
    [InlineData(4, 7000, 1925.00)] // <= 5 CV, > 6000 km: 7000 * 0.275
    [InlineData(6, 2000, 1212.00)] // > 5 CV, <= 3000 km: 2000 * 0.606
    [InlineData(6, 4000, 1899.00)] // > 5 CV, <= 6000 km: 4000 * 0.079 + 1583
    [InlineData(6, 7000, 2401.00)] // > 5 CV, > 6000 km: 7000 * 0.343
    public void CalculateCumulative_Motorcycle_AllBrackets(int cv, decimal km, decimal expected)
    {
        decimal result = FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(
            totalAnnualKm: km,
            vehicleType: VehicleType.Motorcycle,
            fiscalCv: cv,
            isElectric: false
        );

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1000, 315.00)] // <= 2000 km: 1000 * 0.315
    [InlineData(3000, 709.00)] // <= 5000 km: 3000 * 0.079 + 472
    [InlineData(6000, 1038.00)] // > 5000 km: 6000 * 0.173
    public void CalculateCumulative_Scooter_AllBrackets(decimal km, decimal expected)
    {
        decimal result = FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(
            totalAnnualKm: km,
            vehicleType: VehicleType.Scooter,
            fiscalCv: 1,
            isElectric: false
        );

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateTrip_CustomRate_OverridesFiscalScale()
    {
        decimal amount = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: 120m,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            customFixedRate: 0.45m
        );

        Assert.Equal(54.00m, amount);
    }

    [Fact]
    public void CalculateDifferentialTrip_TransitionFromBracket1ToBracket2()
    {
        // 5 CV thermique : 4950 km déjà faits, nouveau trajet de 100 km (total 5050 km)
        // Avant : 4950 * 0.636 = 3148.20 €
        // Après : (5050 * 0.357) + 1395 = 1802.85 + 1395 = 3197.85 €
        // Différence = 3197.85 - 3148.20 = 49.65 €
        decimal tripAmount = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: 100m,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            previousAnnualKm: 4950m
        );

        Assert.Equal(49.65m, tripAmount);
    }

    [Fact]
    public void CalculateDifferentialTrip_TransitionFromBracket2ToBracket3()
    {
        // 5 CV thermique : 19900 km déjà faits, nouveau trajet de 200 km (total 20100 km)
        // Avant (19900 km) : (19900 * 0.357) + 1395 = 7104.30 + 1395 = 8499.30 €
        // Après (20100 km) : 20100 * 0.427 = 8582.70 €
        // Différence = 8582.70 - 8499.30 = 83.40 €
        decimal tripAmount = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: 200m,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            previousAnnualKm: 19900m
        );

        Assert.Equal(83.40m, tripAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-250.5)]
    public void CalculateAmounts_ZeroOrNegativeDistance_ReturnsZero(decimal invalidDistance)
    {
        Assert.Equal(0m, FrenchFiscalScaleCalculator.CalculateCumulativeAnnualAmount(invalidDistance, VehicleType.Car, 5, false));
        Assert.Equal(0m, FrenchFiscalScaleCalculator.CalculateTripAmount(invalidDistance, VehicleType.Car, 5, false));
    }

    [Fact]
    public void CalculateTrip_NegativeOrZeroCustomRate_FallsBackToFiscalScale()
    {
        decimal withNegativeRate = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: 100m,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            customFixedRate: -0.50m
        );

        decimal withZeroRate = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: 100m,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            customFixedRate: 0m
        );

        Assert.Equal(63.60m, withNegativeRate);
        Assert.Equal(63.60m, withZeroRate);
    }

    [Theory]
    [InlineData(VehicleType.Car, 3, false, 0.529)]
    [InlineData(VehicleType.Car, 4, false, 0.606)]
    [InlineData(VehicleType.Car, 5, false, 0.636)]
    [InlineData(VehicleType.Car, 6, false, 0.665)]
    [InlineData(VehicleType.Car, 7, false, 0.697)]
    [InlineData(VehicleType.Car, 5, true, 0.763)]
    [InlineData(VehicleType.Motorcycle, 2, false, 0.395)]
    [InlineData(VehicleType.Motorcycle, 4, false, 0.468)]
    [InlineData(VehicleType.Motorcycle, 6, false, 0.606)]
    [InlineData(VehicleType.Scooter, 1, false, 0.315)]
    public void GetBaseRatePerKm_ReturnsExpectedOfficialRate(VehicleType type, int cv, bool isElectric, decimal expectedRate)
    {
        decimal rate = FrenchFiscalScaleCalculator.GetBaseRatePerKm(type, cv, isElectric);
        Assert.Equal(expectedRate, rate);
    }

    [Fact]
    public void ExpenseReport_ReimbursementRate_CalculatesProperly()
    {
        var report = new ExpenseReport
        {
            ReimbursementRatePercentage = 80m,
            Trips =
            [
                new Trip { DistanceKm = 50m, IsRoundTrip = true, CalculatedAmount = 60m }
            ],
            Receipts =
            [
                new ReceiptExpense { AmountTtc = 40m }
            ]
        };

        Assert.Equal(100m, report.TotalDistanceKm);
        Assert.Equal(60m, report.TotalMileageAmount);
        Assert.Equal(40m, report.TotalReceiptsAmount);
        Assert.Equal(100m, report.TotalGrossAmount);
        Assert.Equal(80.00m, report.TotalReimbursableAmount);
    }

    [Theory]
    [InlineData("BE_FPS", 100, 0, 42.80, "EUR")]
    [InlineData("CH_TCS", 100, 0, 70.00, "CHF")]
    [InlineData("US_IRS", 100, 0, 67.00, "USD")]
    [InlineData("CA_CRA", 100, 0, 70.00, "CAD")]
    [InlineData("CA_CRA", 200, 4900, 134.00, "CAD")] // 100 km * 0.70 + 100 km * 0.64 = 70 + 64 = 134 CAD
    [InlineData("UK_HMRC", 100, 0, 45.00, "GBP")]
    [InlineData("UK_HMRC", 200, 15900, 70.00, "GBP")] // 100 km * 0.45 + 100 km * 0.25 = 45 + 25 = 70 GBP
    [InlineData("CUSTOM", 100, 0, 50.00, "EUR")]
    public void InternationalMileageCalculator_CalculatesAllCountriesAndBrackets(
        string countryScale,
        decimal distanceKm,
        decimal previousAnnualKm,
        decimal expectedAmount,
        string expectedCurrency)
    {
        var (amount, _, currency, description) = InternationalMileageCalculator.CalculateAllowance(
            countryScale: countryScale,
            tripDistanceKm: distanceKm,
            vehicleType: VehicleType.Car,
            fiscalCv: 5,
            isElectric: false,
            previousAnnualKm: previousAnnualKm
        );

        Assert.Equal(expectedAmount, amount);
        Assert.Equal(expectedCurrency, currency);
        Assert.False(string.IsNullOrWhiteSpace(description));
    }

    [Fact]
    public void InternationalMileageCalculator_PresetsAndCurrencySymbols_AreValid()
    {
        var presets = InternationalMileageCalculator.GetPresets();
        Assert.Equal(7, presets.Count);
        Assert.Contains(presets, p => p.Code == "FR_URSSAF" && p.Currency == "EUR");
        Assert.Contains(presets, p => p.Code == "BE_FPS" && p.Currency == "EUR");
        Assert.Contains(presets, p => p.Code == "CH_TCS" && p.Currency == "CHF");
        Assert.Contains(presets, p => p.Code == "CA_CRA" && p.Currency == "CAD");
        Assert.Contains(presets, p => p.Code == "UK_HMRC" && p.Currency == "GBP");
        Assert.Contains(presets, p => p.Code == "US_IRS" && p.Currency == "USD");
        Assert.Contains(presets, p => p.Code == "CUSTOM");

        Assert.Equal("€", InternationalMileageCalculator.GetCurrencySymbol("EUR"));
        Assert.Equal("CHF", InternationalMileageCalculator.GetCurrencySymbol("CHF"));
        Assert.Equal("CA$", InternationalMileageCalculator.GetCurrencySymbol("CAD"));
        Assert.Equal("£", InternationalMileageCalculator.GetCurrencySymbol("GBP"));
        Assert.Equal("$", InternationalMileageCalculator.GetCurrencySymbol("USD"));
    }
}
