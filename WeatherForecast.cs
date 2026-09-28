namespace MedicionPQ
{
    /// <summary>Modelo de ejemplo generado por la plantilla inicial de ASP.NET Core.</summary>
    public class WeatherForecast
    {
        /// <summary>Fecha a la que corresponde el pronóstico.</summary>
        public DateOnly Date { get; set; }

        /// <summary>Temperatura estimada en grados Celsius.</summary>
        public int TemperatureC { get; set; }

        /// <summary>Temperatura Celsius convertida a grados Fahrenheit.</summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>Descripción opcional de las condiciones estimadas.</summary>
        public string? Summary { get; set; }
    }
}
