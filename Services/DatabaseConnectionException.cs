namespace LocalGovProcessor.Services;

public class DatabaseConnectionException : Exception
{
    public DatabaseConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
