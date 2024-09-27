using Ahsoka.Services.Can.Messages;
using Ahsoka.Utility;
using DbcParserLib.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Ahsoka.Services.Can;
/// <summary>
/// Base View Model used in Ahsoka Applications which adds basic INotifyPropertyChanged support.
/// </summary>
public abstract class CanViewModelBase : ViewModelBase, IHasCanData
{
    protected readonly CanMessageData message = null;
    ulong[] data = null;

    /// <summary>
    /// CAN Id for the Modeled Message
    /// </summary>
    public uint Id { get { return message.Id; } }

    /// <summary>
    /// Constructor to Allocate Data (Serialize)
    /// </summary>
    /// <param name="canID">CANId for the Message</param>
    /// <param name="dlc">Data Length of the message</param>
    public CanViewModelBase(uint canID, int dlc)
    {
        message = new CanMessageData()
        {
            Dlc = (uint)dlc,
            Id = canID
        };

        data = new ulong[(int)Math.Ceiling(dlc / 8.0f)];
        Array.Fill(data, ulong.MaxValue); // Init to FF's
    }

    /// <summary>
    /// Constructor to use Existin Data (Deserialize)
    /// </summary>
    /// <param name="message">CAN Message which this Model Object Manages</param>
    public CanViewModelBase(CanMessageData message)
    {
        this.message = message;

        int length = (int)Math.Ceiling(message.Data.Length / 8.0f);
        var dataAdjusted = new byte[length * 8];
        Array.Copy(message.Data, dataAdjusted, message.Data.Length);
        data = new ulong[length];

        // Convert Data to Uint64s
        for (int i = 0; i < data.Length; i++)
            data[i] = BitConverter.ToUInt64(dataAdjusted, i * 8);
    }

    /// <summary>
    /// Get the CAN Message.
    /// </summary>
    /// <returns></returns>
    public CanMessageData CreateCanMessageData()
    {
        // Convert Data to Uint64s
        using MemoryStream stream = new();
        for (int i = 0; i < data.Length; i++)
            stream.Write(BitConverter.GetBytes(data[i]));

        return new CanMessageData()
        {
            Id = message.Id,
            Dlc = message.Dlc,
            Data = stream.ToArray()
        };
    }

    /// <summary>
    /// Internal SetValue Method used to set a value and call the Property Changed with a Single Line of Code.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="newValue">New Value to be Set if it differs from the Base Value</param>
    /// <param name="memberName">Name of the Property or Member</param>
    protected void OnSetValue<T>(T newValue, [CallerMemberName] string memberName = "") where T : struct
    {
        if (GetMetadata().TryGetValue(memberName, out CanPropertyInfo info))
        {
            T baseValue = info.GetValue<T>(data, true, true);
            if (!baseValue.Equals(newValue))
            {
                // Run any behaviors added to this model.
                RunSetBehaviors<T>(baseValue, newValue, info.UniqueId, memberName);

                info.SetValue<T>(ref data, newValue);
                OnPropertyChanged(memberName);
            }
        }
    }

    /// <summary>
    /// Internal GetValue Method used to get a value from the backing data store.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="memberName">Name of the Property or Member</param>
    /// <param name="scaled">Scale the Property Before Returning</param>
    protected T OnGetValue<T>([CallerMemberName] string memberName = "", bool scaled = true) where T : struct
    {
        if (GetMetadata().TryGetValue(memberName, out CanPropertyInfo info))
        {
            return info.GetValue<T>(data, scaled);
        }
        return default;
    }

    /// <summary>
    ///  GetValue Method used to get a value from the backing data store.
    /// </summary>
    /// <typeparam name="T">Type of the Property / Member</typeparam>
    /// <param name="memberName">Name of the Property or Member</param>
    public T GetRawValue<T>(string memberName) where T : struct
    {
        return OnGetValue<T>(memberName, false);
    }

    /// <summary>
    /// Data describing the data layout (data type, position, etc.)
    /// /// </summary>
    protected abstract Dictionary<string, CanPropertyInfo> GetMetadata();
}

/// <summary>
/// Extension for Builder from Generic Base
/// </summary>
public static class CanExtension
{
    /// <summary>
    /// Reference to Generated Metadata
    /// </summary>
    public static ICanMetadata Metadata { get; set; }

    /// <summary>
    /// Creates a Specific Object from a Message
    /// </summary>
    /// <typeparam name="T">Base Class of Generated Object</typeparam>
    /// <param name="message">Can Message with data for construction</param>
    public static T GetObject<T>(this CanMessageData message) where T : CanViewModelBase
    {
        return (T)Metadata?.CreateObject(message);
    }
}

/// <summary>
/// Interface for Objects that can Product Can Data Objects
/// </summary>
public interface IHasCanData
{
    /// <summary>
    ///  Get Can Data to Send 
    /// </summary>
    /// <returns></returns>
    CanMessageData CreateCanMessageData();
}

/// <summary>
/// Message Definition for CAN Configuration Files
/// </summary>
public partial class MessageDefinition
{
    /// <summary>
    /// Description of Message
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{this.Name} ({this.Id})";
    }
}

/// <summary>
/// Signal Definition
/// </summary>
public partial class MessageSignalDefinition
{
   
}

public partial class NodeDefinition
{
    [Range(0, Int32.MaxValue)]
    public Int32 IdValid
    {
        get { return Id; }
        set { Id = value; }
    }

    public int Port
    {
        get { return Ports != null ? Ports.FirstOrDefault() : 0; }
        set { Ports = [value]; }
    }
}

public partial class J1939NodeDefinition
{
    [Range(0, Int32.MaxValue)]
    public Int32 AddressOneValid
    {
        get { return AddressValueOne; }
        set { AddressValueOne = value; }
    }

    [Range(0, Int32.MaxValue)]
    public Int32 AddressTwoValid
    {
        get { return AddressValueTwo; }
        set { AddressValueTwo = value; }
    }

    [Range(0, Int32.MaxValue)]
    public Int32 AddressThreeValid
    {
        get { return AddressValueThree; }
        set { AddressValueThree = value; }
    }
}
