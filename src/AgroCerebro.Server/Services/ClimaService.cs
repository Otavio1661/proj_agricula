using System.Globalization;
using System.Text.Json;
using AgroCerebro.Shared;
using Microsoft.Extensions.Caching.Memory;

namespace AgroCerebro.Server.Services;

public interface IClimaService { Task<ClimaDto> ObterAsync(double lat, double lon, CancellationToken ct = default); }

/// <summary>Open-Meteo (API pública, sem chave): previsão horária de 3 dias.</summary>
public class ClimaService(HttpClient http, IMemoryCache cache, IConfiguration cfg) : IClimaService
{
    public async Task<ClimaDto> ObterAsync(double lat, double lon, CancellationToken ct = default)
    {
        var key = $"clima:{Math.Round(lat, 2)}:{Math.Round(lon, 2)}";
        if (cache.TryGetValue(key, out ClimaDto? hit) && hit != null) return hit;

        var ci = CultureInfo.InvariantCulture;
        var url = $"{cfg["Clima:ForecastUrl"] ?? "https://api.open-meteo.com/v1/forecast"}?latitude={lat.ToString(ci)}&longitude={lon.ToString(ci)}" +
                  "&current=temperature_2m,relative_humidity_2m,wind_speed_10m" +
                  "&hourly=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m,is_day" +
                  "&forecast_days=3&timezone=auto&wind_speed_unit=kmh";
        using var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = doc.RootElement;
        var cur = root.GetProperty("current");
        var h = root.GetProperty("hourly");
        var times = h.GetProperty("time").EnumerateArray().Select(x => DateTime.Parse(x.GetString()!, ci)).ToList();
        double[] Arr(string n) => h.GetProperty(n).EnumerateArray().Select(x => x.ValueKind == JsonValueKind.Null ? 0 : x.GetDouble()).ToArray();
        var t = Arr("temperature_2m"); var u = Arr("relative_humidity_2m"); var p = Arr("precipitation"); var w = Arr("wind_speed_10m"); var d = Arr("is_day");

        var agora = DateTime.Now.AddHours(-1);
        var horas = new List<ClimaHoraDto>();
        for (var i = 0; i < times.Count; i++)
            if (times[i] >= agora) horas.Add(new ClimaHoraDto(times[i], t[i], u[i], p[i], w[i], d[i] > 0));

        var dto = new ClimaDto(lat, lon, cur.GetProperty("temperature_2m").GetDouble(), cur.GetProperty("relative_humidity_2m").GetDouble(),
            cur.GetProperty("wind_speed_10m").GetDouble(), horas, "Open-Meteo");
        cache.Set(key, dto, TimeSpan.FromMinutes(cfg.GetValue("Clima:CacheMinutos", 30)));
        return dto;
    }
}
