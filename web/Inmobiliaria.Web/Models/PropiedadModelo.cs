using System.ComponentModel.DataAnnotations;
using Inmobiliaria.Contracts;
namespace Inmobiliaria.Web.Models;

public sealed class PropiedadModelo
{
    [Range(1, int.MaxValue)] public int IdTipoPropiedad { get; set; }
    [Range(1, int.MaxValue)] public int IdProvincia { get; set; }
    [Required, MaxLength(150)] public string Titulo { get; set; } = "";
    [Required, MaxLength(4000)] public string Descripcion { get; set; } = "";
    [Required, MaxLength(100)] public string Ciudad { get; set; } = "";
    [Required, MaxLength(100)] public string Sector { get; set; } = "";
    [Required, MaxLength(300)] public string Direccion { get; set; } = "";
    [Range(0, 100)] public int Habitaciones { get; set; }
    [Range(0, 100)] public int Banos { get; set; }
    [Range(0, 100)] public int Parqueos { get; set; }
    [Range(0.01, 999999999)] public decimal MetrosCuadrados { get; set; }
    public PropiedadRequest Solicitud() => new(IdTipoPropiedad, Titulo, Descripcion, IdProvincia, Ciudad, Sector, Direccion, Habitaciones, Banos, Parqueos, MetrosCuadrados);
    public static PropiedadModelo Desde(PropiedadDto p) => new() { IdTipoPropiedad = p.IdTipoPropiedad, IdProvincia = p.IdProvincia,
        Titulo = p.Titulo, Descripcion = p.Descripcion, Ciudad = p.Ciudad, Sector = p.Sector, Direccion = p.Direccion,
        Habitaciones = p.Habitaciones, Banos = p.Banos, Parqueos = p.Parqueos, MetrosCuadrados = p.MetrosCuadrados };
}

public sealed class PublicacionModelo
{
    [Range(0.01, 999999999)] public decimal Precio { get; set; }
    [Required, RegularExpression("DOP|USD")] public string Moneda { get; set; } = "DOP";
    [Range(0, 999999999)] public decimal? Deposito { get; set; }
    [Range(0, 999999999)] public decimal? MantenimientoMensual { get; set; }
}
