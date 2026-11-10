using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class HistorialAcciones
{
    public int IdAccion { get; set; }

    public int? IdUsuario { get; set; }

    public string Accion { get; set; } = null!;

    public string TablaAfectada { get; set; } = null!;

    public int? IdRegistro { get; set; }

    public string? Detalles { get; set; }

    public string? DireccionIP { get; set; }

    public DateTime FechaAccion { get; set; }

    public virtual Usuarios? IdUsuarioNavigation { get; set; }
}
