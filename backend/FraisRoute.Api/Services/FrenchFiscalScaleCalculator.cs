namespace FraisRoute.Api.Services;

using FraisRoute.Api.Models;

/// <summary>
/// Moteur de calcul du barème kilométrique officiel français (DGFIP / Urssaf).
/// Prend en charge les tranches annuelles, la puissance fiscale (CV), les deux-roues
/// et la majoration de 20% pour les véhicules 100% électriques.
/// </summary>
public static class FrenchFiscalScaleCalculator
{
    private const decimal ElectricVehicleBonusMultiplier = 1.20m; // +20%

    /// <summary>
    /// Calcule le montant total des indemnités kilométriques pour une distance cumulée donnée.
    /// </summary>
    public static decimal CalculateCumulativeAnnualAmount(
        decimal totalAnnualKm,
        VehicleType vehicleType,
        int fiscalCv,
        bool isElectric)
    {
        if (totalAnnualKm <= 0) return 0m;

        decimal amount = vehicleType switch
        {
            VehicleType.Car => CalculateCarAmount(totalAnnualKm, fiscalCv),
            VehicleType.Motorcycle => CalculateMotorcycleAmount(totalAnnualKm, fiscalCv),
            VehicleType.Scooter => CalculateScooterAmount(totalAnnualKm),
            _ => CalculateCarAmount(totalAnnualKm, fiscalCv)
        };

        if (isElectric)
        {
            amount *= ElectricVehicleBonusMultiplier;
        }

        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calcule l'indemnité pour un trajet spécifique.
    /// Si previousAnnualKm est renseigné, applique la formule différentielle exacte selon le cumul annuel.
    /// Sinon, applique le barème de base (tranche 1) ou le taux fixe personnalisé.
    /// </summary>
    public static decimal CalculateTripAmount(
        decimal tripDistanceKm,
        VehicleType vehicleType,
        int fiscalCv,
        bool isElectric,
        decimal? customFixedRate = null,
        decimal previousAnnualKm = 0m)
    {
        if (tripDistanceKm <= 0) return 0m;

        // Si l'entreprise utilise un taux fixe négocié (ex: 0.45 €/km)
        if (customFixedRate.HasValue && customFixedRate.Value > 0)
        {
            return Math.Round(tripDistanceKm * customFixedRate.Value, 2, MidpointRounding.AwayFromZero);
        }

        if (previousAnnualKm > 0)
        {
            // Calcul différentiel exact : Montant(Cumul + Trajet) - Montant(Cumul)
            decimal totalBefore = CalculateCumulativeAnnualAmount(previousAnnualKm, vehicleType, fiscalCv, isElectric);
            decimal totalAfter = CalculateCumulativeAnnualAmount(previousAnnualKm + tripDistanceKm, vehicleType, fiscalCv, isElectric);
            return Math.Round(totalAfter - totalBefore, 2, MidpointRounding.AwayFromZero);
        }

        // Par défaut pour un trajet unitaire : calcul direct sur la première tranche
        return CalculateCumulativeAnnualAmount(tripDistanceKm, vehicleType, fiscalCv, isElectric);
    }

    /// <summary>
    /// Retourne le taux au kilomètre de référence (tranche < 5000 km pour voitures).
    /// </summary>
    public static decimal GetBaseRatePerKm(VehicleType vehicleType, int fiscalCv, bool isElectric)
    {
        decimal rate = vehicleType switch
        {
            VehicleType.Car => fiscalCv switch
            {
                <= 3 => 0.529m,
                4 => 0.606m,
                5 => 0.636m,
                6 => 0.665m,
                _ => 0.697m // 7 CV et plus
            },
            VehicleType.Motorcycle => fiscalCv switch
            {
                <= 2 => 0.395m,
                <= 5 => 0.468m,
                _ => 0.606m
            },
            VehicleType.Scooter => 0.315m,
            _ => 0.636m
        };

        if (isElectric)
        {
            rate *= ElectricVehicleBonusMultiplier;
        }

        return Math.Round(rate, 3, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateCarAmount(decimal d, int cv)
    {
        return cv switch
        {
            <= 3 => d switch
            {
                <= 5000m => d * 0.529m,
                <= 20000m => (d * 0.316m) + 1065m,
                _ => d * 0.370m
            },
            4 => d switch
            {
                <= 5000m => d * 0.606m,
                <= 20000m => (d * 0.340m) + 1330m,
                _ => d * 0.407m
            },
            5 => d switch
            {
                <= 5000m => d * 0.636m,
                <= 20000m => (d * 0.357m) + 1395m,
                _ => d * 0.427m
            },
            6 => d switch
            {
                <= 5000m => d * 0.665m,
                <= 20000m => (d * 0.374m) + 1457m,
                _ => d * 0.447m
            },
            _ => d switch // 7 CV et plus
            {
                <= 5000m => d * 0.697m,
                <= 20000m => (d * 0.394m) + 1515m,
                _ => d * 0.470m
            }
        };
    }

    private static decimal CalculateMotorcycleAmount(decimal d, int cv)
    {
        return cv switch
        {
            <= 2 => d switch
            {
                <= 3000m => d * 0.395m,
                <= 6000m => (d * 0.099m) + 891m,
                _ => d * 0.248m
            },
            <= 5 => d switch
            {
                <= 3000m => d * 0.468m,
                <= 6000m => (d * 0.082m) + 1158m,
                _ => d * 0.275m
            },
            _ => d switch
            {
                <= 3000m => d * 0.606m,
                <= 6000m => (d * 0.079m) + 1583m,
                _ => d * 0.343m
            }
        };
    }

    private static decimal CalculateScooterAmount(decimal d)
    {
        return d switch
        {
            <= 2000m => d * 0.315m,
            <= 5000m => (d * 0.079m) + 472m,
            _ => d * 0.173m
        };
    }
}
