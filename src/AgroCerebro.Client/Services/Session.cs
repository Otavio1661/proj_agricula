using System.Net.Http.Json;
using System.Net.Http.Headers;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AgroCerebro.Client.Services;

public class Session
{
    private readonly IJSRuntime _js;
    private readonly HttpClient _http;
    public Session(IJSRuntime js, IHttpClientFactory f) { _js = js; _http = f.CreateClient("api"); }

    public string? Token { get; private set; }
    public UsuarioDto? Usuario { get; private set; }
    public int PropriedadeId { get; private set; }
    public List<PropriedadeDto> Propriedades { get; private set; } = new();
    public bool Carregado { get; private set; }
    public bool Autenticado => !string.IsNullOrEmpty(Token);
    public PropriedadeDto? Propriedade => Propriedades.FirstOrDefault(p => p.Id == PropriedadeId);
    public event Action? Mudou;

    public async Task CarregarAsync()
    {
        if (Carregado) return;
        Token = await _js.InvokeAsync<string?>("agro.get", "token");
        var u = await _js.InvokeAsync<string?>("agro.get", "usuario");
        if (u != null) try { Usuario = System.Text.Json.JsonSerializer.Deserialize<UsuarioDto>(u); } catch { }
        int.TryParse(await _js.InvokeAsync<string?>("agro.get", "propriedade"), out var pid);
        PropriedadeId = pid;
        Carregado = true;
        if (Autenticado) await RecarregarPropriedadesAsync();
    }

    public async Task EntrarAsync(AuthResponse r)
    {
        Token = r.Token; Usuario = r.Usuario;
        await _js.InvokeVoidAsync("agro.set", "token", r.Token);
        await _js.InvokeVoidAsync("agro.set", "usuario", System.Text.Json.JsonSerializer.Serialize(r.Usuario));
        await RecarregarPropriedadesAsync();
    }

    public async Task SairAsync()
    {
        Token = null; Usuario = null; Propriedades = new(); PropriedadeId = 0;
        await _js.InvokeVoidAsync("agro.remove", "token");
        await _js.InvokeVoidAsync("agro.remove", "usuario");
        Mudou?.Invoke();
    }

    public async Task RecarregarPropriedadesAsync()
    {
        try { Propriedades = await _http.GetFromJsonAsync<List<PropriedadeDto>>("api/propriedades") ?? new(); }
        catch { /* offline */ }
        if (Propriedades.Count > 0 && Propriedades.All(p => p.Id != PropriedadeId))
            await SelecionarAsync(Propriedades[0].Id);
        else Mudou?.Invoke();
    }

    public async Task SelecionarAsync(int id)
    {
        PropriedadeId = id;
        await _js.InvokeVoidAsync("agro.set", "propriedade", id.ToString());
        Mudou?.Invoke();
    }
}

public class AuthHandler : DelegatingHandler
{
    private readonly IServiceProvider _sp;
    public AuthHandler(IServiceProvider sp) { _sp = sp; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var s = _sp.GetRequiredService<Session>();
        if (s.Autenticado) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", s.Token);
        var resp = await base.SendAsync(req, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized && s.Autenticado)
        {
            await s.SairAsync();
            _sp.GetRequiredService<NavigationManager>().NavigateTo("/login");
        }
        return resp;
    }
}

public static class Bbch
{
    public static string Estagio(int b) => b switch
    {
        < 10 => "0 – Germinação / emergência",
        < 20 => "1 – Desenvolvimento das folhas",
        < 30 => "2 – Perfilhamento / ramificação",
        < 40 => "3 – Alongamento do caule",
        < 50 => "4 – Desenvolvimento de partes colhíveis",
        < 60 => "5 – Emergência da inflorescência",
        < 70 => "6 – Floração",
        < 80 => "7 – Desenvolvimento do fruto",
        < 90 => "8 – Maturação do fruto e semente",
        _ => "9 – Senescência"
    };
}
