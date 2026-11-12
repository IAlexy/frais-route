namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CreateTripDto(
    DateTime Date,
    string Purpose,
    string ClientOrLocation,
    string DepartureAddress,
    string ArrivalAddress,
    decimal DistanceKm,
    bool IsRoundTrip,
    int? VehicleId,
    RateType CalculationMethod,
    decimal? CustomRatePerKm,
    string? Notes
);

public record UpdateTripDto(
    DateTime Date,
    string Purpose,
    string ClientOrLocation,
    string DepartureAddress,
    string ArrivalAddress,
    decimal DistanceKm,
    bool IsRoundTrip,
    int? VehicleId,
    RateType CalculationMethod,
    decimal? CustomRatePerKm,
    string? Notes
);
