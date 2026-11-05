using AgroCerebro.Server.Domain;
using AgroCerebro.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Data;

public static class DbSeeder
{
    /// <summary>Cria o usuário admin (senha configurável) e uma fazenda demo, apenas se ainda não existirem.</summary>
    public static async Task SeedAdminAsync(AppDbContext db, IConfiguration cfg)
    {
        var login = (cfg["Admin:Login"] ?? "admin").ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == login)) return;

        var admin = new Usuario { Nome = "Administrador", Email = login, SenhaHash = Senha.Hash(cfg["Admin:Senha"] ?? "admin123!") };
        var prop = new Propriedade
        {
            Nome = "Fazenda Demo", Municipio = "Sorriso", Uf = "MT", Latitude = -12.5425, Longitude = -55.7211, AreaHa = 120, Dono = admin
        };
        prop.Membros.Add(new PropriedadeMembro { Usuario = admin, Papel = "Dono" });
        var talhao = new Talhao { Propriedade = prop, Nome = "Talhão 1 - Milho", AreaHa = 40, TipoSoloId = 1 };
        talhao.Plantios.Add(new Plantio { CulturaId = 2, DataPlantio = DateTime.UtcNow.AddDays(-25), BbchAtual = 15 });
        db.Add(admin);
        db.Add(talhao);
        await db.SaveChangesAsync();
        db.OcorrenciasPraga.Add(new OcorrenciaPraga { TalhaoId = talhao.Id, PragaId = 1, UsuarioId = admin.Id, Severidade = 3, Notas = "Exemplo: lagarta-do-cartucho" });
        await db.SaveChangesAsync();
    }
}
