using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;

namespace Inmobiliaria.Application.UnitTests;

public class CuentasServiceTests
{
    private static CuentasService Servicio(Escenario e) => new(e.Cuentas.Object, e.Claves.Object, e.Acceso);
    private static RegistroRequest Registro => new(" Ana ", " Perez ", " ANA@EXAMPLE.COM ", "password-prueba", " 8095550000 ", "00100000000");

    [Fact]
    public async Task Registrar_NormalizaDatosYGuardaHash()
    {
        var e = new Escenario();
        e.Claves.Setup(x => x.Hashear(Registro.Password)).Returns("hash-seguro");
        e.Cuentas.Setup(x => x.Registrar(It.IsAny<NuevaCuenta>(), It.IsAny<DateTime>())).ReturnsAsync((EstadoRegistro.Creado, 7));
        var resultado = await Servicio(e).Registrar(Registro, "Vendedor");
        Assert.True(resultado.IsSuccess);
        Assert.Equal(e.Cuenta.Usuario, resultado.Value);
        e.Cuentas.Verify(x => x.Registrar(new NuevaCuenta("ana@example.com", "Ana", "Perez", "8095550000", "hash-seguro", "Vendedor", "00100000000"), It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData(EstadoRegistro.SinRol, "SIN_ROL")]
    [InlineData(EstadoRegistro.Duplicado, "DUPLICADO")]
    [InlineData(EstadoRegistro.DocumentoDuplicado, "DUPLICADO")]
    public async Task Registrar_DevuelveErrorDelRepositorio(EstadoRegistro estado, string codigo)
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.Registrar(It.IsAny<NuevaCuenta>(), It.IsAny<DateTime>())).ReturnsAsync((estado, 0));
        var resultado = await Servicio(e).Registrar(Registro, "Comprador");
        Assert.True(resultado.IsFailure);
        Assert.Equal(codigo, resultado.Error.Code);
    }

    [Theory]
    [InlineData(false, "Activa", true, false)]
    [InlineData(true, "Inactiva", true, false)]
    [InlineData(true, "Activa", false, false)]
    [InlineData(true, "Activa", true, true)]
    public async Task Login_RechazadoNoActualizaUltimoAcceso(bool existe, string estado, bool claveValida, bool suspendida)
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.BuscarPorCorreo("ana@example.com")).ReturnsAsync(existe ? e.Cuenta with { Estado = estado } : null);
        e.Claves.Setup(x => x.Verificar("hash", "clave")).Returns(claveValida);
        e.Cuentas.Setup(x => x.EstaSuspendida(7, It.IsAny<DateTime>())).ReturnsAsync(suspendida);
        Assert.Null(await Servicio(e).IniciarSesion(new(" ANA@EXAMPLE.COM ", "clave")));
        e.Cuentas.Verify(x => x.ActualizarUltimoAcceso(It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task Login_ValidoActualizaUltimoAcceso()
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.BuscarPorCorreo("ana@example.com")).ReturnsAsync(e.Cuenta);
        e.Claves.Setup(x => x.Verificar("hash", "clave")).Returns(true);
        Assert.Equal(e.Cuenta.Usuario, await Servicio(e).IniciarSesion(new(" ANA@EXAMPLE.COM ", "clave")));
        e.Cuentas.Verify(x => x.ActualizarUltimoAcceso(7, It.IsAny<DateTime>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Rechazar_ExigeMotivoSinGuardarDecision(string? motivo)
    {
        var e = new Escenario("Administrador");
        var resultado = await Servicio(e).Decidir(e.Principal, 12, false, motivo);
        Assert.Equal("MOTIVO_REQUERIDO", resultado.Error.Code);
        e.Cuentas.Verify(x => x.Decidir(It.IsAny<int>(), It.IsAny<AdministradorAutorizado>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoDecision.Guardada, null)]
    [InlineData(EstadoDecision.NoEncontrado, "NO_ENCONTRADO")]
    [InlineData(EstadoDecision.YaDecidido, "YA_DECIDIDO")]
    public async Task Decidir_TraduceResultado(EstadoDecision estado, string? codigo)
    {
        var e = new Escenario("Administrador");
        e.Cuentas.Setup(x => x.Decidir(12, new AdministradorAutorizado(7, 20), true, null, It.IsAny<DateTime>())).ReturnsAsync(estado);
        var resultado = await Servicio(e).Decidir(e.Principal, 12, true, null);
        Assert.Equal(codigo is null, resultado.IsSuccess);
        if (codigo is not null) Assert.Equal(codigo, resultado.Error.Code);
    }

    [Fact]
    public async Task Pendientes_ExigeAdministrador()
    {
        var e = new Escenario("Comprador");
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Servicio(e).Pendientes(e.Principal));
        e.Cuentas.Verify(x => x.Pendientes(), Times.Never);
    }

    [Fact]
    public async Task Pendientes_DevuelveSolicitudes()
    {
        var e = new Escenario("Administrador");
        var pendientes = new List<VendedorPendienteDto> { new(12, 7, DateTime.UtcNow) };
        e.Cuentas.Setup(x => x.Pendientes()).ReturnsAsync(pendientes);
        Assert.Equal(pendientes, await Servicio(e).Pendientes(e.Principal));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Bootstrap_NormalizaCorreoYHasheaClave(bool creado)
    {
        var e = new Escenario();
        e.Claves.Setup(x => x.Hashear("clave")).Returns("hash-admin");
        e.Cuentas.Setup(x => x.CrearPrimerAdministrador(It.Is<NuevaCuenta>(c => c.Correo == "admin@example.com" && c.PasswordHash == "hash-admin" && c.Rol == "Administrador"), It.IsAny<DateTime>())).ReturnsAsync(creado);
        Assert.Equal(creado, await Servicio(e).CrearPrimerAdministrador(" ADMIN@EXAMPLE.COM ", "clave"));
        e.Cuentas.Verify(x => x.CrearPrimerAdministrador(It.Is<NuevaCuenta>(c => c.PasswordHash == "hash-admin"), It.IsAny<DateTime>()), Times.Once);
    }
}
