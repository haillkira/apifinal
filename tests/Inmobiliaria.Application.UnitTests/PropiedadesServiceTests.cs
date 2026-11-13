using System.Linq.Expressions;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;

namespace Inmobiliaria.Application.UnitTests;

public class PropiedadesServiceTests
{
    private static PropiedadesService Servicio(Escenario e) => new(e.Propiedades.Object, e.Acceso, e.Principal);

    [Theory]
    [InlineData("Casa", true, 31, null)]
    [InlineData("Apartamento", true, 31, null)]
    [InlineData(null, true, 31, "TIPO_INVALIDO")]
    [InlineData("Terreno", true, 31, "TIPO_INVALIDO")]
    [InlineData("Casa", false, 31, "PROVINCIA_INVALIDA")]
    [InlineData("Casa", true, null, "NO_ENCONTRADA")]
    public async Task Guardar_ValidaCatalogosYAsignaIdSoloAlGuardar(string? tipo, bool provincia, int? id, string? error)
    {
        var e = new Escenario();
        var propiedad = new PropiedadDto { IdTipoPropiedad = 2, IdProvincia = 3 };
        e.Propiedades.Setup(x => x.NombreTipo(2)).ReturnsAsync(tipo);
        e.Propiedades.Setup(x => x.ExisteProvincia(3)).ReturnsAsync(provincia);
        e.Propiedades.Setup(x => x.Guardar(12, propiedad, It.IsAny<DateTime>())).ReturnsAsync(id);
        var resultado = await Servicio(e).Guardar(propiedad);
        Assert.Equal(error is null, resultado.IsSuccess);
        Assert.Equal(error is null ? 31 : 0, propiedad.IdPropiedad);
        if (error is not null) Assert.Equal(error, resultado.Error.Code);
        e.Propiedades.Verify(x => x.Guardar(12, propiedad, It.IsAny<DateTime>()),
            error is "TIPO_INVALIDO" or "PROVINCIA_INVALIDA" ? Times.Never() : Times.Once());
    }

    [Fact]
    public async Task Guardar_CompradorNoPuedePersistir()
    {
        var e = new Escenario("Comprador");
        await Assert.ThrowsAsync<AccesoDenegadoException>(() => Servicio(e).Guardar(new()));
        e.Propiedades.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Buscar_LimitaConsultaAlVendedorActual(bool existe)
    {
        var e = new Escenario();
        var propiedad = existe ? new PropiedadDto { IdPropiedad = 31 } : null;
        e.Propiedades.Setup(x => x.Buscar(12, 31)).ReturnsAsync(propiedad);
        var resultado = await Servicio(e).Buscar(31);
        Assert.True(resultado.IsSuccess);
        Assert.Same(propiedad, resultado.Value);
        e.Propiedades.Verify(x => x.Buscar(12, 31), Times.Once);
    }

    [Fact]
    public async Task GetList_TransmiteCriterioYVendedor()
    {
        var e = new Escenario();
        Expression<Func<PropiedadDto, bool>> criterio = p => p.IdProvincia == 3;
        var lista = new List<PropiedadDto> { new() { IdPropiedad = 31, IdProvincia = 3 } };
        e.Propiedades.Setup(x => x.Listar(12, criterio)).ReturnsAsync(lista);
        var resultado = await Servicio(e).GetList(criterio);
        Assert.True(resultado.IsSuccess);
        Assert.Equal(lista, resultado.Value);
        e.Propiedades.Verify(x => x.Listar(12, criterio), Times.Once);
    }

    [Theory]
    [InlineData(EstadoEliminacion.Eliminada, null)]
    [InlineData(EstadoEliminacion.NoEncontrada, "NO_ENCONTRADA")]
    [InlineData(EstadoEliminacion.ConPublicaciones, "CON_PUBLICACIONES")]
    public async Task Eliminar_RespetaVendedorYTraduceResultado(EstadoEliminacion estado, string? error)
    {
        var e = new Escenario();
        e.Propiedades.Setup(x => x.Eliminar(12, 31)).ReturnsAsync(estado);
        var resultado = await Servicio(e).Eliminar(31);
        Assert.Equal(error is null, resultado.IsSuccess);
        if (error is not null) Assert.Equal(error, resultado.Error.Code);
        e.Propiedades.Verify(x => x.Eliminar(12, 31), Times.Once);
    }
}
