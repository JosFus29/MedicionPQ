using System.Text;
using MedicionPQ.Data;
using MedicionPQ.Middleware;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

// Punto de entrada: configura los servicios y el flujo HTTP de la API.
var builder = WebApplication.CreateBuilder(args);

// Configura Serilog como el sistema de logs de la API.
// Escribe cada entrada en la consola y en un archivo .txt por día dentro de la carpeta Logs (se guardan 30 días).
// Formato: fecha y hora, nivel (INF, WRN, ERR, FTL) y mensaje; si hay una excepción, su detalle en las líneas siguientes.
const string formatoLog = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
builder.Host.UseSerilog((context, configuracion) => configuracion
    .MinimumLevel.Information()
    // Los mensajes internos de ASP.NET Core y EF Core solo se registran si son advertencias o errores.
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    // Muestra la dirección donde escucha la API ("Now listening on...") al iniciar.
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    // Las excepciones no controladas ya las registra RegistroSolicitudesMiddleware; esto evita duplicarlas.
    .MinimumLevel.Override("Microsoft.AspNetCore.Diagnostics", LogEventLevel.Fatal)
    .WriteTo.Console(outputTemplate: formatoLog)
    .WriteTo.File(
        Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: formatoLog));

// Aísla las llaves de desarrollo en el proyecto para no reutilizar un key ring DPAPI incompatible.
if (builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
{
    var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, ".dataprotection-keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// Registra los controladores que publican los endpoints REST.
builder.Services.AddControllers();

// Publica OpenAPI y Swagger UI en desarrollo, con esquemas JWT y comentarios XML.
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "MedicionPQ.xml"), includeControllerXmlComments: true);
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MedicionPQ API",
        Version = "v1",
        Description = "API de autenticación y administración de usuarios."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega el JWT devuelto por /api/Auth/login. Swagger agrega el prefijo Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Permite saber, fuera de los controladores, qué usuario hizo la solicitud actual (lo usa la auditoría).
builder.Services.AddHttpContextAccessor();
// Registra en el log cada alta, modificación y eliminación de datos con sus valores anteriores y nuevos.
builder.Services.AddScoped<AuditoriaInterceptor>();

// Configura Entity Framework Core para usar SQL Server con la cadena DefaultConnection.
// El interceptor de auditoría se ejecuta en cada SaveChangesAsync() de cualquier servicio.
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(serviceProvider.GetRequiredService<AuditoriaInterceptor>()));

// Registra servicios de negocio con ciclo de vida por solicitud HTTP.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
// Registra las operaciones de consulta, alta y actualización de controladores RF.
builder.Services.AddScoped<IControladorRFService, ControladorRFService>();
// Servicio para validar la relación con ControladorRF y guardar medidores QP.
builder.Services.AddScoped<IMedidorQPService, MedidorQPService>();
// Servicio para consultar, agregar, editar y eliminar eventos configurables.
builder.Services.AddScoped<IEventosConfigService, EventosConfigService>();
// Servicio para consultar y administrar el catálogo de unidades.
builder.Services.AddScoped<IUnidadService, UnidadService>();
// Servicio para consultar y administrar los registros de eventos.
builder.Services.AddScoped<IEventosRegService, EventosRegService>();
// Servicios para consultar y administrar ambos catálogos IO.
builder.Services.AddScoped<IIOGrupoElectricoService, IOGrupoElectricoService>();
builder.Services.AddScoped<IIOGrupoFuncionalService, IOGrupoFuncionalService>();
// Servicio para consultar puntos IO y revalidar la contraseña en cada edición administrativa.
builder.Services.AddScoped<IPuntosIOService, PuntosIOService>();
// Servicio de consulta de vistas de medición y edición de lecturas con revalidación administrativa.
builder.Services.AddScoped<IMedicion5MService, Medicion5MService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
// Servicio para leer los archivos de log desde el endpoint /api/Logs.
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<TokenService>();
// Registra el servicio de variables eléctricas (Vars5M) para inyección de dependencias.
builder.Services.AddScoped<IVars5MService, Vars5MService>();

// Comprueba la configuración JWT antes de iniciar para evitar emitir tokens con datos incompletos.
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Configura Jwt:Key con una clave secreta de al menos 32 bytes mediante User Secrets o una variable de entorno.");
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException("Configura Jwt:Issuer y Jwt:Audience antes de iniciar la API.");

// Valida firma, emisor, audiencia y vigencia de cada token Bearer entrante.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Habilita la aplicación de atributos [Authorize] en controladores y acciones.
builder.Services.AddAuthorization();

// Limita CORS a los orígenes del navegador configurados para el frontend.
builder.Services.AddEndpointsApiExplorer();
var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (frontendOrigins.Length > 0)
        policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod();
}));

// Añade el documento OpenAPI nativo de ASP.NET Core.
builder.Services.AddOpenApi();

var app = builder.Build();

// En desarrollo habilita la interfaz de Swagger y el documento OpenAPI.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirige HTTP a HTTPS y procesa CORS antes de autenticación y autorización.
app.UseHttpsRedirection();
app.UseCors("Frontend");

// Primero identifica al solicitante desde su token; luego comprueba sus permisos.
// El registro de solicitudes va entre ambos: ya sabe quién es el usuario y alcanza a ver
// las respuestas 401/403 que genera la autorización.
app.UseAuthentication();
app.UseMiddleware<RegistroSolicitudesMiddleware>();
app.UseAuthorization();

// Asocia las rutas declaradas en los controladores y comienza a escuchar solicitudes.
app.MapControllers();
app.Run();
