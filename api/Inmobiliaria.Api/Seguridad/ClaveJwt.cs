using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Inmobiliaria.Api.Seguridad;

public sealed class ClaveJwt
{
    public bool Configurada { get; }
    public SymmetricSecurityKey ClaveFirma { get; }

    public ClaveJwt(string? secreto, bool desarrollo)
    {
        Configurada = !string.IsNullOrWhiteSpace(secreto) && Encoding.UTF8.GetByteCount(secreto) >= 32;
        if (!Configurada && !desarrollo)
            throw new InvalidOperationException("Configura Jwt__Key con al menos 32 bytes mediante un secreto del entorno.");

        // Sin secreto local podemos arrancar, pero el login permanece cerrado.
        ClaveFirma = new SymmetricSecurityKey(Configurada
            ? Encoding.UTF8.GetBytes(secreto!)
            : RandomNumberGenerator.GetBytes(48));
    }

    public static ClaveJwt Cargar(bool desarrollo)
    {
        using var fuentes = new ConfigurationManager();
        if (desarrollo)
            fuentes.AddUserSecrets<ClaveJwt>(optional: true);
        // Las claves nunca se leen desde los appsettings del repositorio.
        fuentes.AddEnvironmentVariables();
        return new ClaveJwt(fuentes["Jwt:Key"], desarrollo);
    }
}
