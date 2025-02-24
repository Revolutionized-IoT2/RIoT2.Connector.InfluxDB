using RIoT2.Connector.InfluxDB.Services.Interfaces;

namespace RIoT2.Connector.InfluxDB.Services
{
    internal class MqttBackgroundService : IHostedService, IDisposable
    {
        private readonly IConnectorMqttService _mqttService;
        public MqttBackgroundService(IConnectorMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        public void Dispose()
        {
            _mqttService.Dispose();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _mqttService.Start();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _mqttService.Stop();
        }
    }
}
