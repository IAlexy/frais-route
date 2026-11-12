namespace FraisRoute.Api.Data;

using FraisRoute.Api.Models;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ReceiptExpense> ReceiptExpenses => Set<ReceiptExpense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuration ExpenseReport
        modelBuilder.Entity<ExpenseReport>()
            .Property(e => e.ReimbursementRatePercentage)
            .HasPrecision(5, 2);

        // Configuration Trip
        modelBuilder.Entity<Trip>()
            .Property(t => t.DistanceKm)
            .HasPrecision(8, 2);

        modelBuilder.Entity<Trip>()
            .Property(t => t.CalculatedAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Trip>()
            .Property(t => t.CustomRatePerKm)
            .HasPrecision(6, 4);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.ExpenseReport)
            .WithMany(e => e.Trips)
            .HasForeignKey(t => t.ExpenseReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configuration ReceiptExpense
        modelBuilder.Entity<ReceiptExpense>()
            .Property(r => r.AmountTtc)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ReceiptExpense>()
            .Property(r => r.VatAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<ReceiptExpense>()
            .Property(r => r.VatRate)
            .HasPrecision(5, 2);

        modelBuilder.Entity<ReceiptExpense>()
            .HasOne(r => r.ExpenseReport)
            .WithMany(e => e.Receipts)
            .HasForeignKey(r => r.ExpenseReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data initiale pour démarrer immédiatement
        modelBuilder.Entity<Vehicle>().HasData(
            new Vehicle
            {
                Id = 1,
                Name = "Peugeot 208",
                LicensePlate = "GH-456-JK",
                Type = VehicleType.Car,
                FiscalHorsepower = 5,
                IsElectric = false,
                IsDefault = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Vehicle
            {
                Id = 2,
                Name = "Tesla Model 3",
                LicensePlate = "EV-999-FR",
                Type = VehicleType.Car,
                FiscalHorsepower = 6,
                IsElectric = true,
                IsDefault = false,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
