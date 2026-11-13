using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Publicaciones
{
    public int IdPublicacion { get; set; }

    public int IdPropiedad { get; set; }

    public string TipoOperacion { get; set; } = null!;

    public decimal Precio { get; set; }

    public string Moneda { get; set; } = null!;

    public decimal? Deposito { get; set; }

    public decimal? MantenimientoMensual { get; set; }

    public string EstadoPublicacion { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public string? MotivoRechazo { get; set; }

    public bool Destacada { get; set; }

    public virtual ICollection<Conversaciones> Conversaciones { get; set; } = new List<Conversaciones>();

    public virtual ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();

    public virtual Propiedades IdPropiedadNavigation { get; set; } = null!;

    public virtual ICollection<Reportes> Reportes { get; set; } = new List<Reportes>();

    public virtual ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
}
