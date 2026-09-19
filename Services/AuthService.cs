using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InovaGAB.API.DTOs;
using InovaGAB.API.Models;
using InovaGAB.API.Repositories;
using InovaGAB.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InovaGAB.API.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;

    private static readonly string[] ValidProfiles = ["operator", "manager", "leader"];

    public AuthService(IUserRepository userRepository, IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Valida perfil
        if (!ValidProfiles.Contains(request.Perfil))
            throw new ArgumentException($"Perfil inválido. Use: {string.Join(", ", ValidProfiles)}");

        // Verifica e-mail duplicado
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("E-mail já cadastrado.");

        var user = new User
        {
            Nome = request.Nome,
            Email = request.Email.ToLowerInvariant(),
            Perfil = request.Perfil,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            DataCriacao = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("E-mail ou senha incorretos.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("E-mail ou senha incorretos.");

        return BuildAuthResponse(user);
    }


    private AuthResponse BuildAuthResponse(User user)
    {
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpiresInHours);

        return new AuthResponse(token, user.Id, user.Nome, user.Email, user.Perfil, expiresAt);
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),   // garante compatibilidade com User.FindFirstValue
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Nome),
            new Claim(ClaimTypes.Role, user.Perfil),
            new Claim("perfil", user.Perfil),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpiresInHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
