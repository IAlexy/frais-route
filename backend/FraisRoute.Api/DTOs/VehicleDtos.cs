namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CreateVehicleDto(
    string Name,
    string LicensePlate,
    VehicleType Type,
    int FiscalHorsepower,
    bool IsElectric,
    decimal? DefaultCustomRatePerKm,
    bool IsDefault
);

public record UpdateVehicleDto(
    string Name,
    string LicensePlate,
    VehicleType Type,
    int FiscalHorsepower,
    bool IsElectric,
    decimal? DefaultCustomRatePerKm,
    bool IsDefault
);
