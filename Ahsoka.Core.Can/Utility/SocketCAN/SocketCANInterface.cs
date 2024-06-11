using SocketCANSharp;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Ahsoka.Utility.SocketCAN;

/// <summary>
/// Basic Wrapper around SocketCAN Functionality on Linux Systems
/// </summary>
[ExcludeFromCodeCoverage]
internal class SocketCANInterface : SocketCANInterfaceBase
{
    #region Constructors
    /// <summary>
    /// Constructor used to specify an interface name defined in the system
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    public SocketCANInterface(string interfaceNameArg) : base(interfaceNameArg) { }

    /// <summary>
    /// Constructor used to specify an interface name defined in the system 
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    /// <param name="readTimeoutArg">Read timeout for automatic input</param>
    public SocketCANInterface(string interfaceNameArg, TimeSpan readTimeoutArg) : base(interfaceNameArg, readTimeoutArg) { }

    /// <summary>
    /// Returns a SocketCANInterface that has already been started.
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    /// <returns></returns>
    public static SocketCANInterface StartedSocket(string interfaceNameArg)
    {
        SocketCANInterface canInterface = new(interfaceNameArg);
        canInterface.Start();

        return canInterface;
    }

    /// <summary>
    /// Returns a SocketCANInterface that has already been started.
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    /// <param name="readTimeoutArg">Read timeout for automatic input</param>
    /// <returns></returns>
    public static SocketCANInterface StartedSocket(string interfaceNameArg, TimeSpan readTimeoutArg)
    {
        SocketCANInterface canInterface = new(interfaceNameArg, readTimeoutArg);
        canInterface.Start();

        return canInterface;
    }
    #endregion

    #region WriteMessageFunctions
    /// <summary>
    /// Attempts to send the CanFrame using the currently opened SocketCAN Interface. By default the constructor
    /// initializes the timeout for writing the message out to the socket to be 1 second. The call is blocking for the
    /// amount of time specified at the constructor.
    /// </summary>
    /// <param name="msg">Message To Send</param>
    /// <returns></returns>
    public Boolean WriteMessage(CanFrame msg)
    {
        return TryWriteMessage(msg);
    }
    #endregion

    #region ReadMessageFunctions
    /// <summary>
    /// Unlike the corresponding write message, this message is exposed in case the developer wants to manually
    /// query for the messages themselves.
    /// 
    /// NOTE: If there is an event handler that has been associated with the reception of CAN messages then this
    /// function will not work because that would defeat the purpose of having an event handler to receive the
    /// messages.
    /// </summary>
    /// <param name="msg">The message that was recieved.</param>
    /// <returns>Bool indicating if the message was recieved or a timeout occcured</returns>
    public Boolean ReadMessage(ref CanFrame msg)
    {
        return TryReadMessage(ref msg);
    }
    #endregion
}
