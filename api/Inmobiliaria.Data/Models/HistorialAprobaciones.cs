using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class HistorialAprobaciones
{
    public int IdHistorial { get; set; }

    public int IdVendedor { get; set; }

    public int IdAdministrador { get; set; }

    public string Accion { get; set; } = null!;

    public string? Comentario { get; set; }

    public DateTime FechaAccion { get; set; }

    public virtual Administradores IdAdministradorNavigation { get; set; } = null!;

    public virtual Vendedores IdVendedorNavigation { get; set; } = null!;
}
