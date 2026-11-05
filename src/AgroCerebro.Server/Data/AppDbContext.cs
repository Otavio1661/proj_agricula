using AgroCerebro.Server.Domain;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Propriedade> Propriedades => Set<Propriedade>();
    public DbSet<PropriedadeMembro> PropriedadeMembros => Set<PropriedadeMembro>();
    public DbSet<TipoSolo> TiposSolo => Set<TipoSolo>();
    public DbSet<Cultura> Culturas => Set<Cultura>();
    public DbSet<Praga> Pragas => Set<Praga>();
    public DbSet<Talhao> Talhoes => Set<Talhao>();
    public DbSet<Plantio> Plantios => Set<Plantio>();
    public DbSet<Monitoramento> Monitoramentos => Set<Monitoramento>();
    public DbSet<OcorrenciaPraga> OcorrenciasPraga => Set<OcorrenciaPraga>();
    public DbSet<Bioinsumo> Bioinsumos => Set<Bioinsumo>();
    public DbSet<BioinsumoCultura> BioinsumoCulturas => Set<BioinsumoCultura>();
    public DbSet<BioinsumoPraga> BioinsumoPragas => Set<BioinsumoPraga>();
    public DbSet<LoteEstoque> LotesEstoque => Set<LoteEstoque>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<MissaoDrone> MissoesDrone => Set<MissaoDrone>();
    public DbSet<ImagemDrone> ImagensDrone => Set<ImagemDrone>();
    public DbSet<ContatoWhatsApp> ContatosWhatsApp => Set<ContatoWhatsApp>();
    public DbSet<Configuracao> Configuracoes => Set<Configuracao>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        b.Entity<ContatoWhatsApp>().HasIndex(c => new { c.UsuarioId, c.Numero }).IsUnique();

        b.Entity<Propriedade>().HasOne(p => p.Dono).WithMany().HasForeignKey(p => p.DonoId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<PropriedadeMembro>().HasKey(m => new { m.PropriedadeId, m.UsuarioId });
        b.Entity<PropriedadeMembro>().HasOne(m => m.Propriedade).WithMany(p => p.Membros).HasForeignKey(m => m.PropriedadeId);
        b.Entity<PropriedadeMembro>().HasOne(m => m.Usuario).WithMany().HasForeignKey(m => m.UsuarioId);

        b.Entity<BioinsumoCultura>().HasKey(x => new { x.BioinsumoId, x.CulturaId });
        b.Entity<BioinsumoPraga>().HasKey(x => new { x.BioinsumoId, x.PragaId });

        b.Entity<Talhao>().Property(t => t.GeoJson).HasColumnType("longtext");
        b.Entity<Alerta>().HasIndex(a => new { a.PropriedadeId, a.ChaveDedup });

        SeedCatalogo(b);
    }

    // Catálogo inicial (ids fixos para o seed ser determinístico nas migrations).
    private static void SeedCatalogo(ModelBuilder b)
    {
        b.Entity<TipoSolo>().HasData(
            new TipoSolo { Id = 1, Nome = "Latossolo", Textura = "Argilosa" },
            new TipoSolo { Id = 2, Nome = "Argissolo", Textura = "Média" },
            new TipoSolo { Id = 3, Nome = "Neossolo Quartzarênico", Textura = "Arenosa" },
            new TipoSolo { Id = 4, Nome = "Nitossolo", Textura = "Muito argilosa" },
            new TipoSolo { Id = 5, Nome = "Gleissolo", Textura = "Mal drenado" });

        b.Entity<Cultura>().HasData(
            new Cultura { Id = 1, Nome = "Soja" },
            new Cultura { Id = 2, Nome = "Milho" },
            new Cultura { Id = 3, Nome = "Cana-de-açúcar" },
            new Cultura { Id = 4, Nome = "Café" },
            new Cultura { Id = 5, Nome = "Algodão" },
            new Cultura { Id = 6, Nome = "Feijão" },
            new Cultura { Id = 7, Nome = "Hortaliças" });

        b.Entity<Praga>().HasData(
            new Praga { Id = 1, Nome = "Lagarta-do-cartucho (Spodoptera frugiperda)", Tipo = "Inseto" },
            new Praga { Id = 2, Nome = "Percevejo-marrom (Euschistus heros)", Tipo = "Inseto" },
            new Praga { Id = 3, Nome = "Mosca-branca (Bemisia tabaci)", Tipo = "Inseto" },
            new Praga { Id = 4, Nome = "Cigarrinha-das-pastagens / do-milho", Tipo = "Inseto" },
            new Praga { Id = 5, Nome = "Broca-da-cana (Diatraea saccharalis)", Tipo = "Inseto" },
            new Praga { Id = 6, Nome = "Broca-do-café (Hypothenemus hampei)", Tipo = "Inseto" },
            new Praga { Id = 7, Nome = "Mofo-branco (Sclerotinia sclerotiorum)", Tipo = "Fungo" },
            new Praga { Id = 8, Nome = "Nematoide-das-galhas (Meloidogyne)", Tipo = "Nematoide" },
            new Praga { Id = 9, Nome = "Ácaro-rajado (Tetranychus urticae)", Tipo = "Ácaro" },
            new Praga { Id = 10, Nome = "Lagarta-falsa-medideira (Chrysodeixis includens)", Tipo = "Inseto" },
            new Praga { Id = 11, Nome = "Ferrugem-asiática (Phakopsora pachyrhizi)", Tipo = "Fungo" });

        // Requisitos biológicos aproximados e didáticos (validar com bula/fabricante).
        b.Entity<Bioinsumo>().HasData(
            new Bioinsumo { Id = 1, Nome = "Beauveria bassiana", Tipo = "Fungo", AgenteAtivo = "Beauveria bassiana (entomopatogênico)", Descricao = "Controle de percevejos, cigarrinhas, broca-do-café e mosca-branca.", TempMin = 20, TempMax = 30, UmidadeMin = 70, UmidadeMax = 100, BbchMin = 10, BbchMax = 89, ChuvaMaxMm = 0.2, VentoMaxKmh = 15, EvitarSolDireto = true, ArmazTempMax = 8, ArmazInstrucoes = "Refrigerado 2–8 °C, longe da luz.", ValidadeMeses = 12 },
            new Bioinsumo { Id = 2, Nome = "Metarhizium anisopliae", Tipo = "Fungo", AgenteAtivo = "Metarhizium anisopliae (entomopatogênico)", Descricao = "Cigarrinhas e percevejos; exige umidade alta.", TempMin = 22, TempMax = 30, UmidadeMin = 75, UmidadeMax = 100, BbchMin = 10, BbchMax = 89, ChuvaMaxMm = 0.2, VentoMaxKmh = 15, EvitarSolDireto = true, ArmazTempMax = 8, ArmazInstrucoes = "Refrigerado 2–8 °C.", ValidadeMeses = 12 },
            new Bioinsumo { Id = 3, Nome = "Bacillus thuringiensis (Bt)", Tipo = "Bactéria", AgenteAtivo = "Bacillus thuringiensis var. kurstaki", Descricao = "Lagartas desfolhadoras; ação por ingestão.", TempMin = 15, TempMax = 35, UmidadeMin = 40, UmidadeMax = 100, BbchMin = 10, BbchMax = 89, ChuvaMaxMm = 0.2, VentoMaxKmh = 15, EvitarSolDireto = true, ArmazTempMax = 25, ArmazInstrucoes = "Local fresco e seco, ao abrigo da luz.", ValidadeMeses = 18 },
            new Bioinsumo { Id = 4, Nome = "Trichoderma harzianum", Tipo = "Fungo", AgenteAtivo = "Trichoderma harzianum (antagonista)", Descricao = "Mofo-branco e doenças de solo; aplicação no sulco/solo úmido.", TempMin = 15, TempMax = 30, UmidadeMin = 60, UmidadeMax = 100, BbchMin = 0, BbchMax = 69, ChuvaMaxMm = 5, VentoMaxKmh = 20, EvitarSolDireto = true, ArmazTempMax = 25, ArmazInstrucoes = "Local fresco e seco.", ValidadeMeses = 12 },
            new Bioinsumo { Id = 5, Nome = "Bacillus subtilis", Tipo = "Bactéria", AgenteAtivo = "Bacillus subtilis", Descricao = "Biofungicida/promotor de crescimento; foliar ou sulco.", TempMin = 10, TempMax = 35, UmidadeMin = 40, UmidadeMax = 100, BbchMin = 0, BbchMax = 79, ChuvaMaxMm = 1, VentoMaxKmh = 20, EvitarSolDireto = false, ArmazTempMax = 25, ArmazInstrucoes = "Local fresco e seco.", ValidadeMeses = 24 },
            new Bioinsumo { Id = 6, Nome = "Bacillus amyloliquefaciens", Tipo = "Bactéria", AgenteAtivo = "Bacillus amyloliquefaciens", Descricao = "Nematoides e doenças fúngicas via solo/foliar.", TempMin = 10, TempMax = 35, UmidadeMin = 40, UmidadeMax = 100, BbchMin = 0, BbchMax = 79, ChuvaMaxMm = 1, VentoMaxKmh = 20, EvitarSolDireto = false, ArmazTempMax = 25, ArmazInstrucoes = "Local fresco e seco.", ValidadeMeses = 24 },
            new Bioinsumo { Id = 7, Nome = "Baculovirus (SfMNPV / AgMNPV)", Tipo = "Vírus", AgenteAtivo = "Nucleopoliedrovírus", Descricao = "Lagarta-do-cartucho e lagarta-da-soja; muito sensível a UV.", TempMin = 15, TempMax = 32, UmidadeMin = 50, UmidadeMax = 100, BbchMin = 10, BbchMax = 79, ChuvaMaxMm = 0.2, VentoMaxKmh = 12, EvitarSolDireto = true, ArmazTempMax = 8, ArmazInstrucoes = "Refrigerado (ou congelado, conforme rótulo); proteger da luz.", ValidadeMeses = 12 },
            new Bioinsumo { Id = 8, Nome = "Trichogramma pretiosum", Tipo = "Macrorganismo", AgenteAtivo = "Trichogramma pretiosum (parasitoide de ovos)", Descricao = "Liberação em campo para lagartas; sensível a calor e vento.", TempMin = 18, TempMax = 30, UmidadeMin = 50, UmidadeMax = 90, BbchMin = 10, BbchMax = 79, ChuvaMaxMm = 0, VentoMaxKmh = 10, EvitarSolDireto = true, ArmazTempMax = 12, ArmazInstrucoes = "Cartelas a 10–12 °C; liberar em até poucos dias.", ValidadeMeses = 1 },
            new Bioinsumo { Id = 9, Nome = "Cotesia flavipes", Tipo = "Macrorganismo", AgenteAtivo = "Cotesia flavipes (parasitoide de larvas)", Descricao = "Broca-da-cana; liberação nas horas mais amenas.", TempMin = 20, TempMax = 32, UmidadeMin = 50, UmidadeMax = 90, BbchMin = 10, BbchMax = 79, ChuvaMaxMm = 0, VentoMaxKmh = 10, EvitarSolDireto = true, ArmazTempMax = 20, ArmazInstrucoes = "Copos a 18–20 °C até a liberação.", ValidadeMeses = 1 },
            new Bioinsumo { Id = 10, Nome = "Azospirillum brasilense", Tipo = "Bactéria", AgenteAtivo = "Azospirillum brasilense", Descricao = "Fixação biológica de N em milho, trigo; inoculação em semente ou foliar.", TempMin = 15, TempMax = 32, UmidadeMin = 50, UmidadeMax = 100, BbchMin = 0, BbchMax = 39, ChuvaMaxMm = 1, VentoMaxKmh = 20, EvitarSolDireto = true, ArmazTempMax = 25, ArmazInstrucoes = "Fresco, sem sol direto.", ValidadeMeses = 6 },
            new Bioinsumo { Id = 11, Nome = "Bradyrhizobium japonicum", Tipo = "Bactéria", AgenteAtivo = "Bradyrhizobium japonicum", Descricao = "Inoculante para fixação de N na soja (semente/sulco).", TempMin = 15, TempMax = 35, UmidadeMin = 50, UmidadeMax = 100, BbchMin = 0, BbchMax = 19, ChuvaMaxMm = 2, VentoMaxKmh = 25, EvitarSolDireto = true, ArmazTempMax = 25, ArmazInstrucoes = "Sombra e local fresco; usar logo após tratar semente.", ValidadeMeses = 6 });

        b.Entity<BioinsumoCultura>().HasData(
            new BioinsumoCultura { BioinsumoId = 1, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 1, CulturaId = 4 }, new BioinsumoCultura { BioinsumoId = 1, CulturaId = 5 },
            new BioinsumoCultura { BioinsumoId = 2, CulturaId = 3 }, new BioinsumoCultura { BioinsumoId = 2, CulturaId = 2 }, new BioinsumoCultura { BioinsumoId = 2, CulturaId = 1 },
            new BioinsumoCultura { BioinsumoId = 3, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 3, CulturaId = 2 }, new BioinsumoCultura { BioinsumoId = 3, CulturaId = 5 }, new BioinsumoCultura { BioinsumoId = 3, CulturaId = 7 },
            new BioinsumoCultura { BioinsumoId = 4, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 4, CulturaId = 6 }, new BioinsumoCultura { BioinsumoId = 4, CulturaId = 7 },
            new BioinsumoCultura { BioinsumoId = 5, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 5, CulturaId = 2 }, new BioinsumoCultura { BioinsumoId = 5, CulturaId = 4 }, new BioinsumoCultura { BioinsumoId = 5, CulturaId = 6 },
            new BioinsumoCultura { BioinsumoId = 6, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 6, CulturaId = 2 }, new BioinsumoCultura { BioinsumoId = 6, CulturaId = 3 }, new BioinsumoCultura { BioinsumoId = 6, CulturaId = 5 },
            new BioinsumoCultura { BioinsumoId = 7, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 7, CulturaId = 2 },
            new BioinsumoCultura { BioinsumoId = 8, CulturaId = 2 }, new BioinsumoCultura { BioinsumoId = 8, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 8, CulturaId = 3 },
            new BioinsumoCultura { BioinsumoId = 9, CulturaId = 3 },
            new BioinsumoCultura { BioinsumoId = 10, CulturaId = 2 },
            new BioinsumoCultura { BioinsumoId = 11, CulturaId = 1 }, new BioinsumoCultura { BioinsumoId = 11, CulturaId = 6 });

        b.Entity<BioinsumoPraga>().HasData(
            new BioinsumoPraga { BioinsumoId = 1, PragaId = 2 }, new BioinsumoPraga { BioinsumoId = 1, PragaId = 3 }, new BioinsumoPraga { BioinsumoId = 1, PragaId = 6 }, new BioinsumoPraga { BioinsumoId = 1, PragaId = 4 },
            new BioinsumoPraga { BioinsumoId = 2, PragaId = 4 }, new BioinsumoPraga { BioinsumoId = 2, PragaId = 2 },
            new BioinsumoPraga { BioinsumoId = 3, PragaId = 1 }, new BioinsumoPraga { BioinsumoId = 3, PragaId = 10 },
            new BioinsumoPraga { BioinsumoId = 4, PragaId = 7 },
            new BioinsumoPraga { BioinsumoId = 5, PragaId = 7 }, new BioinsumoPraga { BioinsumoId = 5, PragaId = 11 },
            new BioinsumoPraga { BioinsumoId = 6, PragaId = 8 },
            new BioinsumoPraga { BioinsumoId = 7, PragaId = 1 }, new BioinsumoPraga { BioinsumoId = 7, PragaId = 10 },
            new BioinsumoPraga { BioinsumoId = 8, PragaId = 1 }, new BioinsumoPraga { BioinsumoId = 8, PragaId = 5 },
            new BioinsumoPraga { BioinsumoId = 9, PragaId = 5 });
    }
}
