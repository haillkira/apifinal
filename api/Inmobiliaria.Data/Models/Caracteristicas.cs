using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Caracteristicas
{
    public int IdCaracteristica { get; set; }

    public string Nombre { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public virtual ICollection<PropiedadCaracteristicas> PropiedadCaracteristicas { get; set; } = new List<PropiedadCaracteristicas>();
}
