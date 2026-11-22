namespace Deployguard.Services.Security;

public class PrivateRepositoryException : Exception
{
    public PrivateRepositoryException(string message) : base(message)
    {
    }

    public PrivateRepositoryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
