using System.Text;
using AgroCerebro.Server.Data;
using AgroCerebro.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Default")
         ?? "Server=localhost;Port=3306;Database=agrocerebro;User=root;Password=;";
var serverVersion = ServerVersion.Parse(builder.Configuration["Database:ServerVersion"] ?? "10.4.32-mariadb");
builder.Services.AddDbContext<AppDbContext>(o => o.UseMySql(cs, serverVersion));

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ausente");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IClimaService, ClimaService>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("AgroCerebro/1.0");
});
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>(c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddHostedService<AlertaWorker>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAcessoService, AcessoService>();
builder.Services.AddScoped<IRecomendacaoService, RecomendacaoService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Migrations automáticas: facilita subir em qualquer servidor (cria/atualiza o banco no start).
if (builder.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}

app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();   // inclui /uploads (imagens de drone) em wwwroot/uploads
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
