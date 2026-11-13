using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Compradores
{
    public int IdComprador { get; set; }

    public int IdUsuario { get; set; }

    public string Cedula { get; set; } = null!;

    public bool CuentaAprobada { get; set; }

    public DateTime? FechaVerificacion { get; set; }

    public string? PreferenciaContacto { get; set; }

    public virtual ICollection<BusquedasGuardadas> BusquedasGuardadas { get; set; } = new List<BusquedasGuardadas>();

    public virtual ICollection<Conversaciones> Conversaciones { get; set; } = new List<Conversaciones>();

    public virtual ICollection<Favoritos> Favoritos { get; set; } = new List<Favoritos>();

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<Solicitudes> Solicitudes { get; set; } = new List<Solicitudes>();
}
