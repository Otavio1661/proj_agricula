using System.Security.Claims;
using System.Text.Json;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroCerebro.Server.Controllers;

/// <summary>Painel do administrador para conectar números de WhatsApp (QR code) na ApiEnvios.</summary>
[ApiController, Authorize, Route("api/admin/whatsapp")]
public class WhatsAppAdminController(IWhatsAppAdminService wa, IConfiguration cfg) : ControllerBase
{
    private bool Admin => string.Equals(User.FindFirstValue(ClaimTypes.Email), cfg["Admin:Login"] ?? "admin", StringComparison.OrdinalIgnoreCase);

    private static string? Str(JsonElement e, params string[] keys)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        foreach (var k in keys)
            foreach (var p in e.EnumerateObject())
                if (string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase) && (p.Value.ValueKind == JsonValueKind.String || p.Value.ValueKind == JsonValueKind.Number))
                    return p.Value.ToString();
        return null;
    }

    [HttpGet]
    public async Task<ActionResult<WaAdminStatusDto>> Status(CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var ativa = await wa.InstanciaAtivaAsync(ct);
        if (!wa.ApiKeyConfigurada)
            return new WaAdminStatusDto(false, ativa, "Configure WhatsApp:ApiKey (API key da conta na ApiEnvios) para gerenciar instâncias e gerar o QR.", new());
        var (_, json, erro) = await wa.ChamarAsync(HttpMethod.Get, "/v1/instances?limit=100", null, ct);
        var lista = new List<InstanciaWaDto>();
        if (json is { } j)
        {
            JsonElement arr = default;
            if (j.ValueKind == JsonValueKind.Array) arr = j;
            else if (j.ValueKind == JsonValueKind.Object)
                foreach (var p in j.EnumerateObject()) if (p.Value.ValueKind == JsonValueKind.Array) { arr = p.Value; break; }
            if (arr.ValueKind == JsonValueKind.Array)
            {
                var itens = arr.EnumerateArray().ToList();
                var numeros = await Task.WhenAll(itens.Select(i => ListarNumerosAsync(Str(i, "id") ?? "", ct)));
                for (var k = 0; k < itens.Count; k++)
                {
                    var i = itens[k]; var id = Str(i, "id") ?? "";
                    lista.Add(new InstanciaWaDto(id, Str(i, "name", "nome"), Str(i, "connection", "connectionState", "status"), Str(i, "provider"), id == ativa, numeros[k]));
                }
            }
        }
        return new WaAdminStatusDto(true, ativa, erro, lista);
    }

    private async Task<List<NumeroWaDto>> ListarNumerosAsync(string instId, CancellationToken ct)
    {
        var res = new List<NumeroWaDto>();
        var (_, json, _) = await wa.ChamarAsync(HttpMethod.Get, $"/v1/instances/{Uri.EscapeDataString(instId)}/numbers", null, ct);
        if (json is { ValueKind: JsonValueKind.Array } a)
            foreach (var n in a.EnumerateArray())
                res.Add(new NumeroWaDto(Str(n, "id") ?? "", Str(n, "label"), Str(n, "phone"), Str(n, "provider"), Str(n, "connectionState", "status")));
        return res;
    }

    [HttpPost("instancias")]
    public async Task<IActionResult> Criar(CriarInstanciaWaRequest r, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe um nome para a instância.");
        var slug = new string(r.Nome.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Post, "/v1/instances",
            new { provider = r.Provider ?? cfg["WhatsApp:Provider"] ?? "EVOLUTION", name = r.Nome.Trim(), slug }, ct);
        return erro != null ? StatusCode(st, erro) : Ok(json);
    }

    [HttpPost("instancias/{id}/conectar")]
    public async Task<IActionResult> Conectar(string id, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Post, $"/v1/instances/{Uri.EscapeDataString(id)}/connect", null, ct);
        return erro != null ? StatusCode(st, erro) : Ok(json);
    }

    /// <summary>Retorna o QR (data-URL/base64) e o status; o cliente faz polling até CONNECTED.</summary>
    [HttpGet("instancias/{id}/qr")]
    public async Task<ActionResult<QrWaDto>> Qr(string id, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Get, $"/v1/instances/{Uri.EscapeDataString(id)}/qr", null, ct);
        string? qr = null, status = null;
        if (json is { } j)
        {
            qr = Str(j, "qrCode", "qr", "base64", "code");
            status = Str(j, "connectionState", "connection");
            if (qr == null && j.ValueKind == JsonValueKind.Object && j.TryGetProperty("data", out var d)) qr = Str(d, "qr", "qrcode", "base64");
        }
        var (_, sj, _) = await wa.ChamarAsync(HttpMethod.Get, $"/v1/instances/{Uri.EscapeDataString(id)}/status", null, ct);
        if (sj is { } s) status = Str(s, "connectionState", "connection", "status", "state") ?? status;
        if (erro != null && qr == null && status == null) return StatusCode(st, erro);
        return new QrWaDto(qr, status, json?.GetRawText());
    }

    [HttpPost("instancias/{id}/reconectar")]
    public async Task<IActionResult> Reconectar(string id, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Post, $"/v1/instances/{Uri.EscapeDataString(id)}/rotate", null, ct);
        return erro != null ? StatusCode(st, erro) : Ok(json);
    }

    [HttpDelete("instancias/{id}")]
    public async Task<IActionResult> Excluir(string id, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, _, erro) = await wa.ChamarAsync(HttpMethod.Delete, $"/v1/instances/{Uri.EscapeDataString(id)}?confirm=true", null, ct);
        return erro != null ? StatusCode(st, erro) : NoContent();
    }

    /// <summary>Passo 2 da doc: adiciona um número ao pool da instância (sem isso a instância nunca envia).</summary>
    [HttpPost("instancias/{id}/numeros")]
    public async Task<IActionResult> AdicionarNumero(string id, AdicionarNumeroWaRequest r, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Post, $"/v1/instances/{Uri.EscapeDataString(id)}/numbers",
            new { provider = r.Provider ?? cfg["WhatsApp:Provider"] ?? "EVOLUTION", label = string.IsNullOrWhiteSpace(r.Label) ? "Principal" : r.Label.Trim() }, ct);
        return erro != null ? StatusCode(st, erro) : Ok(json);
    }

    /// <summary>Passo 3 da doc: conecta o número e devolve o QR code.</summary>
    [HttpPost("instancias/{id}/numeros/{nid}/conectar")]
    public async Task<ActionResult<QrWaDto>> ConectarNumero(string id, string nid, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, json, erro) = await wa.ChamarAsync(HttpMethod.Post, $"/v1/instances/{Uri.EscapeDataString(id)}/numbers/{Uri.EscapeDataString(nid)}/connect", null, ct);
        if (erro != null) return StatusCode(st, erro);
        return new QrWaDto(json is { } j ? Str(j, "qrCode", "qr") : null, json is { } k ? Str(k, "connectionState") : null, json?.GetRawText());
    }

    /// <summary>Passo 4 da doc: polling. /status é a fonte de verdade; /qr devolve o QR em cache.</summary>
    [HttpGet("instancias/{id}/numeros/{nid}/qr")]
    public async Task<ActionResult<QrWaDto>> QrNumero(string id, string nid, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var baseP = $"/v1/instances/{Uri.EscapeDataString(id)}/numbers/{Uri.EscapeDataString(nid)}";
        var (_, sj, _) = await wa.ChamarAsync(HttpMethod.Get, baseP + "/status", null, ct);
        var status = sj is { } s ? Str(s, "connectionState", "status") : null;
        string? qr = null; string? bruto = null;
        if (!string.Equals(status, "CONNECTED", StringComparison.OrdinalIgnoreCase))
        {
            var (st, qj, erro) = await wa.ChamarAsync(HttpMethod.Get, baseP + "/qr", null, ct);
            if (qj is { } q) { qr = Str(q, "qrCode", "qr"); bruto = q.GetRawText(); status ??= Str(q, "connectionState"); }
            else if (erro != null && status == null) return StatusCode(st, erro);
        }
        return new QrWaDto(qr, status, bruto);
    }

    [HttpDelete("instancias/{id}/numeros/{nid}")]
    public async Task<IActionResult> RemoverNumero(string id, string nid, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        var (st, _, erro) = await wa.ChamarAsync(HttpMethod.Delete, $"/v1/instances/{Uri.EscapeDataString(id)}/numbers/{Uri.EscapeDataString(nid)}", null, ct);
        return erro != null ? StatusCode(st, erro) : NoContent();
    }

    [HttpPut("ativa/{id}")]
    public async Task<IActionResult> Ativar(string id, CancellationToken ct)
    {
        if (!Admin) return Forbid();
        await wa.DefinirAtivaAsync(id, ct);
        return NoContent();
    }
}
