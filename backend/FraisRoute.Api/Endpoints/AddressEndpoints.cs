namespace FraisRoute.Api.Endpoints;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

public static class AddressEndpoints
{
    public static RouteGroupBuilder MapAddressEndpoints(this RouteGroupBuilder group)
    {
        // Autocomplétion d'adresses en France via l'API officielle data.gouv.fr (gratuite, rapide, sans clé)
        group.MapGet("/autocomplete", async (string? q, IHttpClientFactory clientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
            {
                return Results.Ok(Array.Empty<object>());
            }

            var results = await SearchAddressesAsync(q.Trim(), clientFactory);
            return Results.Ok(results);
        })
        .WithName("AutocompleteAddress")
        .WithSummary("Recherche d'adresses françaises via l'API nationale data.gouv.fr");

        // Recherche d'adresses avec validation stricte des paramètres
        group.MapGet("/search", async (string? q, IHttpClientFactory clientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            {
                return Results.BadRequest("Le paramètre de recherche 'q' doit contenir au moins 2 caractères.");
            }

            var results = await SearchAddressesAsync(q.Trim(), clientFactory);
            return Results.Ok(results);
        })
        .WithName("SearchAddress")
        .WithSummary("Recherche d'adresses françaises avec validation de la requête");

        // Calcul d'itinéraire routier entre 2 coordonnées via OSRM (avec fallback vol d'oiseau * 1.25)
        group.MapGet("/route", async (double fromLon, double fromLat, double toLon, double toLat, IHttpClientFactory clientFactory) =>
        {
            if (!AreValidCoordinates(fromLon, fromLat, toLon, toLat))
            {
                return Results.BadRequest("Coordonnées GPS hors limites.");
            }

            var result = await CalculateRouteDistanceAsync(fromLon, fromLat, toLon, toLat, clientFactory);
            return Results.Ok(result);
        })
        .WithName("CalculateRouteDistance")
        .WithSummary("Calcule la distance routière en km entre 2 coordonnées GPS");

        // Alias /distance avec validation des coordonnées
        group.MapGet("/distance", async (double? fromLon, double? fromLat, double? toLon, double? toLat, IHttpClientFactory clientFactory) =>
        {
            if (!fromLon.HasValue || !fromLat.HasValue || !toLon.HasValue || !toLat.HasValue ||
                !AreValidCoordinates(fromLon.Value, fromLat.Value, toLon.Value, toLat.Value))
            {
                return Results.BadRequest("Coordonnées GPS manquantes ou invalides.");
            }

            var result = await CalculateRouteDistanceAsync(fromLon.Value, fromLat.Value, toLon.Value, toLat.Value, clientFactory);
            return Results.Ok(result);
        })
        .WithName("GetAddressDistance")
        .WithSummary("Calcule la distance routière entre deux points GPS validés");

        return group;
    }

    private static bool AreValidCoordinates(double fromLon, double fromLat, double toLon, double toLat)
    {
        return fromLat is >= -90.0 and <= 90.0 &&
               toLat is >= -90.0 and <= 90.0 &&
               fromLon is >= -180.0 and <= 180.0 &&
               toLon is >= -180.0 and <= 180.0;
    }

    private static async Task<IEnumerable<object>> SearchAddressesAsync(string query, IHttpClientFactory clientFactory)
    {
        var client = clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);
        var encodedQuery = Uri.EscapeDataString(query);
        var url = $"https://api-adresse.data.gouv.fr/search/?q={encodedQuery}&limit=6";

        try
        {
            var response = await client.GetFromJsonAsync<JsonObject>(url);
            var features = response?["features"]?.AsArray();

            if (features is null) return Array.Empty<object>();

            return features.Select(f =>
            {
                var props = f?["properties"];
                var coords = f?["geometry"]?["coordinates"]?.AsArray();

                return (object)new
                {
                    Label = props?["label"]?.ToString(),
                    City = props?["city"]?.ToString(),
                    Postcode = props?["postcode"]?.ToString(),
                    Longitude = coords?[0]?.GetValue<double>() ?? 0,
                    Latitude = coords?[1]?.GetValue<double>() ?? 0
                };
            }).ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return Array.Empty<object>();
        }
    }

    private static async Task<object> CalculateRouteDistanceAsync(
        double fromLon,
        double fromLat,
        double toLon,
        double toLat,
        IHttpClientFactory clientFactory)
    {
        var client = clientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(3);

        var osrmUrl = string.Create(
            CultureInfo.InvariantCulture,
            $"https://router.project-osrm.org/route/v1/driving/{fromLon},{fromLat};{toLon},{toLat}?overview=false");

        try
        {
            var osrmResponse = await client.GetFromJsonAsync<JsonObject>(osrmUrl);
            var routes = osrmResponse?["routes"]?.AsArray();
            if (routes is not null && routes.Count > 0)
            {
                var distanceMeters = routes[0]?["distance"]?.GetValue<double>() ?? 0;
                var durationSeconds = routes[0]?["duration"]?.GetValue<double>() ?? 0;

                decimal distanceKm = Math.Round((decimal)(distanceMeters / 1000.0), 1);
                return new
                {
                    DistanceKm = distanceKm,
                    DurationMinutes = (int)Math.Round(durationSeconds / 60.0),
                    Source = "OSRM Driving Route"
                };
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            // Fallback calcul mathématique en vol d'oiseau avec coefficient sinuosité routière de 1.25
        }

        double dLat = (toLat - fromLat) * Math.PI / 180.0;
        double dLon = (toLon - fromLon) * Math.PI / 180.0;
        double a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                   (Math.Cos(fromLat * Math.PI / 180.0) * Math.Cos(toLat * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2));
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        const double earthRadiusKm = 6371.0;
        double crowFlyDistance = earthRadiusKm * c;
        decimal estimatedRoadDistance = Math.Round((decimal)(crowFlyDistance * 1.25), 1);

        return new
        {
            DistanceKm = estimatedRoadDistance,
            DurationMinutes = (int)Math.Round((double)estimatedRoadDistance / 70.0 * 60.0),
            Source = "Estimated (Crow-fly x 1.25)"
        };
    }
}
