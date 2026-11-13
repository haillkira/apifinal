using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;

namespace Inmobiliaria.Application.UnitTests;

public class PublicacionesServiceTests
{
    private static PublicacionesService Servicio(Escenario e) => new(e.Propiedades.Object, e.Acceso);
    private static PublicacionRequest Solicitud => new(31, "Alquiler", 15000m, "DOP", null, null);

    [Theory]
    [InlineData("Venta", "DOP", 15000, 0, 0)]
    [InlineData("Alquiler", "EUR", 15000, 0, 0)]
    [InlineData("Alquiler", "DOP", 0, 0, 0)]
    [InlineData("Alquiler", "DOP", -1, 0, 0)]
    [InlineData("Alquiler", "DOP", 15000, -1, 0)]
    [InlineData("Alquiler", "DOP", 15000, 0, -1)]
    public async Task Publicar_RechazaOfertaInvalidaSinPersistir(string operacion, string moneda, int precio, int deposito, int mantenimiento)
    {
        var e = new Escenario();
        var solicitud = new PublicacionRequest(31, operacion, precio, moneda, deposito, mantenimiento);
        await Assert.ThrowsAsync<ArgumentException>(() => Servicio(e).Publicar(e.Principal, solicitud));
        e.Propiedades.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, "Casa", "NO_ENCONTRADA")]
    [InlineData("Alquilada", "Casa", "NO_DISPONIBLE")]
    [InlineData("Disponible", "Terreno", "TIPO_INVALIDO")]
    [InlineData("Disponible", null, "TIPO_INVALIDO")]
    public async Task Publicar_RechazaPropiedadNoPublicable(string? estado, string? tipo, string error)
    {
        var e = new Escenario();
        e.Propiedades.Setup(x => x.BuscarPublicable(12, 31)).ReturnsAsync(estado is null ? null : new PropiedadPublicable(estado, tipo));
        var resultado = await Servicio(e).Publicar(e.Principal, Solicitud);
        Assert.True(resultado.IsFailure);
        Assert.Equal(error, resultado.Error.Code);
        e.Propiedades.Verify(x => x.Publicar(It.IsAny<PublicacionRequest>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Theory]
    [InlineData("Casa", "DOP")]
    [InlineData("Apartamento", "USD")]
    public async Task Publicar_OfertaValidaDevuelveId(string tipo, string moneda)
    {
        var e = new Escenario();
        var solicitud = Solicitud with { Moneda = moneda };
        e.Propiedades.Setup(x => x.BuscarPublicable(12, 31)).ReturnsAsync(new PropiedadPublicable("Disponible", tipo));
        e.Propiedades.Setup(x => x.Publicar(solicitud, It.IsAny<DateTime>())).ReturnsAsync(50);
        var resultado = await Servicio(e).Publicar(e.Principal, solicitud);
        Assert.True(resultado.IsSuccess);
        Assert.Equal(50, resultado.Value);
        e.Propiedades.Verify(x => x.Publicar(solicitud, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Publicar_VendedorPendienteNoPuedePublicar()
    {
        var e = new Escenario();
        e.Cuentas.Setup(x => x.BuscarVendedorAprobado(7)).ReturnsAsync((int?)null);
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Servicio(e).Publicar(e.Principal, Solicitud));
        e.Propiedades.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData(null, 10001)]
    [InlineData("EUR", 1)]
    public async Task Catalogo_FiltroInvalidoNoConsultaRepositorio(string? moneda, int pagina)
    {
        var e = new Escenario();
        var resultado = await Servicio(e).Catalogo(moneda, pagina);
        Assert.True(resultado.IsFailure);
        Assert.Equal("FILTRO_INVALIDO", resultado.Error.Code);
        e.Propiedades.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("DOP", 10000)]
    [InlineData("USD", 2)]
    public async Task Catalogo_TransmiteFiltrosValidos(string? moneda, int pagina)
    {
        var e = new Escenario();
        var lista = new List<PublicacionResumenDto> { new(50, "Casa", "Santo Domingo", "Centro", 2, 1, 15000m, "DOP", null, null) };
        e.Propiedades.Setup(x => x.Catalogo(moneda, pagina, It.IsAny<DateTime>())).ReturnsAsync(lista);
        var resultado = await Servicio(e).Catalogo(moneda, pagina);
        Assert.True(resultado.IsSuccess);
        Assert.Equal(lista, resultado.Value);
        e.Propiedades.Verify(x => x.Catalogo(moneda, pagina, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Catalogos_DevuelvenTiposYProvincias()
    {
        var e = new Escenario();
        var tipos = new List<TipoPropiedadDto> { new(2, "Casa") };
        var provincias = new List<ProvinciaDto> { new(3, "Santo Domingo") };
        e.Propiedades.Setup(x => x.TiposDeAlquiler()).ReturnsAsync(tipos);
        e.Propiedades.Setup(x => x.Provincias()).ReturnsAsync(provincias);
        Assert.Equal(tipos, await Servicio(e).Tipos());
        Assert.Equal(provincias, await Servicio(e).Provincias());
    }
}
