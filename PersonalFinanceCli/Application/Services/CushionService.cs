using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Application.Services;

public sealed class CushionService
{
    public const string TransferToCushionCategory = "Transfer to cushion";
    public const string TransferFromIncomeCategory = "Transfer from income";

    private readonly ICardRepository _cardRepository;

    public CushionService(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public Card? FindCushionByName()
    {
        var cards = _cardRepository.GetAll();
        return cards.FirstOrDefault(c => c.Name == "Финансовая подушка");
    }

    public Card? FindCushionByContains()
    {
        var cards = _cardRepository.GetAll();
        return cards.FirstOrDefault(c => c.Name.Contains("подушка", StringComparison.OrdinalIgnoreCase));
    }

    public Card CreateCushion(Currency currency)
    {
        var existing = FindCushionByName();
        if (existing != null)
        {
            return existing;
        }

        return _cardRepository.Add(new Card
        {
            Name = "Финансовая подушка",
            Currency = currency,
            InitialBalance = 0m,
            IsDefault = false,
            IsCushion = true
        });
    }

    public decimal DefaultTransferAmount(decimal incomeAmount, string category)
    {
        var hasSalaryWord = category.Contains("Зарплата", StringComparison.OrdinalIgnoreCase);

        if (incomeAmount < 10m)
        {
            if (hasSalaryWord)
            {
                return 1m;
            }

            return 1m;
        }
        else
        {
            if (hasSalaryWord)
            {
                return Floor2(incomeAmount * 0.20m);
            }

            return Floor2(incomeAmount * 0.10m);
        }
    }

    public static decimal Floor2(decimal value)
    {
        return Math.Floor(value * 100m) / 100m;
    }
}
