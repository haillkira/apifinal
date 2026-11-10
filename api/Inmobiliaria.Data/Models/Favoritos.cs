using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Favoritos
{
    public int IdComprador { get; set; }

    public int IdPublicacion { get; set; }

    public DateTime FechaAgregado { get; set; }

    public virtual Compradores IdCompradorNavigation { get; set; } = null!;

    public virtual Publicaciones IdPublicacionNavigation { get; set; } = null!;
}
