using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Conversaciones
{
    public int IdConversacion { get; set; }

    public int IdComprador { get; set; }

    public int IdVendedor { get; set; }

    public int? IdPublicacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? UltimoMensajeEn { get; set; }

    public virtual Compradores IdCompradorNavigation { get; set; } = null!;

    public virtual Publicaciones? IdPublicacionNavigation { get; set; }

    public virtual Vendedores IdVendedorNavigation { get; set; } = null!;

    public virtual ICollection<Mensajes> Mensajes { get; set; } = new List<Mensajes>();
}
