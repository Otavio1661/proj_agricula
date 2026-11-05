using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using AgroCerebro.Shared;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Services;

public interface IRecomendacaoService
{
    Task<List<RecomendacaoDto>> CalcularAsync(int propriedadeId, CancellationToken ct = default);
    /// <summary>Gera alertas persistidos (com deduplicação diária) e dispara WhatsApp para os novos.</summary>
    Task<int> GerarAlertasAsync(int propriedadeId, CancellationToken ct = default);
}

public class RecomendacaoService(AppDbContext db, IClimaService clima, IWhatsAppService wa) : IRecomendacaoService
{
    private static bool Ok(ClimaHoraDto h, Bioinsumo b) =>
        h.TemperaturaC >= b.TempMin && h.TemperaturaC <= b.TempMax &&
        h.UmidadePct >= b.UmidadeMin && h.UmidadePct <= b.UmidadeMax &&
        h.ChuvaMm <= b.ChuvaMaxMm && h.VentoKmh <= b.VentoMaxKmh &&
        (!b.EvitarSolDireto || !h.EDia || h.Hora.Hour >= 17 || h.Hora.Hour < 9);

    public async Task<List<RecomendacaoDto>> CalcularAsync(int propriedadeId, CancellationToken ct = default)
    {
        var prop = await db.Propriedades.AsNoTracking().FirstAsync(p => p.Id == propriedadeId, ct);
        var cl = await clima.ObterAsync(prop.Latitude, prop.Longitude, ct);

        var talhoes = await db.Talhoes.AsNoTracking().Where(t => t.PropriedadeId == propriedadeId)
            .Include(t => t.Plantios.Where(p => p.Ativo)).ThenInclude(p => p.Cultura).ToListAsync(ct);
        var bios = await db.Bioinsumos.AsNoTracking().Include(b => b.Culturas).Include(b => b.Pragas).ThenInclude(x => x.Praga).ToListAsync(ct);
        var desde = DateTime.UtcNow.AddDays(-14);
        var ocorrencias = await db.OcorrenciasPraga.AsNoTracking()
            .Where(o => o.Talhao!.PropriedadeId == propriedadeId && o.Data >= desde && o.Severidade > 0).ToListAsync(ct);

        var res = new List<RecomendacaoDto>();
        foreach (var t in talhoes)
        {
            var plantio = t.Plantios.FirstOrDefault();
            if (plantio == null) continue;
            var pragasTalhao = ocorrencias.Where(o => o.TalhaoId == t.Id).Select(o => o.PragaId).ToHashSet();

            foreach (var b in bios.Where(b => b.Culturas.Any(c => c.CulturaId == plantio.CulturaId)))
            {
                var alvos = b.Pragas.Where(p => pragasTalhao.Contains(p.PragaId)).Select(p => p.Praga!.Nome).ToList();
                // Só recomenda produtos com praga-alvo detectada; sem ocorrência o produto é preventivo (apenas listado como "Aguardar").
                if (plantio.BbchAtual < b.BbchMin || plantio.BbchAtual > b.BbchMax)
                {
                    if (alvos.Count > 0)
                        res.Add(new(t.Id, t.Nome, b.Id, b.Nome, "Inadequado", $"Estágio BBCH {plantio.BbchAtual} fora da faixa do produto ({b.BbchMin}–{b.BbchMax}).", new(), alvos));
                    continue;
                }
                if (alvos.Count == 0) continue;

                var janelas = new List<JanelaAplicacaoDto>();
                List<ClimaHoraDto>? atual = null;
                void Fecha()
                {
                    if (atual is { Count: >= 2 })
                    {
                        var score = 100 - atual.Average(h => Math.Abs(h.TemperaturaC - (b.TempMin + b.TempMax) / 2)) * 3 - atual.Average(h => h.VentoKmh);
                        janelas.Add(new(atual[0].Hora, atual[^1].Hora.AddHours(1), Math.Round(Math.Max(0, score), 0),
                            $"{atual.Average(h => h.TemperaturaC):0}°C, UR {atual.Average(h => h.UmidadePct):0}%, vento {atual.Max(h => h.VentoKmh):0} km/h, sem chuva"));
                    }
                    atual = null;
                }
                foreach (var h in cl.Horas)
                {
                    if (Ok(h, b)) (atual ??= new()).Add(h); else Fecha();
                }
                Fecha();

                var top = janelas.OrderByDescending(j => j.Pontuacao).Take(3).OrderBy(j => j.Inicio).ToList();
                if (top.Count == 0)
                    res.Add(new(t.Id, t.Nome, b.Id, b.Nome, "Aguardar", $"Nenhuma janela nas próximas 72h com {b.TempMin:0}–{b.TempMax:0}°C, UR ≥ {b.UmidadeMin:0}%, vento ≤ {b.VentoMaxKmh:0} km/h e sem chuva.", new(), alvos));
                else
                    res.Add(new(t.Id, t.Nome, b.Id, b.Nome, top[0].Inicio < DateTime.Now.AddHours(6) ? "Ideal" : "Aguardar",
                        $"Praga detectada ({string.Join(", ", alvos)}). Melhor janela: {top[0].Inicio:dd/MM HH:mm}–{top[0].Fim:HH:mm}.", top, alvos));
            }
        }
        return res.OrderBy(r => r.Status == "Ideal" ? 0 : r.Status == "Aguardar" ? 1 : 2).ToList();
    }

    public async Task<int> GerarAlertasAsync(int propriedadeId, CancellationToken ct = default)
    {
        var hoje = DateTime.UtcNow.ToString("yyyyMMdd");
        var novos = new List<Alerta>();

        foreach (var r in (await CalcularAsync(propriedadeId, ct)).Where(r => r.Status == "Ideal"))
        {
            var j = r.Janelas[0];
            novos.Add(new Alerta
            {
                PropriedadeId = propriedadeId, TalhaoId = r.TalhaoId, BioinsumoId = r.BioinsumoId, Tipo = "JanelaAplicacao", Nivel = "Atencao",
                Titulo = $"Janela ideal: {r.BioinsumoNome} em {r.TalhaoNome}",
                Mensagem = $"{r.Motivo} {j.Resumo}.", ChaveDedup = $"jan:{r.TalhaoId}:{r.BioinsumoId}:{hoje}"
            });
        }

        // Armazenamento / validade dos lotes
        var prop = await db.Propriedades.AsNoTracking().FirstAsync(p => p.Id == propriedadeId, ct);
        var lotes = await db.LotesEstoque.AsNoTracking().Include(l => l.Bioinsumo).Where(l => l.PropriedadeId == propriedadeId && l.Quantidade > 0).ToListAsync(ct);
        foreach (var l in lotes)
        {
            if (l.TempArmazenamentoC is { } tc && tc > l.Bioinsumo!.ArmazTempMax)
                novos.Add(new Alerta { PropriedadeId = propriedadeId, BioinsumoId = l.BioinsumoId, Tipo = "Armazenamento", Nivel = "Critico",
                    Titulo = $"Armazenamento inadequado: {l.Bioinsumo.Nome} (lote {l.Lote})",
                    Mensagem = $"Temperatura {tc:0.#}°C acima do limite de {l.Bioinsumo.ArmazTempMax:0}°C. {l.Bioinsumo.ArmazInstrucoes}", ChaveDedup = $"arm:{l.Id}:{hoje}" });
            var dias = (l.Validade - DateTime.UtcNow).TotalDays;
            if (dias <= 30)
                novos.Add(new Alerta { PropriedadeId = propriedadeId, BioinsumoId = l.BioinsumoId, Tipo = "Validade", Nivel = dias <= 7 ? "Critico" : "Atencao",
                    Titulo = dias < 0 ? $"Lote vencido: {l.Bioinsumo!.Nome}" : $"Lote vence em {Math.Ceiling(dias)} dia(s): {l.Bioinsumo!.Nome}",
                    Mensagem = $"Lote {l.Lote}, validade {l.Validade:dd/MM/yyyy}. Organismos vivos perdem viabilidade — use ou descarte conforme orientação.", ChaveDedup = $"val:{l.Id}:{hoje}" });
        }

        var existentes = await db.Alertas.Where(a => a.PropriedadeId == propriedadeId && a.ChaveDedup != null && a.ChaveDedup.EndsWith(hoje))
            .Select(a => a.ChaveDedup!).ToListAsync(ct);
        novos = novos.Where(n => !existentes.Contains(n.ChaveDedup!)).ToList();
        if (novos.Count == 0) return 0;

        db.Alertas.AddRange(novos);
        await db.SaveChangesAsync(ct);

        var texto = $"🌱 *AgroCérebro* — {prop.Nome}\n\n" + string.Join("\n\n", novos.Select(n => $"{(n.Nivel == "Critico" ? "🚨" : "⚠️")} *{n.Titulo}*\n{n.Mensagem}"));
        await wa.EnviarParaPropriedadeAsync(propriedadeId, texto, ct);
        return novos.Count;
    }
}
