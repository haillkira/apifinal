using System.Security.Claims;
using Inmobiliaria.Application.Persistencia;
using Moq;

namespace Inmobiliaria.Application.UnitTests;

public class AccesoServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("invalido")]
    public async Task Usuario_RechazaIdentificadorAusenteOInvalido(string? id)
    {
        var e = new Escenario();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(id is null ? [] : new[] { new Claim(ClaimTypes.NameIdentifier, id) }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Acceso.Usuario(principal));
        e.Cuentas.Verify(x => x.BuscarPorId(It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Inactiva")]
    public async Task Usuario_RechazaCuentaInexistenteOInactiva(string? estado)
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.BuscarPorId(7)).ReturnsAsync(estado is null ? null : e.Cuenta with { Estado = estado });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Acceso.Usuario(e.Principal));
    }

    [Fact]
    public async Task Usuario_RechazaSuspensionVigente()
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.EstaSuspendida(7, It.IsAny<DateTime>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => e.Acceso.Usuario(e.Principal));
    }

    [Fact]
    public async Task Usuario_DevuelveCuentaActiva()
    {
        var e = new Escenario();
        Assert.Equal(e.Cuenta.Usuario, await e.Acceso.Usuario(e.Principal));
    }

    [Fact]
    public async Task Vendedor_DevuelveIdDelPerfilAprobado()
    {
        var e = new Escenario();
        Assert.Equal(12, await e.Acceso.Vendedor(e.Principal));
    }

    [Theory]
    [InlineData("Comprador", 12)]
    [InlineData("Vendedor", null)]
    public async Task Vendedor_RechazaRolIncorrectoOPendiente(string rol, int? perfil)
    {
        var e = new Escenario(rol);
        e.Cuentas.Setup(x => x.BuscarVendedorAprobado(7)).ReturnsAsync(perfil);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => e.Acceso.Vendedor(e.Principal));
    }

    [Fact]
    public async Task Administrador_DevuelveUsuarioYPerfilAutorizados()
    {
        var e = new Escenario("Administrador");
        Assert.Equal(new AdministradorAutorizado(7, 20), await e.Acceso.Administrador(e.Principal));
    }

    [Theory]
    [InlineData("Vendedor", 20)]
    [InlineData("Administrador", null)]
    public async Task Administrador_RechazaRolOPerfilSinPermisos(string rol, int? perfil)
    {
        var e = new Escenario(rol);
        e.Cuentas.Setup(x => x.BuscarAdministradorAutorizado(7)).ReturnsAsync(perfil);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => e.Acceso.Administrador(e.Principal));
    }
}
