using InfluxDB.Client.Writes;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IInfluxDBService
    {
        void Write(PointData data);
    }
}
