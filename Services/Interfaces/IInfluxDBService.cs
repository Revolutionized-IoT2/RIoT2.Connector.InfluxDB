using InfluxDB3.Client.Write;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IInfluxDBService
    {
        Task Write(PointData data);
    }
}
