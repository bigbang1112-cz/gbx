namespace BigBang1112.Gbx.Server.Exceptions;

public class GbxApiClientException : Exception
{
    public GbxApiClientException() { }
    public GbxApiClientException(string message) : base(message) { }
    public GbxApiClientException(string message, Exception inner) : base(message, inner) { }
    protected GbxApiClientException(
      System.Runtime.Serialization.SerializationInfo info,
      System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
