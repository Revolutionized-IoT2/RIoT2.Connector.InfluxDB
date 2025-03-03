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

            _client = new MqttClient(_connector.Configuration.Mqtt.ClientId,
                _connector.Configuration.Mqtt.ServerUrl,
                _connector.Configuration.Mqtt.Username,
                _connector.Configuration.Mqtt.Password);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        public async Task Start()
        {
            try
            {
                await _client.Start(_reportTopic, _commandTopic, _configurationTopic);
                _client.MessageReceived += client_MessageReceived;
                _logger.LogInformation("Connected to MQTT and listening messages");
                await SendNodeOnlineMessage();
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Error connecting MQTT Broker");
                throw new Exception("Could not connect to MQTT Broker", x);
            }
        }

        public async Task SendNodeOnlineMessage()
        {
            await _client.Publish(_onlineTopic, Json.SerializeIgnoreNulls(new NodeOnlineMessage() {
                IsOnline = true,
                Name = "InfluxDBConnector"
            }));
            _logger.LogInformation("Node Online message sent");
        }

        public async Task Stop()
        {
            await _client.Stop();
        }

        private void client_MessageReceived(MqttEventArgs mqttEventArgs)
        {
            try
            {
                if (MqttClient.IsMatch(mqttEventArgs.Topic, _configurationTopic))
                {
                    try
                    {
                        var configurationCommand = Json.Deserialize<ConfigurationCommand>(mqttEventArgs.Message);
                        _templates.Load(configurationCommand.ApiBaseUrl);
                    }
                    catch (Exception x) 
                    {
                        _logger.LogError(x, "Error loading templates");
                        throw new Exception("Error loading templates", x);
                    }
                }

                //save reports to influxDB
                if (MqttClient.IsMatch(mqttEventArgs.Topic, _reportTopic))
                {
                    var report = Report.Create(mqttEventArgs.Message);
                    if (report == null)
                    {
                        _logger.LogWarning("Couldn't create Report {mqttEventArgs.Message}", mqttEventArgs.Message);
                        return;
                    }
                    var point = _mqttMessageHandler.HandleReport(report);
                    if (point != null)
                        _influxDB.Write(point);
                }

                //save conmmands to influxDB
                if (_connector.Configuration.HandleCommands && MqttClient.IsMatch(mqttEventArgs.Topic, _commandTopic))
                {
                    var command = Command.Create(mqttEventArgs.Message);
                    if (command == null)
                    {
                        _logger.LogWarning("Couldn't create Command {mqttEventArgs.Message}", mqttEventArgs.Message);
                        return;
                    }
                    var point = _mqttMessageHandler.HandleCommand(command);
                    if (point != null) 
                        _influxDB.Write(point);
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Could not process mqtt message {mqttEventArgs.Message}", mqttEventArgs.Message);
            }
        }
    }
}
