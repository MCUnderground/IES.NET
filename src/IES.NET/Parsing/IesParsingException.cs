namespace IES.NET.Parsing;

public class IesParsingException : Exception
{
    public IesParsingException() { }
    public IesParsingException(string message) : base(message) { }
    public IesParsingException(string message, Exception inner) : base(message, inner) { }
}
