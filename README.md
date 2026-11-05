# AgroCérebro 🌱 — proj_agricula

Sistema **multiusuário** (web + mobile via **PWA**) que é o "cérebro" do produtor no campo para **manejo de bioinsumos**
(Hackathon): cruza **clima, solo, estágio BBCH e pragas** com os **requisitos biológicos do produto** e gera **alertas e
recomendações** da janela perfeita de aplicação ou de cuidados no armazenamento — inclusive por **WhatsApp**.

## Stack escolhida

| Camada | Tecnologia | Por quê |
|---|---|---|
| Front | **Blazor WebAssembly PWA** (C#) | Um só código web/mobile, instalável, funciona offline (service worker) |
| API | **ASP.NET Core 8** Web API + JWT | Multiusuário com papéis por propriedade |
| Banco | **MySQL/MariaDB** (XAMPP) via **EF Core + Pomelo** | **Migrations** — o banco é criado/atualizado no start |
| Clima | **Open-Meteo** (pública, sem chave) | Previsão horária 72h: temperatura, umidade, chuva, vento, dia/noite |
| Mapas | **Leaflet** + OpenStreetMap + Esri World Imagery (satélite) | Talhões desenhados no mapa (GeoJSON) |
| Drone | Upload de ortomosaico georreferenciado + overlay no mapa + índice **VARI** (RGB) | Ver abaixo |
| WhatsApp | **ApiEnvios** (`POST /v1/instance/{id}/messages/chat`) | Alertas; **até 3 números por conta** |

## Estrutura

```
src/
  AgroCerebro.Shared/   DTOs (contratos API ⇄ cliente)
  AgroCerebro.Server/   API, EF Core (Data/Migrations), serviços (clima, recomendação, WhatsApp, worker de alertas)
  AgroCerebro.Client/   Blazor WASM PWA (páginas, Leaflet, service worker, manifest)
```

## Modelo de dados (resumo)

```
Usuario 1─N PropriedadeMembro N─1 Propriedade 1─N Talhao 1─N Plantio ─N Monitoramento (BBCH)
                                     │               │ N─1 TipoSolo        N─1 Cultura
                                     │               ├─N OcorrenciaPraga N─1 Praga
                                     │               └─N MissaoDrone 1─N ImagemDrone (bounds WGS84)
                                     ├─N LoteEstoque N─1 Bioinsumo
                                     └─N Alerta
Bioinsumo N─N Cultura (BioinsumoCultura)   Bioinsumo N─N Praga-alvo (BioinsumoPraga)
Usuario 1─N ContatoWhatsApp (máx. 3)
```
- **Multiusuário**: `PropriedadeMembro.Papel` = `Dono` | `Agronomo` | `Operador`. Toda consulta é filtrada pela associação do usuário.
- **Bioinsumo** guarda os requisitos biológicos: faixa de temperatura/umidade, BBCH, chuva e vento máximos, evitar sol (UV), temperatura e instruções de armazenamento, validade.

## O "cérebro" (Input → Processamento → Output)

1. **Inputs**: clima (Open-Meteo), tipo de solo, estágio **BBCH**, **ocorrência de pragas** (registrada no app / vista no drone).
2. **Processamento** (`RecomendacaoService`): para cada talhão com plantio ativo, bioinsumos compatíveis com a cultura **e** com a praga detectada; checa BBCH e varre as próximas 72 h procurando blocos de horas onde temperatura, umidade, chuva, vento e (se aplicável) ausência de sol direto satisfazem o produto.
3. **Output**: recomendação `Ideal | Aguardar | Inadequado` com janelas pontuadas; **alertas** (janela de aplicação, armazenamento fora da temperatura, lote vencendo) gravados e enviados por **WhatsApp**. Um worker reavalia a cada 30 min, sem duplicar alertas no mesmo dia.

> Os requisitos do seed são didáticos/aproximados — valide sempre com bula e fabricante.

## Drones — mapeamento de área

1. Voo de mapeamento (DJI + Drone Deploy / WebODM / Pix4D) gera **ortomosaico** + limites.
2. Na aba **Drone**, cria-se a missão do talhão e sobe-se o PNG/JPG com os limites (S/O/N/L) — pode preencher a partir da vista atual do mapa.
3. A imagem é sobreposta ao mapa (opacidade ajustável) e o app calcula **VARI = (G−R)/(G+R−B)** no navegador para colorir vigor vegetativo com câmera RGB comum.
4. Roadmap: câmera multiespectral → NDVI real; pipeline WebODM (NodeODM API) automático; detecção de falhas/pragas por visão computacional.

## Como rodar (dev)

Pré-requisitos: .NET 8 SDK, XAMPP com MySQL/MariaDB ligado.

```powershell
dotnet tool restore
cd src/AgroCerebro.Server
dotnet run          # cria o banco "agrocerebro" e aplica as migrations sozinho
```
Abra http://localhost:5015 (ou a porta exibida). Swagger em `/swagger`.

Configurações (`appsettings.json`; segredos em `appsettings.Development.json` — **ignorado pelo git** — ou variáveis de ambiente):
```
ConnectionStrings__Default = Server=localhost;Port=3306;Database=agrocerebro;User=root;Password=;
Jwt__Key                   = (chave longa e aleatória)
WhatsApp__BaseUrl / WhatsApp__InstanceId / WhatsApp__Token
```
Veja `appsettings.Production.example.json`.

### Migrations
```powershell
dotnet tool restore
dotnet ef migrations add NomeDaMudanca --project src/AgroCerebro.Server --output-dir Data/Migrations
dotnet ef database update --project src/AgroCerebro.Server     # ou deixe o app aplicar no start (Database:MigrateOnStartup=true)
dotnet ef migrations script --idempotent --project src/AgroCerebro.Server -o deploy.sql   # SQL para rodar em servidor sem dotnet-ef
```

### Publicar em servidor
```powershell
dotnet publish src/AgroCerebro.Server -c Release -o publish
```
Suba a pasta `publish`, defina as variáveis acima e execute `dotnet AgroCerebro.Server.dll` atrás de HTTPS (necessário para PWA instalável). O banco é criado/migrado no primeiro start.

## WhatsApp
Aba **WhatsApp**: cada conta cadastra **até 3 números**; botão de teste envia uma mensagem. Os alertas das propriedades da conta vão para os números ativos.
