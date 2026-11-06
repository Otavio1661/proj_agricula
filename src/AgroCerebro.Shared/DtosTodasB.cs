namespace AgroCerebro.Shared;

/// <summary>Talhão com o nome da fazenda (visão "Todas as fazendas").</summary>
public record TalhaoFazendaDto(TalhaoDto Talhao, string FazendaNome, double FazendaLat, double FazendaLon);

/// <summary>Resumo de clima de uma fazenda para o grid comparativo.</summary>
public record ClimaFazendaDto(
    int PropriedadeId, string FazendaNome, double Latitude, double Longitude,
    double? TemperaturaC, double? UmidadePct, double? VentoKmh,
    double? Min24h, double? Max24h, double? Chuva24hMm, List<double> Temp24h, string? Erro);

public record MissaoFazendaDto(MissaoDroneDto Missao, int PropriedadeId, string FazendaNome, string TalhaoNome);
