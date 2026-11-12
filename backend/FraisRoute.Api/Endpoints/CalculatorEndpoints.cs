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
        .WithSummary("Calcule l'indemnité kilométrique selon le barème officiel français ou un taux personnalisé");

        group.MapGet("/ik", (
            decimal distanceKm,
            bool isRoundTrip = false,
            VehicleType vehicleType = VehicleType.Car,
            int fiscalHorsepower = 5,
            bool isElectric = false,
            decimal? customRatePerKm = null,
            decimal previousAnnualKm = 0m) =>
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
                PreviousAnnualKm: previousAnnualKm
            );

            return Results.Ok(BuildSimulationResponse(req));
        })
        .WithName("CalculateIkGet")
        .WithSummary("Simule une indemnité kilométrique via paramètres de requête GET");

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

        decimal amount = FrenchFiscalScaleCalculator.CalculateTripAmount(
            tripDistanceKm: totalKm,
            vehicleType: req.VehicleType,
            fiscalCv: req.FiscalHorsepower,
            isElectric: req.IsElectric,
            customFixedRate: req.CustomRatePerKm,
            previousAnnualKm: req.PreviousAnnualKm
        );

        bool hasCustomRate = req.CustomRatePerKm.HasValue && req.CustomRatePerKm.Value > 0;
        decimal baseRate = hasCustomRate
            ? req.CustomRatePerKm!.Value
            : FrenchFiscalScaleCalculator.GetBaseRatePerKm(req.VehicleType, req.FiscalHorsepower, req.IsElectric);

        string energyLabel = req.IsElectric ? "+20% électrique" : "thermique/hybride";
        string description = hasCustomRate
            ? $"Taux entreprise négocié de {req.CustomRatePerKm!.Value:N3} €/km"
            : $"Barème fiscal officiel Urssaf ({req.FiscalHorsepower} CV, {energyLabel})";

        return new CalculateMileageResponse(
            UnitDistanceKm: req.DistanceKm,
            IsRoundTrip: req.IsRoundTrip,
            TotalDistanceKm: totalKm,
            RatePerKm: baseRate,
            CalculatedAmount: amount,
            CalculationMethodDescription: description
        );
    }
}
