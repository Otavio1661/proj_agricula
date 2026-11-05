using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

[ApiController, Authorize, Route("api")]
public class CampoController(AppDbContext db, IAcessoService acesso, IWebHostEnvironment env, IWhatsAppService wa, IConfiguration cfg) : ControllerBase
{
    private int Uid => acesso.UsuarioId(User);

    private static PlantioDto? MapPlantio(Plantio? p) => p == null ? null : new(p.Id, p.TalhaoId, p.CulturaId, p.Cultura?.Nome ?? "", p.DataPlantio, p.BbchAtual, p.Ativo);
    private static TalhaoDto Map(Talhao t) => new(t.Id, t.PropriedadeId, t.Nome, t.AreaHa, t.GeoJson, t.TipoSoloId, t.TipoSolo?.Nome,
        MapPlantio(t.Plantios.FirstOrDefault(p => p.Ativo)));

    // ===== Catálogo =====
    [HttpGet("catalogo/culturas")] public async Task<List<CulturaDto>> Culturas() => await db.Culturas.Select(c => new CulturaDto(c.Id, c.Nome, c.Descricao)).ToListAsync();
    [HttpGet("catalogo/pragas")] public async Task<List<PragaDto>> Pragas() => await db.Pragas.Select(c => new PragaDto(c.Id, c.Nome, c.Tipo)).ToListAsync();
    [HttpGet("catalogo/solos")] public async Task<List<TipoSoloDto>> Solos() => await db.TiposSolo.Select(c => new TipoSoloDto(c.Id, c.Nome, c.Textura)).ToListAsync();

    [HttpGet("catalogo/bioinsumos")]
    public async Task<List<BioinsumoDto>> Bioinsumos() =>
        await db.Bioinsumos.OrderBy(b => b.Nome).Select(b => new BioinsumoDto(b.Id, b.Nome, b.Tipo, b.AgenteAtivo, b.Descricao, b.TempMin, b.TempMax, b.UmidadeMin, b.UmidadeMax,
            b.BbchMin, b.BbchMax, b.ChuvaMaxMm, b.VentoMaxKmh, b.EvitarSolDireto, b.ArmazTempMax, b.ArmazInstrucoes, b.ValidadeMeses,
            b.Culturas.Select(c => c.Cultura!.Nome).ToList(), b.Pragas.Select(p => p.Praga!.Nome).ToList())).ToListAsync();

    // ===== Talhões =====
    [HttpGet("propriedades/{id:int}/talhoes")]
    public async Task<ActionResult<List<TalhaoDto>>> Talhoes(int id)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        var ts = await db.Talhoes.Where(t => t.PropriedadeId == id).Include(t => t.TipoSolo).Include(t => t.Plantios.Where(p => p.Ativo)).ThenInclude(p => p.Cultura).ToListAsync();
        return ts.Select(Map).ToList();
    }

    [HttpPost("propriedades/{id:int}/talhoes")]
    public async Task<ActionResult<TalhaoDto>> CriarTalhao(int id, TalhaoSalvarRequest r)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return Forbid();
        var t = new Talhao { PropriedadeId = id, Nome = r.Nome, AreaHa = r.AreaHa, GeoJson = r.GeoJson, TipoSoloId = r.TipoSoloId };
        db.Talhoes.Add(t);
        await db.SaveChangesAsync();
        return Map(t);
    }

    [HttpPut("talhoes/{id:int}")]
    public async Task<ActionResult<TalhaoDto>> AtualizarTalhao(int id, TalhaoSalvarRequest r)
    {
        var t = await db.Talhoes.Include(x => x.TipoSolo).Include(x => x.Plantios).ThenInclude(p => p.Cultura).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, t.PropriedadeId)) return Forbid();
        (t.Nome, t.AreaHa, t.GeoJson, t.TipoSoloId) = (r.Nome, r.AreaHa, r.GeoJson, r.TipoSoloId);
        await db.SaveChangesAsync();
        return Map(t);
    }

    [HttpDelete("talhoes/{id:int}")]
    public async Task<IActionResult> ExcluirTalhao(int id)
    {
        var t = await db.Talhoes.FindAsync(id);
        if (t == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, t.PropriedadeId)) return Forbid();
        db.Talhoes.Remove(t);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Plantio / monitoramento / pragas =====
    [HttpPost("talhoes/{id:int}/plantio")]
    public async Task<ActionResult<PlantioDto>> Plantio(int id, PlantioSalvarRequest r)
    {
        var t = await db.Talhoes.Include(x => x.Plantios).FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, t.PropriedadeId)) return Forbid();
        var cultura = await db.Culturas.FindAsync(r.CulturaId);
        if (cultura == null) return BadRequest("Cultura inválida.");
        foreach (var p in t.Plantios) p.Ativo = false; // encerra o ciclo anterior
        var novo = new Plantio { TalhaoId = id, CulturaId = r.CulturaId, DataPlantio = r.DataPlantio, BbchAtual = Math.Clamp(r.BbchAtual, 0, 99), Cultura = cultura };
        db.Plantios.Add(novo);
        await db.SaveChangesAsync();
        return MapPlantio(novo)!;
    }

    [HttpPost("monitoramentos")]
    public async Task<IActionResult> Monitorar(MonitoramentoRequest r)
    {
        var p = await db.Plantios.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == r.PlantioId);
        if (p == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, p.Talhao!.PropriedadeId)) return Forbid();
        p.BbchAtual = Math.Clamp(r.Bbch, 0, 99);
        db.Monitoramentos.Add(new Monitoramento { PlantioId = p.Id, UsuarioId = Uid, Bbch = p.BbchAtual, Notas = r.Notas });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("ocorrencias")]
    public async Task<IActionResult> Ocorrencia(OcorrenciaPragaRequest r)
    {
        var t = await db.Talhoes.FindAsync(r.TalhaoId);
        if (t == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, t.PropriedadeId)) return Forbid();
        db.OcorrenciasPraga.Add(new OcorrenciaPraga { TalhaoId = r.TalhaoId, PragaId = r.PragaId, UsuarioId = Uid, Severidade = Math.Clamp(r.Severidade, 0, 5), Latitude = r.Latitude, Longitude = r.Longitude, Notas = r.Notas });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("talhoes/{id:int}/ocorrencias")]
    public async Task<ActionResult<List<OcorrenciaPragaDto>>> Ocorrencias(int id)
    {
        var t = await db.Talhoes.FindAsync(id);
        if (t == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, t.PropriedadeId)) return Forbid();
        return await db.OcorrenciasPraga.Where(o => o.TalhaoId == id).OrderByDescending(o => o.Data).Take(100)
            .Select(o => new OcorrenciaPragaDto(o.Id, o.TalhaoId, o.PragaId, o.Praga!.Nome, o.Severidade, o.Data, o.Latitude, o.Longitude, o.Notas)).ToListAsync();
    }

    // ===== Drone =====
    private static MissaoDroneDto Map(MissaoDrone m) => new(m.Id, m.TalhaoId, m.Data, m.Equipamento, m.AlturaM, m.Status, m.Imagens.Select(Map).ToList());
    private static ImagemDroneDto Map(ImagemDrone i) => new(i.Id, i.MissaoId, "/uploads/" + i.Arquivo, i.Tipo, i.Sul, i.Oeste, i.Norte, i.Leste, i.GsdCm);

    [HttpGet("talhoes/{id:int}/missoes")]
    public async Task<ActionResult<List<MissaoDroneDto>>> Missoes(int id)
    {
        var t = await db.Talhoes.FindAsync(id);
        if (t == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, t.PropriedadeId)) return Forbid();
        return (await db.MissoesDrone.Include(m => m.Imagens).Where(m => m.TalhaoId == id).OrderByDescending(m => m.Data).ToListAsync()).Select(Map).ToList();
    }

    [HttpPost("missoes")]
    public async Task<ActionResult<MissaoDroneDto>> CriarMissao(MissaoDroneRequest r)
    {
        var t = await db.Talhoes.FindAsync(r.TalhaoId);
        if (t == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, t.PropriedadeId)) return Forbid();
        var m = new MissaoDrone { TalhaoId = r.TalhaoId, Data = r.Data, Equipamento = r.Equipamento, AlturaM = r.AlturaM, Notas = r.Notas };
        db.MissoesDrone.Add(m);
        await db.SaveChangesAsync();
        return Map(m);
    }

    [HttpPost("missoes/{id:int}/imagens"), RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<ImagemDroneDto>> UploadImagem(int id, [FromForm] IFormFile arquivo, [FromForm] string tipo, [FromForm] string sul, [FromForm] string oeste, [FromForm] string norte, [FromForm] string leste, [FromForm] string? gsdCm)
    {
        var m = await db.MissoesDrone.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, m.Talhao!.PropriedadeId)) return Forbid();
        // Parse invariante: o cliente envia ponto decimal, independente da cultura do servidor (pt-BR usa vírgula).
        static double D(string s) => double.Parse(s.Trim().Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
        if (!double.TryParse(sul?.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) || !double.TryParse(oeste?.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) || !double.TryParse(norte?.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) || !double.TryParse(leste?.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return BadRequest("Limites inválidos.");
        double sulD = D(sul), oesteD = D(oeste), norteD = D(norte), lesteD = D(leste);
        double? gsd = string.IsNullOrWhiteSpace(gsdCm) ? null : D(gsdCm);
        var ext = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp")) return BadRequest("Envie JPG, PNG ou WEBP (exporte o ortomosaico do WebODM/Pix4D em PNG/JPG + limites).");
        if (norteD <= sulD || lesteD <= oesteD) return BadRequest("Limites inválidos (norte>sul, leste>oeste).");
        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
        Directory.CreateDirectory(dir);
        var nome = $"{Guid.NewGuid():N}{ext}";
        await using (var fs = System.IO.File.Create(Path.Combine(dir, nome))) await arquivo.CopyToAsync(fs);
        var img = new ImagemDrone { MissaoId = id, Arquivo = nome, Tipo = string.IsNullOrWhiteSpace(tipo) ? "RGB" : tipo, Sul = sulD, Oeste = oesteD, Norte = norteD, Leste = lesteD, GsdCm = gsd };
        db.ImagensDrone.Add(img);
        await db.SaveChangesAsync();
        return Map(img);
    }

    // ===== WhatsApp: até 3 números por conta =====
    private const int LimiteNumeros = 3;
    private static ContatoWhatsAppDto Map(ContatoWhatsApp c) => new(c.Id, c.Nome, c.Numero, c.Ativo);

    [HttpGet("whatsapp")]
    public async Task<WhatsAppStatusDto> WhatsApp() =>
        new(wa.Configurado, LimiteNumeros, (await db.ContatosWhatsApp.Where(c => c.UsuarioId == Uid).ToListAsync()).Select(Map).ToList());

    [HttpPost("whatsapp/numeros")]
    public async Task<ActionResult<ContatoWhatsAppDto>> AddNumero(ContatoWhatsAppRequest r)
    {
        var n = WhatsAppService.Normalizar(r.Numero);
        if (n.Length is < 10 or > 15) return BadRequest("Número inválido. Use DDI+DDD+número, ex.: 5511999998888.");
        if (n.Length <= 11) n = "55" + n; // assume Brasil quando vier só DDD+número
        if (await db.ContatosWhatsApp.CountAsync(c => c.UsuarioId == Uid) >= LimiteNumeros) return BadRequest($"Limite de {LimiteNumeros} números por conta.");
        if (await db.ContatosWhatsApp.AnyAsync(c => c.UsuarioId == Uid && c.Numero == n)) return Conflict("Número já cadastrado.");
        var c = new ContatoWhatsApp { UsuarioId = Uid, Nome = string.IsNullOrWhiteSpace(r.Nome) ? "Contato" : r.Nome.Trim(), Numero = n };
        db.ContatosWhatsApp.Add(c);
        await db.SaveChangesAsync();
        return Map(c);
    }

    [HttpDelete("whatsapp/numeros/{id:int}")]
    public async Task<IActionResult> RemoverNumero(int id)
    {
        var c = await db.ContatosWhatsApp.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == Uid);
        if (c == null) return NotFound();
        db.ContatosWhatsApp.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("whatsapp/numeros/{id:int}/testar")]
    public async Task<IActionResult> Testar(int id)
    {
        var c = await db.ContatosWhatsApp.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == Uid);
        if (c == null) return NotFound();
        if (!wa.Configurado) return StatusCode(503, "Integração WhatsApp não configurada no servidor.");
        var ok = await wa.EnviarAsync(c.Numero, "🌱 *AgroCérebro*: teste de integração com WhatsApp. Você receberá alertas de janela de aplicação e armazenamento aqui.");
        return ok ? NoContent() : StatusCode(502, "Falha ao enviar pela ApiEnvios.");
    }
}
