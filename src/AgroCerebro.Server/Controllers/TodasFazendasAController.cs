using AgroCerebro.Server.Data;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

/// <summary>Visões consolidadas de todas as fazendas do usuário (apenas as em que é membro).</summary>
[ApiController, Authorize, Route("api/todas")]
public class TodasFazendasAController(AppDbContext db, IAcessoService acesso, IServiceScopeFactory sf) : ControllerBase
{
    private int Uid => acesso.UsuarioId(User);

    private async Task<Dictionary<int, string>> MinhasFazendasAsync(CancellationToken ct) =>
        await db.PropriedadeMembros.Where(m => m.UsuarioId == Uid)
            .Select(m => new { m.PropriedadeId, m.Propriedade!.Nome }).ToDictionaryAsync(x => x.PropriedadeId, x => x.Nome, ct);

    [HttpGet("alertas")]
    public async Task<List<AlertaFazendaDto>> Alertas(CancellationToken ct)
    {
        var f = await MinhasFazendasAsync(ct);
        var ids = f.Keys.ToList();
        var al = await db.Alertas.AsNoTracking().Where(a => ids.Contains(a.PropriedadeId)).OrderByDescending(a => a.CriadoEm).Take(300).ToListAsync(ct);
        return al.Select(a => new AlertaFazendaDto(
            new AlertaDto(a.Id, a.PropriedadeId, a.TalhaoId, a.BioinsumoId, a.Tipo, a.Nivel, a.Titulo, a.Mensagem, a.CriadoEm, a.Lido), f[a.PropriedadeId])).ToList();
    }

    [HttpGet("estoque")]
    public async Task<List<LoteFazendaDto>> Estoque(CancellationToken ct)
    {
        var f = await MinhasFazendasAsync(ct);
        var ids = f.Keys.ToList();
        var ls = await db.LotesEstoque.AsNoTracking().Include(l => l.Bioinsumo).Where(l => ids.Contains(l.PropriedadeId)).OrderBy(l => l.Validade).ToListAsync(ct);
        return ls.Select(l => new LoteFazendaDto(
            new LoteEstoqueDto(l.Id, l.PropriedadeId, l.BioinsumoId, l.Bioinsumo!.Nome, l.Lote, l.Quantidade, l.Unidade, l.Validade, l.TempArmazenamentoC), f[l.PropriedadeId])).ToList();
    }

    [HttpGet("recomendacoes")]
    public async Task<RecomendacoesTodasDto> Recomendacoes(CancellationToken ct)
    {
        var f = await MinhasFazendasAsync(ct);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(12));
        // Cada fazenda em seu próprio escopo (DbContext não é thread-safe).
        var tarefas = f.Select(async kv =>
        {
            try
            {
                using var scope = sf.CreateScope();
                var rec = scope.ServiceProvider.GetRequiredService<IRecomendacaoService>();
                var r = await rec.CalcularAsync(kv.Key, cts.Token);
                return (kv, itens: r.Select(x => new RecomendacaoFazendaDto(kv.Key, kv.Value, x)).ToList(), erro: false);
            }
            catch { return (kv, itens: new List<RecomendacaoFazendaDto>(), erro: true); }
        }).ToList();
        var res = await Task.WhenAll(tarefas);
        var itens = res.SelectMany(x => x.itens)
            .OrderBy(x => x.Rec.Status == "Ideal" ? 0 : x.Rec.Status == "Aguardar" ? 1 : 2).ThenBy(x => x.FazendaNome).ToList();
        return new RecomendacoesTodasDto(itens, res.Where(x => x.erro).Select(x => x.kv.Value).ToList());
    }
}
