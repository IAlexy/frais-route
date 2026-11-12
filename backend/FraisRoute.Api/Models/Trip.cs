namespace FraisRoute.Api.Models;

public enum RateType
{
    FiscalScale, // Barème Urssaf / Impôts
    CustomFixedRate // Taux fixe employeur (€/km)
}

public class Trip
{
    public int Id { get; set; }
    public int ExpenseReportId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ExpenseReport? ExpenseReport { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string Purpose { get; set; } = string.Empty; // Motif (ex: Rendez-vous client, Formation)
    public string ClientOrLocation { get; set; } = string.Empty; // Nom du client / Ville
    
    public string DepartureAddress { get; set; } = string.Empty;
    public string ArrivalAddress { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; } // Distance aller simple
    public bool IsRoundTrip { get; set; } = true; // Aller-retour ?

    // Total distance calculée (x2 si aller-retour)
    public decimal TotalDistanceKm => IsRoundTrip ? DistanceKm * 2 : DistanceKm;

    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public RateType CalculationMethod { get; set; } = RateType.FiscalScale;
    public decimal? CustomRatePerKm { get; set; } // Si RateType == CustomFixedRate

    public decimal CalculatedAmount { get; set; } // Montant indemnité kilométrique calculé
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
