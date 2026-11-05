using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace AgroCerebro.Server.Services;

public interface ITokenService { (string token, DateTime expira) Gerar(Usuario u); }

public class TokenService(IConfiguration cfg) : ITokenService
{
    public (string, DateTime) Gerar(Usuario u)
    {
        var expira = DateTime.UtcNow.AddHours(cfg.GetValue("Jwt:ExpiraHoras", 12));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var jwt = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Audience"],
            [new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()), new Claim(ClaimTypes.Name, u.Nome), new Claim(ClaimTypes.Email, u.Email)],
            expires: expira, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expira);
    }
}

public static class Senha
{
    // PBKDF2-SHA256: formato "iter.salt.hash" (base64).
    public static string Hash(string senha)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
        var h = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(senha, salt, 100_000, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        return $"100000.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(h)}";
    }

    public static bool Verificar(string senha, string armazenado)
    {
        var p = armazenado.Split('.');
        if (p.Length != 3 || !int.TryParse(p[0], out var it)) return false;
        var h = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(senha, Convert.FromBase64String(p[1]), it, System.Security.Cryptography.HashAlgorithmName.SHA256, 32);
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(h, Convert.FromBase64String(p[2]));
    }
}

/// <summary>Controle de acesso multiusuário: usuário só enxerga propriedades em que é membro.</summary>
public interface IAcessoService
{
    int UsuarioId(ClaimsPrincipal u);
    Task<string?> PapelAsync(int usuarioId, int propriedadeId);
    Task<bool> PodeLerAsync(int usuarioId, int propriedadeId);
    Task<bool> PodeEditarAsync(int usuarioId, int propriedadeId); // Dono ou Agronomo
}

public class AcessoService(AppDbContext db) : IAcessoService
{
    public int UsuarioId(ClaimsPrincipal u) => int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<string?> PapelAsync(int usuarioId, int propriedadeId) =>
        await db.PropriedadeMembros.Where(m => m.UsuarioId == usuarioId && m.PropriedadeId == propriedadeId)
            .Select(m => m.Papel).FirstOrDefaultAsync();

    public async Task<bool> PodeLerAsync(int usuarioId, int propriedadeId) => await PapelAsync(usuarioId, propriedadeId) != null;

    public async Task<bool> PodeEditarAsync(int usuarioId, int propriedadeId)
    {
        var p = await PapelAsync(usuarioId, propriedadeId);
        return p is "Dono" or "Agronomo";
    }
}
