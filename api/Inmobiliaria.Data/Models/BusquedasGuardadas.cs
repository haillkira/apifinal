using System;
using System.Collections.Generic;

namespace Inmobiliaria.Data.Entities.DatabaseFirst;

public partial class BusquedasGuardadas
{
    public int IdBusqueda { get; set; }

    public int IdComprador { get; set; }

    public string NombreBusqueda { get; set; } = null!;

    public string FiltrosJson { get; set; } = null!;

    public bool NotificarNuevosResultados { get; set; }

    public DateTime FechaCreacion { get; set; }

    public virtual Compradores IdCompradorNavigation { get; set; } = null!;
}
