namespace MedicionPQ.Modelos.Medidores;

public class EthernetParams
{
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public int Timeout { get; set; }
}