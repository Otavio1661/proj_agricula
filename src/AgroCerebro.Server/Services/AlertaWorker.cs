using AgroCerebro.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Services;

/// <summary>A cada 30 min avalia todas as propriedades, gera alertas e envia WhatsApp.</summary>
public class AlertaWorker(IServiceScopeFactory sf, ILogger<AlertaWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = sf.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var rec = scope.ServiceProvider.GetRequiredService<IRecomendacaoService>();
                foreach (var id in await db.Propriedades.Select(p => p.Id).ToListAsync(ct))
                {
                    try { await rec.GerarAlertasAsync(id, ct); }
                    catch (Exception ex) { log.LogWarning(ex, "Falha ao gerar alertas da propriedade {Id}", id); }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogWarning(ex, "AlertaWorker"); }
            await Task.Delay(TimeSpan.FromMinutes(30), ct);
        }
    }
}
