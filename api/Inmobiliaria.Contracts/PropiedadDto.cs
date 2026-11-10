namespace Inmobiliaria.Contracts;

public sealed class PropiedadDto
{
    public int IdPropiedad { get; set; }
    public int IdTipoPropiedad { get; init; }
    public int IdProvincia { get; init; }
    public string Titulo { get; init; } = "";
    public string Descripcion { get; init; } = "";
    public string Ciudad { get; init; } = "";
    public string Sector { get; init; } = "";
    public string Direccion { get; init; } = "";
    public int Habitaciones { get; init; }
    public int Banos { get; init; }
    public int Parqueos { get; init; }
    public decimal MetrosCuadrados { get; init; }
    public string EstadoPropiedad { get; init; } = "";
}
