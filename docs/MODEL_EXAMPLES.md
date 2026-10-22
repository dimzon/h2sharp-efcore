# Примеры EF на модели форума

Исполняемые примеры находятся в `test/H2Sharp.EntityFrameworkCore.ModelTests`.
Каждый пример проверяет результат по данным приложения.

| Файл | Пример использования |
|---|---|
| `ForumSearchExamples.cs` | `StartsWith`, `EndsWith`, `Contains` с пользовательским текстом, включая `%`, `_`, `\` и null; превью заголовков, домен email, заметки модератора |
| `ForumCursorAndBulkExamples.cs` | Пагинация по `(CreatedAt, Id)`, `ExecuteUpdateAsync` для закрытия тем, `ExecuteDelete` скрытых ответов, увеличение просмотров |
| `ForumPreferenceExamples.cs` | Подписки в массивах: `Contains`, индекс, `Skip`/`Take`, `Concat`; параметризованные списки; сохранение JSON и binary |
| `ForumStatisticsExamples.cs` | Дисперсия, стандартное отклонение, корреляция и регрессия, экспорт групп в массивы и JSON |
| `ForumKeyGenerationExamples.cs` | HiLo-ключи до `SaveChanges`, GUID, синхронная и асинхронная запись, чтение другим контекстом |
| `ForumCalendarExamples.cs` | `DateOnly.AddDays/AddMonths/AddYears`, `DayNumber`, выделение даты и времени, строковые поля экрана |

В примерах вызовы EF остаются обычными запросами приложения. `ReferenceForum` — тестовая
обвязка: получает команду через публичный `CreateDbCommand`, передаёт SQL и параметры
эталонной H2 2.5 на JVM и читает строки. Это позволяет проверять SQL без подстройки под
пробелы порта. PostgreSQL SQL в запросы не подставляется.

Чтение `JsonDocument.RootElement.GetProperty` после `Single` выполняется в .NET.
SQL агрегаты `JSON_ARRAYAGG` и `JSON_OBJECTAGG` — документированные возможности H2;
функции чтения JSON-пути не добавлялись.

Отдельные строгие проверки порта сохраняют `BlockedByPort`: сравнение составного курсора
([h2sharp#18](https://github.com/dimzon/h2sharp/issues/18)) и вычисление UPDATE для каждой строки
([h2sharp#17](https://github.com/dimzon/h2sharp/issues/17)), поиск байта через `POSITION`
([h2sharp#19](https://github.com/dimzon/h2sharp/issues/19)). Парные JVM-примеры проверяют
правильные строки, а портовые проверки остаются красными до исправления движка.

## Покрытие

Замер выполняется **только ModelTests, Debug** на `net9.0` и `net10.0`:
4772 / 9493 строк = **50,27 %** (было 39,93 %, +10,34 процентного пункта).
Порог `tools/CheckCoverage.ps1` — **50 %**.
Модульные проверки внутренних API в это число не входят.
