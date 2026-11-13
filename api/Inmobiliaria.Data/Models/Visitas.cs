using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Visitas
{
    public int IdVisita { get; set; }

    public int IdSolicitud { get; set; }

    public DateTime FechaVisita { get; set; }

    public string EstadoVisita { get; set; } = null!;

    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Solicitudes IdSolicitudNavigation { get; set; } = null!;
}
