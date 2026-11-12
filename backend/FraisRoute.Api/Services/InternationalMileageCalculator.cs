namespace FraisRoute.Api.Services;

using FraisRoute.Api.DTOs;
using FraisRoute.Api.Models;

/// <summary>
/// Moteur de calcul international multi-barèmes pour les indemnités kilométriques :
/// - FR_URSSAF : France (DGFIP / Urssaf - tranches 5k/20k + puissance CV + bonus 20% électrique)
/// - BE_FPS    : Belgique (SPF Finances - indemnité forfaitaire officielle au km)
/// - CH_TCS    : Suisse (Barème TCS / Administration fiscale cantonale - CHF/km)
/// - CA_CRA    : Canada (Agence du revenu du Canada ARC/CRA - paliers 5 000 km en CAD)
/// - UK_HMRC   : Royaume-Uni (HMRC Approved Mileage Allowance Payments - paliers 16 000 km en GBP)
/// - US_IRS    : États-Unis (IRS Standard Mileage Rate - USD/km)
/// - CUSTOM    : Forfait kilométrique entreprise personnalisé toute devise
/// </summary>
public static class InternationalMileageCalculator
{
    public const string ScaleFranceUrssaf = "FR_URSSAF";
    public const string ScaleBelgiumFps = "BE_FPS";
    public const string ScaleSwitzerlandTcs = "CH_TCS";
    public const string ScaleCanadaCra = "CA_CRA";
    public const string ScaleUkHmrc = "UK_HMRC";
    public const string ScaleUsIrs = "US_IRS";
    public const string ScaleCustom = "CUSTOM";

    private static readonly IReadOnlyList<MileagePresetDto> Presets =
    [
        new(
            Code: ScaleFranceUrssaf,
            Name: "France — Barème Fiscal Urssaf / DGFIP",
            Country: "FR",
            Currency: "EUR",
            CurrencySymbol: "€",
            DefaultRatePerKm: 0.636m,
            SupportsFiscalHorsepower: true,
            SupportsElectricBonus: true,
            Description: "Barème progressif par tranches (5 000 / 20 000 km) selon CV fiscaux et majoration +20% électrique"
        ),
        new(
            Code: ScaleBelgiumFps,
            Name: "Belgique — SPF Finances",
            Country: "BE",
            Currency: "EUR",
            CurrencySymbol: "€",
            DefaultRatePerKm: 0.428m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Indemnité kilométrique forfaitaire officielle belge (0,428 €/km)"
        ),
        new(
            Code: ScaleSwitzerlandTcs,
            Name: "Suisse — Barème TCS / Fédéral",
            Country: "CH",
            Currency: "CHF",
            CurrencySymbol: "CHF",
            DefaultRatePerKm: 0.700m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Indemnité kilométrique standard suisse (0,70 CHF/km)"
        ),
        new(
            Code: ScaleCanadaCra,
            Name: "Canada — ARC / CRA Automobile Allowance",
            Country: "CA",
            Currency: "CAD",
            CurrencySymbol: "CA$",
            DefaultRatePerKm: 0.700m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Barème ARC : 0,70 CA$/km sur les 5 000 premiers km puis 0,64 CA$/km au-delà"
        ),
        new(
            Code: ScaleUkHmrc,
            Name: "Royaume-Uni — HMRC AMAP",
            Country: "GB",
            Currency: "GBP",
            CurrencySymbol: "£",
            DefaultRatePerKm: 0.450m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Barème HMRC : 0,45 £/km jusqu'à 16 000 km annuels puis 0,25 £/km au-delà"
        ),
        new(
            Code: ScaleUsIrs,
            Name: "USA — IRS Standard Mileage",
            Country: "US",
            Currency: "USD",
            CurrencySymbol: "$",
            DefaultRatePerKm: 0.670m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Barème fédéral IRS Standard Mileage (0,67 $/km)"
        ),
        new(
            Code: ScaleCustom,
            Name: "International — Forfait Entreprise Personnalisé",
            Country: "ALL",
            Currency: "EUR",
            CurrencySymbol: "€",
            DefaultRatePerKm: 0.500m,
            SupportsFiscalHorsepower: false,
            SupportsElectricBonus: false,
            Description: "Taux kilométrique fixe librement défini par la politique voyage de l'organisation"
        )
    ];

    public static IReadOnlyList<MileagePresetDto> GetPresets() => Presets;

    public static MileagePresetDto GetPresetOrDefault(string? countryScale)
    {
        if (string.IsNullOrWhiteSpace(countryScale))
        {
            return Presets[0];
        }

        return Presets.FirstOrDefault(p => string.Equals(p.Code, countryScale.Trim(), StringComparison.OrdinalIgnoreCase))
               ?? Presets[0];
    }

    public static string GetCurrencySymbol(string? currencyCode)
    {
        return (currencyCode?.Trim().ToUpperInvariant()) switch
        {
            "USD" => "$",
            "GBP" => "£",
            "CHF" => "CHF",
            "CAD" => "CA$",
            _ => "€"
        };
    }

    public static (decimal Amount, decimal EffectiveRatePerKm, string Currency, string Description) CalculateAllowance(
        string? countryScale,
        decimal tripDistanceKm,
        VehicleType vehicleType,
        int fiscalCv,
        bool isElectric,
        decimal? customFixedRate = null,
        decimal previousAnnualKm = 0m,
        string? overrideCurrency = null)
    {
        var preset = GetPresetOrDefault(countryScale);
        string currency = !string.IsNullOrWhiteSpace(overrideCurrency)
            ? overrideCurrency.Trim().ToUpperInvariant()
            : preset.Currency;

        if (tripDistanceKm <= 0)
        {
            return (0m, preset.DefaultRatePerKm, currency, preset.Name);
        }

        // Si un taux forfaitaire personnalisé est explicitement demandé ou si le barème est CUSTOM
        if ((customFixedRate.HasValue && customFixedRate.Value > 0m) ||
            string.Equals(preset.Code, ScaleCustom, StringComparison.OrdinalIgnoreCase))
        {
            decimal rate = customFixedRate.HasValue && customFixedRate.Value > 0m
                ? customFixedRate.Value
                : preset.DefaultRatePerKm;
            decimal customAmount = Math.Round(tripDistanceKm * rate, 2, MidpointRounding.AwayFromZero);
            return (
                customAmount,
                rate,
                currency,
                $"Taux entreprise personnalisé ({rate:N3} {GetCurrencySymbol(currency)}/km)"
            );
        }

        switch (preset.Code)
        {
            case ScaleBelgiumFps:
            {
                decimal rate = vehicleType == VehicleType.Scooter ? 0.350m : 0.428m;
                decimal amount = Math.Round(tripDistanceKm * rate, 2, MidpointRounding.AwayFromZero);
                return (amount, rate, currency, $"Barème SPF Finances Belgique ({rate:N3} €/km)");
            }

            case ScaleSwitzerlandTcs:
            {
                decimal rate = vehicleType switch
                {
                    VehicleType.Motorcycle => 0.400m,
                    VehicleType.Scooter => 0.250m,
                    _ => 0.700m
                };
                decimal amount = Math.Round(tripDistanceKm * rate, 2, MidpointRounding.AwayFromZero);
                return (amount, rate, currency, $"Barème Suisse TCS ({rate:N2} CHF/km)");
            }

            case ScaleCanadaCra:
            {
                decimal before = CalculateTieredAmount(previousAnnualKm, 5000m, 0.70m, 0.64m);
                decimal after = CalculateTieredAmount(previousAnnualKm + tripDistanceKm, 5000m, 0.70m, 0.64m);
                decimal amount = Math.Round(after - before, 2, MidpointRounding.AwayFromZero);
                decimal effectiveRate = Math.Round(amount / tripDistanceKm, 3, MidpointRounding.AwayFromZero);
                return (amount, effectiveRate, currency, "Barème Canada ARC/CRA (0,70 CA$ ≤ 5 000 km, 0,64 CA$ au-delà)");
            }

            case ScaleUkHmrc:
            {
                decimal firstRate = vehicleType == VehicleType.Motorcycle ? 0.24m : 0.45m;
                decimal secondRate = vehicleType == VehicleType.Motorcycle ? 0.24m : 0.25m;
                decimal before = CalculateTieredAmount(previousAnnualKm, 16000m, firstRate, secondRate);
                decimal after = CalculateTieredAmount(previousAnnualKm + tripDistanceKm, 16000m, firstRate, secondRate);
                decimal amount = Math.Round(after - before, 2, MidpointRounding.AwayFromZero);
                decimal effectiveRate = Math.Round(amount / tripDistanceKm, 3, MidpointRounding.AwayFromZero);
                return (amount, effectiveRate, currency, "Barème UK HMRC AMAP (0,45 £ ≤ 16 000 km, 0,25 £ au-delà)");
            }

            case ScaleUsIrs:
            {
                const decimal rate = 0.670m;
                decimal amount = Math.Round(tripDistanceKm * rate, 2, MidpointRounding.AwayFromZero);
                return (amount, rate, currency, "Barème USA IRS Standard Mileage (0,67 $/km)");
            }

            default:
            {
                decimal frAmount = FrenchFiscalScaleCalculator.CalculateTripAmount(
                    tripDistanceKm: tripDistanceKm,
                    vehicleType: vehicleType,
                    fiscalCv: fiscalCv,
                    isElectric: isElectric,
                    customFixedRate: null,
                    previousAnnualKm: previousAnnualKm
                );
                decimal frBaseRate = FrenchFiscalScaleCalculator.GetBaseRatePerKm(vehicleType, fiscalCv, isElectric);
                string energyLabel = isElectric ? "+20% électrique" : "thermique/hybride";
                return (
                    frAmount,
                    frBaseRate,
                    currency,
                    $"Barème fiscal officiel Urssaf ({fiscalCv} CV, {energyLabel})"
                );
            }
        }
    }

    private static decimal CalculateTieredAmount(decimal totalKm, decimal thresholdKm, decimal rate1, decimal rate2)
    {
        if (totalKm <= 0m) return 0m;
        if (totalKm <= thresholdKm) return totalKm * rate1;
        return (thresholdKm * rate1) + ((totalKm - thresholdKm) * rate2);
    }
}
