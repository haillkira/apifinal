using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Contracts;
public sealed record ProgramarVisitaRequest(DateTimeOffset Fecha, [MaxLength(1000)] string? Notas);
public sealed record VisitaDto(int IdVisita, DateTime FechaUtc, string Estado, string? Notas);
