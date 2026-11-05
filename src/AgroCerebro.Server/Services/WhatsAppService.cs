using System.Net.Http.Json;
using System.Text.RegularExpressions;
using AgroCerebro.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Services;

public interface IWhatsAppService
{
    bool Configurado { get; }
    Task<bool> EnviarAsync(string numero, string texto, CancellationToken ct = default);
    /// <summary>Envia para todos os números ativos (até 3) das contas com acesso à propriedade.</summary>
    Task EnviarParaPropriedadeAsync(int propriedadeId, string texto, CancellationToken ct = default);
}

/// <summary>Integração com a ApiEnvios: POST /v1/instance/{id}/messages/chat com header Token.</summary>
public class WhatsAppService(HttpClient http, IConfiguration cfg, AppDbContext db, IWhatsAppAdminService admin, ILogger<WhatsAppService> log) : IWhatsAppService
{
    private string? Base => cfg["WhatsApp:BaseUrl"]?.TrimEnd('/');
    private string? Instance => cfg["WhatsApp:InstanceId"];
    private string? ApiKey => cfg["WhatsApp:ApiKey"];
    private string? Token => cfg["WhatsApp:Token"];
    public bool Configurado => !string.IsNullOrWhiteSpace(Base) && (!string.IsNullOrWhiteSpace(ApiKey) || (!string.IsNullOrWhiteSpace(Instance) && !string.IsNullOrWhiteSpace(Token)));

    public static string Normalizar(string n) => Regex.Replace(n ?? "", @"\D", "");

    public async Task<bool> EnviarAsync(string numero, string texto, CancellationToken ct = default)
    {
        if (!Configurado) return false;
        try
        {
            // Instância ativa escolhida pelo admin (painel WhatsApp); cai no InstanceId do appsettings.
            var inst = await admin.InstanciaAtivaAsync(ct) ?? Instance;
            using var req = !string.IsNullOrWhiteSpace(ApiKey)
                ? new HttpRequestMessage(HttpMethod.Post, $"{Base}/v1/messages") { Content = JsonContent.Create(new { to = Normalizar(numero), type = "text", text = texto, instanceId = inst }) }
                : new HttpRequestMessage(HttpMethod.Post, $"{Base}/v1/instance/{inst}/messages/chat") { Content = JsonContent.Create(new { to = Normalizar(numero), body = texto }) };
            if (!string.IsNullOrWhiteSpace(ApiKey)) req.Headers.Add("x-api-key", ApiKey); else req.Headers.Add("Token", Token);
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                log.LogWarning("WhatsApp falhou {Status}: {Body}", (int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex) { log.LogWarning(ex, "Erro ao enviar WhatsApp"); return false; }
    }

    public async Task EnviarParaPropriedadeAsync(int propriedadeId, string texto, CancellationToken ct = default)
    {
        if (!Configurado) return;
        var numeros = await db.PropriedadeMembros.Where(m => m.PropriedadeId == propriedadeId)
            .Join(db.ContatosWhatsApp.Where(c => c.Ativo), m => m.UsuarioId, c => c.UsuarioId, (m, c) => c.Numero)
            .Distinct().ToListAsync(ct);
        foreach (var n in numeros) await EnviarAsync(n, texto, ct);
    }
}
