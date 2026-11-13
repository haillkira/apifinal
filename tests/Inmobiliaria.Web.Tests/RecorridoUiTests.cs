using System.Net;
using System.Net.Http.Json;
using Bunit;
using Inmobiliaria.Contracts;
using Inmobiliaria.Web.Components.Pages;
using Inmobiliaria.Web.Models;
using Inmobiliaria.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Inmobiliaria.Web.Tests;

public sealed class RecorridoUiTests
{
    private sealed class ApiSimulada : HttpMessageHandler
    {
        public bool ConGaleria { get; set; }
        public SolicitudAlquilerRequest? Solicitud { get; private set; }
        public PropiedadRequest? Propiedad { get; private set; }
        public PublicacionRequest? Publicacion { get; private set; }
        public string? Token { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Token = request.Headers.Authorization?.Parameter;
            var ruta = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post)
            {
                if (ruta.EndsWith("solicitudes")) Solicitud = await request.Content!.ReadFromJsonAsync<SolicitudAlquilerRequest>(cancellationToken);
                if (ruta.EndsWith("propiedades")) Propiedad = await request.Content!.ReadFromJsonAsync<PropiedadRequest>(cancellationToken);
                if (ruta.EndsWith("publicaciones")) Publicacion = await request.Content!.ReadFromJsonAsync<PublicacionRequest>(cancellationToken);
                return new(HttpStatusCode.Created) { Content = JsonContent.Create(new { id = 1 }) };
            }
            object datos = ruta switch
            {
                "/api/v1/publicaciones/15/imagenes" => ConGaleria ? new[] { new ImagenDto(1, "foto1.jpg", 0), new ImagenDto(2, "foto2.jpg", 1) } : Array.Empty<ImagenDto>(),
                "/api/v1/publicaciones" => new[] { new PublicacionResumen(15, "Casa de prueba", "Ciudad", "Sector", 2, 1, 15000, "DOP", null, null) },
                "/api/v1/catalogos/tipos" => new[] { new InmobiliariaApi.TipoDto(1, "Casa") },
                "/api/v1/catalogos/provincias" => new[] { new InmobiliariaApi.ProvinciaDto(1, "Santo Domingo") },
                "/api/v1/propiedades/1" => new PropiedadDto { IdPropiedad = 1, Titulo = "Casa de prueba" },
                "/api/v1/propiedades" => Array.Empty<PropiedadDto>(),
                _ => throw new InvalidOperationException("Ruta no prevista: " + ruta)
            };
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(datos) };
        }
    }

    private static ApiSimulada Preparar(BunitContext context, string? rol)
    {
        var handler = new ApiSimulada();
        var sesion = new SesionUsuario();
        if (rol is not null) sesion.Iniciar(new("token-prueba", DateTime.UtcNow.AddMinutes(30), new(7, "Ana", "Perez", rol)));
        context.Services.AddSingleton(sesion);
        context.Services.AddSingleton(new InmobiliariaApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost") }));
        return handler;
    }

    [Fact]
    public void Galeria_PermiteRecorrerImagenes()
    {
        using var context = new BunitContext();
        Preparar(context, null).ConGaleria = true;
        var page = context.Render<Inmobiliaria.Web.Components.GaleriaPublicacion>(p => p.Add(x => x.Id, 15).Add(x => x.Titulo, "Casa"));
        Assert.EndsWith("foto1.jpg", page.Find("img").GetAttribute("src"));
        page.Find("button[aria-label='Imagen siguiente']").Click();
        Assert.EndsWith("foto2.jpg", page.Find("img").GetAttribute("src"));
        page.Find("button[aria-label='Imagen anterior']").Click();
        Assert.EndsWith("foto1.jpg", page.Find("img").GetAttribute("src"));
    }

    [Fact]
    public void Comprador_EnviaSolicitudDesdeCatalogo()
    {
        using var context = new BunitContext();
        var handler = Preparar(context, "Comprador");
        var page = context.Render<Publicaciones>();
        page.Find("button").Click();
        page.Find("textarea").Change("Me interesa");
        page.FindAll("button").Single(b => b.TextContent == "Enviar solicitud").Click();
        page.WaitForAssertion(() => Assert.Contains("Solicitud enviada", page.Markup));
        Assert.Equal(new SolicitudAlquilerRequest(15, "Me interesa"), handler.Solicitud);
        Assert.Equal("token-prueba", handler.Token);
    }

    [Fact]
    public void Visitante_RecibeEnlaceDeLoginSinBotonDeSolicitud()
    {
        using var context = new BunitContext();
        Preparar(context, null);
        var page = context.Render<Publicaciones>();
        Assert.Empty(page.FindAll("button"));
        Assert.Equal("/login", page.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void Vendedor_CreaPropiedadDesdeFormulario()
    {
        using var context = new BunitContext();
        var handler = Preparar(context, "Vendedor");
        var page = context.Render<EditarPropiedad>();
        page.Find("#titulo").Change("Mi casa");
        page.Find("#descripcion").Change("Casa amplia");
        page.Find("#tipo").Change("1");
        page.Find("#provincia").Change("1");
        page.Find("#ciudad").Change("Santo Domingo");
        page.Find("#sector").Change("Centro");
        page.Find("#direccion").Change("Calle 1");
        page.Find("#metros").Change("80");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.NotNull(handler.Propiedad));
        Assert.Equal("Mi casa", handler.Propiedad!.Titulo);
        Assert.Equal(80, handler.Propiedad.MetrosCuadrados);
        Assert.EndsWith("/mis-propiedades", context.Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public void Vendedor_PublicaAlquilerDesdeFormulario()
    {
        using var context = new BunitContext();
        var handler = Preparar(context, "Vendedor");
        var page = context.Render<PublicarPropiedad>(p => p.Add(x => x.Id, 1));
        page.Find("#precio").Change("15000");
        page.Find("form").Submit();
        page.WaitForAssertion(() => Assert.Contains("Tu alquiler ya está publicado", page.Markup));
        Assert.Equal(new PublicacionRequest(1, "Alquiler", 15000, "DOP", null, null), handler.Publicacion);
    }
}
