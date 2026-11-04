using System.ComponentModel.DataAnnotations;
namespace Inmobiliaria.Contracts;
public sealed record RegistroRequest([Required,MaxLength(100)] string Nombre, [Required,MaxLength(100)] string Apellido, [Required,EmailAddress,MaxLength(254)] string Correo, [Required,MinLength(12),MaxLength(128)] string Password, [Required,MaxLength(30)] string Telefono, [Required,RegularExpression(@"^\d{11}$")] string Cedula);
public sealed record LoginRequest([Required,EmailAddress] string Correo,[Required] string Password);
public sealed record UsuarioResponse(int IdUsuario,string Nombre,string Apellido,string Rol);
public sealed record LoginResponse(string AccessToken,DateTime ExpiraEn,UsuarioResponse Usuario);
public sealed record DecisionRequest([MaxLength(1000)] string? Motivo);
public sealed record PropiedadRequest([Range(1,int.MaxValue)] int IdTipoPropiedad,[Required,MaxLength(150)] string Titulo,[Required,MaxLength(4000)] string Descripcion,[Range(1,int.MaxValue)] int IdProvincia,[Required,MaxLength(100)] string Ciudad,[Required,MaxLength(100)] string Sector,[Required,MaxLength(300)] string Direccion,[Range(0,100)] int Habitaciones,[Range(0,100)] int Banos,[Range(0,100)] int Parqueos,[Range(typeof(decimal),"0.01","999999999")] decimal MetrosCuadrados);
public sealed record PublicacionRequest([Range(1,int.MaxValue)] int IdPropiedad,[Required] string TipoOperacion,[Range(typeof(decimal),"0.01","999999999")] decimal Precio,[Required] string Moneda,decimal? Deposito,decimal? MantenimientoMensual);
