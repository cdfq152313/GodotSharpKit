using System.Text;
using Microsoft.CodeAnalysis;

namespace GdExtension;

public class Log
{
    private static bool _debug = true;

    public Log(IncrementalGeneratorInitializationContext context, string name)
    {
        _context = context;
        _name = name;
    }

    private readonly IncrementalGeneratorInitializationContext _context;
    private readonly string _name;
    private int counter = 0;
    private StringBuilder _stringBuilder = new();

    private void Print(string level, string message)
    {
        _stringBuilder.AppendLine($"// {DateTime.Now:HH:mm:ss.fff}: {level}: {message}");
    }

    public void Debug(string message)
    {
        Print("Debug", message);
    }

    public void Flush()
    {
        if (_debug && _stringBuilder.Length != 0)
        {
            var file = $"{_name}{counter++}.log.cs";
            var result = _stringBuilder.ToString();
            _context.RegisterPostInitializationOutput(ctx => ctx.AddSource(file, result));
            _stringBuilder = new StringBuilder();
        }
    }
}
