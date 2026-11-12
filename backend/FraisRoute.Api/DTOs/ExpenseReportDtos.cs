namespace FraisRoute.Api.DTOs;

using FraisRoute.Api.Models;

public record CreateExpenseReportDto(
    string Title,
    string EmployeeName,
    string? EmployeeEmail,
    string CompanyName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal ReimbursementRatePercentage = 100m
);

public record UpdateExpenseReportDto(
    string Title,
    string EmployeeName,
    string? EmployeeEmail,
    string CompanyName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    ReportStatus Status,
    decimal ReimbursementRatePercentage
);

public record UpdateReportStatusDto(
    ReportStatus Status
);

public record ExpenseReportSummaryDto(
    int Id,
    string Title,
    string EmployeeName,
    string CompanyName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    ReportStatus Status,
    decimal ReimbursementRatePercentage,
    int TripsCount,
    decimal TotalDistanceKm,
    decimal TotalMileageAmount,
    int ReceiptsCount,
    decimal TotalReceiptsAmount,
    decimal TotalGrossAmount,
    decimal TotalReimbursableAmount,
    DateTime CreatedAt
);

public record ExpenseReportDetailDto(
    int Id,
    string Title,
    string EmployeeName,
    string? EmployeeEmail,
    string CompanyName,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    ReportStatus Status,
    decimal ReimbursementRatePercentage,
    decimal TotalDistanceKm,
    decimal TotalMileageAmount,
    decimal TotalReceiptsAmount,
    decimal TotalGrossAmount,
    decimal TotalReimbursableAmount,
    List<Trip> Trips,
    List<ReceiptExpense> Receipts,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
