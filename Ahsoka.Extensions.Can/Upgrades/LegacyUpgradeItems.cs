using Ahsoka.Services.Can;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;

internal class UpgradeItems_1_255 : UpgradeMethod
{
    public UpgradeItems_1_255() : base(CanMetadataTools.CanConfigurationExtension, "1.254.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        foreach (var item in document["nodes"].AsArray())
        {
            if (item["isSelf"] != null && item["isSelf"].AsValue().ToString() == "true")
                item["nodeType"] = "Self";
        }

        foreach (var item in document["messages"].AsArray())
        {
            item["userDefined"] = "true";
        }
    }
}

internal class UpgradeItemsCAN_2_7 : UpgradeMethod
{
    public UpgradeItemsCAN_2_7() : base(CanMetadataTools.CanConfigurationExtension, "2.8.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        if (document["canPort"] != null)
        {
            string canPort = document["canPort"].GetValue<string>();
            string baudRate = document["baudRate"].GetValue<string>();
            string canInterface = document["canInterface"].GetValue<string>();
            bool promTx = document["promiscuousTransmit"].GetValue<bool>();
            bool promRx = document["promiscuousReceive"].GetValue<bool>();

            var ports = new JsonArray
            {
                new JsonObject()
                {
                    { "port", 1 },
                    { "baudRate", baudRate },
                    { "canInterface", canInterface },
                    { "canInterfacePath", canPort },
                    { "promiscuousTransmit", promTx },
                    { "promiscuousReceive", promRx },
                    { "userDefinded", true },
                }
            };
            document["ports"] = ports;

            foreach (var item in document["nodes"].AsArray())
            {
                item["ports"] = new JsonArray { "1" };
            }

            foreach (var item in document["messages"].AsArray())
            {
                int txNode = item["transmitNodeId"].GetValue<int>();
                int rxNode = item["receiveNodeId"] != null ? item["receiveNodeId"].GetValue<int>() : -1;
                item["transmitNodes"] = new JsonArray { "-1", txNode };
                item["receiveNodes"] = new JsonArray { "-1", rxNode };
            }
        }
    }
}