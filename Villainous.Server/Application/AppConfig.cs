namespace Villainous.Server.Application;

public class AppConfig
{
    public JwtConfig Jwt { get; set; } = null!;

    public class JwtConfig
    {
        public string Issuer { get; set; } = null!;
        public string Key { get; set; } = null!;
    }
}