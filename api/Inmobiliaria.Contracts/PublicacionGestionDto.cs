namespace Inmobiliaria.Contracts;
public sealed record PublicacionGestionDto(int IdPublicacion, int IdPropiedad, string Titulo, string Estado, decimal Precio, string Moneda);
