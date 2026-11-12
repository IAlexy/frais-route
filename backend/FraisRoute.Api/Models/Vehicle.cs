namespace FraisRoute.Api.Models;

public enum VehicleType
{
    Car,
    Motorcycle,
    Scooter
}

public class Vehicle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // ex: "Peugeot 208"
    public string LicensePlate { get; set; } = string.Empty; // ex: "AA-123-BB"
    public VehicleType Type { get; set; } = VehicleType.Car;
    public int FiscalHorsepower { get; set; } = 5; // CV fiscaux (ex: 4, 5, 6, 7...)
    public bool IsElectric { get; set; } // +20% bonus Urssaf si électrique
    public decimal? DefaultCustomRatePerKm { get; set; } // Taux fixe alternatif (ex: 0.45 €/km)
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
