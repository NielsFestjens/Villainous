using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Villainous.Server.Application;

public static class JWTHelper
{
    public static string GenerateJSONWebToken(AppConfig appConfig, Guid id)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appConfig.Jwt.Key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(JwtRegisteredClaimNames.Jti, id.ToString()),
        };

        var token = new JwtSecurityToken(appConfig.Jwt.Issuer, appConfig.Jwt.Issuer, claims, expires: DateTime.Now.AddMinutes(120), signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}