using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Vista_ReportesPendientes
{
    public int IdReporte { get; set; }

    public int IdUsuarioReporta { get; set; }

    public int? IdPublicacion { get; set; }

    public int? IdUsuarioReportado { get; set; }

    public string Categoria { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string EstadoReporte { get; set; } = null!;

    public DateTime FechaReporte { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int? ResueltoPor { get; set; }

    public string? Resolucion { get; set; }
}
