using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Reservaciones
{
    public int IdReservacion { get; set; }

    public int IdSolicitud { get; set; }

    public decimal MontoReserva { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public string EstadoReservacion { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public virtual Solicitudes IdSolicitudNavigation { get; set; } = null!;

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();
}
