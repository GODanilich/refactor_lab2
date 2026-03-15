# PersonalFinanceCli

Консольное приложение для домашнего бюджета на .NET 8 (`net8.0`).

## Установка .NET 8

1. Скачайте и установите SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Проверьте установку:

```bash
dotnet --version
```

Должна выводиться версия `8.x`.

## Структура решения

- `PersonalFinanceCli` — основное приложение
- `PersonalFinanceCli.Tests` — тесты xUnit + coverage через `coverlet.collector`

## Сборка

```bash
dotnet build
```

## Тесты и coverage

```bash
dotnet test
```

Результат покрытия (Cobertura) сохраняется в `TestResults/.../coverage.cobertura.xml`.

## Запуск

### Интерактивный режим (REPL)

```bash
dotnet run --project PersonalFinanceCli
```

После запуска показывается prompt `> `, приложение не завершается после одной команды.

Поддерживаются:

- `help` — список команд и примеры
- `exit` — выход из REPL
- `card add`, `card set-default`, `expense add`, `income add`, `limit set` — после выполнения печатается дневной отчет
- `report day [--date ...]` — печатает отчет и возвращается в REPL
- `limit show`, `card list` — печатают результат и возвращаются в REPL

### Wizard mode для неполных команд

Если не хватает обязательных параметров, приложение задает вопросы и дополняет команду.

Примеры вопросов:

- `Amount?`
- `Category?`
- `Card? (enter to use default)`
- `Date? (YYYY-MM-DD, enter = today)`
- `Currency (RUB/EUR)?`
- `Daily limit amount?`

На любом шаге можно ввести `cancel`, чтобы отменить операцию.
При неверном формате числа/даты/валюты вопрос повторяется.

### Одноразовый запуск через аргументы

```bash
dotnet run --project PersonalFinanceCli -- <args>
```

Примеры одноразового запуска:

```bash
dotnet run --project PersonalFinanceCli -- card add "Tinkoff" RUB 1000
dotnet run --project PersonalFinanceCli -- card add "Cash" RUB 200
dotnet run --project PersonalFinanceCli -- card list
dotnet run --project PersonalFinanceCli -- card set-default 2

dotnet run --project PersonalFinanceCli -- income add 1500 "Salary"
dotnet run --project PersonalFinanceCli -- expense add 12.5 "Food" --note "Lunch"
dotnet run --project PersonalFinanceCli -- expense add 3.4 "Coffee" --date 2026-03-01

dotnet run --project PersonalFinanceCli -- limit set 1000
dotnet run --project PersonalFinanceCli -- limit show

dotnet run --project PersonalFinanceCli -- report day
dotnet run --project PersonalFinanceCli -- report day --date 2026-03-01
```

## Хранение данных

Данные сохраняются в `data.json` в текущей рабочей директории.

Формат:

```json
{
  "cards": [],
  "transactions": [],
  "dailyLimits": []
}
```
