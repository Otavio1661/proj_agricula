using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgroCerebro.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bioinsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AgenteAtivo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TempMin = table.Column<double>(type: "double", nullable: false),
                    TempMax = table.Column<double>(type: "double", nullable: false),
                    UmidadeMin = table.Column<double>(type: "double", nullable: false),
                    UmidadeMax = table.Column<double>(type: "double", nullable: false),
                    BbchMin = table.Column<int>(type: "int", nullable: false),
                    BbchMax = table.Column<int>(type: "int", nullable: false),
                    ChuvaMaxMm = table.Column<double>(type: "double", nullable: false),
                    VentoMaxKmh = table.Column<double>(type: "double", nullable: false),
                    EvitarSolDireto = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ArmazTempMax = table.Column<double>(type: "double", nullable: false),
                    ArmazInstrucoes = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ValidadeMeses = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bioinsumos", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Culturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Culturas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Pragas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pragas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TiposSolo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Textura = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposSolo", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(180)", maxLength: 180, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SenhaHash = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BioinsumoCulturas",
                columns: table => new
                {
                    BioinsumoId = table.Column<int>(type: "int", nullable: false),
                    CulturaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioinsumoCulturas", x => new { x.BioinsumoId, x.CulturaId });
                    table.ForeignKey(
                        name: "FK_BioinsumoCulturas_Bioinsumos_BioinsumoId",
                        column: x => x.BioinsumoId,
                        principalTable: "Bioinsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BioinsumoCulturas_Culturas_CulturaId",
                        column: x => x.CulturaId,
                        principalTable: "Culturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BioinsumoPragas",
                columns: table => new
                {
                    BioinsumoId = table.Column<int>(type: "int", nullable: false),
                    PragaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BioinsumoPragas", x => new { x.BioinsumoId, x.PragaId });
                    table.ForeignKey(
                        name: "FK_BioinsumoPragas_Bioinsumos_BioinsumoId",
                        column: x => x.BioinsumoId,
                        principalTable: "Bioinsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BioinsumoPragas_Pragas_PragaId",
                        column: x => x.PragaId,
                        principalTable: "Pragas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ContatosWhatsApp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Numero = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContatosWhatsApp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContatosWhatsApp_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Propriedades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Municipio = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uf = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<double>(type: "double", nullable: false),
                    Longitude = table.Column<double>(type: "double", nullable: false),
                    AreaHa = table.Column<double>(type: "double", nullable: false),
                    DonoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propriedades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Propriedades_Usuarios_DonoId",
                        column: x => x.DonoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PropriedadeId = table.Column<int>(type: "int", nullable: false),
                    TalhaoId = table.Column<int>(type: "int", nullable: true),
                    BioinsumoId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nivel = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Titulo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mensagem = table.Column<string>(type: "varchar(600)", maxLength: 600, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CriadoEm = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Lido = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ChaveDedup = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LotesEstoque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PropriedadeId = table.Column<int>(type: "int", nullable: false),
                    BioinsumoId = table.Column<int>(type: "int", nullable: false),
                    Lote = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantidade = table.Column<double>(type: "double", nullable: false),
                    Unidade = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Validade = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TempArmazenamentoC = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesEstoque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesEstoque_Bioinsumos_BioinsumoId",
                        column: x => x.BioinsumoId,
                        principalTable: "Bioinsumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LotesEstoque_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PropriedadeMembros",
                columns: table => new
                {
                    PropriedadeId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Papel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropriedadeMembros", x => new { x.PropriedadeId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_PropriedadeMembros_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropriedadeMembros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Talhoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PropriedadeId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AreaHa = table.Column<double>(type: "double", nullable: false),
                    GeoJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TipoSoloId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Talhoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Talhoes_Propriedades_PropriedadeId",
                        column: x => x.PropriedadeId,
                        principalTable: "Propriedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Talhoes_TiposSolo_TipoSoloId",
                        column: x => x.TipoSoloId,
                        principalTable: "TiposSolo",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MissoesDrone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TalhaoId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Equipamento = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlturaM = table.Column<double>(type: "double", nullable: true),
                    Notas = table.Column<string>(type: "varchar(400)", maxLength: 400, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissoesDrone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissoesDrone_Talhoes_TalhaoId",
                        column: x => x.TalhaoId,
                        principalTable: "Talhoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OcorrenciasPraga",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TalhaoId = table.Column<int>(type: "int", nullable: false),
                    PragaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Severidade = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Latitude = table.Column<double>(type: "double", nullable: true),
                    Longitude = table.Column<double>(type: "double", nullable: true),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OcorrenciasPraga", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OcorrenciasPraga_Pragas_PragaId",
                        column: x => x.PragaId,
                        principalTable: "Pragas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OcorrenciasPraga_Talhoes_TalhaoId",
                        column: x => x.TalhaoId,
                        principalTable: "Talhoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Plantios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TalhaoId = table.Column<int>(type: "int", nullable: false),
                    CulturaId = table.Column<int>(type: "int", nullable: false),
                    DataPlantio = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    BbchAtual = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plantios_Culturas_CulturaId",
                        column: x => x.CulturaId,
                        principalTable: "Culturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plantios_Talhoes_TalhaoId",
                        column: x => x.TalhaoId,
                        principalTable: "Talhoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ImagensDrone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MissaoId = table.Column<int>(type: "int", nullable: false),
                    Arquivo = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sul = table.Column<double>(type: "double", nullable: false),
                    Oeste = table.Column<double>(type: "double", nullable: false),
                    Norte = table.Column<double>(type: "double", nullable: false),
                    Leste = table.Column<double>(type: "double", nullable: false),
                    GsdCm = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagensDrone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagensDrone_MissoesDrone_MissaoId",
                        column: x => x.MissaoId,
                        principalTable: "MissoesDrone",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Monitoramentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PlantioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Bbch = table.Column<int>(type: "int", nullable: false),
                    Notas = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monitoramentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Monitoramentos_Plantios_PlantioId",
                        column: x => x.PlantioId,
                        principalTable: "Plantios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Bioinsumos",
                columns: new[] { "Id", "AgenteAtivo", "ArmazInstrucoes", "ArmazTempMax", "BbchMax", "BbchMin", "ChuvaMaxMm", "Descricao", "EvitarSolDireto", "Nome", "TempMax", "TempMin", "Tipo", "UmidadeMax", "UmidadeMin", "ValidadeMeses", "VentoMaxKmh" },
                values: new object[,]
                {
                    { 1, "Beauveria bassiana (entomopatogênico)", "Refrigerado 2–8 °C, longe da luz.", 8.0, 89, 10, 0.20000000000000001, "Controle de percevejos, cigarrinhas, broca-do-café e mosca-branca.", true, "Beauveria bassiana", 30.0, 20.0, "Fungo", 100.0, 70.0, 12, 15.0 },
                    { 2, "Metarhizium anisopliae (entomopatogênico)", "Refrigerado 2–8 °C.", 8.0, 89, 10, 0.20000000000000001, "Cigarrinhas e percevejos; exige umidade alta.", true, "Metarhizium anisopliae", 30.0, 22.0, "Fungo", 100.0, 75.0, 12, 15.0 },
                    { 3, "Bacillus thuringiensis var. kurstaki", "Local fresco e seco, ao abrigo da luz.", 25.0, 89, 10, 0.20000000000000001, "Lagartas desfolhadoras; ação por ingestão.", true, "Bacillus thuringiensis (Bt)", 35.0, 15.0, "Bactéria", 100.0, 40.0, 18, 15.0 },
                    { 4, "Trichoderma harzianum (antagonista)", "Local fresco e seco.", 25.0, 69, 0, 5.0, "Mofo-branco e doenças de solo; aplicação no sulco/solo úmido.", true, "Trichoderma harzianum", 30.0, 15.0, "Fungo", 100.0, 60.0, 12, 20.0 },
                    { 5, "Bacillus subtilis", "Local fresco e seco.", 25.0, 79, 0, 1.0, "Biofungicida/promotor de crescimento; foliar ou sulco.", false, "Bacillus subtilis", 35.0, 10.0, "Bactéria", 100.0, 40.0, 24, 20.0 },
                    { 6, "Bacillus amyloliquefaciens", "Local fresco e seco.", 25.0, 79, 0, 1.0, "Nematoides e doenças fúngicas via solo/foliar.", false, "Bacillus amyloliquefaciens", 35.0, 10.0, "Bactéria", 100.0, 40.0, 24, 20.0 },
                    { 7, "Nucleopoliedrovírus", "Refrigerado (ou congelado, conforme rótulo); proteger da luz.", 8.0, 79, 10, 0.20000000000000001, "Lagarta-do-cartucho e lagarta-da-soja; muito sensível a UV.", true, "Baculovirus (SfMNPV / AgMNPV)", 32.0, 15.0, "Vírus", 100.0, 50.0, 12, 12.0 },
                    { 8, "Trichogramma pretiosum (parasitoide de ovos)", "Cartelas a 10–12 °C; liberar em até poucos dias.", 12.0, 79, 10, 0.0, "Liberação em campo para lagartas; sensível a calor e vento.", true, "Trichogramma pretiosum", 30.0, 18.0, "Macrorganismo", 90.0, 50.0, 1, 10.0 },
                    { 9, "Cotesia flavipes (parasitoide de larvas)", "Copos a 18–20 °C até a liberação.", 20.0, 79, 10, 0.0, "Broca-da-cana; liberação nas horas mais amenas.", true, "Cotesia flavipes", 32.0, 20.0, "Macrorganismo", 90.0, 50.0, 1, 10.0 },
                    { 10, "Azospirillum brasilense", "Fresco, sem sol direto.", 25.0, 39, 0, 1.0, "Fixação biológica de N em milho, trigo; inoculação em semente ou foliar.", true, "Azospirillum brasilense", 32.0, 15.0, "Bactéria", 100.0, 50.0, 6, 20.0 },
                    { 11, "Bradyrhizobium japonicum", "Sombra e local fresco; usar logo após tratar semente.", 25.0, 19, 0, 2.0, "Inoculante para fixação de N na soja (semente/sulco).", true, "Bradyrhizobium japonicum", 35.0, 15.0, "Bactéria", 100.0, 50.0, 6, 25.0 }
                });

            migrationBuilder.InsertData(
                table: "Culturas",
                columns: new[] { "Id", "Descricao", "Nome" },
                values: new object[,]
                {
                    { 1, null, "Soja" },
                    { 2, null, "Milho" },
                    { 3, null, "Cana-de-açúcar" },
                    { 4, null, "Café" },
                    { 5, null, "Algodão" },
                    { 6, null, "Feijão" },
                    { 7, null, "Hortaliças" }
                });

            migrationBuilder.InsertData(
                table: "Pragas",
                columns: new[] { "Id", "Nome", "Tipo" },
                values: new object[,]
                {
                    { 1, "Lagarta-do-cartucho (Spodoptera frugiperda)", "Inseto" },
                    { 2, "Percevejo-marrom (Euschistus heros)", "Inseto" },
                    { 3, "Mosca-branca (Bemisia tabaci)", "Inseto" },
                    { 4, "Cigarrinha-das-pastagens / do-milho", "Inseto" },
                    { 5, "Broca-da-cana (Diatraea saccharalis)", "Inseto" },
                    { 6, "Broca-do-café (Hypothenemus hampei)", "Inseto" },
                    { 7, "Mofo-branco (Sclerotinia sclerotiorum)", "Fungo" },
                    { 8, "Nematoide-das-galhas (Meloidogyne)", "Nematoide" },
                    { 9, "Ácaro-rajado (Tetranychus urticae)", "Ácaro" },
                    { 10, "Lagarta-falsa-medideira (Chrysodeixis includens)", "Inseto" },
                    { 11, "Ferrugem-asiática (Phakopsora pachyrhizi)", "Fungo" }
                });

            migrationBuilder.InsertData(
                table: "TiposSolo",
                columns: new[] { "Id", "Nome", "Textura" },
                values: new object[,]
                {
                    { 1, "Latossolo", "Argilosa" },
                    { 2, "Argissolo", "Média" },
                    { 3, "Neossolo Quartzarênico", "Arenosa" },
                    { 4, "Nitossolo", "Muito argilosa" },
                    { 5, "Gleissolo", "Mal drenado" }
                });

            migrationBuilder.InsertData(
                table: "BioinsumoCulturas",
                columns: new[] { "BioinsumoId", "CulturaId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 1, 4 },
                    { 1, 5 },
                    { 2, 1 },
                    { 2, 2 },
                    { 2, 3 },
                    { 3, 1 },
                    { 3, 2 },
                    { 3, 5 },
                    { 3, 7 },
                    { 4, 1 },
                    { 4, 6 },
                    { 4, 7 },
                    { 5, 1 },
                    { 5, 2 },
                    { 5, 4 },
                    { 5, 6 },
                    { 6, 1 },
                    { 6, 2 },
                    { 6, 3 },
                    { 6, 5 },
                    { 7, 1 },
                    { 7, 2 },
                    { 8, 1 },
                    { 8, 2 },
                    { 8, 3 },
                    { 9, 3 },
                    { 10, 2 },
                    { 11, 1 },
                    { 11, 6 }
                });

            migrationBuilder.InsertData(
                table: "BioinsumoPragas",
                columns: new[] { "BioinsumoId", "PragaId" },
                values: new object[,]
                {
                    { 1, 2 },
                    { 1, 3 },
                    { 1, 4 },
                    { 1, 6 },
                    { 2, 2 },
                    { 2, 4 },
                    { 3, 1 },
                    { 3, 10 },
                    { 4, 7 },
                    { 5, 7 },
                    { 5, 11 },
                    { 6, 8 },
                    { 7, 1 },
                    { 7, 10 },
                    { 8, 1 },
                    { 8, 5 },
                    { 9, 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_PropriedadeId_ChaveDedup",
                table: "Alertas",
                columns: new[] { "PropriedadeId", "ChaveDedup" });

            migrationBuilder.CreateIndex(
                name: "IX_BioinsumoCulturas_CulturaId",
                table: "BioinsumoCulturas",
                column: "CulturaId");

            migrationBuilder.CreateIndex(
                name: "IX_BioinsumoPragas_PragaId",
                table: "BioinsumoPragas",
                column: "PragaId");

            migrationBuilder.CreateIndex(
                name: "IX_ContatosWhatsApp_UsuarioId_Numero",
                table: "ContatosWhatsApp",
                columns: new[] { "UsuarioId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagensDrone_MissaoId",
                table: "ImagensDrone",
                column: "MissaoId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesEstoque_BioinsumoId",
                table: "LotesEstoque",
                column: "BioinsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesEstoque_PropriedadeId",
                table: "LotesEstoque",
                column: "PropriedadeId");

            migrationBuilder.CreateIndex(
                name: "IX_MissoesDrone_TalhaoId",
                table: "MissoesDrone",
                column: "TalhaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Monitoramentos_PlantioId",
                table: "Monitoramentos",
                column: "PlantioId");

            migrationBuilder.CreateIndex(
                name: "IX_OcorrenciasPraga_PragaId",
                table: "OcorrenciasPraga",
                column: "PragaId");

            migrationBuilder.CreateIndex(
                name: "IX_OcorrenciasPraga_TalhaoId",
                table: "OcorrenciasPraga",
                column: "TalhaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantios_CulturaId",
                table: "Plantios",
                column: "CulturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Plantios_TalhaoId",
                table: "Plantios",
                column: "TalhaoId");

            migrationBuilder.CreateIndex(
                name: "IX_PropriedadeMembros_UsuarioId",
                table: "PropriedadeMembros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Propriedades_DonoId",
                table: "Propriedades",
                column: "DonoId");

            migrationBuilder.CreateIndex(
                name: "IX_Talhoes_PropriedadeId",
                table: "Talhoes",
                column: "PropriedadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Talhoes_TipoSoloId",
                table: "Talhoes",
                column: "TipoSoloId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alertas");

            migrationBuilder.DropTable(
                name: "BioinsumoCulturas");

            migrationBuilder.DropTable(
                name: "BioinsumoPragas");

            migrationBuilder.DropTable(
                name: "ContatosWhatsApp");

            migrationBuilder.DropTable(
                name: "ImagensDrone");

            migrationBuilder.DropTable(
                name: "LotesEstoque");

            migrationBuilder.DropTable(
                name: "Monitoramentos");

            migrationBuilder.DropTable(
                name: "OcorrenciasPraga");

            migrationBuilder.DropTable(
                name: "PropriedadeMembros");

            migrationBuilder.DropTable(
                name: "MissoesDrone");

            migrationBuilder.DropTable(
                name: "Bioinsumos");

            migrationBuilder.DropTable(
                name: "Plantios");

            migrationBuilder.DropTable(
                name: "Pragas");

            migrationBuilder.DropTable(
                name: "Culturas");

            migrationBuilder.DropTable(
                name: "Talhoes");

            migrationBuilder.DropTable(
                name: "Propriedades");

            migrationBuilder.DropTable(
                name: "TiposSolo");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
