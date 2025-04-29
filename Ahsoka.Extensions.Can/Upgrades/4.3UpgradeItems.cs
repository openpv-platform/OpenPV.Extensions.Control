using Ahsoka.Services.Can;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Ahsoka.Core.Utility;

public class UpgradeItemsPackage_4_3 : UpgradeMethod
{
    public UpgradeItemsPackage_4_3() : base(CanMetadataTools.CanConfigurationExtension, "4.4") { }

    public override void UpgradeData(JsonNode document, Dictionary<string, object> context)
    {
        foreach (var message in document["messages"].AsArray())
        {
            if (message["mask"] == null)
                message["mask"] = 0x1FFFFFFF; //29 bit mask
        }
    }
}