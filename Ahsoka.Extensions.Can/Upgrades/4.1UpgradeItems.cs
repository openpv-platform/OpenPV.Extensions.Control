using Ahsoka.Services.Can;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;



public class UpgradeItemsPackage_4_1 : UpgradeMethod
{
    public UpgradeItemsPackage_4_1() : base(CanMetadataTools.CanConfigurationExtension, "4.2.0") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        foreach (var message in document["messages"].AsArray())
        {
            foreach (var signal in message["signals"].AsArray())
            {
                if (!signal["byteOrder"].AsValue().ToString().Contains("Order"))
                    signal["byteOrder"] = $"Order{signal["byteOrder"]}";
            }
        }
    }
}

