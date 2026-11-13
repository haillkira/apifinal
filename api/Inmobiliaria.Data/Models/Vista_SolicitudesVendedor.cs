using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Vista_SolicitudesVendedor
{
    public int IdSolicitud { get; set; }

    public int IdComprador { get; set; }

    public int IdPublicacion { get; set; }

    public string TipoSolicitud { get; set; } = null!;

    public string? Mensaje { get; set; }

    public string EstadoSolicitud { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    public int IdVendedor { get; set; }
}
