using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ahsoka.Services.IO;

[ExcludeFromCodeCoverage]
internal class IOTests
{
    private static readonly Dictionary<string, string> _testResults = new();

    public static void TestIOService()
    {
        /*
            Tests the IO service and client. 
        */
        Console.WriteLine("---Starting IO Service CommandLine Tests---");

        // Start client for IO Service
        var client = new IOServiceClient();
        client.Start();

        TestHardwareCapabilities(client);
        TestInputPinDefualtValues(client);
        TestToggleOutputs(client);

        // print the test results
        if (_testResults.Count > 0)
        {
            Console.WriteLine("---Printing failed tests---");
            foreach (var kvPair in _testResults)
            {
                Console.WriteLine($"Test: {kvPair.Key} => {kvPair.Value}");
            }

        }
        else
            Console.WriteLine("All tests PASSED!");

        // Finally, display any current values for quick inspection
        DisplayCurrentVoltageValues(client);

        // Stop the Runtimes
        client.Stop();

    }

    private static void DisplayCurrentVoltageValues(IOServiceClient client)
    {
        /*
            Diagnostics test to provide quick inspection of input voltages to user.
        */
        DigitalInputList dInList = client.RequestDigitalInputs();
        AnalogInputList aInList = client.RequestAnalogInputs();

        Console.WriteLine("-------Printing Input Voltages in miliVolts-------");
        foreach (AnalogInput a in aInList.AnalogInputs)
        {
            GetInputResponse response = client.GetAnalogInput(a);
            Console.WriteLine($"[] Analog Input {a.Pin} -> {response.Value}");
        }

        foreach (DigitalInput d in dInList.DigitalInputs)
        {
            GetInputResponse response = client.GetDigitalInput(d);

            Console.WriteLine($"[] Digital Input {d.Pin} -> {response.Value}");
        }

    }

    private static void TestHardwareCapabilities(IOServiceClient client)
    {
        /*
            This Test will verify the IO Pin count for each requested list
            based on known RCD hardware capabilities.
            The General Market RCD is capable of reading 2 analog and 2 digital input pins,
            and setting 2 digital output pins. It does not have Analog Outputs.
            The Balboa Varaint has no GPIO.
        */
        DigitalInputList dInList = client.RequestDigitalInputs();
        AnalogInputList aInList = client.RequestAnalogInputs();
        AnalogOutputList aOutList = client.RequestAnalogOutputs();
        DigitalOutputList dOutList = client.RequestDigitalOutputs();

        if (dInList.DigitalInputs.Count != 2)
            _testResults.Add("Digital Input Count", $"Failed: expected count: 2, actual count: {dInList.DigitalInputs.Count}");

        if (aInList.AnalogInputs.Count != 2)
            _testResults.Add("Analog Input Count", $"Failed: expected count: 2, actual count: {aInList.AnalogInputs.Count}");

        if (dOutList.DigitalOutputs.Count != 2)
            _testResults.Add("Digital Output Count", $"Failed: expected count: 2, actual count: {dOutList.DigitalOutputs.Count}");

        if (aOutList.AnalogOutputs.Count != 0)
            _testResults.Add("Analog Output Count", $"Failed: expected count: 0, actual count: {aOutList.AnalogOutputs.Count}");

    }

    private static void TestInputPinDefualtValues(IOServiceClient client)
    {
        /*
            This test will verify known default values for the RCD
            General Market IO Pins.
            Each AnalogInput is pulled high so should sit around 5V
            without any external connection while each Digital Input is
            pulled low.
        */
        DigitalInputList dInList = client.RequestDigitalInputs();
        AnalogInputList aInList = client.RequestAnalogInputs();

        foreach (AnalogInput a in aInList.AnalogInputs)
        {
            GetInputResponse response = client.GetAnalogInput(a);
            if (response.Value is > 5000 or < 4000)
                _testResults.Add("Analog Input Defualt Value", $"Failed: expected value: 4-5V, actual value (in milivolts): {response.Value}");
        }

        foreach (DigitalInput d in dInList.DigitalInputs)
        {
            GetInputResponse response = client.GetDigitalInput(d);
            if (response.Value > 1000)
                _testResults.Add("Digital Input Defualt Value", $"Failed: expected value: < 1V, actual value (in milivolts): {response.Value}");
        }
    }

    private static void TestToggleOutputs(IOServiceClient client)
    {
        /*
            This test will toggle the digital outputs and verify the return status
            of each call. Furthermore, it will verify that the return status of 
            a call to toggle the analog outputs describes the error
        */

        AnalogOutput FakeAnalogOut = new()
        {
            Pin = 1,
            Name = "FakeAnalogOutput1"
        };
        SetOutputResponse FakeResponse = client.SetAnalogOut(FakeAnalogOut);
        if (String.IsNullOrEmpty(FakeResponse.ErrorDescription))
            _testResults.Add("Fake Analog Output Test", $"Failed: no error returned.");

        DigitalOutputList dOutList = client.RequestDigitalOutputs();
        // will toggle both outputs 4 times
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < dOutList.DigitalOutputs.Count; j++)
            {
                // First set LOW
                dOutList.DigitalOutputs[j].State = PinState.Low;
                SetOutputResponse response = client.SetDigitalOut(dOutList.DigitalOutputs[j]);
                if (!String.IsNullOrEmpty(response.ErrorDescription))
                {
                    _testResults.Add("Set Output Pin LOW", $"Failed with error: {response.ErrorDescription}");
                    return;
                }
                Thread.Sleep(1000);     // wait for 1 sec in case user is expecting to see output toggle on a scope

                // Then set HIGH
                dOutList.DigitalOutputs[j].State = PinState.High;
                response = client.SetDigitalOut(dOutList.DigitalOutputs[j]);
                if (!String.IsNullOrEmpty(response.ErrorDescription))
                {
                    _testResults.Add("Set Output Pin HIGH", $"Failed with error: {response.ErrorDescription}");
                    return;
                }
                Thread.Sleep(1000);

            }
        }

    }

}

