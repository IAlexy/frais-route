namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CreateReceiptExpenseDto(
    DateTime Date,
    ReceiptCategory Category,
    string Merchant,
    decimal AmountTtc,
    decimal? VatRate,
    decimal? VatAmount,
    string? Notes
);

public record UpdateReceiptExpenseDto(
    DateTime Date,
    ReceiptCategory Category,
    string Merchant,
    decimal AmountTtc,
    decimal? VatRate,
    decimal? VatAmount,
    string? Notes
);
