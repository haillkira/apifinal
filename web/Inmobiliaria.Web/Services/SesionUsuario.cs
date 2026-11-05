using Inmobiliaria.Contracts;

namespace Inmobiliaria.Web.Services;

public class SesionUsuario
{
    public string? Token { get; private set; }
    public UsuarioResponse? Usuario { get; private set; }
    public bool EstaAutenticado => Token is not null;

    public event Action? Cambio;

    public void Iniciar(LoginResponse r)
    {
        Token = r.AccessToken;
        Usuario = r.Usuario;
        Cambio?.Invoke();
    }

    public void Cerrar()
    {
        Token = null;
        Usuario = null;
        Cambio?.Invoke();
    }
}