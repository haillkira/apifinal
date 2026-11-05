using System.Net.Http.Json;
using Inmobiliaria.Web.Models;

namespace Inmobiliaria.Web.Services;

public class InmobiliariaApi(HttpClient http)
{
    public async Task<List<PublicacionResumen>> GetPublicacionesAsync()
        => await http.GetFromJsonAsync<List<PublicacionResumen>>("api/v1/publicaciones") ?? [];
}