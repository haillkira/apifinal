using Inmobiliaria.Application.Persistencia;
using Microsoft.AspNetCore.Identity;

namespace Inmobiliaria.Api.Seguridad;

internal sealed class ClavesIdentity : IClavesCuenta
{
    private readonly PasswordHasher<string> hasher = new();

    public string Hashear(string password) => hasher.HashPassword("", password);

    public bool Verificar(string hash, string password) =>
        hasher.VerifyHashedPassword("", hash, password) != PasswordVerificationResult.Failed;
}
