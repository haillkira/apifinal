using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class TiposPropiedad
{
    public int IdTipoPropiedad { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual ICollection<Propiedades> Propiedades { get; set; } = new List<Propiedades>();
}
