namespace AgroCerebro.Shared;

// ===== Auth =====
public record RegistroRequest(string Nome, string Email, string Senha);
public record LoginRequest(string Email, string Senha);
public record AuthResponse(string Token, DateTime ExpiraEm, UsuarioDto Usuario);
public record UsuarioDto(int Id, string Nome, string Email);

// ===== Propriedades / talhões =====
public record PropriedadeDto(int Id, string Nome, string? Municipio, string? Uf, double Latitude, double Longitude, double AreaHa);
public record PropriedadeSalvarRequest(string Nome, string? Municipio, string? Uf, double Latitude, double Longitude, double AreaHa);
public record MembroDto(int UsuarioId, string Nome, string Email, string Papel);
public record ConvidarMembroRequest(string Email, string Papel); // Papel: Agronomo | Operador

public record TalhaoDto(int Id, int PropriedadeId, string Nome, double AreaHa, string? GeoJson, int? TipoSoloId, string? TipoSoloNome, PlantioDto? PlantioAtivo);
public record TalhaoSalvarRequest(string Nome, double AreaHa, string? GeoJson, int? TipoSoloId);

public record TipoSoloDto(int Id, string Nome, string Textura);
public record CulturaDto(int Id, string Nome, string? Descricao);
public record PragaDto(int Id, string Nome, string Tipo);

public record PlantioDto(int Id, int TalhaoId, int CulturaId, string CulturaNome, DateTime DataPlantio, int BbchAtual, bool Ativo);
public record PlantioSalvarRequest(int CulturaId, DateTime DataPlantio, int BbchAtual);

public record MonitoramentoRequest(int PlantioId, int Bbch, string? Notas);
public record OcorrenciaPragaRequest(int TalhaoId, int PragaId, int Severidade, double? Latitude, double? Longitude, string? Notas);
public record OcorrenciaPragaDto(int Id, int TalhaoId, int PragaId, string PragaNome, int Severidade, DateTime Data, double? Latitude, double? Longitude, string? Notas);

// ===== Bioinsumos =====
public record BioinsumoDto(
    int Id, string Nome, string Tipo, string AgenteAtivo, string? Descricao,
    double TempMin, double TempMax, double UmidadeMin, double UmidadeMax,
    int BbchMin, int BbchMax, double ChuvaMaxMm, double VentoMaxKmh,
    bool EvitarSolDireto, double ArmazTempMax, string? ArmazInstrucoes, int ValidadeMeses,
    List<string> Culturas, List<string> Pragas);

public record LoteEstoqueDto(int Id, int PropriedadeId, int BioinsumoId, string BioinsumoNome, string Lote, double Quantidade, string Unidade, DateTime Validade, double? TempArmazenamentoC);
public record LoteEstoqueRequest(int BioinsumoId, string Lote, double Quantidade, string Unidade, DateTime Validade, double? TempArmazenamentoC);

// ===== Clima =====
public record ClimaHoraDto(DateTime Hora, double TemperaturaC, double UmidadePct, double ChuvaMm, double VentoKmh, bool EDia);
public record ClimaDto(double Latitude, double Longitude, double TemperaturaAtualC, double UmidadeAtualPct, double VentoAtualKmh, List<ClimaHoraDto> Horas, string Fonte);

// ===== Recomendações / alertas =====
public record JanelaAplicacaoDto(DateTime Inicio, DateTime Fim, double Pontuacao, string Resumo);
public record RecomendacaoDto(
    int TalhaoId, string TalhaoNome, int BioinsumoId, string BioinsumoNome,
    string Status,            // Ideal | Aguardar | Inadequado
    string Motivo,
    List<JanelaAplicacaoDto> Janelas, List<string> Pragas);

public record AlertaDto(int Id, int PropriedadeId, int? TalhaoId, int? BioinsumoId, string Tipo, string Nivel, string Titulo, string Mensagem, DateTime CriadoEm, bool Lido);

// ===== Drone =====
public record MissaoDroneRequest(int TalhaoId, DateTime Data, string? Equipamento, double? AlturaM, string? Notas);
public record MissaoDroneDto(int Id, int TalhaoId, DateTime Data, string? Equipamento, double? AlturaM, string? Status, List<ImagemDroneDto> Imagens);
public record ImagemDroneDto(int Id, int MissaoId, string Url, string Tipo, double Sul, double Oeste, double Norte, double Leste, double? GsdCm);

public record PainelDto(int Propriedades, int Talhoes, double AreaTotalHa, int AlertasNaoLidos, List<AlertaDto> UltimosAlertas, List<RecomendacaoDto> Recomendacoes);

// ===== WhatsApp (máx. 3 números por conta) =====
public record ContatoWhatsAppDto(int Id, string Nome, string Numero, bool Ativo);
public record ContatoWhatsAppRequest(string Nome, string Numero);
public record WhatsAppStatusDto(bool Configurado, int Limite, List<ContatoWhatsAppDto> Numeros);
