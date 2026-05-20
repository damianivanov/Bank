namespace Bank.Core.Exceptions;

public class BankException : Exception
{
    public int StatusCode { get; }

    public BankException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
