namespace FraisRoute.Api.Endpoints;

using FraisRoute.Api.Data;
using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using FraisRoute.Api.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

public static class ExpenseReportEndpoints
{
    public static RouteGroupBuilder MapExpenseReportEndpoints(this RouteGroupBuilder group)
    {
        // Liste toutes les notes de frais (résumés)
        group.MapGet("/", async (AppDbContext db) =>
        {
            var entities = await db.ExpenseReports
                .Include(e => e.Trips)
                .Include(e => e.Receipts)
                .OrderByDescending(e => e.PeriodStart)
                .ToListAsync();

            var reports = entities.Select(e => new ExpenseReportSummaryDto(
                e.Id,
                e.Title,
                e.EmployeeName,
                e.CompanyName,
                e.PeriodStart,
                e.PeriodEnd,
                e.Status,
                e.ReimbursementRatePercentage,
                e.Trips.Count,
                e.TotalDistanceKm,
                e.TotalMileageAmount,
                e.Receipts.Count,
                e.TotalReceiptsAmount,
                e.TotalGrossAmount,
                e.TotalReimbursableAmount,
                e.CreatedAt,
                e.CountryScale,
                e.Currency
            )).ToList();

            return Results.Ok(reports);
        })
        .WithName("GetAllReports")
        .WithSummary("Récupère la liste de toutes les notes de frais");

        // Détail d'une note de frais
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var report = await db.ExpenseReports
                .Include(e => e.Trips)
                    .ThenInclude(t => t.Vehicle)
                .Include(e => e.Receipts)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (report is null) return Results.NotFound();

            var detail = new ExpenseReportDetailDto(
                report.Id,
                report.Title,
                report.EmployeeName,
                report.EmployeeEmail,
                report.CompanyName,
                report.PeriodStart,
                report.PeriodEnd,
                report.Status,
                report.ReimbursementRatePercentage,
                report.TotalDistanceKm,
                report.TotalMileageAmount,
                report.TotalReceiptsAmount,
                report.TotalGrossAmount,
                report.TotalReimbursableAmount,
                report.Trips.OrderBy(t => t.Date).ToList(),
                report.Receipts.OrderBy(r => r.Date).ToList(),
                report.CreatedAt,
                report.UpdatedAt,
                report.CountryScale,
                report.Currency
            );

            return Results.Ok(detail);
        })
        .WithName("GetReportDetail");

        // Création d'une note de frais
        group.MapPost("/", async (CreateExpenseReportDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.EmployeeName) ||
                dto.PeriodEnd < dto.PeriodStart ||
                dto.ReimbursementRatePercentage is <= 0m or > 100m)
            {
                return Results.BadRequest("Données de note de frais invalides.");
            }

            var conflictExists = await db.ExpenseReports.AnyAsync(e =>
                e.EmployeeName == dto.EmployeeName &&
                e.PeriodStart.Year == dto.PeriodStart.Year &&
                e.PeriodStart.Month == dto.PeriodStart.Month);

            if (conflictExists)
            {
                return Results.Conflict("Une note de frais existe déjà pour ce salarié sur cette période mensuelle.");
            }

            var preset = InternationalMileageCalculator.GetPresetOrDefault(dto.CountryScale);
            var currency = !string.IsNullOrWhiteSpace(dto.Currency)
                ? dto.Currency.Trim().ToUpperInvariant()
                : preset.Currency;

            var report = new ExpenseReport
            {
                Title = dto.Title.Trim(),
                EmployeeName = dto.EmployeeName.Trim(),
                EmployeeEmail = dto.EmployeeEmail?.Trim(),
                CompanyName = dto.CompanyName.Trim(),
                PeriodStart = dto.PeriodStart,
                PeriodEnd = dto.PeriodEnd,
                ReimbursementRatePercentage = dto.ReimbursementRatePercentage,
                CountryScale = preset.Code,
                Currency = currency,
                Status = ReportStatus.Draft,
                CreatedAt = DateTime.UtcNow
            };

            db.ExpenseReports.Add(report);
            await db.SaveChangesAsync();

            return Results.Created($"/api/reports/{report.Id}", report);
        })
        .WithName("CreateReport");

        // Mise à jour d'une note de frais
        group.MapPut("/{id:int}", async (int id, UpdateExpenseReportDto dto, AppDbContext db) =>
        {
            var report = await db.ExpenseReports.FindAsync(id);
            if (report is null) return Results.NotFound();

            var preset = InternationalMileageCalculator.GetPresetOrDefault(dto.CountryScale);

            report.Title = dto.Title;
            report.EmployeeName = dto.EmployeeName;
            report.EmployeeEmail = dto.EmployeeEmail;
            report.CompanyName = dto.CompanyName;
            report.PeriodStart = dto.PeriodStart;
            report.PeriodEnd = dto.PeriodEnd;
            report.Status = dto.Status;
            report.ReimbursementRatePercentage = dto.ReimbursementRatePercentage;
            report.CountryScale = preset.Code;
            report.Currency = !string.IsNullOrWhiteSpace(dto.Currency)
                ? dto.Currency.Trim().ToUpperInvariant()
                : preset.Currency;
            report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(report);
        })
        .WithName("UpdateReport");

        // Mise à jour du statut d'une note de frais
        group.MapPatch("/{id:int}/status", async (int id, UpdateReportStatusDto dto, AppDbContext db) =>
        {
            if (!Enum.IsDefined(dto.Status))
            {
                return Results.BadRequest("Statut de note de frais invalide.");
            }

            var report = await db.ExpenseReports.FindAsync(id);
            if (report is null) return Results.NotFound();

            report.Status = dto.Status;
            report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(report);
        })
        .WithName("UpdateReportStatus")
        .WithSummary("Modifie le statut de validation d'une note de frais");

        // Suppression d'une note de frais
        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var report = await db.ExpenseReports.FindAsync(id);
            if (report is null) return Results.NotFound();

            db.ExpenseReports.Remove(report);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteReport");

        // Téléchargement / Visualisation du PDF officiel généré avec QuestPDF (supporte ?lang=fr|en)
        group.MapGet("/{id:int}/pdf", async (int id, string? lang, AppDbContext db) =>
        {
            var report = await db.ExpenseReports
                .Include(e => e.Trips)
                    .ThenInclude(t => t.Vehicle)
                .Include(e => e.Receipts)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (report is null) return Results.NotFound();

            var document = new ExpenseReportPdfDocument(report, lang);
            byte[] pdfBytes = document.GeneratePdf();

            var prefix = string.Equals(document.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? "Expense_Report"
                : "Note_de_frais";
            var fileName = $"{prefix}_NDF-{report.Id:D5}_{report.PeriodStart:yyyy-MM}.pdf";
            return Results.File(pdfBytes, "application/pdf", fileName);
        })
        .WithName("ExportReportPdf")
        .WithSummary("Génère le document PDF officiel multilingue de la note de frais");

        return group;
    }
}
