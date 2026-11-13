using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;
namespace Inmobiliaria.Application.UnitTests;

public sealed class SolicitudesServiceTests
{
    [Theory]
    [InlineData("Vendedor")]
    [InlineData("Administrador")]
    public async Task Crear_SoloComprador(string rol)
    {
        var e = new Escenario(rol);
        var repo = new Mock<ISolicitudesRepository>();
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => new SolicitudesService(repo.Object, e.Acceso).Crear(e.Principal, new(1, null)));
        repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(EstadoSolicitudCreada.Creada, null)]
    [InlineData(EstadoSolicitudCreada.Duplicada, "DUPLICADA")]
    [InlineData(EstadoSolicitudCreada.NoDisponible, "NO_DISPONIBLE")]
    public async Task Crear_NormalizaYTraduceResultado(EstadoSolicitudCreada estado, string? error)
    {
        var e = new Escenario("Comprador");
        var repo = new Mock<ISolicitudesRepository>();
        repo.Setup(r => r.Crear(7, new SolicitudAlquilerRequest(10, "Hola"), It.IsAny<DateTime>())).ReturnsAsync((estado, 23));
        var result = await new SolicitudesService(repo.Object, e.Acceso).Crear(e.Principal, new(10, " Hola "));
        Assert.Equal(error is null, result.IsSuccess);
        if (error is null) Assert.Equal(23, result.Value); else Assert.Equal(error, result.Error.Code);
        repo.Verify(r => r.Crear(7, new SolicitudAlquilerRequest(10, "Hola"), It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Crear_RechazaMensajeLargo()
    {
        var e = new Escenario("Comprador");
        var repo = new Mock<ISolicitudesRepository>();
        var result = await new SolicitudesService(repo.Object, e.Acceso).Crear(e.Principal, new(1, new string('a', 2001)));
        Assert.Equal("DATOS_INVALIDOS", result.Error.Code);
        repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("Comprador", false)]
    [InlineData("Vendedor", true)]
    public async Task Listar_FiltraPorUsuarioYRol(string rol, bool vendedor)
    {
        var e = new Escenario(rol);
        var repo = new Mock<ISolicitudesRepository>();
        var datos = new List<SolicitudDto> { new(1, 2, "Casa", "Ana", "Pendiente", null, DateTime.UtcNow) };
        repo.Setup(r => r.Listar(7, vendedor)).ReturnsAsync(datos);
        Assert.Equal(datos, await new SolicitudesService(repo.Object, e.Acceso).Listar(e.Principal));
        repo.Verify(r => r.Listar(7, vendedor), Times.Once);
    }
}
