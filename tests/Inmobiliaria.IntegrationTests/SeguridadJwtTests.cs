using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Inmobiliaria.Api.Seguridad;
using Microsoft.IdentityModel.Tokens;

namespace Inmobiliaria.IntegrationTests;

// Estas pruebas no usan SQL: también pueden ejecutarse en Ubuntu.
public sealed class SeguridadJwtTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("demasiado-corta")]
    public void Produccion_RechazaSecretosAusentesOCortos(string? secreto)
    {
        Assert.Throws<InvalidOperationException>(() => new ClaveJwt(secreto, desarrollo: false));
    }

    [Fact]
    public void Desarrollo_SinSecretoNoHabilitaLoginYGeneraClavesDistintas()
    {
        var primera = new ClaveJwt(null, desarrollo: true);
        var segunda = new ClaveJwt(null, desarrollo: true);
        Assert.False(primera.Configurada);
        Assert.False(primera.ClaveFirma.Key.SequenceEqual(segunda.ClaveFirma.Key));
    }

    [Fact]
    public void FirmaJwt_SeValidaConLaMismaClaveYRechazaOtraClave()
    {
        var secreto = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var clave = new ClaveJwt(secreto, desarrollo: false);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(new JwtSecurityToken("prueba", "clientes",
            [new Claim(ClaimTypes.NameIdentifier, "7")], expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(clave.ClaveFirma, SecurityAlgorithms.HmacSha256)));
        var validacion = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "prueba",
            ValidateAudience = true, ValidAudience = "clientes",
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            IssuerSigningKey = clave.ClaveFirma
        };
        var usuario = handler.ValidateToken(token, validacion, out _);
        Assert.Equal("7", usuario.FindFirstValue(ClaimTypes.NameIdentifier));
        validacion.IssuerSigningKey = new ClaveJwt(null, desarrollo: true).ClaveFirma;
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(token, validacion, out _));
    }
}
