using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class Notificaciones
{
    public int IdNotificacion { get; set; }

    public int IdUsuarioDestino { get; set; }

    public string TipoNotificacion { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string? UrlDestino { get; set; }

    public bool Leida { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaLectura { get; set; }

    public virtual Usuarios IdUsuarioDestinoNavigation { get; set; } = null!;
}
