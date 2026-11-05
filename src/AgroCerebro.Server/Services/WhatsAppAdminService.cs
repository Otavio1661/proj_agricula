using System.Net.Http.Json;
using System.Text.Json;
using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Services;

public interface IWhatsAppAdminService
{
    bool ApiKeyConfigurada { get; }
    Task<string?> InstanciaAtivaAsync(CancellationToken ct = default);
    Task DefinirAtivaAsync(string id, CancellationToken ct = default);
    Task<(int status, JsonElement? json, string? erro)> ChamarAsync(HttpMethod m, string path, object? body = null, CancellationToken ct = default);
}

/// <summary>Gerenciamento de instâncias da ApiEnvios (/v1/instances…) — exige a API key da conta (x-api-key).</summary>
public class WhatsAppAdminService(HttpClient http, IConfiguration cfg, AppDbContext db) : IWhatsAppAdminService
{
    private const string ChaveAtiva = "WhatsApp.InstanciaAtiva";
    public bool ApiKeyConfigurada => !string.IsNullOrWhiteSpace(cfg["WhatsApp:ApiKey"]) && !string.IsNullOrWhiteSpace(cfg["WhatsApp:BaseUrl"]);

    public async Task<string?> InstanciaAtivaAsync(CancellationToken ct = default) =>
        (await db.Configuracoes.AsNoTracking().FirstOrDefaultAsync(c => c.Chave == ChaveAtiva, ct))?.Valor ?? cfg["WhatsApp:InstanceId"];

    public async Task DefinirAtivaAsync(string id, CancellationToken ct = default)
    {
        var c = await db.Configuracoes.FindAsync(new object[] { ChaveAtiva }, ct);
        if (c == null) db.Configuracoes.Add(new Configuracao { Chave = ChaveAtiva, Valor = id }); else c.Valor = id;
        await db.SaveChangesAsync(ct);
    }

    public async Task<(int, JsonElement?, string?)> ChamarAsync(HttpMethod m, string path, object? body = null, CancellationToken ct = default)
    {
        if (!ApiKeyConfigurada) return (503, null, "API key da ApiEnvios não configurada (WhatsApp:ApiKey).");
        try
        {
            using var req = new HttpRequestMessage(m, cfg["WhatsApp:BaseUrl"]!.TrimEnd('/') + path);
            req.Headers.Add("x-api-key", cfg["WhatsApp:ApiKey"]);
            if (body != null) req.Content = JsonContent.Create(body);
            using var resp = await http.SendAsync(req, ct);
            var txt = await resp.Content.ReadAsStringAsync(ct);
            JsonElement? json = null;
            try { if (!string.IsNullOrWhiteSpace(txt)) json = JsonDocument.Parse(txt).RootElement.Clone(); } catch { }
            if (!resp.IsSuccessStatusCode)
            {
                string? msg = null;
                if (json is { ValueKind: JsonValueKind.Object } j)
                    foreach (var k in new[] { "error", "message" })
                        if (j.TryGetProperty(k, out var e) && e.ValueKind == JsonValueKind.String) { msg = e.GetString(); break; }
                return ((int)resp.StatusCode, json, msg ?? $"ApiEnvios respondeu {(int)resp.StatusCode}.");
            }
            return ((int)resp.StatusCode, json, null);
        }
        catch (Exception ex) { return (502, null, "Falha ao falar com a ApiEnvios: " + ex.Message); }
    }
}
