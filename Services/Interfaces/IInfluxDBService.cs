using InfluxDB.Client.Writes;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IInfluxDBService
    {
        /// <summary>Accepts a point into the bounded, volatile write queue; this is not a delivery acknowledgement.</summary>
        /// <exception cref="InvalidOperationException">The writer is not running or its queue is full.</exception>
        void Write(PointData data);
    }
}
