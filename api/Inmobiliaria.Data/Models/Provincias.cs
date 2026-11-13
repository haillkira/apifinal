using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Provincias
{
    public int IdProvincia { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Propiedades> Propiedades { get; set; } = new List<Propiedades>();
}
