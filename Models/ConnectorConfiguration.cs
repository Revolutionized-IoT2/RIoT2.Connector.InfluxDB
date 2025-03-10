﻿using RIoT2.Core.Models;

namespace RIoT2.Connector.InfluxDB.Models
{
    public class ConnectorConfiguration
    {
        public bool HandleCommands { get; set; }
        public string InfluxHost { get; set; }

        public string InfluxToken { get; set; }

        public string InfluxBucket { get; set; }
        public string InfluxOrganization { get; set; }

        public MqttConfiguration Mqtt { get; set; }
    }
}
