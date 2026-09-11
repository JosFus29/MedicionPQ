using Microsoft.EntityFrameworkCore;
using MedicionPQ.Data;

var builder = WebApplication.CreateBuilder(args);

// Añadir servicios de controladores
builder.Services.AddControllers();

// conectar Entity Framework con la base de datos
builder.Services.AddDbContext<MedicionPQ.Data.AppDBContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



// Configuración de autenticación con JWT
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        //Agregaremos parámetros para la validación del token
    });

// Configuración para Swagger y OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var app = builder.Build();

// Configurar el pipeline HTTP (Middlewares)
if (app.Environment.IsDevelopment())
{
    // todo lo que es desarrollo en un solo bloque
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
