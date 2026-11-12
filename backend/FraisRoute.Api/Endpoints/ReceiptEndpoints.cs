namespace FraisRoute.Api.Endpoints;

using FraisRoute.Api.Data;
using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public static class ReceiptEndpoints
{
    public static RouteGroupBuilder MapReceiptEndpoints(this RouteGroupBuilder group)
    {
        // Liste les reçus d'une note de frais
        group.MapGet("/{reportId:int}/receipts", async (int reportId, AppDbContext db) =>
        {
            var receipts = await db.ReceiptExpenses
                .Where(r => r.ExpenseReportId == reportId)
                .OrderBy(r => r.Date)
                .ToListAsync();

            return Results.Ok(receipts);
        })
        .WithName("GetReceiptsByReport");

        // Ajoute un reçu (métadonnées) avec calcul automatique de la TVA si non fournie
        group.MapPost("/{reportId:int}/receipts", async (int reportId, CreateReceiptExpenseDto dto, AppDbContext db) =>
        {
            if (dto.AmountTtc <= 0 || string.IsNullOrWhiteSpace(dto.Merchant))
            {
                return Results.BadRequest("Montant TTC ou commerçant invalide.");
            }

            var report = await db.ExpenseReports.FindAsync(reportId);
            if (report is null) return Results.NotFound("Note de frais introuvable");

            decimal? vatAmount = dto.VatAmount;
            if (!vatAmount.HasValue && dto.VatRate.HasValue)
            {
                vatAmount = dto.VatRate.Value > 0m
                    ? Math.Round(dto.AmountTtc * (dto.VatRate.Value / (100m + dto.VatRate.Value)), 2, MidpointRounding.AwayFromZero)
                    : 0m;
            }

            var receipt = new ReceiptExpense
            {
                ExpenseReportId = reportId,
                Date = dto.Date,
                Category = dto.Category,
                Merchant = dto.Merchant.Trim(),
                AmountTtc = dto.AmountTtc,
                VatRate = dto.VatRate,
                VatAmount = vatAmount,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            report.UpdatedAt = DateTime.UtcNow;
            db.ReceiptExpenses.Add(receipt);
            await db.SaveChangesAsync();

            return Results.Created($"/api/reports/{reportId}/receipts/{receipt.Id}", receipt);
        })
        .WithName("AddReceiptToReport");

        // Upload de photo de reçu (photo prise sur smartphone ou justificatif PDF/image)
        group.MapPost("/{reportId:int}/receipts/{receiptId:int}/upload", async (
            int reportId,
            int receiptId,
            IFormFile file,
            IWebHostEnvironment env,
            AppDbContext db) =>
        {
            var receipt = await db.ReceiptExpenses.FirstOrDefaultAsync(r => r.Id == receiptId && r.ExpenseReportId == reportId);
            if (receipt is null) return Results.NotFound();

            if (file.Length == 0) return Results.BadRequest("Fichier vide");

            var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "receipts");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"receipt_{reportId}_{receiptId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            receipt.ReceiptImagePath = $"/uploads/receipts/{fileName}";
            var report = await db.ExpenseReports.FindAsync(reportId);
            if (report is not null) report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { receipt.ReceiptImagePath });
        })
        .WithName("UploadReceiptImage")
        .DisableAntiforgery();

        // Supprime un reçu
        group.MapDelete("/{reportId:int}/receipts/{receiptId:int}", async (int reportId, int receiptId, AppDbContext db) =>
        {
            var receipt = await db.ReceiptExpenses.FirstOrDefaultAsync(r => r.Id == receiptId && r.ExpenseReportId == reportId);
            if (receipt is null) return Results.NotFound();

            db.ReceiptExpenses.Remove(receipt);
            var report = await db.ExpenseReports.FindAsync(reportId);
            if (report is not null) report.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteReceipt");

        return group;
    }
}
