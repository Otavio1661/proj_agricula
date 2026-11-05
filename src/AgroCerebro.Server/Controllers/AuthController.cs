using AgroCerebro.Server.Data;
using AgroCerebro.Server.Domain;
using AgroCerebro.Server.Services;
using AgroCerebro.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroCerebro.Server.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
{
    [HttpPost("registrar")]
    public async Task<ActionResult<AuthResponse>> Registrar(RegistroRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.Nome) || string.IsNullOrWhiteSpace(r.Email) || r.Senha is not { Length: >= 6 })
            return BadRequest("Informe nome, e-mail e senha (mín. 6 caracteres).");
        var email = r.Email.Trim().ToLowerInvariant();
        if (await db.Usuarios.AnyAsync(u => u.Email == email)) return Conflict("E-mail já cadastrado.");
        var u = new Usuario { Nome = r.Nome.Trim(), Email = email, SenhaHash = Senha.Hash(r.Senha) };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        return Resp(u);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest r)
    {
        var email = (r.Email ?? "").Trim().ToLowerInvariant();
        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.Email == email);
        if (u == null || !Senha.Verificar(r.Senha ?? "", u.SenhaHash)) return Unauthorized("E-mail ou senha inválidos.");
        return Resp(u);
    }

    private AuthResponse Resp(Usuario u)
    {
        var (t, e) = tokens.Gerar(u);
        return new AuthResponse(t, e, new UsuarioDto(u.Id, u.Nome, u.Email));
    }
}
