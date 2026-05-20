namespace Bank.Core.Common;

public class CommonJsonModel<T>
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }

    public static CommonJsonModel<T> SuccessResult(T data)
    {
        return new CommonJsonModel<T>
        {
            Success = true,
            Data = data,
        };
    }

    public static CommonJsonModel<T> ErrorResult(string error)
    {
        return new CommonJsonModel<T>
        {
            Success = false,
            Error = error,
        };
    }
}
