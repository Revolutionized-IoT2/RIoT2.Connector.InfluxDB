using InfluxDB3.Client;
using InfluxDB3.Client.Write;
using RIoT2.Connector.InfluxDB.Services.Interfaces;

namespace RIoT2.Connector.InfluxDB.Services
{

    // https://www.influxdata.com/blog/getting-started-with-c-and-influxdb/
    // https://grafana.com/docs/grafana/latest/getting-started/get-started-grafana-influxdb/
    
    public class InfluxDBService : IInfluxDBService
    {
        private readonly InfluxDBClient _client;

        public InfluxDBService(IConnectorConfigurationService configuration)
        {
            _client = new InfluxDBClient(configuration.Configuration.InfluxHost,
                configuration.Configuration.InfluxToken,
                configuration.Configuration.InfluxDatabase);
        }

        public async Task Write(PointData data)
        {
            await _client.WritePointAsync(data);
        }
    }
}