using System.Net;
using Inmobiliaria.Web.Services;
using Microsoft.AspNetCore.Components.Forms;

namespace Inmobiliaria.Web.Tests;

public sealed class CargaImagenesTests
{
    private sealed class ArchivoPrueba : IBrowserFile
    {
        public string Name => "foto.jpg";
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => 5 * 1024 * 1024;
        public string ContentType => "image/jpeg";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (Size > maxAllowedSize) throw new IOException("Imagen demasiado grande.");
            return new MemoryStream(new byte[(int)Size]);
        }
    }

    private sealed class Transporte(int? fallarEn = null) : HttpMessageHandler
    {
        public int Peticiones { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Peticiones++;
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Single((MultipartFormDataContent)request.Content!);
            var cuerpo = await request.Content!.ReadAsByteArrayAsync(cancellationToken);
            Assert.True(cuerpo.Length < 8 * 1024 * 1024);
            return Peticiones == fallarEn
                ? new(HttpStatusCode.Conflict) { Content = new StringContent("Capacidad agotada") }
                : new(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task OchoFotosDeCincoMb_SeEnvianSinSuperarElLimitePorPeticion()
    {
        using var transporte = new Transporte();
        using var http = new HttpClient(transporte) { BaseAddress = new Uri("https://localhost") };
        var api = new InmobiliariaApi(http);
        IBrowserFile[] fotos = Enumerable.Range(0, 8).Select(_ => (IBrowserFile)new ArchivoPrueba()).ToArray();
        Assert.Null(await api.SubirImagenes(7, fotos, "token-prueba"));
        Assert.Equal(8, transporte.Peticiones);
    }

    [Fact]
    public async Task FalloEnSegundaFoto_DetieneCargaEInformaGuardadoParcial()
    {
        using var transporte = new Transporte(fallarEn: 2);
        using var http = new HttpClient(transporte) { BaseAddress = new Uri("https://localhost") };
        var api = new InmobiliariaApi(http);
        IBrowserFile[] fotos = [new ArchivoPrueba(), new ArchivoPrueba(), new ArchivoPrueba()];
        var error = await api.SubirImagenes(7, fotos, "token-prueba");
        Assert.Contains("algunas imágenes pueden haberse guardado", error);
        Assert.Equal(2, transporte.Peticiones);
    }
}
