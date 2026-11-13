using Inmobiliaria.Contracts;
using Inmobiliaria.Web.Models;
using Microsoft.AspNetCore.Identity.Data;
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using LoginRequest = Inmobiliaria.Contracts.LoginRequest;

namespace Inmobiliaria.Web.Services;

public class InmobiliariaApi(HttpClient http)
{
    public string UrlImagen(string ruta) => new Uri(http.BaseAddress!, ruta).ToString();
    public async Task<string> ImagenPrivada(int propiedad, int imagen, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/propiedades/{propiedad}/imagenes/{imagen}/archivo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return "data:image/jpeg;base64," + Convert.ToBase64String(await response.Content.ReadAsByteArrayAsync());
    }
    public async Task<string?> SubirImagenes(int propiedad, IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile> archivos, string token)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            foreach (var archivo in archivos)
            {
                var stream = new StreamContent(archivo.OpenReadStream(5 * 1024 * 1024));
                content.Add(stream, "archivos", "imagen");
            }
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v1/propiedades/{propiedad}/imagenes") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request);
            return response.IsSuccessStatusCode ? null : await LeerErrorAsync(response);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException)
        { return "No se pudo completar la carga. Actualiza las imágenes antes de reintentar."; }
    }
    public sealed record TipoDto(int IdTipoPropiedad, string Nombre);
    public sealed record ProvinciaDto(int IdProvincia, string Nombre);

    public async Task<T> Consultar<T>(string ruta, string? token = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException(await LeerErrorAsync(response));
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<string?> Enviar(HttpMethod metodo, string ruta, object? datos, string token)
    {
        try
        {
            using var request = new HttpRequestMessage(metodo, ruta);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (datos is not null) request.Content = JsonContent.Create(datos);
            using var response = await http.SendAsync(request);
            return response.IsSuccessStatusCode ? null : await LeerErrorAsync(response);
        }
        catch (HttpRequestException) { return "No se pudo conectar. Inténtalo de nuevo."; }
        catch (TaskCanceledException) { return "La solicitud tardó demasiado. Actualiza el listado antes de reintentar."; }
    }
    public async Task<List<PublicacionResumen>> GetPublicacionesAsync()
        => await Consultar<List<PublicacionResumen>>("api/v1/publicaciones") ?? [];

    public async Task<(LoginResponse? Datos, string? Error)> LoginAsync(string correo, string password)
    {
        var resp = await http.PostAsJsonAsync("api/v1/auth/login", new LoginRequest(correo, password));
        if (resp.IsSuccessStatusCode)
        {
            var datos = await resp.Content.ReadFromJsonAsync<LoginResponse>();
            return datos is null ? (null, "La API respondió sin datos.") : (datos, null);
        }
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            return (null, "Correo o contraseña incorrectos.");
        return (null, await LeerErrorAsync(resp));
    }

    // La autorización se envía por solicitud para evitar mezclar sesiones de usuarios.
    public async Task<(List<PropiedadDto>? Datos, string? Error)> GetMisPropiedadesAsync(string token)
    {
        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "api/v1/propiedades");
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var resp = await http.SendAsync(solicitud);
        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            return (null, "La sesión ha expirado. Inicia sesión nuevamente.");
        if (resp.StatusCode == HttpStatusCode.Forbidden)
            return (null, "Necesitas una cuenta de vendedor aprobada. Si acabas de registrarte, espera la aprobación del administrador y actualiza el listado.");
        if (!resp.IsSuccessStatusCode)
            return (null, await LeerErrorAsync(resp));
        return (await resp.Content.ReadFromJsonAsync<List<PropiedadDto>>() ?? [], null);
    }

    // tipo: "compradores" o "vendedores". Devuelve null si todo salió bien, o el mensaje de error.
    public async Task<string?> RegistrarAsync(string tipo, RegistroRequest datos)
    {
        var resp = await http.PostAsJsonAsync($"api/v1/auth/{tipo}", datos);
        return resp.IsSuccessStatusCode ? null : await LeerErrorAsync(resp);
    }

    // Convierte la respuesta de error de la API en un texto para mostrar.
    private static async Task<string> LeerErrorAsync(HttpResponseMessage resp)
    {
        if (resp.StatusCode == HttpStatusCode.Unauthorized) return "Tu sesión no es válida. Inicia sesión nuevamente.";
        if (resp.StatusCode == HttpStatusCode.Forbidden) return "No tienes permiso para esta operación o tu cuenta está pendiente de aprobación.";
        try
        {
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
            if (doc.TryGetProperty("mensaje", out var mensaje)) return mensaje.GetString() ?? "No se pudo completar la operación.";
            if (doc.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Object)
            {
                var msgs = errs.EnumerateObject()
                    .SelectMany(p => p.Value.EnumerateArray().Select(m => m.GetString()));
                return string.Join(" ", msgs);
            }
            if (doc.TryGetProperty("title", out var t) && t.GetString() is { } titulo)
                return titulo;
        }
        catch { }
        return $"Error {(int)resp.StatusCode}.";
    }
}
