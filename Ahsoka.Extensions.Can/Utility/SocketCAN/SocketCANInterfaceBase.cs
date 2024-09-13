using SocketCANSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Ahsoka.Utility.SocketCAN;

/// <summary>
/// SocketCAN Interface Constants
/// </summary>
[ExcludeFromCodeCoverage]
internal static class SocketCANInterfaceConsts
{
    /// <summary>
    /// Set Extended Frame
    /// </summary>
    public static uint CanFrameExtendedFrameFlag = 0x80000000;

    /// <summary>
    /// Set RTR Frame
    /// </summary>
    public static uint CanFrameRTRFrameFlag = 0x40000000;
}

/// <summary>
/// Base Class for SocketCAN Interface Objects
/// </summary>
[ExcludeFromCodeCoverage]
internal abstract class SocketCANInterfaceBase : IDisposable
{
    #region Fields
    private readonly string interfaceName;
    private readonly Timeval readTimeout;
    private Boolean isStarted = false;

    private SafeFileDescriptorHandle socketHandle;
    // We generate a sockethandle with the socketCANSharp library that we are using, but we are going to be wrapping
    // that in a socket structure to simplify the management of the underlying socket.
    private readonly List<CanFilter> CANFiltersList = new();

    /// <summary>
    /// Indicates if the CAN Interface's Start method has been invoked.
    /// </summary>
    protected internal bool IsStarted { get => isStarted; private set => isStarted = value; }
    #endregion

    #region Constructors
    /// <summary>
    /// Constructor used to specify an interface name defined in the system 
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    public SocketCANInterfaceBase(string interfaceNameArg)
    {
        interfaceName = interfaceNameArg;
        readTimeout = new Timeval(1, 0);
    }

    /// <summary>
    /// Constructor used to specify an interface name defined in the system 
    /// </summary>
    /// <param name="interfaceNameArg">Name of the Interface</param>
    /// <param name="readTimeoutArg">Read timeout for automatic input</param>
    public SocketCANInterfaceBase(string interfaceNameArg, TimeSpan readTimeoutArg) : this(interfaceNameArg)
    {
        // We are going to convert the TimeSpan value to the Timeval structure that is going to be used to set the
        // timeout of the socket we are using.
        readTimeout = new Timeval(readTimeoutArg.Milliseconds / 1000, readTimeoutArg.Milliseconds % 1000);
    }

    /// <summary>
    /// Dispose Method
    /// </summary>
    public void Dispose()
    {
        try
        {
            Stop();
        }
        finally
        {
            socketHandle.Dispose();
        }
    }
    #endregion

    #region WriteMessageFunctions
    /// <summary>
    /// Attempts to write a CanFrame directly to the CAN Interface. By default the constructor initializes this with a 1
    /// second timeout, so it is blocking for up to 1 second.
    /// </summary>
    /// <param name="msg">Message to Write</param>
    /// <returns>Result of the Operation</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected Boolean TryWriteMessage(CanFrame msg)
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("The socket hasn't been started yet.");
        }

        int writeByteCount;
        writeByteCount = LibcNativeMethods.Write(socketHandle, ref msg, Marshal.SizeOf(typeof(CanFrame)));

        // Make sure that we return whether or not we were able to actually read a message from the socket.
        return (writeByteCount > 0);
    }
    #endregion

    #region ReadMessageFunctions
    /// <summary>
    /// Directly Read a CanFrame from the Interface.  Will block if no data is available
    /// 
    /// NOTE: If there is an event handler that has been associated with the reception of CAN messages then this
    /// function will not work because that would defeat the purpose of having an event handler to receive the
    /// messages.
    /// </summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    protected Boolean TryReadMessage(ref CanFrame msg)
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("The socket hasn't been started yet.");
        }

        int readByteCount;
        readByteCount = LibcNativeMethods.Read(socketHandle, ref msg, Marshal.SizeOf(typeof(CanFrame)));

        // Make sure that we return whether or not we were able to actually read a message from the socket.
        return (readByteCount > 0);
    }
    #endregion

    #region Start and Stop Function
    /// <summary>
    /// Initialize a CAN Socket Communication and Open the Socket
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        if (IsStarted)
        {
            throw new InvalidOperationException("The socket has already been started.");
        }

        socketHandle = InitializeCANSocket(interfaceName);

        IsStarted = true;
    }

    /// <summary>
    /// Close the CAN Socket.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public void Stop()
    {
        if (!IsStarted)
        {
            throw new InvalidOperationException("The socket hasn't been started yet.");
        }

        // Close and dispose of the socket.
        socketHandle.Close();

        IsStarted = false;
    }

    private SafeFileDescriptorHandle InitializeCANSocket(string interfaceName)
    {
        var socketHandle = LibcNativeMethods.Socket(SocketCanConstants.PF_CAN, SocketType.Raw, SocketCanProtocolType.CAN_RAW);

        if (socketHandle.IsInvalid)
        {
            // There is an error and we cannot continue.
            throw new InvalidOperationException($"Failed to create the socket for interface \"{interfaceName}\". Errno: {LibcNativeMethods.Errno}");
        }

        // We are going to be using the can1 interface because that is the populated CAN interface on the RCD
        // units.
        var interfaceRequest = new Ifreq(interfaceName);
        int ioctlResult = LibcNativeMethods.Ioctl(socketHandle, SocketCanConstants.SIOCGIFINDEX, interfaceRequest);
        if (ioctlResult == -1)
        {
            // There was an error when running the iocto function, specifically when looking up the interface by
            // name.
            throw new InvalidOperationException($"There was an error looking up the interface by name \"{interfaceName}\". Errno: {LibcNativeMethods.Errno}");
        }

        // int arg = 1;
        // ioctlResult = LibcNativeMethods.Ioctl(socketHandle, SocketCanConstants.FIONBIO, ref arg);
        // if (ioctlResult == -1)
        // {
        //     // There was an error setting the option to make the read call non-blocking. If we can't set it
        //     // non-blocking then it's not even worth it.
        //     throw new InvalidOperationException($"There was an error setting the interface read as non-blocking \"{interfaceName}\". Errno: {LibcNativeMethods.Errno}");
        // }

        int sockOptsResult = LibcNativeMethods.SetSockOpt(socketHandle, SocketLevel.SOL_SOCKET, SocketLevelOptions.SO_RCVTIMEO, readTimeout, Marshal.SizeOf(typeof(Timeval)));
        if (sockOptsResult != 0)
        {
            // There was an error setting the timeout of the read call.
            throw new InvalidOperationException($"There was an error setting the timeout for the read call on the \"{interfaceName}\". Errno: {LibcNativeMethods.Errno}");
        }

        sockOptsResult = LibcNativeMethods.SetSockOpt(socketHandle, SocketLevel.SOL_SOCKET, SocketLevelOptions.SO_SNDTIMEO, readTimeout, Marshal.SizeOf(typeof(Timeval)));
        if (sockOptsResult != 0)
        {
            // There was an error setting the timeout of the read call.
            throw new InvalidOperationException($"There was an error setting the timeout for the write call on the \"{interfaceName}\". Errno: {LibcNativeMethods.Errno}");
        }



        var address = new SockAddrCan(interfaceRequest.IfIndex);
        int bindResult = LibcNativeMethods.Bind(socketHandle, address, Marshal.SizeOf(typeof(SockAddrCan)));
        if (bindResult == -1)
        {
            // Failed to bind the interface to the address.
            throw new InvalidOperationException($"There was an error binding the interface to the address. Errno: {LibcNativeMethods.Errno}");
        }

        return socketHandle;
    }
    #endregion

    #region Filters
    /// <summary>
    /// Adds a filter to the list of CAN Filters.  These are applied to the Raw CAN Socket immediately
    /// </summary>
    /// <param name="filter">Filter to Add</param>
    /// <returns>Result of the Operation</returns>
    public Boolean AddCANFilter(CanFilter filter)
    {
        CANFiltersList.Add(filter);

        // Make sure that we update the can filters that are applied when we are done adding the filter to our list.
        return ApplyCANFilterList();
    }

    /// <summary>
    /// Clear the list of CAN Filters
    /// </summary>
    /// <returns>Result of the Operation</returns>
    public Boolean ClearCANFilters()
    {
        CANFiltersList.Clear();

        return ResetCANFilterList();
    }

    private Boolean ResetCANFilterList()
    {
        int setSockOptResult = LibcNativeMethods.SetSockOpt(
            socketHandle,
            SocketLevel.SOL_CAN_RAW,
            CanSocketOptions.CAN_RAW_FILTER,
            null,
            0
        );
        return setSockOptResult == 0;
    }

    private Boolean ApplyCANFilterList()
    {
        CanFilter[] canFiltersArray = CANFiltersList.ToArray();
        int setSockOptResult = LibcNativeMethods.SetSockOpt(
            socketHandle,
            SocketLevel.SOL_CAN_RAW,
            CanSocketOptions.CAN_RAW_FILTER, // There is also a CAN_INV_FILTER that activates when the filters doesn't match.
            canFiltersArray,
            Marshal.SizeOf(typeof(CanFilter)) * canFiltersArray.Length
        );
        return setSockOptResult == 0;
    }
    #endregion
}
