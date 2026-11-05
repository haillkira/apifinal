namespace Inmobiliaria.Web.Models;

public record PublicacionResumen(
    int IdPublicacion,
    string Titulo,
    string Ciudad,
    string Sector,
    int Habitaciones,
    int Banos,
    decimal Precio,
    string Moneda,
    decimal? Deposito,
    decimal? MantenimientoMensual);