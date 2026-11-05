using System.Text.Json;

namespace AgroCerebro.Client.Services;

public static class Forms
{
    /// <summary>Lê a mensagem de erro devolvida pelo servidor (texto puro, JSON string ou ProblemDetails).</summary>
    public static async Task<string> ErroAsync(HttpResponseMessage r, string padrao)
    {
        try
        {
            var t = (await r.Content.ReadAsStringAsync()).Trim();
            if (!string.IsNullOrEmpty(t))
            {
                if (t.StartsWith('"')) t = JsonSerializer.Deserialize<string>(t) ?? t;
                else if (t.StartsWith('{'))
                {
                    using var d = JsonDocument.Parse(t);
                    if (d.RootElement.TryGetProperty("detail", out var det)) t = det.GetString() ?? t;
                    else if (d.RootElement.TryGetProperty("title", out var ti)) t = ti.GetString() ?? t;
                }
                if (t.Length is > 0 and < 300 && !t.StartsWith('<')) return t;
            }
        }
        catch { }
        return r.StatusCode switch
        {
            System.Net.HttpStatusCode.Forbidden => "Você não tem permissão para esta ação.",
            System.Net.HttpStatusCode.Unauthorized => "Sessão expirada. Entre novamente.",
            _ => $"{padrao} (erro {(int)r.StatusCode})."
        };
    }

    public static string? Obrigatorio(string? v, string campo) => string.IsNullOrWhiteSpace(v) ? $"Informe {campo}." : null;
    public static string? Faixa(double v, double min, double max, string campo) => v < min || v > max ? $"{campo} deve estar entre {min:0.####} e {max:0.####}." : null;
}
