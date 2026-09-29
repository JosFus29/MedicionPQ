using System.Text;
using MedicionPQ.Data;
using MedicionPQ.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

// Punto de entrada: configura los servicios y el flujo HTTP de la API.
var builder = WebApplication.CreateBuilder(args);

// En desarrollo, recupera User Secrets si la configuración automática no proporcionó
// una clave JWT válida (por ejemplo, si una variable de entorno vacía la sustituyó).
var configuredJwtKey = builder.Configuration["Jwt:Key"];
if (builder.Environment.IsDevelopment()
    && (string.IsNullOrWhiteSpace(configuredJwtKey) || Encoding.UTF8.GetByteCount(configuredJwtKey) < 32))
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
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

// Configura Entity Framework Core para usar SQL Server con la cadena DefaultConnection.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registra servicios de negocio con ciclo de vida por solicitud HTTP.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IMedidorService, MedidorService>();

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
app.UseAuthentication();
app.UseAuthorization();

// Asocia las rutas declaradas en los controladores y comienza a escuchar solicitudes.
app.MapControllers();
app.Run();
