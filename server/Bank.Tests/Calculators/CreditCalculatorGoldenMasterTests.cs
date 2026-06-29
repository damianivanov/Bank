using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Bank.Core.Enums;
using Bank.Core.JsonModels.Calculators;
using Bank.Services.Calculators;
using FluentAssertions;

namespace Bank.Tests.Calculators;

public class CreditCalculatorGoldenMasterTests
{
    private static CreditCalculatorService CreateService() => new(TimeProvider.System);

    public static IEnumerable<object[]> Scenarios =>
    [
        ["basic_annuity"],
        ["declining"],
        ["zero_rate"],
        ["promo_grace_complex"],
        ["form_request"],
    ];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task CalculateAsync_ProducesIdenticalSchedule(string scenario)
    {
        var request = BuildRequest(scenario);

        var result = await CreateService().CalculateAsync(request);

        var startDate = result.PaymentSchedule[0].Date;
        foreach (var item in result.PaymentSchedule)
        {
            item.Date.Should().Be(startDate.AddMonths(item.Month));
        }

        var actual = Serialize(result);
        var file = GoldenFilePath(scenario);

        if (!File.Exists(file))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, actual);
            return;
        }

        var expected = File.ReadAllText(file).Replace("\r\n", "\n");
        actual.Should().Be(expected,
            "the golden master for '{0}' must stay numerically identical across the refactor", scenario);
    }

    private static CreditCalculatorRequest BuildRequest(string scenario) => scenario switch
    {
        "basic_annuity" => new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Annuity,
        },
        "declining" => new CreditCalculatorRequest
        {
            LoanAmount = 10000m,
            TermInMonths = 12,
            InterestRate = 10m,
            PaymentType = PaymentType.Declining,
        },
        "zero_rate" => new CreditCalculatorRequest
        {
            LoanAmount = 12000m,
            TermInMonths = 12,
            InterestRate = 0m,
            PaymentType = PaymentType.Annuity,
        },
        "promo_grace_complex" => new CreditCalculatorRequest
        {
            LoanAmount = 50000m,
            TermInMonths = 60,
            InterestRate = 8m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 6,
            PromoRate = 5m,
            GracePeriod = 3,
            ApplicationFee = new Fee { Type = FeeType.Percent, Value = 2m },
            ProcessingFee = new Fee { Type = FeeType.Currency, Value = 200m },
            MonthlyManagementFee = new Fee { Type = FeeType.Currency, Value = 5m },
            AnnualManagementFee = new Fee { Type = FeeType.Currency, Value = 50m },
        },
        "form_request" => new CreditCalculatorRequest
        {
            LoanAmount = 200000m,
            TermInMonths = 300,
            InterestRate = 2.5m,
            PaymentType = PaymentType.Annuity,
            PromoPeriod = 3,
            PromoRate = 2m,
            GracePeriod = 3,
            ApplicationFee = new Fee { Type = FeeType.Currency, Value = 300m },
            OtherAnnualFees = new Fee { Type = FeeType.Currency, Value = 300m },
            OtherMonthlyFees = new Fee { Type = FeeType.Currency, Value = 20m },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown golden scenario"),
    };

    private static string Serialize(CreditCalculatorResponse response)
    {
        var culture = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();

        builder.Append("APR=").Append(response.APR.ToString(culture)).Append('\n');
        builder.Append("AverageMonthlyPayment=").Append(response.AverageMonthlyPayment.ToString(culture)).Append('\n');
        builder.Append("TotalAmountWithFees=").Append(response.TotalAmountWithFees.ToString(culture)).Append('\n');
        builder.Append("TotalFees=").Append(response.TotalFees.ToString(culture)).Append('\n');
        builder.Append("TotalInterest=").Append(response.TotalInterest.ToString(culture)).Append('\n');
        builder.Append("TotalPayments=").Append(response.TotalPayments.ToString(culture)).Append('\n');
        builder.Append("Month|Payment|Principal|Interest|RemainingBalance|Fees|CashFlow\n");

        foreach (var item in response.PaymentSchedule)
        {
            builder
                .Append(item.Month.ToString(culture)).Append('|')
                .Append(item.Payment.ToString(culture)).Append('|')
                .Append(item.Principal.ToString(culture)).Append('|')
                .Append(item.Interest.ToString(culture)).Append('|')
                .Append(item.RemainingBalance.ToString(culture)).Append('|')
                .Append(item.Fees.ToString(culture)).Append('|')
                .Append(item.CashFlow.ToString(culture)).Append('\n');
        }

        return builder.ToString();
    }

    private static string GoldenFilePath(string scenario, [CallerFilePath] string callerPath = "")
        => Path.Combine(Path.GetDirectoryName(callerPath)!, "GoldenMaster", scenario + ".txt");
}
