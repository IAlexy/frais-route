namespace FraisRoute.Api.Endpoints;

using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using FraisRoute.Api.Services;

public static class CalculatorEndpoints
{
    public static RouteGroupBuilder MapCalculatorEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/simulate", (CalculateMileageRequest req) =>
        {
            if (req.DistanceKm <= 0)
            {
                return Results.BadRequest("La distance doit être strictement positive.");
            }

            return Results.Ok(BuildSimulationResponse(req));
        })
        .WithName("SimulateMileage")
        .WithSummary("Calcule l'indemnité kilométrique selon le barème officiel choisi ou un taux personnalisé");

        group.MapGet("/ik", (
            decimal distanceKm,
            bool isRoundTrip = false,
            VehicleType vehicleType = VehicleType.Car,
            int fiscalHorsepower = 5,
            bool isElectric = false,
            decimal? customRatePerKm = null,
            decimal previousAnnualKm = 0m,
            string countryScale = "FR_URSSAF",
            string? currency = null) =>
        {
            if (distanceKm <= 0 || fiscalHorsepower <= 0)
            {
                return Results.BadRequest("Paramètres de simulation invalides.");
            }

            var req = new CalculateMileageRequest(
                DistanceKm: distanceKm,
                IsRoundTrip: isRoundTrip,
                VehicleType: vehicleType,
                FiscalHorsepower: fiscalHorsepower,
                IsElectric: isElectric,
                CustomRatePerKm: customRatePerKm,
                PreviousAnnualKm: previousAnnualKm,
                CountryScale: countryScale,
                Currency: currency
            );

            return Results.Ok(BuildSimulationResponse(req));
        })
        .WithName("CalculateIkGet")
        .WithSummary("Simule une indemnité kilométrique via paramètres de requête GET");

        group.MapGet("/presets", () =>
        {
            return Results.Ok(InternationalMileageCalculator.GetPresets());
        })
        .WithName("GetMileagePresets")
        .WithSummary("Retourne la liste des barèmes kilométriques internationaux supportés (FR, BE, CH, CA, UK, US, CUSTOM)");

        group.MapGet("/rates", () =>
        {
            var rates = new[]
            {
                new { Cv = 3, BaseRateThermal = 0.529m, BaseRateElectric = Math.Round(0.529m * 1.2m, 3) },
                new { Cv = 4, BaseRateThermal = 0.606m, BaseRateElectric = Math.Round(0.606m * 1.2m, 3) },
                new { Cv = 5, BaseRateThermal = 0.636m, BaseRateElectric = Math.Round(0.636m * 1.2m, 3) },
                new { Cv = 6, BaseRateThermal = 0.665m, BaseRateElectric = Math.Round(0.665m * 1.2m, 3) },
                new { Cv = 7, BaseRateThermal = 0.697m, BaseRateElectric = Math.Round(0.697m * 1.2m, 3) }
            };

            return Results.Ok(rates);
        })
        .WithName("GetStandardRates")
        .WithSummary("Retourne la grille de base du barème kilométrique automobile");

        return group;
    }

    private static CalculateMileageResponse BuildSimulationResponse(CalculateMileageRequest req)
    {
        decimal totalKm = req.IsRoundTrip ? req.DistanceKm * 2 : req.DistanceKm;
        var preset = InternationalMileageCalculator.GetPresetOrDefault(req.CountryScale);

        var (amount, effectiveRate, currency, description) = InternationalMileageCalculator.CalculateAllowance(
            countryScale: preset.Code,
            tripDistanceKm: totalKm,
            vehicleType: req.VehicleType,
            fiscalCv: req.FiscalHorsepower,
            isElectric: req.IsElectric,
            customFixedRate: req.CustomRatePerKm,
            previousAnnualKm: req.PreviousAnnualKm,
            overrideCurrency: req.Currency
        );

        return new CalculateMileageResponse(
            UnitDistanceKm: req.DistanceKm,
            IsRoundTrip: req.IsRoundTrip,
            TotalDistanceKm: totalKm,
            RatePerKm: effectiveRate,
            CalculatedAmount: amount,
            CalculationMethodDescription: description,
            CountryScale: preset.Code,
            Currency: currency
        );
    }
}
