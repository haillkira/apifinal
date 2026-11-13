using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class ImagenesPropiedades
{
    public int IdImagen { get; set; }

    public int IdPropiedad { get; set; }

    public string UrlImagen { get; set; } = null!;

    public bool EsPrincipal { get; set; }

    public int Orden { get; set; }

    public string? TextoAlternativo { get; set; }

    public DateTime FechaSubida { get; set; }

    public virtual Propiedades IdPropiedadNavigation { get; set; } = null!;
}
