using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Mensajes
{
    public int IdMensaje { get; set; }

    public int IdConversacion { get; set; }

    public int IdRemitente { get; set; }

    public string Contenido { get; set; } = null!;

    public DateTime FechaEnvio { get; set; }

    public bool Leido { get; set; }

    public DateTime? FechaLectura { get; set; }

    public virtual Conversaciones IdConversacionNavigation { get; set; } = null!;

    public virtual Usuarios IdRemitenteNavigation { get; set; } = null!;
}
