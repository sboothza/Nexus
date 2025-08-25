namespace Nexus.Client;

public class RemoteException : Exception
{
    public RemoteException() : base()
    {
        StackTrace = StackTrace;
    }

    public RemoteException(string message) : base(message)
    {
        StackTrace = StackTrace;
    }

    public RemoteException(string message, Exception innerException) : base(message, innerException)
    {
        StackTrace = StackTrace;
    }

    public RemoteException(string message, string stackTrace) : base(message)
    {
        StackTrace = stackTrace;
    }
    
    public override string StackTrace { get; }
}