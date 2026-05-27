namespace Bank.Core.Common;

public class CommonJsonModel<T>
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
    public T? Data { get; set; }

    public static CommonJsonModel<T> SuccessResult(T data, string? warning = null)
    {
        return new CommonJsonModel<T>
        {
            Success = true,
            Warning = warning,
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

    public static CommonJsonModel<T> ErrorResult(string error, T data)
    {
        return new CommonJsonModel<T>
        {
            Success = false,
            Error = error,
            Data = data,
        };
    }
}
