namespace FraisRoute.Api.Endpoints;

using FraisRoute.Api.Data;
using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using FraisRoute.Api.Services;
using Microsoft.EntityFrameworkCore;

public static class TripEndpoints
{
    public static RouteGroupBuilder MapTripEndpoints(this RouteGroupBuilder group)
    {
        // Liste les trajets d'une note de frais
        group.MapGet("/{reportId:int}/trips", async (int reportId, AppDbContext db) =>
        {
            var trips = await db.Trips
                .Include(t => t.Vehicle)
                .Where(t => t.ExpenseReportId == reportId)
                .OrderBy(t => t.Date)
                .ToListAsync();

            return Results.Ok(trips);
        })
        .WithName("GetTripsByReport");

        // Ajoute un trajet à une note de frais
        group.MapPost("/{reportId:int}/trips", async (int reportId, CreateTripDto dto, AppDbContext db) =>
        {
            if (dto.DistanceKm <= 0 || string.IsNullOrWhiteSpace(dto.Purpose))
            {
                return Results.BadRequest("Distance ou motif de déplacement invalide.");
            }

            var report = await db.ExpenseReports.FindAsync(reportId);
            if (report is null) return Results.NotFound("Note de frais introuvable");

            // Récupère le véhicule spécifié ou le véhicule par défaut
            Vehicle? vehicle;
            if (dto.VehicleId.HasValue)
            {
                vehicle = await db.Vehicles.FindAsync(dto.VehicleId.Value);
            }
            else
            {
                vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.IsDefault)
                          ?? await db.Vehicles.FirstOrDefaultAsync();
            }

            decimal totalKm = dto.IsRoundTrip ? dto.DistanceKm * 2 : dto.DistanceKm;
            decimal calculatedAmount;

            if (dto.CalculationMethod == RateType.CustomFixedRate && dto.CustomRatePerKm.HasValue)
            {
                calculatedAmount = Math.Round(totalKm * dto.CustomRatePerKm.Value, 2, MidpointRounding.AwayFromZero);
            }
            else if (vehicle is not null)
            {
                var existingAnnualTrips = await db.Trips
                    .Where(t => t.VehicleId == vehicle.Id && t.Date.Year == dto.Date.Year)
                    .ToListAsync();

                decimal previousAnnualKm = existingAnnualTrips.Sum(t => t.TotalDistanceKm);

                var (amount, _, _, _) = InternationalMileageCalculator.CalculateAllowance(
                    countryScale: report.CountryScale,
                    tripDistanceKm: totalKm,
                    vehicleType: vehicle.Type,
                    fiscalCv: vehicle.FiscalHorsepower,
                    isElectric: vehicle.IsElectric,
                    customFixedRate: vehicle.DefaultCustomRatePerKm,
                    previousAnnualKm: previousAnnualKm,
                    overrideCurrency: report.Currency
                );
                calculatedAmount = amount;
            }
            else
            {
                var (amount, _, _, _) = InternationalMileageCalculator.CalculateAllowance(
                    countryScale: report.CountryScale,
                    tripDistanceKm: totalKm,
                    vehicleType: VehicleType.Car,
                    fiscalCv: 5,
                    isElectric: false,
                    overrideCurrency: report.Currency
                );
                calculatedAmount = amount;
            }

            var trip = new Trip
            {
                ExpenseReportId = reportId,
                Date = dto.Date,
                Purpose = dto.Purpose.Trim(),
                ClientOrLocation = dto.ClientOrLocation,
                DepartureAddress = dto.DepartureAddress,
                ArrivalAddress = dto.ArrivalAddress,
                DistanceKm = dto.DistanceKm,
                IsRoundTrip = dto.IsRoundTrip,
                VehicleId = vehicle?.Id,
                CalculationMethod = dto.CalculationMethod,
                CustomRatePerKm = dto.CustomRatePerKm,
                CalculatedAmount = calculatedAmount,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            report.UpdatedAt = DateTime.UtcNow;
            db.Trips.Add(trip);
            await db.SaveChangesAsync();

            return Results.Created($"/api/reports/{reportId}/trips/{trip.Id}", trip);
        })
        .WithName("AddTripToReport");

        // Met à jour un trajet
        group.MapPut("/{reportId:int}/trips/{tripId:int}", async (int reportId, int tripId, UpdateTripDto dto, AppDbContext db) =>
        {
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.ExpenseReportId == reportId);
            if (trip is null) return Results.NotFound();

            var report = await db.ExpenseReports.FindAsync(reportId);

            Vehicle? vehicle = null;
            if (dto.VehicleId.HasValue)
            {
                vehicle = await db.Vehicles.FindAsync(dto.VehicleId.Value);
            }

            decimal totalKm = dto.IsRoundTrip ? dto.DistanceKm * 2 : dto.DistanceKm;
            decimal calculatedAmount;

            if (dto.CalculationMethod == RateType.CustomFixedRate && dto.CustomRatePerKm.HasValue)
            {
                calculatedAmount = Math.Round(totalKm * dto.CustomRatePerKm.Value, 2, MidpointRounding.AwayFromZero);
            }
            else if (vehicle is not null)
            {
                var (amount, _, _, _) = InternationalMileageCalculator.CalculateAllowance(
                    countryScale: report?.CountryScale,
                    tripDistanceKm: totalKm,
                    vehicleType: vehicle.Type,
                    fiscalCv: vehicle.FiscalHorsepower,
                    isElectric: vehicle.IsElectric,
                    customFixedRate: vehicle.DefaultCustomRatePerKm,
                    overrideCurrency: report?.Currency
                );
                calculatedAmount = amount;
            }
            else
            {
                var (amount, _, _, _) = InternationalMileageCalculator.CalculateAllowance(
                    countryScale: report?.CountryScale,
                    tripDistanceKm: totalKm,
                    vehicleType: VehicleType.Car,
                    fiscalCv: 5,
                    isElectric: false,
                    overrideCurrency: report?.Currency
                );
                calculatedAmount = amount;
            }

            trip.Date = dto.Date;
            trip.Purpose = dto.Purpose;
            trip.ClientOrLocation = dto.ClientOrLocation;
            trip.DepartureAddress = dto.DepartureAddress;
            trip.ArrivalAddress = dto.ArrivalAddress;
            trip.DistanceKm = dto.DistanceKm;
            trip.IsRoundTrip = dto.IsRoundTrip;
            trip.VehicleId = vehicle?.Id;
            trip.CalculationMethod = dto.CalculationMethod;
            trip.CustomRatePerKm = dto.CustomRatePerKm;
            trip.CalculatedAmount = calculatedAmount;
            trip.Notes = dto.Notes;

            if (report is not null) report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(trip);
        })
        .WithName("UpdateTrip");

        // Supprime un trajet
        group.MapDelete("/{reportId:int}/trips/{tripId:int}", async (int reportId, int tripId, AppDbContext db) =>
        {
            var trip = await db.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.ExpenseReportId == reportId);
            if (trip is null) return Results.NotFound();

            db.Trips.Remove(trip);
            var report = await db.ExpenseReports.FindAsync(reportId);
            if (report is not null) report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteTrip");

        return group;
    }
}
