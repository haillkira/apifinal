using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Solicitudes
{
    public int IdSolicitud { get; set; }

    public int IdComprador { get; set; }

    public int IdPublicacion { get; set; }

    public string TipoSolicitud { get; set; } = null!;

    public string? Mensaje { get; set; }

    public string EstadoSolicitud { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaRespuesta { get; set; }
    public string? Respuesta { get; set; }

    public virtual ICollection<Contratos> Contratos { get; set; } = new List<Contratos>();

    public virtual Compradores IdCompradorNavigation { get; set; } = null!;

    public virtual Publicaciones IdPublicacionNavigation { get; set; } = null!;

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual Reservaciones? Reservaciones { get; set; }

    public virtual ICollection<Visitas> Visitas { get; set; } = new List<Visitas>();
}
