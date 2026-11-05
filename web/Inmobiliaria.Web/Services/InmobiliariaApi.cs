using Inmobiliaria.Contracts;
using Inmobiliaria.Web.Models;
using Microsoft.AspNetCore.Identity.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LoginRequest = Inmobiliaria.Contracts.LoginRequest;

namespace Inmobiliaria.Web.Services;

public class InmobiliariaApi(HttpClient http)
{
    public async Task<List<PublicacionResumen>> GetPublicacionesAsync()
        => await http.GetFromJsonAsync<List<PublicacionResumen>>("api/v1/publicaciones") ?? [];

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

    // tipo: "compradores" o "vendedores". Devuelve null si todo salió bien, o el mensaje de error.
    public async Task<string?> RegistrarAsync(string tipo, RegistroRequest datos)
    {
        var resp = await http.PostAsJsonAsync($"api/v1/auth/{tipo}", datos);
        return resp.IsSuccessStatusCode ? null : await LeerErrorAsync(resp);
    }

    // Convierte la respuesta de error de la API en un texto para mostrar.
    private static async Task<string> LeerErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var doc = await resp.Content.ReadFromJsonAsync<JsonElement>();
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