using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Tests;

public class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText _content;

    public InMemoryAdditionalText(string path, string content)
    {
        Path = path;
        _content = SourceText.From(content, Encoding.UTF8);
    }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _content;

    internal class BinaryText : InMemoryAdditionalText
    {
        public BinaryText(string path) : base(path, string.Empty) { }

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            throw new InvalidDataException("Binary content not supported");
    }
}
