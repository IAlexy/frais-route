namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CalculateMileageRequest(
    decimal DistanceKm,
    bool IsRoundTrip,
    VehicleType VehicleType,
    int FiscalHorsepower,
    bool IsElectric,
    decimal? CustomRatePerKm,
    decimal PreviousAnnualKm = 0m
);

public record CalculateMileageResponse(
    decimal UnitDistanceKm,
    bool IsRoundTrip,
    decimal TotalDistanceKm,
    decimal RatePerKm,
    decimal CalculatedAmount,
    string CalculationMethodDescription
);
