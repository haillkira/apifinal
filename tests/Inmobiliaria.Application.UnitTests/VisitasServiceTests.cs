using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Contracts;
using Moq;
namespace Inmobiliaria.Application.UnitTests;
public class VisitasServiceTests {
 [Fact] public async Task FechaPasada_NoPersiste() {
 var e=new Escenario();var repo=new Mock<IVisitasRepository>();var servicio=new VisitasService(repo.Object,e.Acceso);
 Assert.Equal("DATOS_INVALIDOS",await servicio.Cambiar(e.Principal,1,new(DateTimeOffset.UtcNow.AddDays(-1),null),null,false));repo.VerifyNoOtherCalls();
 }
 [Fact] public async Task NotasLargas_NoPersiste() {
 var e=new Escenario();var repo=new Mock<IVisitasRepository>();var servicio=new VisitasService(repo.Object,e.Acceso);
 Assert.Equal("DATOS_INVALIDOS",await servicio.Cambiar(e.Principal,1,new(DateTimeOffset.UtcNow.AddDays(1),new string('x',1001)),null,false));repo.VerifyNoOtherCalls();
 }
 [Fact] public async Task Administrador_NoPuedeConsultarVisitas() {
 var e=new Escenario("Administrador");var repo=new Mock<IVisitasRepository>();
 await Assert.ThrowsAsync<AccesoDenegadoException>(() => new VisitasService(repo.Object,e.Acceso).Listar(e.Principal,1));repo.VerifyNoOtherCalls();
 }
}
