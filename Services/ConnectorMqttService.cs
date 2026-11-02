using InfluxDB.Client.Writes;
using RIoT2.Connector.InfluxDB.Services.Interfaces;
using RIoT2.Core;
using RIoT2.Core.Models;
using RIoT2.Core.Utils;

namespace RIoT2.Connector.InfluxDB.Services
{
    public class ConnectorMqttService : IConnectorMqttService
    {
        private MqttClient _client;
        private readonly IConnectorConfigurationService _connector;
        private readonly ILogger<ConnectorMqttService> _logger;
        private readonly IMqttMessageHandlerService _mqttMessageHandler;
        private readonly IInfluxDBService _influxDB;
        private readonly ITemplateService _templates;

        private string _reportTopic;
        private string _commandTopic;
        private string _onlineTopic;
        private string _configurationTopic;
        private readonly string _orchestratorOnlineTopic;

        public ConnectorMqttService(ILogger<ConnectorMqttService> logger,
            IMqttMessageHandlerService mqttMessageHandlerService,
            IInfluxDBService influxDBService,
            ITemplateService templateService,
            IConnectorConfigurationService configuration) 
        {
            _logger = logger;
            _mqttMessageHandler = mqttMessageHandlerService;
            _influxDB = influxDBService;
            _connector = configuration;
            _templates = templateService;

            _reportTopic = Constants.Get("+", MqttTopic.Report);
            _commandTopic = Constants.Get("+", MqttTopic.Command);
            _onlineTopic = Constants.Get(_connector.Configuration.Mqtt.ClientId, MqttTopic.NodeOnline);
            _configurationTopic = Constants.Get(_connector.Configuration.Mqtt.ClientId, MqttTopic.Configuration);
            _orchestratorOnlineTopic = Constants.Get("+", MqttTopic.OrchestratorOnline);

            _client = new MqttClient(_connector.Configuration.Mqtt, logger);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public async Task Start()
        {
            try
            {
                _client.MessageReceivedAsync += HandleMessageReceivedAsync;
                _client.ConnectedAsync += SendNodeOnlineMessage;
                await _client.Start(_reportTopic, _commandTopic, _configurationTopic, _orchestratorOnlineTopic);
                _logger.LogInformation("Connected to MQTT and listening messages");
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Error connecting MQTT Broker");
                throw new Exception("Could not connect to MQTT Broker", x);
            }
        }

        public async Task SendNodeOnlineMessage()
        {
            await _client.PublishAsync(_onlineTopic, Json.SerializeIgnoreNulls(new NodeOnlineMessage() {
                IsOnline = true,
                Name = "InfluxDBConnector"
            }));
            _logger.LogInformation("Node Online message sent");
        }

        public async Task Stop()
        {
            _client.MessageReceivedAsync -= HandleMessageReceivedAsync;
            _client.ConnectedAsync -= SendNodeOnlineMessage;
            await _client.Stop();
        }

        private async Task HandleMessageReceivedAsync(MqttEventArgs mqttEventArgs)
        {
            try
            {
                if (MqttClient.IsMatch(mqttEventArgs.Topic, _orchestratorOnlineTopic))
                    await SendNodeOnlineMessage();
                if (MqttClient.IsMatch(mqttEventArgs.Topic, _configurationTopic))
                {
                    try
                    {
                        var configurationCommand = Json.Deserialize<ConfigurationCommand>(mqttEventArgs.Message);
                        await _templates.LoadAsync(configurationCommand!.ApiBaseUrl);
                    }
                    catch (Exception x) 
                    {
                        _logger.LogError(x, "Error loading templates");
                        return;
                    }
                }

                //save reports to influxDB
                if (MqttClient.IsMatch(mqttEventArgs.Topic, _reportTopic))
                {
                    var report = Report.Create(mqttEventArgs.Message);
                    if (report == null)
                    {
                        ConnectorMetrics.Message("report", ConnectorMetrics.Invalid);
                        _logger.LogWarning("Couldn't create Report {mqttEventArgs.Message}", mqttEventArgs.Message);
                        return;
                    }
                    Queue("report", () => _mqttMessageHandler.HandleReport(report));
                }

                //save conmmands to influxDB
                if (_connector.Configuration.HandleCommands && MqttClient.IsMatch(mqttEventArgs.Topic, _commandTopic))
                {
                    var command = Command.Create(mqttEventArgs.Message);
                    if (command == null)
                    {
                        ConnectorMetrics.Message("command", ConnectorMetrics.Invalid);
                        _logger.LogWarning("Couldn't create Command {mqttEventArgs.Message}", mqttEventArgs.Message);
                        return;
                    }
                    Queue("command", () => _mqttMessageHandler.HandleCommand(command));
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Could not process mqtt message {mqttEventArgs.Message}", mqttEventArgs.Message);
            }
        }

        // Converts a message to a point and queues it for InfluxDB, counting the result.
        private void Queue(string type, Func<PointData?> convert)
        {
            try
            {
                var point = convert();
                if (point == null)
                {
                    ConnectorMetrics.Message(type, ConnectorMetrics.Ignored);
                    return;
                }
                _influxDB.Write(point);
                ConnectorMetrics.Message(type, ConnectorMetrics.Queued);
            }
            catch (Exception)
            {
                ConnectorMetrics.Message(type, ConnectorMetrics.Failed);
                throw;
            }
        }
    }
}
