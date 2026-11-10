using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Vista_VendedoresPendientes
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

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Correo { get; set; } = null!;
}
