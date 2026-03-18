using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;
using PersonalFinanceCli.Infrastructure.Time;

namespace PersonalFinanceCli.Application.CommandHandlers;

public sealed class AddTransactionHandler
{
    public const string TransferToCushion = "Transfer to cushion";
    public const string TransferFromIncome = "Transfer from income";

    private readonly ITransactionRepository _transactionRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IClock _clock;

    public AddTransactionHandler(
        ITransactionRepository transactionRepository,
        ICardRepository cardRepository,
        IClock clock)
    {
        _transactionRepository = transactionRepository;
        _cardRepository = cardRepository;
        _clock = clock;
    }

    public Transaction Handle(
        TransactionType type,
        decimal amount,
        string category,
        int? cardId,
        DateOnly? date,
        string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Amount must be > 0.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new InvalidOperationException("Category cannot be empty.");
        }

        var resolvedCardId = EnsureCardSelectedFallback(cardId, type);
        var selectedCard = _cardRepository.GetById(resolvedCardId);
        if (selectedCard is null)
        {
            throw new InvalidOperationException("Card not found.");
        }

        var trx = new Transaction
        {
            CardId = resolvedCardId,
            Amount = amount,
            Category = category,
            Date = date ?? _clock.Today,
            Note = note,
            Type = type
        };

        return _transactionRepository.Add(trx);
    }

    public int EnsureCardSelectedFallback(int? cardId, TransactionType type)
    {
        if (cardId.HasValue)
            return ResolveExplicitCard(cardId.Value);

        return type == TransactionType.Expense
            ? ResolveExpenseCard()
            : ResolveIncomeCard();
    }

    private int ResolveExplicitCard(int cardId)
    {
        var card = _cardRepository.GetById(cardId);
        if (card == null)
            throw new InvalidOperationException("Card not found.");

        return card.Id;
    }

    private int ResolveExpenseCard()
    {
        var card = _cardRepository.GetDefaultByDataStore() ?? _cardRepository.GetFirst();
        if (card == null)
            throw new InvalidOperationException("No cards available.");

        return card.Id;
    }

    private int ResolveIncomeCard()
    {
        var card = _cardRepository.GetDefault() ?? _cardRepository.GetFirst();
        if (card == null)
            throw new InvalidOperationException("No cards available.");

        return card.Id;
    }


    public int ResolveCardId(int? cardId)
    {
        return EnsureCardSelectedFallback(cardId, TransactionType.Income);
    }

    public Card? FindCushionCardLoose()
    {
        var cards = _cardRepository.GetAll();
        var byFlag = cards.FirstOrDefault(c => c.IsCushion);
        if (byFlag != null)
        {
            return byFlag;
        }

        var exact = cards.FirstOrDefault(c => c.Name == "Финансовая подушка");
        if (exact != null)
        {
            return exact;
        }

        return cards.FirstOrDefault(c => c.Name.Contains("подушка"));
    }

    public void AddTransferPair(int fromCardId, int cushionCardId, decimal amount, DateOnly? date)
    {
        var transferDate = date ?? _clock.Today;

        _transactionRepository.Add(new Transaction
        {
            CardId = fromCardId,
            Amount = amount,
            Category = TransferToCushion,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Expense
        });

        _transactionRepository.Add(new Transaction
        {
            CardId = cushionCardId,
            Amount = amount,
            Category = TransferFromIncome,
            Date = transferDate,
            Note = "auto",
            Type = TransactionType.Income
        });
    }
}
