namespace FraisRoute.Api.Models;

public enum ReportStatus
{
    Draft,      // Brouillon
    Submitted,  // Soumis
    Approved,   // Approuvé / Validé
    Rejected    // Rejeté
}

public class ExpenseReport
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty; // ex: "Frais de route - Mars 2026"
    public string EmployeeName { get; set; } = string.Empty;
    public string? EmployeeEmail { get; set; }
    public string CompanyName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    // Barème pays/région applicable (FR_URSSAF, BE_FPS, CH_TCS, CA_CRA, UK_HMRC, US_IRS, CUSTOM)
    public string CountryScale { get; set; } = "FR_URSSAF";

    // Devise ISO de la note de frais (EUR, CHF, CAD, GBP, USD)
    public string Currency { get; set; } = "EUR";

    // Taux de prise en charge employeur en % (défaut: 100%, peut être 80%, etc.)
    public decimal ReimbursementRatePercentage { get; set; } = 100m;

    public List<Trip> Trips { get; set; } = new();
    public List<ReceiptExpense> Receipts { get; set; } = new();

    public decimal TotalDistanceKm => Trips.Sum(t => t.TotalDistanceKm);
    public decimal TotalMileageAmount => Trips.Sum(t => t.CalculatedAmount);
    public decimal TotalReceiptsAmount => Receipts.Sum(r => r.AmountTtc);

    // Total brut avant taux de prise en charge
    public decimal TotalGrossAmount => TotalMileageAmount + TotalReceiptsAmount;

    // Total net à rembourser
    public decimal TotalReimbursableAmount => Math.Round(TotalGrossAmount * (ReimbursementRatePercentage / 100m), 2);

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
