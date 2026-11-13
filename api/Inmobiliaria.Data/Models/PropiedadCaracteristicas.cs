using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class PropiedadCaracteristicas
{
    public int IdPropiedad { get; set; }

    public int IdCaracteristica { get; set; }

    public string? Valor { get; set; }

    public virtual Caracteristicas IdCaracteristicaNavigation { get; set; } = null!;

    public virtual Propiedades IdPropiedadNavigation { get; set; } = null!;
}
