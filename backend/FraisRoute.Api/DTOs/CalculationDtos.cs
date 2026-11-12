namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CalculateMileageRequest(
    decimal DistanceKm,
    bool IsRoundTrip,
    VehicleType VehicleType,
    int FiscalHorsepower,
    bool IsElectric,
    decimal? CustomRatePerKm,
    decimal PreviousAnnualKm = 0m,
    string CountryScale = "FR_URSSAF",
    string? Currency = null
);

public record CalculateMileageResponse(
    decimal UnitDistanceKm,
    bool IsRoundTrip,
    decimal TotalDistanceKm,
    decimal RatePerKm,
    decimal CalculatedAmount,
    string CalculationMethodDescription,
    string CountryScale = "FR_URSSAF",
    string Currency = "EUR"
);

public record MileagePresetDto(
    string Code,
    string Name,
    string Country,
    string Currency,
    string CurrencySymbol,
    decimal DefaultRatePerKm,
    bool SupportsFiscalHorsepower,
    bool SupportsElectricBonus,
    string Description
);
