namespace AgroCerebro.Shared;

/// <summary>Resumo de uma fazenda para o painel multi-fazendas.</summary>
public record FazendaResumoDto(
    int Id, string Nome, string? Municipio, string? Uf, double AreaHa, double Latitude, double Longitude, string Papel,
    int Talhoes, double AreaTalhoesHa, List<string> Culturas, int PragasAtivas,
    int AlertasNaoLidos, int AlertasCriticos, int LotesEstoque, int LotesVencendo30d,
    double? TemperaturaC, double? UmidadePct, double? VentoKmh,
    int? RecomendacoesIdeais, DateTime? ProximaJanela, string Situacao); // Situacao: Ok | Atencao | Critico
