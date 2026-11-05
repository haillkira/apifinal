using System.ComponentModel.DataAnnotations;

namespace Inmobiliaria.Web.Models;

public class LoginModelo
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = "";
}

public class RegistroModelo
{
    [Required(ErrorMessage = "El nombre es obligatorio."), MaxLength(100)]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "El apellido es obligatorio."), MaxLength(100)]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio."), EmailAddress(ErrorMessage = "El correo no es válido."), MaxLength(254)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(12, ErrorMessage = "La contraseña debe tener al menos 12 caracteres."), MaxLength(128)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "El teléfono es obligatorio."), MaxLength(30)]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "La cédula es obligatoria.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "La cédula debe tener exactamente 11 dígitos.")]
    public string Cedula { get; set; } = "";
}