using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Contratos
{
    public int IdContrato { get; set; }

    public int IdSolicitud { get; set; }

    public string TipoContrato { get; set; } = null!;

    public string NumeroContrato { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public decimal MontoAcordado { get; set; }

    public string Moneda { get; set; } = null!;

    public string? DocumentoUrl { get; set; }

    public string EstadoContrato { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual Solicitudes IdSolicitudNavigation { get; set; } = null!;
}
