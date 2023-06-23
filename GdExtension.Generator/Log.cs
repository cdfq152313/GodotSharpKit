using System.Text;
using Microsoft.CodeAnalysis;

namespace GdExtension;

public class Log
{
    private static bool _debug = true;

    public Log(string name)
    {
        _name = name;
    }

    private readonly string _name;
    private StringBuilder _stringBuilder = new();

    private void Print(string level, string message)
    {
        _stringBuilder.AppendLine($"// {DateTime.Now:HH:mm:ss.fff}: {level}: {message}");
    }

    public void Debug(string message)
    {
        Print("Debug", message);
    }

    public void Flush(IncrementalGeneratorInitializationContext context)
    {
        if (_debug)
        {
            context.RegisterPostInitializationOutput(
                ctx => ctx.AddSource($"{_name}.log.cs", _stringBuilder.ToString())
            );
        }
    }

    public void Flush(SourceProductionContext context)
    {
        if (_debug)
        {
            context.AddSource($"{_name}.log.cs", _stringBuilder.ToString());
        }
    }
}
