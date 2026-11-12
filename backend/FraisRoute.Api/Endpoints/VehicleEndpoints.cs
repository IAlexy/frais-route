namespace FraisRoute.Api.Endpoints;

using FraisRoute.Api.Data;
using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using Microsoft.EntityFrameworkCore;

public static class VehicleEndpoints
{
    public static RouteGroupBuilder MapVehicleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", async (AppDbContext db) =>
        {
            var vehicles = await db.Vehicles.OrderByDescending(v => v.IsDefault).ThenBy(v => v.Name).ToListAsync();
            return Results.Ok(vehicles);
        })
        .WithName("GetVehicles")
        .WithSummary("Liste tous les véhicules enregistrés");

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var vehicle = await db.Vehicles.FindAsync(id);
            return vehicle is not null ? Results.Ok(vehicle) : Results.NotFound();
        })
        .WithName("GetVehicleById");

        group.MapPost("/", async (CreateVehicleDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.LicensePlate) || dto.FiscalHorsepower <= 0)
            {
                return Results.BadRequest("Données de véhicule invalides.");
            }

            var hasAnyDefault = await db.Vehicles.AnyAsync(v => v.IsDefault);
            var isDefault = dto.IsDefault || !hasAnyDefault;

            if (isDefault)
            {
                var currentDefaults = await db.Vehicles.Where(v => v.IsDefault).ToListAsync();
                foreach (var v in currentDefaults)
                {
                    v.IsDefault = false;
                }
            }

            var vehicle = new Vehicle
            {
                Name = dto.Name.Trim(),
                LicensePlate = dto.LicensePlate.Trim().ToUpperInvariant(),
                Type = dto.Type,
                FiscalHorsepower = dto.FiscalHorsepower,
                IsElectric = dto.IsElectric,
                DefaultCustomRatePerKm = dto.DefaultCustomRatePerKm,
                IsDefault = isDefault,
                CreatedAt = DateTime.UtcNow
            };

            db.Vehicles.Add(vehicle);
            await db.SaveChangesAsync();

            return Results.Created($"/api/vehicles/{vehicle.Id}", vehicle);
        })
        .WithName("CreateVehicle");

        group.MapPut("/{id:int}", async (int id, UpdateVehicleDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.LicensePlate) || dto.FiscalHorsepower <= 0)
            {
                return Results.BadRequest("Données de véhicule invalides.");
            }

            var vehicle = await db.Vehicles.FindAsync(id);
            if (vehicle is null) return Results.NotFound();

            if (dto.IsDefault && !vehicle.IsDefault)
            {
                var currentDefaults = await db.Vehicles.Where(v => v.IsDefault && v.Id != id).ToListAsync();
                foreach (var v in currentDefaults)
                {
                    v.IsDefault = false;
                }
            }

            vehicle.Name = dto.Name.Trim();
            vehicle.LicensePlate = dto.LicensePlate.Trim().ToUpperInvariant();
            vehicle.Type = dto.Type;
            vehicle.FiscalHorsepower = dto.FiscalHorsepower;
            vehicle.IsElectric = dto.IsElectric;
            vehicle.DefaultCustomRatePerKm = dto.DefaultCustomRatePerKm;
            vehicle.IsDefault = dto.IsDefault;

            await db.SaveChangesAsync();
            return Results.Ok(vehicle);
        })
        .WithName("UpdateVehicle");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var vehicle = await db.Vehicles.FindAsync(id);
            if (vehicle is null) return Results.NotFound();

            var wasDefault = vehicle.IsDefault;
            db.Vehicles.Remove(vehicle);
            await db.SaveChangesAsync();

            if (wasDefault)
            {
                var nextDefault = await db.Vehicles.OrderBy(v => v.Id).FirstOrDefaultAsync();
                if (nextDefault is not null)
                {
                    nextDefault.IsDefault = true;
                    await db.SaveChangesAsync();
                }
            }

            return Results.NoContent();
        })
        .WithName("DeleteVehicle");

        return group;
    }
}
