namespace FraisRoute.Api.Models;

public enum ReceiptCategory
{
    Toll,       // Péage
    Parking,    // Stationnement
    Meal,       // Repas
    Fuel,       // Carburant
    Hotel,      // Hébergement
    Other       // Autre
}

public class ReceiptExpense
{
    public int Id { get; set; }
    public int ExpenseReportId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ExpenseReport? ExpenseReport { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;
    public ReceiptCategory Category { get; set; } = ReceiptCategory.Toll;
    public string Merchant { get; set; } = string.Empty; // ex: APRR, Indigo, Restaurant Leffe
    
    public decimal AmountTtc { get; set; } // Montant TTC
    public decimal? VatRate { get; set; } // Taux TVA (ex: 20.0, 10.0)
    public decimal? VatAmount { get; set; } // Montant TVA
    
    public string? ReceiptImagePath { get; set; } // URL ou nom de fichier de la photo
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
