using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MedicionPQ.Modelos.Medidores;

namespace MedicionPQ.Services.Medidores;

public class JsonStore
{
    private readonly FuenteDatosOptions _fuenteDatos;
    private readonly string _contentRootPath;

    public JsonStore(
        IOptions<FuenteDatosOptions> opciones, 
        IWebHostEnvironment env)
    {
        _fuenteDatos = opciones.Value;
        _contentRootPath = env.ContentRootPath;
    }

}
