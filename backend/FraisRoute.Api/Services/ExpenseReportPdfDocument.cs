namespace FraisRoute.Api.Services;

using FraisRoute.Api.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public class ExpenseReportPdfDocument : IDocument
{
    public ExpenseReport Report { get; }

    private string CurrencySymbol => InternationalMileageCalculator.GetCurrencySymbol(Report.Currency);

    private string ScaleName => InternationalMileageCalculator.GetPresetOrDefault(Report.CountryScale).Name;

    public ExpenseReportPdfDocument(ExpenseReport report)
    {
        Report = report;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().Text("NOTE DE FRAIS & INDEMNITÉS KILOMÉTRIQUES")
                        .FontSize(15).Bold().FontColor(Colors.Blue.Darken3);

                    titleCol.Item().Text($"Réf : NDF-{Report.Id:D5} | {Report.Title}")
                        .FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);

                    titleCol.Item().Text($"Période du {Report.PeriodStart:dd/MM/yyyy} au {Report.PeriodEnd:dd/MM/yyyy} | Barème : {ScaleName} ({Report.Currency})")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(140).Column(statusCol =>
                {
                    var statusColor = Report.Status switch
                    {
                        ReportStatus.Approved => Colors.Green.Darken2,
                        ReportStatus.Submitted => Colors.Orange.Darken2,
                        ReportStatus.Rejected => Colors.Red.Darken2,
                        _ => Colors.Grey.Darken2
                    };

                    var statusText = Report.Status switch
                    {
                        ReportStatus.Approved => "VALIDÉE",
                        ReportStatus.Submitted => "SOUMISE",
                        ReportStatus.Rejected => "REFUSÉE",
                        _ => "BROUILLON"
                    };

                    statusCol.Item().Border(1).BorderColor(statusColor).Padding(6).AlignCenter()
                        .Text(statusText).FontSize(11).Bold().FontColor(statusColor);
                });
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

            // Coordonnées Salarié & Entreprise
            col.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(empCol =>
                {
                    empCol.Item().Text("BÉNÉFICIAIRE").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                    empCol.Item().Text(string.IsNullOrWhiteSpace(Report.EmployeeName) ? "Non spécifié" : Report.EmployeeName).Bold();
                    if (!string.IsNullOrWhiteSpace(Report.EmployeeEmail))
                    {
                        empCol.Item().Text(Report.EmployeeEmail).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(15);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(compCol =>
                {
                    compCol.Item().Text("ENTREPRISE / CLIENT").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                    compCol.Item().Text(string.IsNullOrWhiteSpace(Report.CompanyName) ? "Non spécifiée" : Report.CompanyName).Bold();
                    compCol.Item().Text($"Date d'édition : {DateTime.UtcNow:dd/MM/yyyy}").FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingBottom(12);
        });
    }

    private void ComposeContent(IContainer container)
    {
        var symbol = CurrencySymbol;

        container.Column(col =>
        {
            // Section 1 : Trajets & Kilomètres
            col.Item().PaddingTop(6).Text("1. DÉPLACEMENTS & INDEMNITÉS KILOMÉTRIQUES")
                .FontSize(11).Bold().FontColor(Colors.Blue.Darken2);

            if (Report.Trips.Count == 0)
            {
                col.Item().Padding(8).Text("Aucun trajet enregistré sur cette période.").Italic().FontColor(Colors.Grey.Darken1);
            }
            else
            {
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(65);  // Date
                        columns.RelativeColumn(2);   // Motif / Lieu
                        columns.RelativeColumn(3);   // Trajet (De -> A)
                        columns.ConstantColumn(45);  // Type
                        columns.ConstantColumn(50);  // Distance
                        columns.ConstantColumn(70);  // Montant
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Motif").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Itinéraire").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("A/R").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Km").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Total IK").Bold();
                    });

                    foreach (var trip in Report.Trips.OrderBy(t => t.Date))
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text($"{trip.Date:dd/MM/yyyy}");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Column(mCol =>
                        {
                            mCol.Item().Text(trip.Purpose).Bold();
                            if (!string.IsNullOrWhiteSpace(trip.ClientOrLocation))
                                mCol.Item().Text(trip.ClientOrLocation).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4)
                            .Text($"{trip.DepartureAddress} -> {trip.ArrivalAddress}");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4)
                            .Text(trip.IsRoundTrip ? "Oui (x2)" : "Simple");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                            .Text($"{trip.TotalDistanceKm:N1} km");

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                            .Text($"{trip.CalculatedAmount:N2} {symbol}").Bold();
                    }
                });

                // Sous-total IK
                col.Item().PaddingTop(4).AlignRight().Text(text =>
                {
                    text.Span($"Total kilomètres : {Report.TotalDistanceKm:N1} km  |  ").SemiBold();
                    text.Span($"Sous-total IK : {Report.TotalMileageAmount:N2} {symbol}").Bold().FontColor(Colors.Blue.Darken3);
                });
            }

            // Section 2 : Frais annexes (Péages, Parking, Repas)
            col.Item().PaddingTop(14).Text("2. FRAIS RÉELS & JUSTIFICATIFS (Péages, Parkings, Repas)")
                .FontSize(11).Bold().FontColor(Colors.Blue.Darken2);

            if (Report.Receipts.Count == 0)
            {
                col.Item().Padding(8).Text("Aucun frais annexe enregistré sur cette période.").Italic().FontColor(Colors.Grey.Darken1);
            }
            else
            {
                col.Item().PaddingTop(4).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(65);  // Date
                        columns.ConstantColumn(85);  // Catégorie
                        columns.RelativeColumn(3);   // Commerçant / Détail
                        columns.ConstantColumn(60);  // Justificatif
                        columns.ConstantColumn(70);  // Montant TTC
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Date").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Catégorie").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Commerçant / Description").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Reçu joint").Bold();
                        header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("TTC").Bold();
                    });

                    foreach (var receipt in Report.Receipts.OrderBy(r => r.Date))
                    {
                        var catName = receipt.Category switch
                        {
                            ReceiptCategory.Toll => "Péage",
                            ReceiptCategory.Parking => "Stationnement",
                            ReceiptCategory.Meal => "Repas",
                            ReceiptCategory.Fuel => "Carburant",
                            ReceiptCategory.Hotel => "Hébergement",
                            _ => "Autre"
                        };

                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text($"{receipt.Date:dd/MM/yyyy}");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(catName);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(receipt.Merchant);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4)
                            .Text(!string.IsNullOrEmpty(receipt.ReceiptImagePath) ? "Oui (Photo)" : "Non");
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).AlignRight()
                            .Text($"{receipt.AmountTtc:N2} {symbol}").Bold();
                    }
                });

                // Sous-total Reçus
                col.Item().PaddingTop(4).AlignRight().Text(text =>
                {
                    text.Span($"Sous-total Frais réels : {Report.TotalReceiptsAmount:N2} {symbol}").Bold().FontColor(Colors.Blue.Darken3);
                });
            }

            // Récapitulatif Final & Calcul du Remboursement
            col.Item().PaddingTop(16).Border(1.5f).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(10).Column(recapCol =>
            {
                recapCol.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Indemnités Kilométriques :");
                    r.ConstantItem(110).AlignRight().Text($"{Report.TotalMileageAmount:N2} {symbol}");
                });
                recapCol.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Frais Réels (Justificatifs joints) :");
                    r.ConstantItem(110).AlignRight().Text($"{Report.TotalReceiptsAmount:N2} {symbol}");
                });
                recapCol.Item().Row(r =>
                {
                    r.RelativeItem().Text("Total Brut :").Bold();
                    r.ConstantItem(110).AlignRight().Text($"{Report.TotalGrossAmount:N2} {symbol}").Bold();
                });

                if (Report.ReimbursementRatePercentage < 100m)
                {
                    recapCol.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Taux de prise en charge employeur ({Report.ReimbursementRatePercentage:N0}%) :")
                            .FontColor(Colors.Orange.Darken3);
                        r.ConstantItem(110).AlignRight().Text($"{Report.ReimbursementRatePercentage:N0} %")
                            .FontColor(Colors.Orange.Darken3);
                    });
                }

                recapCol.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Blue.Lighten1);

                recapCol.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("NET TOTAL À REMBOURSER :").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);
                    r.ConstantItem(130).AlignRight().Text($"{Report.TotalReimbursableAmount:N2} {symbol}").FontSize(12).Bold().FontColor(Colors.Blue.Darken3);
                });
            });

            // Signatures
            col.Item().PaddingTop(20).Row(signRow =>
            {
                signRow.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Height(65).Column(s =>
                {
                    s.Item().Text("Signature de l'employé(e)").FontSize(8).Bold();
                    s.Item().Text("« Certifié sincère et conforme aux déplacements effectués »").FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                });

                signRow.ConstantItem(20);

                signRow.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Height(65).Column(s =>
                {
                    s.Item().Text("Validation Direction / RH / Comptabilité").FontSize(8).Bold();
                    s.Item().Text("« Bon à payer »").FontSize(7).Italic().FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(x =>
            {
                x.Span($"Document généré automatiquement — Barème appliqué : {ScaleName} ({Report.Currency}).")
                    .FontSize(7).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }
}
