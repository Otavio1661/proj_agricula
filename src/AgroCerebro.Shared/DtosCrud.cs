namespace AgroCerebro.Shared;

// ===== Catálogo (admin) =====
/// <summary>Classe mutável (não record) para o formulário Blazor poder usar @bind direto nas propriedades.</summary>
public class BioinsumoSalvarRequest
{
    public string Nome { get; set; } = "";
    public string Tipo { get; set; } = "Fungo";
    public string AgenteAtivo { get; set; } = "";
    public string? Descricao { get; set; }
    public double TempMin { get; set; } = 15;
    public double TempMax { get; set; } = 30;
    public double UmidadeMin { get; set; } = 50;
    public double UmidadeMax { get; set; } = 100;
    public int BbchMin { get; set; }
    public int BbchMax { get; set; } = 99;
    public double ChuvaMaxMm { get; set; } = 0.2;
    public double VentoMaxKmh { get; set; } = 15;
    public bool EvitarSolDireto { get; set; } = true;
    public double ArmazTempMax { get; set; } = 25;
    public string? ArmazInstrucoes { get; set; }
    public int ValidadeMeses { get; set; } = 12;
    public List<int> CulturaIds { get; set; } = new();
    public List<int> PragaIds { get; set; } = new();
}

public record BioinsumoEdicaoDto(BioinsumoSalvarRequest Dados, int Id);

public record CulturaSalvarRequest(string Nome, string? Descricao);
public record PragaSalvarRequest(string Nome, string Tipo);
public record TipoSoloSalvarRequest(string Nome, string Textura);

// ===== Demais CRUDs =====
public record MissaoDroneEditarRequest(DateTime Data, string? Equipamento, double? AlturaM, string? Notas, string? Status);
public record OcorrenciaPragaEditarRequest(int PragaId, int Severidade, string? Notas);
public record PlantioEditarRequest(int CulturaId, DateTime DataPlantio, int BbchAtual, bool Ativo);
public record MonitoramentoDto(int Id, int PlantioId, DateTime Data, int Bbch, string? Notas);
