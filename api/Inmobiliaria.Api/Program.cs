using System.Text;
using System.Security.Claims;
using Inmobiliaria.Contracts;
using Inmobiliaria.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Inmobiliaria.Api.Seguridad;
using Inmobiliaria.Application.Persistencia;
using Inmobiliaria.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<AccesoService>();
builder.Services.AddScoped<CuentasService>();
builder.Services.AddScoped<PublicacionesService>();
builder.Services.AddScoped<SolicitudesService>();
builder.Services.AddScoped<VisitasService>();
builder.Services.AddScoped<ArchivoService>();
builder.Services.AddScoped<IClavesCuenta, ClavesIdentity>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext?.User
    ?? new ClaimsPrincipal());
builder.Services.AddScoped<Aplicada1.Core.IServiceResult<PropiedadDto, int>, PropiedadesService>();
var cs=builder.Configuration.GetConnectionString("InmobiliariaConnection");
builder.Services.AddInmobiliariaData(cs ?? "");
var key=builder.Configuration["Jwt:Key"];
if(!builder.Environment.IsDevelopment() && (string.IsNullOrWhiteSpace(cs) || string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key)<32))throw new InvalidOperationException("Configura conexion y clave JWT de produccion.");
// Clave efimera local: permite arrancar sin credenciales; login exige clave configurada.
var validationKey=string.IsNullOrWhiteSpace(key)?Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48)):key;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new TokenValidationParameters{ValidateIssuer=true,ValidateAudience=true,ValidateLifetime=true,ValidateIssuerSigningKey=true,ValidIssuer=builder.Configuration["Jwt:Issuer"],ValidAudience=builder.Configuration["Jwt:Audience"],IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(validationKey)),ClockSkew=TimeSpan.FromSeconds(30)});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=60,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
builder.Services.AddOpenApi();
var app=builder.Build();
if (app.Environment.IsDevelopment())
{
    app.Logger.LogInformation("Contexto EF Core activo: {Contexto}", Inmobiliaria.Data.DependencyInjection.NombreContexto);
    app.MapOpenApi();
    app.MapScalarApiReference();
}
if (args.Contains("--bootstrap-admin"))
{
    if(string.IsNullOrWhiteSpace(cs))throw new InvalidOperationException("Configura InmobiliariaConnection.");
    using var scope = app.Services.CreateScope();
    var cuentas = scope.ServiceProvider.GetRequiredService<CuentasService>();
    var email = builder.Configuration["Bootstrap:Email"]?.Trim();
    var password = builder.Configuration["Bootstrap:Password"];
    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 16)
        throw new InvalidOperationException("Configura Bootstrap:Email y Password (16 caracteres minimo) mediante secretos.");

    if (!await cuentas.CrearPrimerAdministrador(email, password))
        throw new InvalidOperationException("Ya existe administrador; bootstrap bloqueado.");
    return;
}
app.Use(async(ctx,next)=>
{
    try
    {
        if(ctx.Request.Path.StartsWithSegments("/api") && string.IsNullOrWhiteSpace(cs)){await Results.Problem(statusCode:503,title:"Configura ConnectionStrings:InmobiliariaConnection para usar Azure SQL.").ExecuteAsync(ctx);return;}
        await next();
    }
    catch(Exception e) when(!ctx.Response.HasStarted)
    {
        var status=e switch{ArgumentException=>400,UnauthorizedAccessException=>401,AccesoDenegadoException=>403,DbUpdateException=>409,_=>500};
        app.Logger.LogWarning("Peticion fallida. Tipo: {Tipo}",e.GetType().Name);
        await Results.Problem(statusCode:status,title:status switch{400=>e.Message,401=>"Sesion no valida o cuenta no activa.",403=>"No tienes permiso o falta aprobacion.",409=>"Conflicto de datos. Revisa la operacion.",_=>"Error interno; revisa la configuracion y los registros."}).ExecuteAsync(ctx);
    }
});
if(!app.Environment.IsDevelopment())app.UseHttpsRedirection();
app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.MapGet("/",()=>Results.Ok(new{proyecto="Inmobiliaria",equipo="Luilton y Yerferson",alcance="Solo alquiler",estado="Base inicial; consulta README"}));
app.MapGet("/health",()=>Results.Ok(new{status="ok",databaseConfigured=!string.IsNullOrWhiteSpace(cs),databaseChecked=false}));
app.MapControllers();app.Run();

public partial class Program { }
