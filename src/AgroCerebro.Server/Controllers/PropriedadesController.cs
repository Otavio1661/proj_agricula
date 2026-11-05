using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

[ApiController, Authorize, Route("api")]
public class PropriedadesController(AppDbContext db, IAcessoService acesso, IClimaService clima, IRecomendacaoService rec) : ControllerBase
{
    private int Uid => acesso.UsuarioId(User);
    private static PropriedadeDto Map(Propriedade p) => new(p.Id, p.Nome, p.Municipio, p.Uf, p.Latitude, p.Longitude, p.AreaHa);

    [HttpGet("propriedades")]
    public async Task<List<PropriedadeDto>> Listar() =>
        await db.PropriedadeMembros.Where(m => m.UsuarioId == Uid).Select(m => m.Propriedade!).OrderBy(p => p.Nome)
            .Select(p => new PropriedadeDto(p.Id, p.Nome, p.Municipio, p.Uf, p.Latitude, p.Longitude, p.AreaHa)).ToListAsync();

    [HttpPost("propriedades")]
    public async Task<ActionResult<PropriedadeDto>> Criar(PropriedadeSalvarRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome)) return BadRequest("Nome obrigatório.");
        var p = new Propriedade { Nome = r.Nome.Trim(), Municipio = r.Municipio, Uf = r.Uf, Latitude = r.Latitude, Longitude = r.Longitude, AreaHa = r.AreaHa, DonoId = Uid };
        p.Membros.Add(new PropriedadeMembro { UsuarioId = Uid, Papel = "Dono" });
        db.Propriedades.Add(p);
        await db.SaveChangesAsync();
        return Map(p);
    }

    [HttpPut("propriedades/{id:int}")]
    public async Task<ActionResult<PropriedadeDto>> Atualizar(int id, PropriedadeSalvarRequest r)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return Forbid();
        var p = await db.Propriedades.FindAsync(id);
        if (p == null) return NotFound();
        (p.Nome, p.Municipio, p.Uf, p.Latitude, p.Longitude, p.AreaHa) = (r.Nome, r.Municipio, r.Uf, r.Latitude, r.Longitude, r.AreaHa);
        await db.SaveChangesAsync();
        return Map(p);
    }

    [HttpDelete("propriedades/{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        if (await acesso.PapelAsync(Uid, id) != "Dono") return Forbid();
        var p = await db.Propriedades.FindAsync(id);
        if (p == null) return NotFound();
        db.Propriedades.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Membros (multiusuário) ----
    [HttpGet("propriedades/{id:int}/membros")]
    public async Task<ActionResult<List<MembroDto>>> Membros(int id)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        return await db.PropriedadeMembros.Where(m => m.PropriedadeId == id)
            .Select(m => new MembroDto(m.UsuarioId, m.Usuario!.Nome, m.Usuario.Email, m.Papel)).ToListAsync();
    }

    [HttpPost("propriedades/{id:int}/membros")]
    public async Task<IActionResult> Convidar(int id, ConvidarMembroRequest r)
    {
        if (await acesso.PapelAsync(Uid, id) != "Dono") return Forbid();
        if (r.Papel is not ("Agronomo" or "Operador")) return BadRequest("Papel deve ser Agronomo ou Operador.");
        var email = r.Email.Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        if (u == null) return NotFound("Usuário não cadastrado — peça para criar a conta primeiro.");
        var m = await db.PropriedadeMembros.FindAsync(id, u.Id);
        if (m == null) db.PropriedadeMembros.Add(new PropriedadeMembro { PropriedadeId = id, UsuarioId = u.Id, Papel = r.Papel });
        else if (m.Papel != "Dono") m.Papel = r.Papel;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Clima / recomendações / alertas ----
    [HttpGet("propriedades/{id:int}/clima")]
    public async Task<ActionResult<ClimaDto>> Clima(int id, CancellationToken ct)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        var p = await db.Propriedades.FindAsync(new object[] { id }, ct);
        try { return await clima.ObterAsync(p!.Latitude, p.Longitude, ct); }
        catch (HttpRequestException) { return StatusCode(502, "Serviço de clima indisponível."); }
    }

    [HttpGet("propriedades/{id:int}/recomendacoes")]
    public async Task<ActionResult<List<RecomendacaoDto>>> Recomendacoes(int id, CancellationToken ct)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        try { return await rec.CalcularAsync(id, ct); }
        catch (HttpRequestException) { return StatusCode(502, "Serviço de clima indisponível."); }
    }

    [HttpGet("propriedades/{id:int}/alertas")]
    public async Task<ActionResult<List<AlertaDto>>> Alertas(int id)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        return await db.Alertas.Where(a => a.PropriedadeId == id).OrderByDescending(a => a.CriadoEm).Take(100)
            .Select(a => new AlertaDto(a.Id, a.PropriedadeId, a.TalhaoId, a.BioinsumoId, a.Tipo, a.Nivel, a.Titulo, a.Mensagem, a.CriadoEm, a.Lido)).ToListAsync();
    }

    /// <summary>Força a avaliação agora (gera alertas + WhatsApp).</summary>
    [HttpPost("propriedades/{id:int}/alertas/gerar")]
    public async Task<ActionResult<int>> GerarAlertas(int id, CancellationToken ct)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return Forbid();
        try { return await rec.GerarAlertasAsync(id, ct); }
        catch (HttpRequestException) { return StatusCode(502, "Serviço de clima indisponível."); }
    }

    [HttpPost("alertas/{id:int}/lido")]
    public async Task<IActionResult> Lido(int id)
    {
        var a = await db.Alertas.FindAsync(id);
        if (a == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, a.PropriedadeId)) return Forbid();
        a.Lido = true;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Estoque ----
    [HttpGet("propriedades/{id:int}/estoque")]
    public async Task<ActionResult<List<LoteEstoqueDto>>> Estoque(int id)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        return await db.LotesEstoque.Where(l => l.PropriedadeId == id).OrderBy(l => l.Validade)
            .Select(l => new LoteEstoqueDto(l.Id, l.PropriedadeId, l.BioinsumoId, l.Bioinsumo!.Nome, l.Lote, l.Quantidade, l.Unidade, l.Validade, l.TempArmazenamentoC)).ToListAsync();
    }

    [HttpPost("propriedades/{id:int}/estoque")]
    public async Task<ActionResult<LoteEstoqueDto>> AddEstoque(int id, LoteEstoqueRequest r)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return Forbid();
        var bio = await db.Bioinsumos.FindAsync(r.BioinsumoId);
        if (bio == null) return BadRequest("Bioinsumo inválido.");
        var l = new LoteEstoque { PropriedadeId = id, BioinsumoId = r.BioinsumoId, Lote = r.Lote, Quantidade = r.Quantidade, Unidade = r.Unidade, Validade = r.Validade, TempArmazenamentoC = r.TempArmazenamentoC };
        db.LotesEstoque.Add(l);
        await db.SaveChangesAsync();
        return new LoteEstoqueDto(l.Id, id, bio.Id, bio.Nome, l.Lote, l.Quantidade, l.Unidade, l.Validade, l.TempArmazenamentoC);
    }

    // ---- Painel ----
    [HttpGet("painel")]
    public async Task<PainelDto> Painel(CancellationToken ct)
    {
        var ids = await db.PropriedadeMembros.Where(m => m.UsuarioId == Uid).Select(m => m.PropriedadeId).ToListAsync(ct);
        var talhoes = await db.Talhoes.Where(t => ids.Contains(t.PropriedadeId)).Select(t => t.AreaHa).ToListAsync(ct);
        var alertas = await db.Alertas.Where(a => ids.Contains(a.PropriedadeId)).OrderByDescending(a => a.CriadoEm).Take(100).ToListAsync(ct);
        var recs = new List<RecomendacaoDto>();
        foreach (var id in ids.Take(5))
            try { recs.AddRange(await rec.CalcularAsync(id, ct)); } catch (HttpRequestException) { }
        return new PainelDto(ids.Count, talhoes.Count, talhoes.Sum(), alertas.Count(a => !a.Lido),
            alertas.Take(5).Select(a => new AlertaDto(a.Id, a.PropriedadeId, a.TalhaoId, a.BioinsumoId, a.Tipo, a.Nivel, a.Titulo, a.Mensagem, a.CriadoEm, a.Lido)).ToList(),
            recs.Take(10).ToList());
    }
}
