using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Vista_PublicacionesDisponibles
{
    public int IdPublicacion { get; set; }

    public int IdPropiedad { get; set; }

    public string TipoOperacion { get; set; } = null!;

    public decimal Precio { get; set; }

    public string Moneda { get; set; } = null!;

    public decimal? Deposito { get; set; }

    public decimal? MantenimientoMensual { get; set; }

    public string EstadoPublicacion { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaPublicacion { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public string? MotivoRechazo { get; set; }

    public bool Destacada { get; set; }

    public int IdVendedor { get; set; }
}
