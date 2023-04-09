namespace BigBang1112.Gbx.Server.Exceptions;

public class GbxApiServerException : Exception
{
    public GbxApiServerException() { }
    public GbxApiServerException(string message) : base(message) { }
    public GbxApiServerException(string message, Exception inner) : base(message, inner) { }
    protected GbxApiServerException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
