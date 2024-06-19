using Ahsoka.Services.System.Platform;
using Ahsoka.System;

namespace Ahsoka.Commands;

[CommandLinePlugin]
internal static class TestCommands
{
    [CommandLineMethod(@"     --TestIO: Run Test Script")]
    private static void TestIO(string[] args)
    {
        IOTests.TestSystemService();
    }
}