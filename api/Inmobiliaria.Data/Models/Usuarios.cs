using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Usuarios
{
    public int IdUsuario { get; set; }

    public int IdRol { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string? FotoPerfilUrl { get; set; }

    public string EstadoCuenta { get; set; } = null!;

    public bool CorreoVerificado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual Administradores? Administradores { get; set; }

    public virtual Compradores? Compradores { get; set; }

    public virtual ICollection<HistorialAcciones> HistorialAcciones { get; set; } = new List<HistorialAcciones>();

    public virtual Roles IdRolNavigation { get; set; } = null!;

    public virtual ICollection<Mensajes> Mensajes { get; set; } = new List<Mensajes>();

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual ICollection<Reportes> ReportesIdUsuarioReportaNavigation { get; set; } = new List<Reportes>();

    public virtual ICollection<Reportes> ReportesIdUsuarioReportadoNavigation { get; set; } = new List<Reportes>();

    public virtual ICollection<Reportes> ReportesResueltoPorNavigation { get; set; } = new List<Reportes>();

    public virtual ICollection<Suspensiones> Suspensiones { get; set; } = new List<Suspensiones>();

    public virtual Vendedores? VendedoresIdUsuarioNavigation { get; set; }

    public virtual ICollection<Vendedores> VendedoresVerificadoPorNavigation { get; set; } = new List<Vendedores>();
}
