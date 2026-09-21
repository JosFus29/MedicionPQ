using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MedicionPQ.Data;
using MedicionPQ.Services;
using Microsoft.OpenApi;

// Inicializa el constructor de la aplicación web
var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURACIÓN DE SERVICIOS (Contenedor de dependencias)
// ============================================================

// Añadir servicios de controladores (Habilita el uso de controladores para la API)
builder.Services.AddControllers();

// Conectar Entity Framework con la base de datos
// Lee la cadena de conexión "DefaultConnection" desde el archivo appsettings.json
builder.Services.AddDbContext<MedicionPQ.Data.AppDBContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configuración de autenticación con JWT
// Establece "Bearer" como el esquema de autenticación por defecto
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Reglas para validar que el token que envía el cliente sea legítimo
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                  // Valida quién emitió el token
            ValidateAudience = true,                // Valida para quién fue emitido
            ValidateLifetime = true,                // Valida que el token no esté vencido
            ValidateIssuerSigningKey = true,        // Valida que la firma criptográfica coincida

            ValidIssuer = builder.Configuration["Jwt:Issuer"],     // Lee el emisor esperado de la configuración
            ValidAudience = builder.Configuration["Jwt:Audience"], // Lee el receptor esperado de la configuración
            IssuerSigningKey = new SymmetricSecurityKey(           // Clave secreta para desencriptar/validar la firma
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

            ClockSkew = TimeSpan.Zero // Sin tolerancia extra al expirar (expira en el segundo exacto)
        };
    });

// Habilita el servicio de autorización (para usar [Authorize] en los endpoints)
builder.Services.AddAuthorization();

// Registra el TokenService para inyectarlo en los controladores (se crea una instancia por cada petición HTTP)
builder.Services.AddScoped<TokenService>();


// ============================================================
// 2. CONFIGURACIÓN DE SWAGGER (Documentación de la API)
// ============================================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Información general que aparecerá en la página de Swagger
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MedicionPQ", Version = "v1" });

    // Crea el botón "Authorize" en Swagger y le enseña cómo enviar el token en la cabecera
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa tu token JWT (sin la palabra 'Bearer', Swagger la agrega solo)"
    });

    // Aplica la seguridad globalmente para que las peticiones desde Swagger incluyan el candado/token
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// Soporte nativo para OpenAPI en versiones recientes de .NET
builder.Services.AddOpenApi();

// Construye la aplicación con todos los servicios registrados arriba
var app = builder.Build();


// ============================================================
// 3. PIPELINE HTTP (Middlewares) - El orden aquí es importante
// ============================================================

// Configurar el pipeline HTTP (Middlewares)
if (app.Environment.IsDevelopment())
{
    // Todo lo que es desarrollo en un solo bloque (Habilita la interfaz gráfica de Swagger)
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirige el tráfico HTTP hacia HTTPS por seguridad
app.UseHttpsRedirection();

// 1ro: Lee el token para saber QUIÉN es el usuario
app.UseAuthentication();

// 2do: Verifica si el usuario tiene PERMISO para acceder a la ruta
app.UseAuthorization();

// 3ro: Dirige la petición al controlador correspondiente
app.MapControllers();

// Arranca el servidor
app.Run();