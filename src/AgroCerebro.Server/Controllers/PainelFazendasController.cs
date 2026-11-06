using AgroCerebro.Server.Data;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

[ApiController, Authorize, Route("api/painel")]
public class PainelFazendasController(AppDbContext db, IAcessoService acesso, IClimaService clima, IRecomendacaoService rec) : ControllerBase
{
    [HttpGet("fazendas")]
    public async Task<ActionResult<List<FazendaResumoDto>>> Fazendas(CancellationToken ct)
    {
        var uid = acesso.UsuarioId(User);
        var membros = await db.PropriedadeMembros.AsNoTracking().Where(m => m.UsuarioId == uid)
            .Select(m => new { m.Papel, P = m.Propriedade! }).OrderBy(x => x.P.Nome).ToListAsync(ct);
        var ids = membros.Select(m => m.P.Id).ToList();
        if (ids.Count == 0) return new List<FazendaResumoDto>();

        // Consultas agregadas em lote (sequenciais: DbContext não é thread-safe).
        var talhoes = await db.Talhoes.AsNoTracking().Where(t => ids.Contains(t.PropriedadeId))
            .Select(t => new { t.Id, t.PropriedadeId, t.AreaHa }).ToListAsync(ct);
        var culturas = await db.Plantios.AsNoTracking().Where(p => p.Ativo && ids.Contains(p.Talhao!.PropriedadeId))
            .Select(p => new { p.Talhao!.PropriedadeId, Nome = p.Cultura!.Nome }).ToListAsync(ct);
        var desde = DateTime.UtcNow.AddDays(-14);
        var pragas = await db.OcorrenciasPraga.AsNoTracking().Where(o => o.Data >= desde && o.Severidade > 0 && ids.Contains(o.Talhao!.PropriedadeId))
            .Select(o => new { o.Talhao!.PropriedadeId, o.Severidade }).ToListAsync(ct);
        var alertas = await db.Alertas.AsNoTracking().Where(a => !a.Lido && ids.Contains(a.PropriedadeId))
            .Select(a => new { a.PropriedadeId, a.Nivel }).ToListAsync(ct);
        var lotes = await db.LotesEstoque.AsNoTracking().Where(l => ids.Contains(l.PropriedadeId) && l.Quantidade > 0)
            .Select(l => new { l.PropriedadeId, l.Validade }).ToListAsync(ct);

        // Clima e recomendações em paralelo, com timeout curto e falha tolerada.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(12));
        var climas = await Task.WhenAll(membros.Select(async m =>
        {
            try { return await clima.ObterAsync(m.P.Latitude, m.P.Longitude, cts.Token); } catch { return null; }
        }));

        var lista = new List<FazendaResumoDto>();
        for (var i = 0; i < membros.Count; i++)
        {
            var p = membros[i].P; var c = climas[i];
            var ts = talhoes.Where(t => t.PropriedadeId == p.Id).ToList();
            var al = alertas.Where(a => a.PropriedadeId == p.Id).ToList();
            var lt = lotes.Where(l => l.PropriedadeId == p.Id).ToList();
            var pr = pragas.Where(x => x.PropriedadeId == p.Id).ToList();
            var criticos = al.Count(a => a.Nivel == "Critico");
            var vencidos = lt.Count(l => l.Validade < DateTime.UtcNow);

            int? ideais = null; DateTime? proxima = null;
            try
            {
                var rs = (await rec.CalcularAsync(p.Id, cts.Token)).Where(r => r.Status == "Ideal").ToList();
                ideais = rs.Count;
                proxima = rs.SelectMany(r => r.Janelas).Select(j => (DateTime?)j.Inicio).OrderBy(d => d).FirstOrDefault();
            }
            catch { }

            var situacao = criticos > 0 || vencidos > 0 || pr.Any(x => x.Severidade >= 4) ? "Critico"
                : al.Count > 0 || pr.Count > 0 || lt.Any(l => l.Validade < DateTime.UtcNow.AddDays(30)) ? "Atencao" : "Ok";

            lista.Add(new FazendaResumoDto(p.Id, p.Nome, p.Municipio, p.Uf, p.AreaHa, p.Latitude, p.Longitude, membros[i].Papel,
                ts.Count, ts.Sum(t => t.AreaHa), culturas.Where(x => x.PropriedadeId == p.Id).Select(x => x.Nome).Distinct().OrderBy(n => n).ToList(), pr.Count,
                al.Count, criticos, lt.Count, lt.Count(l => l.Validade < DateTime.UtcNow.AddDays(30)),
                c?.TemperaturaAtualC, c?.UmidadeAtualPct, c?.VentoAtualKmh, ideais, proxima, situacao));
        }
        return lista;
    }
}
