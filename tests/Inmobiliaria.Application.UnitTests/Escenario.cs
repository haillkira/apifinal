using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;

namespace Inmobiliaria.Application.UnitTests;

internal sealed class Escenario
{
    public Mock<ICuentasRepository> Cuentas { get; } = new();
    public Mock<IPropiedadRepository> Propiedades { get; } = new();
    public Mock<IClavesCuenta> Claves { get; } = new();
    public ClaimsPrincipal Principal { get; } = new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, "7")], "Test"));
    public AccesoService Acceso => new(Cuentas.Object);
    public Cuenta Cuenta { get; }

    public Escenario(string rol = "Vendedor")
    {
        Cuenta = new(new UsuarioResponse(7, "Ana", "Perez", rol), "Activa", "hash");
        Cuentas.Setup(x => x.BuscarPorId(7)).ReturnsAsync(Cuenta);
        Cuentas.Setup(x => x.BuscarVendedorAprobado(7)).ReturnsAsync(12);
        Cuentas.Setup(x => x.BuscarAdministradorAutorizado(7)).ReturnsAsync(20);
    }
}
