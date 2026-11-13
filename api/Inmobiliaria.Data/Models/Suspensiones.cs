using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Suspensiones
{
    public int IdSuspension { get; set; }

    public int IdUsuario { get; set; }

    public int IdAdministrador { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activa { get; set; }

    public virtual Administradores IdAdministradorNavigation { get; set; } = null!;

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;
}
