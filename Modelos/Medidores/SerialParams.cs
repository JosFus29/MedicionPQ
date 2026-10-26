namespace MedicionPQ.Modelos.Medidores;

public class SerialParams
{
    public string COM { get; set; }
    public int BaudRate { get; set; }
    public int Paridad { get; set; }
    public int BitsDatos { get; set; }
    public int BitsParo { get; set; }
    public int Timeout { get; set; }
}
