using InfluxDB.Client;
using InfluxDB.Client.Writes;
using RIoT2.Connector.InfluxDB.Services.Interfaces;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class InfluxDBService : IInfluxDBService
    {
        private readonly InfluxDBClient _client;
        private readonly string _bucket;
        private readonly string _org;

        public InfluxDBService(IConnectorConfigurationService configuration)
        {
            _client = new InfluxDBClient(configuration.Configuration.InfluxHost, configuration.Configuration.InfluxToken);
            _bucket = configuration.Configuration.InfluxBucket;
            _org = configuration.Configuration.InfluxOrganization;
        }

        private void write(Action<WriteApi> action)
        {
            using var write = _client.GetWriteApi();
            action(write);
        }

        public void Write(PointData data)
        {
            write(write =>
            {
                write.WritePoint(data, _bucket, _org);
            });
        }
    }
}