using PersonalFinanceCli.Application.Repositories;
using PersonalFinanceCli.Domain.Entities;
using PersonalFinanceCli.Domain.ValueObjects;

namespace PersonalFinanceCli.Domain.Services;

public sealed class DailyReportService
{
    private readonly ICardRepository _cardRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ILimitRepository _limitRepository;

    public DailyReportService(
        ICardRepository cardRepository,
        ITransactionRepository transactionRepository,
        ILimitRepository limitRepository)
    {
        _cardRepository = cardRepository;
        _transactionRepository = transactionRepository;
        _limitRepository = limitRepository;
    }

public DailyReport Generate(DateOnly date)
    {
        var cards = _cardRepository.GetAll();
        var currency = ResolveReportCurrency(cards);
        var allTransactions = _transactionRepository.GetAll();

        var selectedCardIds = SelectCardIdsByCurrency(cards, currency);
        var totals = CalculateTotals(date, selectedCardIds, allTransactions);
        var balances = CalculateBalances(cards, allTransactions);
        var limit = _limitRepository.GetByDate(date);

        return new DailyReport(
            date,
            currency,
            totals.Income,
            totals.Expense,
            totals.CategoryTotals,
            balances,
            limit);
    }

    private static Currency ResolveReportCurrency(IReadOnlyList<Card> cards)
    {
        return cards.FirstOrDefault(c => c.IsDefault)?.Currency
            ?? cards.FirstOrDefault()?.Currency
            ?? Currency.RUB;
    }

    private static HashSet<int> SelectCardIdsByCurrency(IReadOnlyList<Card> cards, Currency currency)
    {
        return cards.Where(c => c.Currency == currency)
            .Select(c => c.Id)
            .ToHashSet();
    }

    private static (decimal Income, decimal Expense, Dictionary<string, decimal> CategoryTotals)
        CalculateTotals(DateOnly date, HashSet<int> cardIds, IReadOnlyList<Transaction> allTransactions)
    {
        decimal income = 0m;
        decimal expense = 0m;
        var categoryTotals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var transaction in allTransactions)
        {
            if (!TransactionIsInReport(transaction, date, cardIds))
                continue;

            if (transaction.Type == TransactionType.Income)
            {
                income += transaction.Amount;
                continue;
            }

            expense += transaction.Amount;
            AddExpenseToCategory(categoryTotals, transaction);
        }

        return (income, expense, categoryTotals);
    }

    private static bool TransactionIsInReport(Transaction transaction, DateOnly date, HashSet<int> cardIds)
    {
        return transaction.Date == date && cardIds.Contains(transaction.CardId);
    }

    private static void AddExpenseToCategory(Dictionary<string, decimal> totals, Transaction transaction)
    {
        if (totals.ContainsKey(transaction.Category))
        {
            totals[transaction.Category] += transaction.Amount;
            return;
        }

        totals[transaction.Category] = transaction.Amount;
    }

    private static List<CardBalanceLine> CalculateBalances(
        IReadOnlyList<Card> cards,
        IReadOnlyList<Transaction> allTransactions)
    {
        var result = new List<CardBalanceLine>();

        foreach (var card in cards)
        {
            var balance = CalculateCardBalance(card, allTransactions);
            result.Add(new CardBalanceLine(card.Id, card.Name, card.IsDefault, balance, card.Currency));
        }

        return result;
    }

    private static decimal CalculateCardBalance(Card card, IReadOnlyList<Transaction> allTransactions)
    {
        decimal balance = card.InitialBalance;

        foreach (var trx in allTransactions.Where(x => x.CardId == card.Id))
        {
            balance += trx.Type == TransactionType.Income ? trx.Amount : -trx.Amount;
        }

        return balance;
    }

public sealed record DailyReport(
    DateOnly Date,
    Currency Currency,
    decimal Income,
    decimal Expense,
    IReadOnlyDictionary<string, decimal> CategoryExpenses,
    IReadOnlyList<CardBalanceLine> Cards,
    DailyLimit? Limit);

public sealed record CardBalanceLine(int CardId, string CardName, bool IsDefault, decimal Balance, Currency Currency);
