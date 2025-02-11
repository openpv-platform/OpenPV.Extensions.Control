using Ahsoka.Core;
using Ahsoka.Installer;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ahsoka.Services.Can;

internal class CanServiceMessages : AhsokaMessagesBase
{
    public CanServiceMessages() : base(CanService.Name)
    {
        this.RegisterServiceRequest(CanMessageTypes.Ids.OpenCommunicationChannel, typeof(EmptyNotification), typeof(CanApplicationConfiguration), false, false);
        this.RegisterServiceRequest(CanMessageTypes.Ids.CloseCommunicationChannel, typeof(EmptyNotification), typeof(EmptyNotification), false, false);

        this.RegisterServiceRequest(CanMessageTypes.Ids.SendCanMessages, typeof(CanMessageDataCollection), typeof(CanMessageResult));
        this.RegisterServiceRequest(CanMessageTypes.Ids.SendRecurringCanMessage, typeof(RecurringCanMessage), typeof(CanMessageResult));

        this.RegisterServiceNotification(CanMessageTypes.Ids.CanMessagesReceived, typeof(CanMessageDataCollection));
        this.RegisterServiceNotification(CanMessageTypes.Ids.NetworkStateChanged, typeof(CanState));

        // Add a Filter / Port For Clients
        this.RegisterServiceRequest(CanMessageTypes.Ids.ApplyMessageFilter, typeof(ClientCanFilter), typeof(EmptyNotification));

        // Coprocessor Registration
        this.RegisterServiceNotification(CanMessageTypes.Ids.CanServiceIsReadyNotification, typeof(EmptyNotification));
        this.RegisterServiceNotification(CanMessageTypes.Ids.CoprocessorIsReadyNotification, typeof(EmptyNotification), false, true);
        this.RegisterServiceRequest(CanMessageTypes.Ids.CoprocessorHeartbeat, typeof(EmptyNotification), typeof(EmptyNotification), false, true);
    }

    protected override Dictionary<string, byte[]> OnGetAdditionalClientResources(ApplicationType type, bool includeImplementation = true)
    {
        var result = base.OnGetAdditionalClientResources(type);
        result.Add("inc\\services\\CanServiceClientExtensions.h", Properties.CANResources.CanServiceClientExtensionsH);
        result.Add("inc\\services\\IHasCanData.h", Properties.CANResources.IHasCanData);
        result.Add("inc\\services\\CanServiceClientIncludes.h", Properties.CANResources.CanServiceClientIncludes);

        result.Add("Ahsoka.Proto\\CanConfiguration.proto", Properties.CANResources.CanConfiguration);
        result.Add("Ahsoka.Proto\\CanService.proto", Properties.CANResources.CanService);

        if (includeImplementation)
            result.Add("inc\\services\\CanServiceClientExtensions.cxx", Properties.CANResources.CanServiceClientExtensionsCXX);

        return result;
    }

    /// <InheritDoc/>
    protected override void OnGetParameters(out string group, List<ParameterData> values, PackageInformation info)
    {
        group = CanService.Name;

        string config = info?.ServiceInfo?.RuntimeConfiguration?.ExtensionInfo?.FirstOrDefault(x => x.ExtensionName == "CAN Service Extension").ConfigurationFile;
        if (config != null)
        {
            string configFile = Path.Combine(Path.GetDirectoryName(info.GetPackageInfoPath()), config);
            if (File.Exists(configFile))
            {
                CanClientConfiguration canConfig = ConfigurationFileLoader.LoadFile<CanClientConfiguration>(configFile);
                foreach (var item in canConfig.Messages)
                {
                    foreach (var signal in item.Signals)
                    {
                        values.Add(new()
                        {
                            ObjectName = item.Name,
                            Name = signal.Name,
                            DefaultValue = signal.DefaultValue,
                            MinimumValue = signal.Minimum,
                            MaximumValue = signal.Maximum,
                            Enumerations = signal.Values,
                            ValueType = GetValueTypes(signal)
                        });
                    }
                }
            }
        }
    }

    private ParameterValueTypes GetValueTypes(MessageSignalDefinition signal)
    {
        switch (signal.ValueType)
        {
            case Can.ValueType.Signed:
                return ParameterValueTypes.SignedInteger;
            case Can.ValueType.Unsigned:
                return ParameterValueTypes.UnsignedInteger;
            case Can.ValueType.Float:
                return ParameterValueTypes.Float;
            case Can.ValueType.Double:
                return ParameterValueTypes.Double;
            case Can.ValueType.Enum:
                return ParameterValueTypes.SignedInteger;
            default:
                return ParameterValueTypes.UnsignedInteger;
        }
    }
}

