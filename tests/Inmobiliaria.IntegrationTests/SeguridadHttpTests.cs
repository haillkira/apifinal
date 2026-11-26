using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Inmobiliaria.Api.Seguridad;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Inmobiliaria.IntegrationTests;

[CollectionDefinition("Configuracion JWT", DisableParallelization = true)]
public sealed class ConfiguracionJwtCollection;

[Collection("Configuracion JWT")]
public sealed class SeguridadHttpTests
{
    private static readonly string Secreto = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

    private static WebApplicationFactory<Program> Aplicacion(
        Mock<ICuentasRepository>? cuentas = null,
        Mock<IImagenesRepository>? imagenes = null,
        bool claveConfigurada = true,
        bool conexionConfigurada = true,
        bool usarCargaReal = false,
        string entorno = "Development")
    {
        var claves = new Mock<IClavesCuenta>();
        claves.Setup(c => c.Verificar(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(entorno);
            builder.UseSetting("ConnectionStrings:InmobiliariaConnection", conexionConfigurada
                ? "Server=localhost;Database=SoloMocks;Integrated Security=true;TrustServerCertificate=true" : "");
            builder.UseSetting("Jwt:Issuer", "pruebas-http");
            builder.UseSetting("Jwt:Audience", "clientes-http");
            builder.ConfigureServices(services =>
            {
                services.AddSingleton((cuentas ?? new Mock<ICuentasRepository>()).Object);
                services.AddSingleton((imagenes ?? new Mock<IImagenesRepository>()).Object);
                services.AddSingleton(claves.Object);
                if (!usarCargaReal)
                    services.AddSingleton(new ClaveJwt(claveConfigurada ? Secreto : null, desarrollo: true));
            });
        });
    }

    private static Mock<ICuentasRepository> CuentaActiva()
    {
        var repositorio = new Mock<ICuentasRepository>();
        var cuenta = new Cuenta(new UsuarioResponse(7, "Ana", "Perez", "Vendedor"), "Activa", "hash-simulado");
        repositorio.Setup(r => r.BuscarPorCorreo("ana@example.test")).ReturnsAsync(cuenta);
        repositorio.Setup(r => r.BuscarPorId(7)).ReturnsAsync(cuenta);
        return repositorio;
    }

    [Fact]
    public async Task Login_EmiteTokenQueAutorizaPerfilYRechazaTokenAlterado()
    {
        var cuentas = CuentaActiva();
        using var app = Aplicacion(cuentas);
        using var cliente = app.CreateClient();
        using var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("ana@example.test", "password-simulado"));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var sesion = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(sesion);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        using var perfil = await cliente.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
        Assert.Equal(7, (await perfil.Content.ReadFromJsonAsync<UsuarioResponse>())!.IdUsuario);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken + "alterado");
        using var alterado = await cliente.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, alterado.StatusCode);
    }

    [Fact]
    public async Task Login_SinClaveConfiguradaDevuelve503YNoConsultaCuentas()
    {
        var cuentas = new Mock<ICuentasRepository>(MockBehavior.Strict);
        using var app = Aplicacion(cuentas, claveConfigurada: false);
        using var cliente = app.CreateClient();
        using var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("ana@example.test", "password-simulado"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        cuentas.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Login_CuentaDesconocidaDevuelve401()
    {
        using var app = Aplicacion();
        using var cliente = app.CreateClient();
        using var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("nadie@example.test", "password-simulado"));
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Health_InformaConfiguracionSinConectarConLaBase(bool configurada)
    {
        using var app = Aplicacion(conexionConfigurada: configurada);
        using var cliente = app.CreateClient();
        using var respuesta = await cliente.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var datos = await respuesta.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(configurada, datos.GetProperty("databaseConfigured").GetBoolean());
        Assert.False(datos.GetProperty("databaseChecked").GetBoolean());
    }

    [Fact]
    public async Task SinConexion_ApiDevuelve503PeroInicioSigueDisponible()
    {
        using var app = Aplicacion(conexionConfigurada: false);
        using var cliente = app.CreateClient();
        using var api = await cliente.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, api.StatusCode);
        using var inicio = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
    }

    [Theory]
    [InlineData("../fuera.jpg", HttpStatusCode.BadRequest)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg\n", HttpStatusCode.BadRequest)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg", HttpStatusCode.NotFound)]
    public async Task Imagenes_ImpideRutasExternasYRechazaNombresInvalidos(string nombre, HttpStatusCode estado)
    {
        var imagenes = new Mock<IImagenesRepository>();
        imagenes.Setup(r => r.PropiedadPublica(11)).ReturnsAsync(7);
        imagenes.Setup(r => r.Listar(7)).ReturnsAsync([new ImagenDto(3, nombre, 0)]);
        using var app = Aplicacion(imagenes: imagenes);
        using var cliente = app.CreateClient();
        using var respuesta = await cliente.GetAsync("/api/v1/publicaciones/11/imagenes/3/archivo");
        Assert.Equal(estado, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cargar_ObtieneClaveDelEntornoEnAmbosEntornos(bool desarrollo)
    {
        var anterior = Environment.GetEnvironmentVariable("Jwt__Key");
        try
        {
            Environment.SetEnvironmentVariable("Jwt__Key", Secreto);
            var clave = ClaveJwt.Cargar(desarrollo);
            Assert.True(clave.Configurada);
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(Secreto), clave.ClaveFirma.Key);
        }
        finally { Environment.SetEnvironmentVariable("Jwt__Key", anterior); }
    }

    [Fact]
    public void Produccion_SinConexionSeNiegaAArrancar()
    {
        using var app = Aplicacion(conexionConfigurada: false, entorno: "Production");
        Assert.Throws<InvalidOperationException>(() => app.CreateClient());
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Arranque_UsaLaCargaRealDelSecretoParaFirmarYValidarJwt(string entorno)
    {
        var anterior = Environment.GetEnvironmentVariable("Jwt__Key");
        try
        {
            Environment.SetEnvironmentVariable("Jwt__Key", Secreto);
            using var app = Aplicacion(CuentaActiva(), usarCargaReal: true, entorno: entorno);
            using var cliente = app.CreateClient();
            using var respuesta = await cliente.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest("ana@example.test", "password-simulado"));
            Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
            var sesion = await respuesta.Content.ReadFromJsonAsync<LoginResponse>();
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion!.AccessToken);
            using var perfil = await cliente.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, perfil.StatusCode);
        }
        finally { Environment.SetEnvironmentVariable("Jwt__Key", anterior); }
    }
}
