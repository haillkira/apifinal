using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Inmobiliaria.Contracts;

namespace Inmobiliaria.Admin;

public partial class Form1 : Form
{
    private HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly TextBox direccion = new() { Width=260, Text=Environment.GetEnvironmentVariable("INMOBILIARIA_API_URL")??"http://localhost:5180" };
    private readonly TextBox correo = new() { Width=220, PlaceholderText="Correo del administrador" };
    private readonly TextBox clave = new() { Width=180, UseSystemPasswordChar=true, PlaceholderText="Contraseña" };
    private readonly Button entrar = new() { Text="Iniciar sesión", AutoSize=true };
    private readonly Button salir = new() { Text="Cerrar sesión", AutoSize=true, Enabled=false };
    private readonly Label estado = new() { AutoSize=true, Text="Inicia sesión con un administrador autorizado.", Padding=new Padding(12) };
    private readonly DataGridView vendedores = CrearGrid();
    private readonly DataGridView reportes = CrearGrid();
    private readonly Button actualizar = new() { Text="Actualizar pendientes", AutoSize=true };
    private readonly Button aprobar = new() { Text="Aprobar seleccionado", AutoSize=true };
    private readonly Button rechazar = new() { Text="Rechazar seleccionado", AutoSize=true };
    private readonly Button verReportes = new() { Text="Actualizar reportes", AutoSize=true };
    private readonly TextBox motivo = new() { Width=360, MaxLength=1000, PlaceholderText="Motivo obligatorio al rechazar" };
    private readonly TabControl tabs = new() { Dock=DockStyle.Fill, Enabled=false };
    private bool conectado;
    private sealed record Vendedor(int IdVendedor,int IdUsuario,DateTime FechaSolicitud,string Correo,string Nombre);

    public Form1()
    {
        InitializeComponent();
        Text="Inmobiliaria · Administración"; Size=new Size(1250,780); MinimumSize=new Size(850,600); StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Segoe UI",10); BackColor=Color.FromArgb(248,248,242);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=4, ColumnCount=1, Padding=new Padding(20) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,115));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        layout.Controls.Add(new Label {Text="Administración de Inmobiliaria",Font=new Font("Segoe UI",20,FontStyle.Bold),AutoSize=true,ForeColor=Color.FromArgb(32,61,53)},0,0);
        var login=new FlowLayoutPanel {Dock=DockStyle.Fill,WrapContents=true,AutoScroll=true};
        login.Controls.AddRange([new Label {Text="API",AutoSize=true,Padding=new Padding(0,6,0,0)},direccion,correo,clave,entrar,salir]);layout.Controls.Add(login,0,1);layout.Controls.Add(estado,0,2);layout.Controls.Add(tabs,0,3);Controls.Add(layout);
        var pendientes=new TabPage("Vendedores pendientes");var panel=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};panel.RowStyles.Add(new RowStyle(SizeType.Absolute,85));panel.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var acciones=new FlowLayoutPanel {Dock=DockStyle.Fill,AutoScroll=true};acciones.Controls.AddRange([actualizar,aprobar,rechazar,motivo]);panel.Controls.Add(acciones,0,0);panel.Controls.Add(vendedores,0,1);pendientes.Controls.Add(panel);tabs.TabPages.Add(pendientes);
        var paginaReportes=new TabPage("Reportes (consulta)");var reportLayout=new TableLayoutPanel {Dock=DockStyle.Fill,RowCount=2,ColumnCount=1};reportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));reportLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));reportLayout.Controls.Add(verReportes,0,0);reportLayout.Controls.Add(reportes,0,1);paginaReportes.Controls.Add(reportLayout);tabs.TabPages.Add(paginaReportes);
        entrar.Click+=async(_,_)=>await Ejecutar(Login);
        salir.Click+=(_,_)=>Cerrar();
        actualizar.Click+=async(_,_)=>await Ejecutar(Cargar);
        aprobar.Click+=async(_,_)=>await Ejecutar(()=>Decidir(true));
        rechazar.Click+=async(_,_)=>await Ejecutar(()=>Decidir(false));
        verReportes.Click+=async(_,_)=>await Ejecutar(async()=>{reportes.DataSource=await Get<List<ReporteAdminDto>>("api/v1/admin/reportes");estado.Text="Reportes actualizados. Selecciona una fila para consultar sus datos.";});
        FormClosed+=(_,_)=>http.Dispose();AcceptButton=entrar;
    }
    private static DataGridView CrearGrid()=>new() {Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,BackgroundColor=Color.White,RowHeadersVisible=false,AutoGenerateColumns=true};
    private async Task Ejecutar(Func<Task> accion)
    {
        UseWaitCursor=true;entrar.Enabled=false;tabs.Enabled=false;salir.Enabled=false;
        try{await accion();}
        catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException){estado.Text=ex.Message;MessageBox.Show(this,ex is TaskCanceledException?"La API tardó demasiado. Actualiza antes de reintentar.":ex.Message,"Operación no completada",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        finally{UseWaitCursor=false;entrar.Enabled = !conectado;tabs.Enabled=conectado;salir.Enabled=conectado;}
    }
    private async Task Validar(HttpResponseMessage response)
    {
        if(response.IsSuccessStatusCode)return;
        if(response.StatusCode==HttpStatusCode.Unauthorized){Cerrar();throw new InvalidOperationException("Sesión no válida o credenciales incorrectas.");}
        if(response.StatusCode==HttpStatusCode.Forbidden)throw new InvalidOperationException("La cuenta no tiene permisos administrativos suficientes.");
        string mensaje="Error de API: "+(int)response.StatusCode;
        try{var json=await response.Content.ReadFromJsonAsync<JsonElement>();if(json.TryGetProperty("mensaje",out var texto)||json.TryGetProperty("title",out texto))mensaje=texto.GetString()??mensaje;}catch(JsonException){}
        throw new InvalidOperationException(mensaje);
    }
    private async Task<T?> Get<T>(string ruta){using var response=await http.GetAsync(ruta);await Validar(response);return await response.Content.ReadFromJsonAsync<T>();}
    private async Task Login()
    {
        if(!Uri.TryCreate(direccion.Text.Trim().TrimEnd('/')+"/",UriKind.Absolute,out var uri)|| (uri.Scheme!="https" && !(uri.Scheme=="http"&&uri.IsLoopback)))throw new InvalidOperationException("Usa HTTPS para una API remota o HTTP únicamente para localhost.");
        http.Dispose();http=new HttpClient {Timeout=TimeSpan.FromSeconds(30),BaseAddress=uri};
        var credenciales=new LoginRequest(correo.Text.Trim(),clave.Text);clave.Clear();
        using var response=await http.PostAsJsonAsync("api/v1/auth/login",credenciales);await Validar(response);
        var login=await response.Content.ReadFromJsonAsync<LoginResponse>()??throw new InvalidOperationException("Respuesta de login vacía.");
        if(login.Usuario.Rol!="Administrador")throw new InvalidOperationException("Se requiere una cuenta de administrador.");
        http.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",login.AccessToken);
        try{await Cargar();conectado=true;direccion.Enabled=false;correo.Enabled=false;clave.Enabled=false;estado.Text="Sesión administrativa iniciada. "+login.Usuario.Nombre;}
        catch{http.DefaultRequestHeaders.Authorization=null;throw;}
    }
    private async Task Cargar(){vendedores.DataSource=await Get<List<Vendedor>>("api/v1/admin/vendedores/pendientes");estado.Text="Pendientes actualizados. Selecciona una cuenta antes de aprobar o rechazar.";}
    private async Task Decidir(bool aceptar)
    {
        if(vendedores.CurrentRow?.DataBoundItem is not Vendedor seleccionado)throw new InvalidOperationException("Selecciona un vendedor pendiente.");
        if(!aceptar&&string.IsNullOrWhiteSpace(motivo.Text))throw new InvalidOperationException("Escribe el motivo del rechazo.");
        if(MessageBox.Show(this,$"¿{(aceptar?"Aprobar":"Rechazar")} a {seleccionado.Nombre} ({seleccionado.Correo})?","Confirmar decisión",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        using var response=await http.PostAsJsonAsync($"api/v1/admin/vendedores/{seleccionado.IdVendedor}/{(aceptar?"aprobar":"rechazar")}",new DecisionRequest(aceptar?null:motivo.Text.Trim()));await Validar(response);motivo.Clear();await Cargar();estado.Text="Decisión guardada con historial y notificación.";
    }
    private void Cerrar(){conectado=false;http.DefaultRequestHeaders.Authorization=null;clave.Clear();vendedores.DataSource=null;reportes.DataSource=null;tabs.Enabled=false;entrar.Enabled=true;salir.Enabled=false;direccion.Enabled=true;correo.Enabled=true;clave.Enabled=true;estado.Text="Sesión cerrada.";}
}
