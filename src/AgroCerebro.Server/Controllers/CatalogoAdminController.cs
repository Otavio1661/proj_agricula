using System.Security.Claims;
using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

/// <summary>Escrita do catálogo (bioinsumos, culturas, pragas, solos) restrita ao administrador.</summary>
[ApiController, Authorize, Route("api/catalogo")]
public class CatalogoAdminController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    private bool EAdmin => string.Equals(User.FindFirstValue(ClaimTypes.Email), cfg["Admin:Login"] ?? "admin", StringComparison.OrdinalIgnoreCase);
    private ActionResult? Nega() => EAdmin ? null : StatusCode(403, "Apenas o administrador pode alterar o catálogo.");

    [HttpGet("eu-admin")] public bool EuAdmin() => EAdmin;

    // ===== Bioinsumos =====
    private static string? Valida(BioinsumoSalvarRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return "Informe o nome do produto.";
        if (string.IsNullOrWhiteSpace(r.Tipo)) return "Informe o tipo (Fungo, Bactéria, Vírus…).";
        if (r.TempMin > r.TempMax) return "Temperatura mínima não pode ser maior que a máxima.";
        if (r.UmidadeMin > r.UmidadeMax || r.UmidadeMin < 0 || r.UmidadeMax > 100) return "Umidade deve estar entre 0 e 100% (mín ≤ máx).";
        if (r.BbchMin > r.BbchMax || r.BbchMin < 0 || r.BbchMax > 99) return "BBCH deve estar entre 0 e 99 (mín ≤ máx).";
        if (r.ChuvaMaxMm < 0 || r.VentoMaxKmh < 0) return "Chuva e vento máximos não podem ser negativos.";
        if (r.ValidadeMeses < 0) return "Validade (meses) inválida.";
        return null;
    }

    private static void Aplica(Bioinsumo b, BioinsumoSalvarRequest r)
    {
        (b.Nome, b.Tipo, b.AgenteAtivo, b.Descricao) = (r.Nome.Trim(), r.Tipo.Trim(), (r.AgenteAtivo ?? "").Trim(), r.Descricao);
        (b.TempMin, b.TempMax, b.UmidadeMin, b.UmidadeMax) = (r.TempMin, r.TempMax, r.UmidadeMin, r.UmidadeMax);
        (b.BbchMin, b.BbchMax, b.ChuvaMaxMm, b.VentoMaxKmh) = (r.BbchMin, r.BbchMax, r.ChuvaMaxMm, r.VentoMaxKmh);
        (b.EvitarSolDireto, b.ArmazTempMax, b.ArmazInstrucoes, b.ValidadeMeses) = (r.EvitarSolDireto, r.ArmazTempMax, r.ArmazInstrucoes, r.ValidadeMeses);
    }

    [HttpGet("bioinsumos/{id:int}/editar")]
    public async Task<ActionResult<BioinsumoEdicaoDto>> Editar(int id)
    {
        var b = await db.Bioinsumos.Include(x => x.Culturas).Include(x => x.Pragas).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        return new BioinsumoEdicaoDto(new BioinsumoSalvarRequest
        {
            Nome = b.Nome, Tipo = b.Tipo, AgenteAtivo = b.AgenteAtivo, Descricao = b.Descricao, TempMin = b.TempMin, TempMax = b.TempMax,
            UmidadeMin = b.UmidadeMin, UmidadeMax = b.UmidadeMax, BbchMin = b.BbchMin, BbchMax = b.BbchMax, ChuvaMaxMm = b.ChuvaMaxMm,
            VentoMaxKmh = b.VentoMaxKmh, EvitarSolDireto = b.EvitarSolDireto, ArmazTempMax = b.ArmazTempMax, ArmazInstrucoes = b.ArmazInstrucoes,
            ValidadeMeses = b.ValidadeMeses, CulturaIds = b.Culturas.Select(c => c.CulturaId).ToList(), PragaIds = b.Pragas.Select(p => p.PragaId).ToList()
        }, b.Id);
    }

    [HttpPost("bioinsumos")]
    public async Task<ActionResult<int>> CriarBio(BioinsumoSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (Valida(r) is { } e) return BadRequest(e);
        if (await db.Bioinsumos.AnyAsync(x => x.Nome == r.Nome.Trim())) return Conflict("Já existe um bioinsumo com esse nome.");
        var b = new Bioinsumo();
        Aplica(b, r);
        foreach (var c in r.CulturaIds.Distinct()) b.Culturas.Add(new BioinsumoCultura { CulturaId = c });
        foreach (var p in r.PragaIds.Distinct()) b.Pragas.Add(new BioinsumoPraga { PragaId = p });
        db.Bioinsumos.Add(b);
        await db.SaveChangesAsync();
        return b.Id;
    }

    [HttpPut("bioinsumos/{id:int}")]
    public async Task<IActionResult> AtualizarBio(int id, BioinsumoSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (Valida(r) is { } e) return BadRequest(e);
        var b = await db.Bioinsumos.Include(x => x.Culturas).Include(x => x.Pragas).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        if (await db.Bioinsumos.AnyAsync(x => x.Nome == r.Nome.Trim() && x.Id != id)) return Conflict("Já existe um bioinsumo com esse nome.");
        Aplica(b, r);
        b.Culturas.Clear(); b.Pragas.Clear();
        foreach (var c in r.CulturaIds.Distinct()) b.Culturas.Add(new BioinsumoCultura { BioinsumoId = id, CulturaId = c });
        foreach (var p in r.PragaIds.Distinct()) b.Pragas.Add(new BioinsumoPraga { BioinsumoId = id, PragaId = p });
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("bioinsumos/{id:int}")]
    public async Task<IActionResult> ExcluirBio(int id)
    {
        if (Nega() is { } n) return n;
        var b = await db.Bioinsumos.Include(x => x.Culturas).Include(x => x.Pragas).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        var lotes = await db.LotesEstoque.CountAsync(l => l.BioinsumoId == id);
        if (lotes > 0) return Conflict($"Não é possível excluir: há {lotes} lote(s) em estoque deste produto. Remova os lotes primeiro.");
        db.Bioinsumos.Remove(b);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Culturas =====
    [HttpPost("culturas")]
    public async Task<ActionResult<CulturaDto>> CriarCultura(CulturaSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome da cultura.");
        if (await db.Culturas.AnyAsync(c => c.Nome == r.Nome.Trim())) return Conflict("Cultura já cadastrada.");
        var c = new Cultura { Nome = r.Nome.Trim(), Descricao = r.Descricao };
        db.Culturas.Add(c);
        await db.SaveChangesAsync();
        return new CulturaDto(c.Id, c.Nome, c.Descricao);
    }

    [HttpPut("culturas/{id:int}")]
    public async Task<IActionResult> AtualizarCultura(int id, CulturaSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome da cultura.");
        var c = await db.Culturas.FindAsync(id);
        if (c == null) return NotFound();
        if (await db.Culturas.AnyAsync(x => x.Nome == r.Nome.Trim() && x.Id != id)) return Conflict("Já existe outra cultura com esse nome.");
        (c.Nome, c.Descricao) = (r.Nome.Trim(), r.Descricao);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("culturas/{id:int}")]
    public async Task<IActionResult> ExcluirCultura(int id)
    {
        if (Nega() is { } n) return n;
        var c = await db.Culturas.FindAsync(id);
        if (c == null) return NotFound();
        var plantios = await db.Plantios.CountAsync(p => p.CulturaId == id);
        if (plantios > 0) return Conflict($"Não é possível excluir: a cultura é usada em {plantios} plantio(s).");
        db.BioinsumoCulturas.RemoveRange(db.BioinsumoCulturas.Where(x => x.CulturaId == id));
        db.Culturas.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Pragas =====
    [HttpPost("pragas")]
    public async Task<ActionResult<PragaDto>> CriarPraga(PragaSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome da praga.");
        if (await db.Pragas.AnyAsync(c => c.Nome == r.Nome.Trim())) return Conflict("Praga já cadastrada.");
        var p = new Praga { Nome = r.Nome.Trim(), Tipo = string.IsNullOrWhiteSpace(r.Tipo) ? "Inseto" : r.Tipo.Trim() };
        db.Pragas.Add(p);
        await db.SaveChangesAsync();
        return new PragaDto(p.Id, p.Nome, p.Tipo);
    }

    [HttpPut("pragas/{id:int}")]
    public async Task<IActionResult> AtualizarPraga(int id, PragaSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome da praga.");
        var p = await db.Pragas.FindAsync(id);
        if (p == null) return NotFound();
        if (await db.Pragas.AnyAsync(x => x.Nome == r.Nome.Trim() && x.Id != id)) return Conflict("Já existe outra praga com esse nome.");
        (p.Nome, p.Tipo) = (r.Nome.Trim(), string.IsNullOrWhiteSpace(r.Tipo) ? p.Tipo : r.Tipo.Trim());
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("pragas/{id:int}")]
    public async Task<IActionResult> ExcluirPraga(int id)
    {
        if (Nega() is { } n) return n;
        var p = await db.Pragas.FindAsync(id);
        if (p == null) return NotFound();
        var oc = await db.OcorrenciasPraga.CountAsync(o => o.PragaId == id);
        if (oc > 0) return Conflict($"Não é possível excluir: a praga tem {oc} ocorrência(s) registrada(s).");
        db.BioinsumoPragas.RemoveRange(db.BioinsumoPragas.Where(x => x.PragaId == id));
        db.Pragas.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Tipos de solo =====
    [HttpPost("solos")]
    public async Task<ActionResult<TipoSoloDto>> CriarSolo(TipoSoloSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome do tipo de solo.");
        if (await db.TiposSolo.AnyAsync(c => c.Nome == r.Nome.Trim())) return Conflict("Tipo de solo já cadastrado.");
        var s = new TipoSolo { Nome = r.Nome.Trim(), Textura = (r.Textura ?? "").Trim() };
        db.TiposSolo.Add(s);
        await db.SaveChangesAsync();
        return new TipoSoloDto(s.Id, s.Nome, s.Textura);
    }

    [HttpPut("solos/{id:int}")]
    public async Task<IActionResult> AtualizarSolo(int id, TipoSoloSalvarRequest r)
    {
        if (Nega() is { } n) return n;
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Informe o nome do tipo de solo.");
        var s = await db.TiposSolo.FindAsync(id);
        if (s == null) return NotFound();
        if (await db.TiposSolo.AnyAsync(x => x.Nome == r.Nome.Trim() && x.Id != id)) return Conflict("Já existe outro tipo de solo com esse nome.");
        (s.Nome, s.Textura) = (r.Nome.Trim(), (r.Textura ?? "").Trim());
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("solos/{id:int}")]
    public async Task<IActionResult> ExcluirSolo(int id)
    {
        if (Nega() is { } n) return n;
        var s = await db.TiposSolo.FindAsync(id);
        if (s == null) return NotFound();
        var tal = await db.Talhoes.CountAsync(t => t.TipoSoloId == id);
        if (tal > 0) return Conflict($"Não é possível excluir: o solo é usado em {tal} talhão(ões).");
        db.TiposSolo.Remove(s);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
