namespace Bank.Services.Calculators;

public interface ICalculator<TRequest, TResponse>
{
    Task<TResponse> CalculateAsync(TRequest request);
}
