using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Vendedores
{
    public int IdVendedor { get; set; }

    public int IdUsuario { get; set; }

    public string Cedula { get; set; } = null!;

    public string TipoVendedor { get; set; } = null!;

    public string? NombreComercial { get; set; }

    public string? NumeroLicencia { get; set; }

    public bool CuentaAprobada { get; set; }

    public string EstadoAprobacion { get; set; } = null!;

    public DateTime FechaSolicitud { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public string? MotivoRechazo { get; set; }

    public int? VerificadoPor { get; set; }

    public virtual ICollection<Conversaciones> Conversaciones { get; set; } = new List<Conversaciones>();

    public virtual ICollection<HistorialAprobaciones> HistorialAprobaciones { get; set; } = new List<HistorialAprobaciones>();

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Propiedades> Propiedades { get; set; } = new List<Propiedades>();

    public virtual Usuarios? VerificadoPorNavigation { get; set; }
}
