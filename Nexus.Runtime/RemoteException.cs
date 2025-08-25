namespace Nexus.Runtime;

public class RemoteException : Exception
{
    private string _stackTrace;

    public RemoteException() : base()
    {
        _stackTrace = StackTrace;
    }

    public RemoteException(string message) : base(message)
    {
        _stackTrace = StackTrace;
    }

    public RemoteException(string message, Exception innerException) : base(message, innerException)
    {
        _stackTrace = StackTrace;
    }

    public RemoteException(string message, string stackTrace) : base(message)
    {
        _stackTrace = stackTrace;
    }
}