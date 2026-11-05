using System.ComponentModel.DataAnnotations;

namespace AgroCerebro.Server.Domain;

public class Usuario
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(200)] public string SenhaHash { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Propriedade
{
    public int Id { get; set; }
    [MaxLength(150)] public string Nome { get; set; } = "";
    [MaxLength(100)] public string? Municipio { get; set; }
    [MaxLength(2)] public string? Uf { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AreaHa { get; set; }
    public int DonoId { get; set; }
    public Usuario? Dono { get; set; }
    public List<PropriedadeMembro> Membros { get; set; } = new();
    public List<Talhao> Talhoes { get; set; } = new();
}

/// <summary>Multiusuário: quem tem acesso à propriedade e com qual papel (Dono, Agronomo, Operador).</summary>
public class PropriedadeMembro
{
    public int PropriedadeId { get; set; }
    public Propriedade? Propriedade { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [MaxLength(20)] public string Papel { get; set; } = "Operador";
}

public class TipoSolo
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(40)] public string Textura { get; set; } = "";
}

public class Cultura
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(300)] public string? Descricao { get; set; }
}

public class Praga
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(40)] public string Tipo { get; set; } = "Inseto"; // Inseto, Fungo, Nematoide, Ácaro
}

public class Talhao
{
    public int Id { get; set; }
    public int PropriedadeId { get; set; }
    public Propriedade? Propriedade { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    public double AreaHa { get; set; }
    /// <summary>Polígono GeoJSON (desenhado no mapa ou importado do voo de drone).</summary>
    public string? GeoJson { get; set; }
    public int? TipoSoloId { get; set; }
    public TipoSolo? TipoSolo { get; set; }
    public List<Plantio> Plantios { get; set; } = new();
}

public class Plantio
{
    public int Id { get; set; }
    public int TalhaoId { get; set; }
    public Talhao? Talhao { get; set; }
    public int CulturaId { get; set; }
    public Cultura? Cultura { get; set; }
    public DateTime DataPlantio { get; set; }
    /// <summary>Estágio fenológico atual, escala BBCH (0–99).</summary>
    public int BbchAtual { get; set; }
    public bool Ativo { get; set; } = true;
}

public class Monitoramento
{
    public int Id { get; set; }
    public int PlantioId { get; set; }
    public Plantio? Plantio { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public int Bbch { get; set; }
    [MaxLength(500)] public string? Notas { get; set; }
}

public class OcorrenciaPraga
{
    public int Id { get; set; }
    public int TalhaoId { get; set; }
    public Talhao? Talhao { get; set; }
    public int PragaId { get; set; }
    public Praga? Praga { get; set; }
    public int UsuarioId { get; set; }
    /// <summary>0 = ausente … 5 = infestação severa.</summary>
    public int Severidade { get; set; }
    public DateTime Data { get; set; } = DateTime.UtcNow;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    [MaxLength(500)] public string? Notas { get; set; }
}

/// <summary>Requisitos biológicos do produto — o "cérebro" cruza o clima/BBCH/pragas com isto.</summary>
public class Bioinsumo
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nome { get; set; } = "";
    [MaxLength(40)] public string Tipo { get; set; } = "";          // Fungo, Bactéria, Vírus, Macrorganismo, Extrato
    [MaxLength(150)] public string AgenteAtivo { get; set; } = "";
    [MaxLength(500)] public string? Descricao { get; set; }
    public double TempMin { get; set; }
    public double TempMax { get; set; }
    public double UmidadeMin { get; set; }
    public double UmidadeMax { get; set; } = 100;
    public int BbchMin { get; set; }
    public int BbchMax { get; set; } = 99;
    public double ChuvaMaxMm { get; set; } = 0.2;     // chuva tolerada na janela (lavagem)
    public double VentoMaxKmh { get; set; } = 15;     // deriva
    public bool EvitarSolDireto { get; set; } = true; // UV inativa fungos/vírus → aplicar fim de tarde/noite
    public double ArmazTempMax { get; set; } = 25;
    [MaxLength(400)] public string? ArmazInstrucoes { get; set; }
    public int ValidadeMeses { get; set; } = 12;
    public List<BioinsumoCultura> Culturas { get; set; } = new();
    public List<BioinsumoPraga> Pragas { get; set; } = new();
}

public class BioinsumoCultura
{
    public int BioinsumoId { get; set; }
    public Bioinsumo? Bioinsumo { get; set; }
    public int CulturaId { get; set; }
    public Cultura? Cultura { get; set; }
}

public class BioinsumoPraga
{
    public int BioinsumoId { get; set; }
    public Bioinsumo? Bioinsumo { get; set; }
    public int PragaId { get; set; }
    public Praga? Praga { get; set; }
}

public class LoteEstoque
{
    public int Id { get; set; }
    public int PropriedadeId { get; set; }
    public Propriedade? Propriedade { get; set; }
    public int BioinsumoId { get; set; }
    public Bioinsumo? Bioinsumo { get; set; }
    [MaxLength(60)] public string Lote { get; set; } = "";
    public double Quantidade { get; set; }
    [MaxLength(10)] public string Unidade { get; set; } = "L";
    public DateTime Validade { get; set; }
    public double? TempArmazenamentoC { get; set; }
}

public class Alerta
{
    public int Id { get; set; }
    public int PropriedadeId { get; set; }
    public Propriedade? Propriedade { get; set; }
    public int? TalhaoId { get; set; }
    public int? BioinsumoId { get; set; }
    [MaxLength(30)] public string Tipo { get; set; } = "";   // JanelaAplicacao, Praga, Armazenamento, Validade
    [MaxLength(15)] public string Nivel { get; set; } = "Info"; // Info, Atencao, Critico
    [MaxLength(150)] public string Titulo { get; set; } = "";
    [MaxLength(600)] public string Mensagem { get; set; } = "";
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public bool Lido { get; set; }
    [MaxLength(120)] public string? ChaveDedup { get; set; }
}

public class MissaoDrone
{
    public int Id { get; set; }
    public int TalhaoId { get; set; }
    public Talhao? Talhao { get; set; }
    public DateTime Data { get; set; }
    [MaxLength(80)] public string? Equipamento { get; set; }
    public double? AlturaM { get; set; }
    [MaxLength(400)] public string? Notas { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Concluida";
    public List<ImagemDrone> Imagens { get; set; } = new();
}

/// <summary>Ortomosaico/foto georreferenciada (bounds WGS84) para sobrepor no mapa.</summary>
public class ImagemDrone
{
    public int Id { get; set; }
    public int MissaoId { get; set; }
    public MissaoDrone? Missao { get; set; }
    [MaxLength(300)] public string Arquivo { get; set; } = "";
    [MaxLength(20)] public string Tipo { get; set; } = "RGB"; // RGB, NDVI, Multiespectral
    public double Sul { get; set; }
    public double Oeste { get; set; }
    public double Norte { get; set; }
    public double Leste { get; set; }
    public double? GsdCm { get; set; }
}

/// <summary>Número de WhatsApp da conta que recebe alertas (máx. 3 por usuário).</summary>
public class ContatoWhatsApp
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    [MaxLength(80)] public string Nome { get; set; } = "";
    [MaxLength(20)] public string Numero { get; set; } = ""; // somente dígitos com DDI, ex.: 5511999998888
    public bool Ativo { get; set; } = true;
}
