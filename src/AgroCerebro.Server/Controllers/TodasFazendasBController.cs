using AgroCerebro.Server.Data;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

/// <summary>Visões consolidadas (todas as fazendas do usuário): talhões, clima e missões de drone.</summary>
[ApiController, Authorize, Route("api/todas")]
public class TodasFazendasBController(AppDbContext db, IAcessoService acesso, IClimaService clima) : ControllerBase
{
    private async Task<List<(int Id, string Nome, double Lat, double Lon)>> MinhasFazendasAsync(CancellationToken ct)
    {
        var uid = acesso.UsuarioId(User);
        var l = await db.PropriedadeMembros.AsNoTracking().Where(m => m.UsuarioId == uid)
            .Select(m => new { m.Propriedade!.Id, m.Propriedade.Nome, m.Propriedade.Latitude, m.Propriedade.Longitude })
            .OrderBy(p => p.Nome).ToListAsync(ct);
        return l.Select(p => (p.Id, p.Nome, p.Latitude, p.Longitude)).ToList();
    }

    [HttpGet("talhoes")]
    public async Task<List<TalhaoFazendaDto>> Talhoes(CancellationToken ct)
    {
        var fz = await MinhasFazendasAsync(ct);
        var ids = fz.Select(f => f.Id).ToList();
        var ts = await db.Talhoes.AsNoTracking().Where(t => ids.Contains(t.PropriedadeId)).Include(t => t.TipoSolo)
            .Include(t => t.Plantios.Where(p => p.Ativo)).ThenInclude(p => p.Cultura).ToListAsync(ct);
        return ts.OrderBy(t => t.PropriedadeId).ThenBy(t => t.Nome).Select(t =>
        {
            var f = fz.First(x => x.Id == t.PropriedadeId);
            var pl = t.Plantios.FirstOrDefault(p => p.Ativo);
            var plDto = pl == null ? null : new PlantioDto(pl.Id, pl.TalhaoId, pl.CulturaId, pl.Cultura?.Nome ?? "", pl.DataPlantio, pl.BbchAtual, pl.Ativo);
            return new TalhaoFazendaDto(new TalhaoDto(t.Id, t.PropriedadeId, t.Nome, t.AreaHa, t.GeoJson, t.TipoSoloId, t.TipoSolo?.Nome, plDto), f.Nome, f.Lat, f.Lon);
        }).ToList();
    }

    [HttpGet("clima")]
    public async Task<List<ClimaFazendaDto>> Clima(CancellationToken ct)
    {
        var fz = await MinhasFazendasAsync(ct);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(12));
        var tarefas = fz.Select(async f =>
        {
            try
            {
                var c = await clima.ObterAsync(f.Lat, f.Lon, cts.Token);
                var h24 = c.Horas.Take(24).ToList();
                return new ClimaFazendaDto(f.Id, f.Nome, f.Lat, f.Lon, c.TemperaturaAtualC, c.UmidadeAtualPct, c.VentoAtualKmh,
                    h24.Count > 0 ? h24.Min(h => h.TemperaturaC) : null, h24.Count > 0 ? h24.Max(h => h.TemperaturaC) : null,
                    h24.Sum(h => h.ChuvaMm), h24.Select(h => h.TemperaturaC).ToList(), null);
            }
            catch (Exception)
            {
                return new ClimaFazendaDto(f.Id, f.Nome, f.Lat, f.Lon, null, null, null, null, null, null, new(), "Clima indisponível");
            }
        });
        return (await Task.WhenAll(tarefas)).ToList();
    }

    [HttpGet("missoes")]
    public async Task<List<MissaoFazendaDto>> Missoes(CancellationToken ct)
    {
        var fz = await MinhasFazendasAsync(ct);
        var ids = fz.Select(f => f.Id).ToList();
        var ms = await db.MissoesDrone.AsNoTracking().Include(m => m.Imagens).Include(m => m.Talhao)
            .Where(m => ids.Contains(m.Talhao!.PropriedadeId)).OrderByDescending(m => m.Data).ToListAsync(ct);
        return ms.Select(m => new MissaoFazendaDto(
            new MissaoDroneDto(m.Id, m.TalhaoId, m.Data, m.Equipamento, m.AlturaM, m.Status,
                m.Imagens.Select(i => new ImagemDroneDto(i.Id, i.MissaoId, "/uploads/" + i.Arquivo, i.Tipo, i.Sul, i.Oeste, i.Norte, i.Leste, i.GsdCm)).ToList()),
            m.Talhao!.PropriedadeId, fz.First(f => f.Id == m.Talhao.PropriedadeId).Nome, m.Talhao.Nome)).ToList();
    }
}
