namespace AgroCerebro.Shared;

// Visões consolidadas "Todas as fazendas" (alertas, recomendações, estoque).
public record AlertaFazendaDto(AlertaDto Alerta, string FazendaNome);
public record RecomendacaoFazendaDto(int PropriedadeId, string FazendaNome, RecomendacaoDto Rec);
public record LoteFazendaDto(LoteEstoqueDto Lote, string FazendaNome);
/// <summary>Resultado por fazenda das recomendações; Erro preenchido quando o clima falhou/expirou.</summary>
public record RecomendacoesTodasDto(List<RecomendacaoFazendaDto> Itens, List<string> FazendasComErro);
