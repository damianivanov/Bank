using Bank.Core.Enums;
using Bank.Core.Exceptions;
using Bank.Core.JsonModels.Calculators;
using Bank.DB;
using Bank.DB.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bank.Services.Calculators;

public class SavedCalculationService : ISavedCalculationService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext dbContext;
    private readonly ICreditCalculatorService creditCalculatorService;
    private readonly ILeasingCalculatorService leasingCalculatorService;
    private readonly IRefinancingCalculatorService refinancingCalculatorService;

    public SavedCalculationService(
        AppDbContext dbContext,
        ICreditCalculatorService creditCalculatorService,
        ILeasingCalculatorService leasingCalculatorService,
        IRefinancingCalculatorService refinancingCalculatorService)
    {
        this.dbContext = dbContext;
        this.creditCalculatorService = creditCalculatorService;
        this.leasingCalculatorService = leasingCalculatorService;
        this.refinancingCalculatorService = refinancingCalculatorService;
    }

    public async Task<SavedCalculationModel> SaveAsync(long userId, SaveCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BankException("Необходимо е име, за да се запази изчислението.");
        }

        // Валидиране чрез реално изчисляване на inputs-ите (ниво 3): записват се само валидни, изчислими inputs.
        var inputsJson = await ValidateAndSerializeAsync(request);

        var entity = new SavedCalculation
        {
            UserId = userId,
            Type = request.Type,
            Name = name,
            InputsJson = inputsJson,
        };

        dbContext.SavedCalculations.Add(entity);
        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return Map(entity);
    }

    public async Task<SavedCalculationModel> UpdateAsync(long userId, long id, SaveCalculationRequest request, CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BankException("Необходимо е име, за да се запази изчислението.");
        }

        var entity = await dbContext.SavedCalculations
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new BankException("Запазеното изчисление не е намерено.", 404);

        // Типът е неизменим — редактираш inputs-ите на същия калкулатор, не превръщаш кредит в лизинг.
        if (entity.Type != request.Type)
        {
            throw new BankException("Видът калкулатор на запазено изчисление не може да бъде променян.");
        }

        // Същата валидация-чрез-изчисляване (ниво 3) като при създаване: записваме само валидни, изчислими inputs.
        var inputsJson = await ValidateAndSerializeAsync(request);

        entity.Name = name;
        entity.InputsJson = inputsJson;

        await dbContext.SaveChangesAsync(userId, cancellationToken);

        return Map(entity);
    }

    public async Task<IReadOnlyCollection<SavedCalculationModel>> ListAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.SavedCalculations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.DateCreated)
            .Select(c => new SavedCalculationModel
            {
                Id = c.Id,
                Type = c.Type,
                Name = c.Name,
                CreatedAtUtc = c.DateCreated,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedCalculationDetailsModel> GetAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SavedCalculations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new BankException("Запазеното изчисление не е намерено.", 404);

        var details = new SavedCalculationDetailsModel
        {
            Id = entity.Id,
            Type = entity.Type,
            Name = entity.Name,
            CreatedAtUtc = entity.DateCreated,
        };

        // Планът/графикът се преизчислява при четене — никога не се съхранява.
        switch (entity.Type)
        {
            case CalculatorType.Credit:
                var creditInputs = Deserialize<CreditCalculatorRequest>(entity.InputsJson);
                details.CreditInputs = creditInputs;
                details.CreditResult = await creditCalculatorService.CalculateAsync(creditInputs);
                break;
            case CalculatorType.Leasing:
                var leasingInputs = Deserialize<LeasingCalculatorRequest>(entity.InputsJson);
                details.LeasingInputs = leasingInputs;
                details.LeasingResult = await leasingCalculatorService.CalculateAsync(leasingInputs);
                break;
            case CalculatorType.Refinancing:
                var refinancingInputs = Deserialize<RefinancingCalculatorRequest>(entity.InputsJson);
                details.RefinancingInputs = refinancingInputs;
                details.RefinancingResult = await refinancingCalculatorService.CalculateAsync(refinancingInputs);
                break;
            default:
                throw new BankException("Неподдържан вид калкулатор.");
        }

        return details;
    }

    public async Task DeleteAsync(long userId, long id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.SavedCalculations
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken)
            ?? throw new BankException("Запазеното изчисление не е намерено.", 404);

        dbContext.SavedCalculations.Remove(entity);
        await dbContext.SaveChangesAsync(userId, cancellationToken);
    }

    private async Task<string> ValidateAndSerializeAsync(SaveCalculationRequest request)
    {
        switch (request.Type)
        {
            case CalculatorType.Credit:
                var credit = request.Credit ?? throw new BankException("Необходими са входни данни за кредитното изчисление.");
                await creditCalculatorService.CalculateAsync(credit);
                return Serialize(credit);
            case CalculatorType.Leasing:
                var leasing = request.Leasing ?? throw new BankException("Необходими са входни данни за лизинговото изчисление.");
                await leasingCalculatorService.CalculateAsync(leasing);
                return Serialize(leasing);
            case CalculatorType.Refinancing:
                var refinancing = request.Refinancing ?? throw new BankException("Необходими са входни данни за изчислението за рефинансиране.");
                await refinancingCalculatorService.CalculateAsync(refinancing);
                return Serialize(refinancing);
            default:
                throw new BankException("Неподдържан вид калкулатор.");
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, SerializerOptions)
        ?? throw new BankException("Данните на запазеното изчисление са повредени.", 500);

    private static SavedCalculationModel Map(SavedCalculation entity) => new()
    {
        Id = entity.Id,
        Type = entity.Type,
        Name = entity.Name,
        CreatedAtUtc = entity.DateCreated,
    };
}
