using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Administradores
{
    public int IdAdministrador { get; set; }

    public int IdUsuario { get; set; }

    public string NivelPermiso { get; set; } = null!;

    public virtual ICollection<HistorialAprobaciones> HistorialAprobaciones { get; set; } = new List<HistorialAprobaciones>();

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Suspensiones> Suspensiones { get; set; } = new List<Suspensiones>();
}
