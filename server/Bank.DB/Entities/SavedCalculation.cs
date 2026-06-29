using Bank.Core.Enums;
using Bank.DB.Entities.Base;

namespace Bank.DB.Entities;

public class SavedCalculation : BaseTrackUserEntity
{
    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public CalculatorType Type { get; set; }

    public string Name { get; set; } = string.Empty;

    // Сериализиран request DTO, съответстващ на Type; резултатът се преизчислява при четене, не се съхранява.
    public string InputsJson { get; set; } = string.Empty;
}
