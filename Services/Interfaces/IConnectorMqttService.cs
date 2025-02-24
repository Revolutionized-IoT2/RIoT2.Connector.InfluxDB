namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IConnectorMqttService : IDisposable
    {
        Task Start();
        Task Stop();
    }
}
