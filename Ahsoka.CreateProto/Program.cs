using Ahsoka.Core;
using Ahsoka.Core.Utility;
using Ahsoka.Services.Can;

namespace Ahsoka;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine(DateTime.Now.ToString("hh:mm:ss.fff"));

        // Embedded Targets Don't Load Extensions...Assumed to be Compiled In.
        // Initialing Assembly Resolver
        AssemblyResolver.Init();

        //force assembly load
        var service = new CanService();

        CommandLine.Execute(args);
    }
}
