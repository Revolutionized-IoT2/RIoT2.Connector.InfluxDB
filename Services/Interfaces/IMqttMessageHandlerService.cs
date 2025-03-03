using InfluxDB.Client.Writes;
using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Services.Interfaces
{
    public interface IMqttMessageHandlerService
    {
        PointData HandleReport(Report report);
        PointData HandleCommand(Command command);
    }
}