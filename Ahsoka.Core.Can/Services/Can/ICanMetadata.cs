using Ahsoka.System;

namespace Ahsoka.Services.Can;

/// <summary>
/// Type Definitions for CAN Metadata
/// </summary>
public interface ICanMetadata : IMetadataProvider<uint, string, CanViewModelBase, CanPropertyInfo, CanMessageData>
{
}
