using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Pagos
{
    public int IdPago { get; set; }

    public int IdComprador { get; set; }

    public int? IdSolicitud { get; set; }

    public int? IdReservacion { get; set; }

    public decimal Monto { get; set; }

    public string Moneda { get; set; } = null!;

    public string Concepto { get; set; } = null!;

    public string MetodoPago { get; set; } = null!;

    public string? ReferenciaExterna { get; set; }

    public string EstadoPago { get; set; } = null!;

    public DateTime? FechaPago { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Compradores IdCompradorNavigation { get; set; } = null!;

    public virtual Reservaciones? IdReservacionNavigation { get; set; }

    public virtual Solicitudes? IdSolicitudNavigation { get; set; }
}
