using AgroCerebro.Server.Data;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

/// <summary>Completa o CRUD das entidades ligadas à propriedade (U/D de estoque, alertas, missões, imagens, ocorrências, plantios, membros).</summary>
[ApiController, Authorize, Route("api")]
public class CrudController(AppDbContext db, IAcessoService acesso, IWebHostEnvironment env) : ControllerBase
{
    private int Uid => acesso.UsuarioId(User);

    // ===== Estoque =====
    [HttpPut("propriedades/{id:int}/estoque/{loteId:int}")]
    public async Task<ActionResult<LoteEstoqueDto>> AtualizarLote(int id, int loteId, LoteEstoqueRequest r)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return StatusCode(403, "Sem permissão para editar o estoque desta propriedade.");
        var l = await db.LotesEstoque.Include(x => x.Bioinsumo).FirstOrDefaultAsync(x => x.Id == loteId && x.PropriedadeId == id);
        if (l == null) return NotFound();
        if (string.IsNullOrWhiteSpace(r.Lote)) return BadRequest("Informe o código do lote.");
        if (r.Quantidade < 0) return BadRequest("Quantidade não pode ser negativa.");
        var bio = await db.Bioinsumos.FindAsync(r.BioinsumoId);
        if (bio == null) return BadRequest("Bioinsumo inválido.");
        (l.BioinsumoId, l.Lote, l.Quantidade, l.Unidade, l.Validade, l.TempArmazenamentoC) = (r.BioinsumoId, r.Lote.Trim(), r.Quantidade, r.Unidade, r.Validade, r.TempArmazenamentoC);
        await db.SaveChangesAsync();
        return new LoteEstoqueDto(l.Id, id, bio.Id, bio.Nome, l.Lote, l.Quantidade, l.Unidade, l.Validade, l.TempArmazenamentoC);
    }

    [HttpDelete("propriedades/{id:int}/estoque/{loteId:int}")]
    public async Task<IActionResult> ExcluirLote(int id, int loteId)
    {
        if (!await acesso.PodeEditarAsync(Uid, id)) return StatusCode(403, "Sem permissão para editar o estoque desta propriedade.");
        var l = await db.LotesEstoque.FirstOrDefaultAsync(x => x.Id == loteId && x.PropriedadeId == id);
        if (l == null) return NotFound();
        db.LotesEstoque.Remove(l);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Alertas =====
    [HttpDelete("alertas/{id:int}")]
    public async Task<IActionResult> ExcluirAlerta(int id)
    {
        var a = await db.Alertas.FindAsync(id);
        if (a == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, a.PropriedadeId)) return Forbid();
        db.Alertas.Remove(a);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("propriedades/{id:int}/alertas/lidos")]
    public async Task<IActionResult> MarcarTodosLidos(int id)
    {
        if (!await acesso.PodeLerAsync(Uid, id)) return Forbid();
        await db.Alertas.Where(a => a.PropriedadeId == id && !a.Lido).ExecuteUpdateAsync(s => s.SetProperty(a => a.Lido, true));
        return NoContent();
    }

    // ===== Missões e imagens de drone =====
    [HttpPut("missoes/{id:int}")]
    public async Task<ActionResult<MissaoDroneDto>> AtualizarMissao(int id, MissaoDroneEditarRequest r)
    {
        var m = await db.MissoesDrone.Include(x => x.Talhao).Include(x => x.Imagens).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, m.Talhao!.PropriedadeId)) return Forbid();
        if (r.AlturaM is < 0) return BadRequest("Altura de voo inválida.");
        (m.Data, m.Equipamento, m.AlturaM, m.Notas) = (r.Data, r.Equipamento, r.AlturaM, r.Notas);
        if (!string.IsNullOrWhiteSpace(r.Status)) m.Status = r.Status;
        await db.SaveChangesAsync();
        return new MissaoDroneDto(m.Id, m.TalhaoId, m.Data, m.Equipamento, m.AlturaM, m.Status,
            m.Imagens.Select(i => new ImagemDroneDto(i.Id, i.MissaoId, "/uploads/" + i.Arquivo, i.Tipo, i.Sul, i.Oeste, i.Norte, i.Leste, i.GsdCm)).ToList());
    }

    [HttpDelete("missoes/{id:int}")]
    public async Task<IActionResult> ExcluirMissao(int id)
    {
        var m = await db.MissoesDrone.Include(x => x.Talhao).Include(x => x.Imagens).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, m.Talhao!.PropriedadeId)) return Forbid();
        foreach (var i in m.Imagens) ApagaArquivo(i.Arquivo);
        db.MissoesDrone.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("imagens/{id:int}")]
    public async Task<IActionResult> ExcluirImagem(int id)
    {
        var i = await db.ImagensDrone.Include(x => x.Missao).ThenInclude(m => m!.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (i == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, i.Missao!.Talhao!.PropriedadeId)) return Forbid();
        ApagaArquivo(i.Arquivo);
        db.ImagensDrone.Remove(i);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private void ApagaArquivo(string nome)
    {
        try
        {
            var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads");
            var f = Path.Combine(dir, Path.GetFileName(nome)); // GetFileName evita path traversal
            if (System.IO.File.Exists(f)) System.IO.File.Delete(f);
        }
        catch { /* arquivo preso/ausente: não bloqueia a exclusão do registro */ }
    }

    // ===== Ocorrências de praga =====
    [HttpPut("ocorrencias/{id:int}")]
    public async Task<IActionResult> AtualizarOcorrencia(int id, OcorrenciaPragaEditarRequest r)
    {
        var o = await db.OcorrenciasPraga.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, o.Talhao!.PropriedadeId)) return Forbid();
        if (!await db.Pragas.AnyAsync(p => p.Id == r.PragaId)) return BadRequest("Praga inválida.");
        (o.PragaId, o.Severidade, o.Notas) = (r.PragaId, Math.Clamp(r.Severidade, 0, 5), r.Notas);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("ocorrencias/{id:int}")]
    public async Task<IActionResult> ExcluirOcorrencia(int id)
    {
        var o = await db.OcorrenciasPraga.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (o == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, o.Talhao!.PropriedadeId)) return Forbid();
        db.OcorrenciasPraga.Remove(o);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Plantios / monitoramentos =====
    [HttpPut("plantios/{id:int}")]
    public async Task<ActionResult<PlantioDto>> AtualizarPlantio(int id, PlantioEditarRequest r)
    {
        var p = await db.Plantios.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, p.Talhao!.PropriedadeId)) return StatusCode(403, "Sem permissão para editar plantios.");
        var c = await db.Culturas.FindAsync(r.CulturaId);
        if (c == null) return BadRequest("Cultura inválida.");
        if (r.Ativo) // apenas um plantio ativo por talhão
            await db.Plantios.Where(x => x.TalhaoId == p.TalhaoId && x.Id != id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Ativo, false));
        (p.CulturaId, p.DataPlantio, p.BbchAtual, p.Ativo) = (r.CulturaId, r.DataPlantio, Math.Clamp(r.BbchAtual, 0, 99), r.Ativo);
        await db.SaveChangesAsync();
        return new PlantioDto(p.Id, p.TalhaoId, p.CulturaId, c.Nome, p.DataPlantio, p.BbchAtual, p.Ativo);
    }

    [HttpDelete("plantios/{id:int}")]
    public async Task<IActionResult> ExcluirPlantio(int id)
    {
        var p = await db.Plantios.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, p.Talhao!.PropriedadeId)) return StatusCode(403, "Sem permissão para excluir plantios.");
        db.Monitoramentos.RemoveRange(db.Monitoramentos.Where(m => m.PlantioId == id));
        db.Plantios.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("plantios/{id:int}/monitoramentos")]
    public async Task<ActionResult<List<MonitoramentoDto>>> Monitoramentos(int id)
    {
        var p = await db.Plantios.Include(x => x.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (!await acesso.PodeLerAsync(Uid, p.Talhao!.PropriedadeId)) return Forbid();
        return await db.Monitoramentos.Where(m => m.PlantioId == id).OrderByDescending(m => m.Data).Take(100)
            .Select(m => new MonitoramentoDto(m.Id, m.PlantioId, m.Data, m.Bbch, m.Notas)).ToListAsync();
    }

    [HttpDelete("monitoramentos/{id:int}")]
    public async Task<IActionResult> ExcluirMonitoramento(int id)
    {
        var m = await db.Monitoramentos.Include(x => x.Plantio).ThenInclude(p => p!.Talhao).FirstOrDefaultAsync(x => x.Id == id);
        if (m == null) return NotFound();
        if (!await acesso.PodeEditarAsync(Uid, m.Plantio!.Talhao!.PropriedadeId)) return StatusCode(403, "Sem permissão.");
        db.Monitoramentos.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ===== Membros =====
    [HttpDelete("propriedades/{id:int}/membros/{usuarioId:int}")]
    public async Task<IActionResult> RemoverMembro(int id, int usuarioId)
    {
        if (await acesso.PapelAsync(Uid, id) != "Dono") return StatusCode(403, "Apenas o Dono gerencia a equipe.");
        var m = await db.PropriedadeMembros.FindAsync(id, usuarioId);
        if (m == null) return NotFound();
        if (m.Papel == "Dono") return BadRequest("O Dono da propriedade não pode ser removido.");
        db.PropriedadeMembros.Remove(m);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("propriedades/{id:int}/membros/{usuarioId:int}")]
    public async Task<IActionResult> AlterarPapel(int id, int usuarioId, ConvidarMembroRequest r)
    {
        if (await acesso.PapelAsync(Uid, id) != "Dono") return StatusCode(403, "Apenas o Dono gerencia a equipe.");
        if (r.Papel is not ("Agronomo" or "Operador")) return BadRequest("Papel deve ser Agronomo ou Operador.");
        var m = await db.PropriedadeMembros.FindAsync(id, usuarioId);
        if (m == null) return NotFound();
        if (m.Papel == "Dono") return BadRequest("O papel do Dono não pode ser alterado.");
        m.Papel = r.Papel;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
