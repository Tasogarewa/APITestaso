using Backend.AppDbContext;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public JwtService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(ApplicationUser user)
        {
            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"]!;
            var jwtAudience = _configuration["Jwt:Audience"]!;

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                Log.Error("Недійсна конфігурація JWT: Key, Issuer або Audience відсутні");
                throw new InvalidOperationException("Недійсна конфігурація JWT");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName ?? "")
        };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            Log.Information($"Згенеровано токени для користувача {user.Id}: AccessToken={accessToken}, RefreshToken={refreshToken}");
            return (accessToken, refreshToken);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);
            if (refreshToken == null)
            {
                Log.Warning($"RefreshToken не знайдено або анульовано: {token}");
            }
            else if (refreshToken.IsExpired)
            {
                Log.Warning($"RefreshToken закінчився: {token}, Expires: {refreshToken.Expires}");
            }
            return refreshToken;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
        }
    }

}
