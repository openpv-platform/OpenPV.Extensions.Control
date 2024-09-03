using Ahsoka.Installer;
using Ahsoka.System;
using Ahsoka.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ahsoka.Core.DataService;

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
public class TestGeneratorDotNetPlugin : IViewModelGenerator
{
    public List<string> SupportedGenerators => new List<string> { "Ahsoka.Core.Can" };

    ApplicationType IViewModelGenerator.SupportedType => ApplicationType.Dotnet;

    void IViewModelGenerator.ExtendHeader(StringBuilder generatorOutput, string nameSpace, bool isHeader)
    {
        generatorOutput.AppendLine("/*ExtendHeader*/");
    }

    void IViewModelGenerator.ExtendConstructor(StringBuilder generatorOutput, string nameSpace)
    {
        generatorOutput.AppendLine("\t\t/*ExtendConstructor*/");
    }

    void IViewModelGenerator.ExtendSetter(StringBuilder generatorOutput, string propertyName, string dataType)
    {
        generatorOutput.Append("/*ExtendSetter*/");
    }

    void IViewModelGenerator.ExtendAfterClassOutput(StringBuilder generatorOutput, string className)
    {
        generatorOutput.AppendLine("/*ExtendAfterClassOutput*/");
    }

    void IViewModelGenerator.ExtendMethods(StringBuilder generatorOutput, string className, bool isHeader)
    {
        generatorOutput.AppendLine("/*ExtendMethods*/");
    }
}

/// <summary>
/// Base Interface for adding Behaviors to Ahsoka Endpoint Clients
/// </summary>
public class TestGeneratorCPPPlugin : IViewModelGenerator
{
    public List<string> SupportedGenerators => new List<string> { "Ahsoka.Core.Can" };

    ApplicationType IViewModelGenerator.SupportedType => ApplicationType.Cpp;

    void IViewModelGenerator.ExtendHeader(StringBuilder generatorOutput, string nameSpace, bool isHeader)
    {
        generatorOutput.AppendLine($"/*ExtendHeader{(isHeader ? "+Header" : "+Impl")}*/");
    }

    void IViewModelGenerator.ExtendConstructor(StringBuilder generatorOutput, string nameSpace)
    {
        generatorOutput.AppendLine("\t\t/*ExtendConstructor*/");
    }

    void IViewModelGenerator.ExtendSetter(StringBuilder generatorOutput, string propertyName, string dataType)
    {
        generatorOutput.Append("/*ExtendSetter*/;");
    }

    void IViewModelGenerator.ExtendAfterClassOutput(StringBuilder generatorOutput, string className)
    {
        generatorOutput.AppendLine("/*ExtendAfterClassOutput*/;");
    }

    void IViewModelGenerator.ExtendMethods(StringBuilder generatorOutput, string className, bool isHeader)
    {
        generatorOutput.AppendLine($"/*ExtendMethods{(isHeader ? "+Header" : "+Impl")}*/;");
    }
}