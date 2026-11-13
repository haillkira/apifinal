using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Propiedades
{
    public int IdPropiedad { get; set; }

    public int IdVendedor { get; set; }

    public int IdTipoPropiedad { get; set; }

    public string Titulo { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int IdProvincia { get; set; }

    public string Ciudad { get; set; } = null!;

    public string Sector { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string? ReferenciaDireccion { get; set; }

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public int Habitaciones { get; set; }

    public int Banos { get; set; }

    public int Parqueos { get; set; }

    public decimal MetrosCuadrados { get; set; }

    public bool Amueblado { get; set; }

    public bool TienePiscina { get; set; }

    public bool TieneAscensor { get; set; }

    public string EstadoPropiedad { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? ActualizadoEn { get; set; }
    public DateTime? EliminadoEn { get; set; }

    public virtual Provincias IdProvinciaNavigation { get; set; } = null!;

    public virtual TiposPropiedad IdTipoPropiedadNavigation { get; set; } = null!;

    public virtual Vendedores IdVendedorNavigation { get; set; } = null!;

    public virtual ICollection<ImagenesPropiedades> ImagenesPropiedades { get; set; } = new List<ImagenesPropiedades>();

    public virtual ICollection<PropiedadCaracteristicas> PropiedadCaracteristicas { get; set; } = new List<PropiedadCaracteristicas>();

    public virtual ICollection<Publicaciones> Publicaciones { get; set; } = new List<Publicaciones>();
}
