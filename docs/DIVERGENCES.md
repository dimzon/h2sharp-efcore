# DIVERGENCES — что провайдер делает правильно, а движок ещё не умеет

Проверено прогоном против пиннованного эталона (`h2-2.5.259-SNAPSHOT.jar`). Слева — что
провайдер генерирует, справа — что с этим делает эталонная H2 2.5 и что пока делает порт.

Правило реестра: **провайдер не подстраивается под текущие пробелы движка.** Он генерирует
то, что требует H2 2.5, и записывает расхождение здесь. Обход был бы временным решением,
закрепляющим временное ограничение в наблюдаемом поведении.

**Не работает — значит дефект порта.** Тест, падающий из-за неперенесённой в порт функции,
помечен трейтом `BlockedByPort` и остаётся в наборе: он делает видимым, что порт до сих пор
не догнал H2. Поэтому набор раздваивается на два прогона:

* `dotnet test --filter "Category!=BlockedByPort"` — зелёный: провайдер корректен настолько,
  насколько позволяет порт;
* `dotnet test --filter "Category=BlockedByPort"` — перечень дефектов порта, на которые
  провайдер подписан.

## Реестр пересмотра: что трогать, когда порт закрывает issue

Тесты с трейтом `BlockedByPort` утверждают отказ и сами просигнализируют, когда порт
догонит: отказ пропадёт, и тест упадёт иначе. Хуже с тестами, которые обходят пробел
порта и зеленеют без него: закрытие issue порта их не роняет, и протухание видно
только здесь. Таблица — что именно пересмотреть при закрытии каждого issue.

| Issue порта | Обход в провайдере/тестах | Что сделать при закрытии |
|---|---|---|
| dimzon/h2sharp#23 (`UNNEST`, `WITH ORDINALITY`) | закрыт (коммит движка «Arrays end to end» `f24e67e`): `Independent_array_is_unnested_into_rows_on_the_port` живёт | — |
| dimzon/h2sharp#14 (тип `ARRAY`, литерал) | закрыт целиком: `The_rows_are_asked_for_with_a_list_in_a_variable` и `The_strings_are_joined_with_a_separator` живут | — |
| dimzon/h2sharp#18 (row-value) | закрыт: `Composite_key_lookup`, `Composite_cursor_reads_the_next_page_without_offset` живут | — |
| dimzon/h2sharp#17 (`UPDATE` с выражением) | закрыт: `View_counts_are_updated_with_a_database_expression` живёт | — |
| dimzon/h2sharp#19 (`POSITION` binary) | закрыт (коммит движка «Read POSITION through readConcat» `418fc11`): `Binary_token_is_searched_for_a_marker_byte` разблокирован | — |
| dimzon/h2sharp#20 (операции дат) | закрыт (коммит движка «Subtract timestamps and compute intervals» `22fbbdd`): `The_rows_are_asked_for_with_a_date_moved_by_a_unit` разблокирован | — |
| dimzon/h2sharp#21 (выражения в `ORDER BY`) | закрыт (коммит движка «Read complete expressions in ORDER BY» `be3d11b`) | — |
| dimzon/h2sharp#22 (операторы `~`, `~*`) | закрыт (коммит движка «Read regex operators» `29e8e10`) | — |
| dimzon/h2sharp#27 (`EXISTS`) | закрыт (коммит движка «Ask subqueries whether a row exists» `d86b2e1`): `It_is_asked_whether_there_is_a_row`, `It_is_asked_whether_every_row_holds_a_condition`, `It_is_asked_whether_there_is_a_row_without_waiting` разблокированы | — |
| dimzon/h2sharp#13 (`FINAL TABLE`, identity, `ALTER TABLE`) | `FINAL TABLE` и identity портированы (`f197e34`, `3c4e993`): `H2IdentityTests` (2/2) и вставки форума идут в зелёном прогоне, identity-значение возвращается. Осталась одна форма: `FINAL TABLE (INSERT … DEFAULT VALUES)` не получает identity — девять тестов `StoreGeneratedH2Test` (пробел 22) | issue открыт не из-за них, а из-за `COMMENT ON`, `CREATE ALIAS` и остальных действий `ALTER TABLE` (пробелы 7 и 21); при его закрытии пересмотреть только тесты, упавшие на этих формах |
| dimzon/h2sharp#33 (`INTERSECT`, `EXCEPT`) | закрыт (коммит движка «Port set operations» `69d522a`): `The_rows_of_two_queries_are_met`, `The_rows_of_two_queries_are_set_apart` живут | — |
| dimzon/h2sharp#34 (`SAVEPOINT`) | закрыт (коммит движка `69d522a`): `The_change_is_kept_when_the_transaction_is_committed`, `The_change_is_thrown_away_when_the_transaction_is_rolled_back` живут | — |
| dimzon/h2sharp#35 (`WITHIN GROUP`, `FILTER`) | закрыт (коммит движка `69d522a`): `Two_gatherings_in_one_query_take_their_own_order_and_filter` живёт | — |
| dimzon/h2sharp#36 (E-строки) | закрыт (коммит движка `69d522a`): `The_string_is_trimmed`, `The_rows_are_asked_for_with_a_trimmed_string` живут | — |
| dimzon/h2sharp#37 (`CAST(агрегат)` в скалярном подзапросе) | закрыт (коммит движка `69d522a`): `The_rows_are_asked_for_with_a_table_joined_from_the_left` живёт | — |
| dimzon/h2sharp#38 (`FILTER` молча игнорировался) | закрыт (коммит движка `69d522a`): `Two_gatherings` проверяет фильтр | — |
| `ARRAY_REMOVE` (dimzon/h2sharp#24) | провайдер пишет `array_remove` в `TranslateWhere`; не читает ни порт, ни эталон. Код оставлен как есть: если порт реализует функцию, код провайдера заработает без изменений | при закрытии проверить оба движка; если эталон не добавит функцию — решать отдельно (фильтр только через `UNNEST`, колонки отклонять явно) |
| ambient-транзакции (`EnlistTransaction` в ADO-слое порта) | **закрыто**: ADO-слой зачисляет соединение, `AmbientTransactionsSupported => true`, ручные скипы убраны. #96 закрыт: восемь кейсов `SaveChanges_uses_ambient_transaction_with_connectionString` и `SaveChanges_throws_for_suppressed_ambient_transactions` сняты с трейта и зелёные. Соединение, закрытое в одной области, оставляет свою сессию enlistment'у; открытие в другой области получает отдельную сессию, поэтому nested/independent ambient и `BeginTransaction` после закрытого enlisted-соединения тоже зелёные | — |
| байт, который вмещает .NET (`GetByte` в ADO-слое порта) | **закрыто**: #94 исправлен в движке, три теста `H2ByteSubstringTests` сняты с трейта и зелёные | — |
| каталог схем в сессии (`INFORMATION_SCHEMA.SCHEMATA` с параметром) | **закрыто (#95)**: `A_schema_of_its_own_is_made_and_found` — обычный `[Fact]` в `ModelTests` и зелёный | — |
| `$args[n]` compiled query | **закрыто**: ADO-rewriter привязывает элемент массива как отдельный скалярный параметр; два `NorthwindCompiledQueryH2Test` сняты с трейта | сырой SQL `?1[1]` эталон тоже отвергает — не переносить в парсер |
| decimal шире CLR `decimal` | **закрыто**: провайдер оборачивает decimal-деление в `round(..., 28)`; `GetDecimal()` округляет, `GetValue()` возвращает строку по объявленному типу колонки | решение принимать по типу, не по значению |
| перехват транзакций (`H2Transaction.IsolationLevel` отвечает запрошенное, а не фактическое; `H2Connection` отказывает во второй транзакции на соединении) | **закрыто**: ADO-слой теперь отвечает фактический уровень и позволяет suppress-and-begin; оба набора `TransactionInterceptionH2Test` (72 теста) сняты с трейта и зелёные | — |
| in-memory база и тестовое оборудование (`DefaultValuesTest`) | **закрыто**: база живёт в data source и переживает смену соединений; `Can_use_H2_default_values` снят с трейта и зелёный | — |

Постоянное, не требующее пересмотра: квантованный `LIKE`/`ILIKE` через `OR`/`AND`
(у H2 формы нет вовсе), отказ коррелированного `UNNEST` (ограничение эталона),
`BITAND`/`BITOR`/`BITXOR` (порт читает), клиентское `<<`/`>>` (базовый перевод
EF их не отдаёт генератору, как в апстриме).

## Измерение

   | Набор | Прошло | Упало | Всего |
   |---|---:|---:|---:|
   | `H2Sharp.EntityFrameworkCore.FunctionalTests` | 12398 | 0 | 12520 (122 пропущено) |
   | `H2Sharp.EntityFrameworkCore.Tests` | 151 | 0 | 151 |
   | `H2Sharp.EntityFrameworkCore.ModelTests` | 325 | 0 | 325 |
   | **Итого** | **12874** | **0** | **12996** |

   Зелёный гейт: 12874 теста на `net9.0` и `net10.0` против локально собранного
   `H2Sharp 0.1.44-alpha.2` (24 ноября 2026); пакет движка будет опубликован перед
   публикацией провайдера. Провайдер не зависит от соседнего чек-аута движка: он берёт пакет
   с nuget.org. С прошлого замера в гейт вернулись 239 тестов из закрытых кластеров и
   снятых трейтов, затем 150 из `ValueConvertersEndToEndH2Test`, 56 из
   `CustomConvertersH2Test`, 3 unit-теста границ дат, 1668 и затем 1061 из возвращённых
   спецификационных наборов.

Набор `H2Sharp.EntityFrameworkCore.ModelTests` — модель форума: таблицы, наполнение базы
одними и теми же строками, запросы по этим строкам, правка и удаление, типы, функции H2,
сбор строк в один текст, сырые запросы и транзакции. Он зелёный целиком, 325/325.
Разблокированы: row-value
сравнения (#18), `UPDATE` с выражением (#17), параметр-массив и `CONCAT_WS` (#14),
живой `UNNEST` (#23), обрезка пробелов (#12), `EXISTS` (#27), `SAVEPOINT` (#34),
`WITHIN GROUP`/`FILTER` (#35), E-строки (#36), `CAST(агрегат)` в скалярном подзапросе
(#37) и массив из столбцов строки (`ARRAY["s"."Id", "s"."Id" * 10]`). Порог покрытия
держит `tools/CheckCoverage.ps1`: 62 % при текущем замере 62,14 %.

   Разделение по трейту (функциональные тесты):

   | Прогон | Прошло | Упало | Всего |
   |---|---:|---:|---:|
   | `Category!=BlockedByPort` | 12398 | 0 | 12520 (122 пропущено) |
   | `Category=BlockedByPort` | 5645/5437 | 3344/3552 | 9080 (85 пропущено) |

   Документированный `-p:H2SharpProject=...` теперь реализован в
   `src/H2Sharp.EntityFrameworkCore/H2Sharp.EntityFrameworkCore.csproj`: без свойства
   используется пакет `H2Sharp`, со свойством — `ProjectReference` на локальный движок.
   Прогон 11403/11488 повторён против локального движка `C:/Work/h2sharp` после upstream-портов
   и правки ADO-привязки `$args[n]`: functional 10927/10927, model 325/325, unit 151/151
   на net9 и net10.

   Два теста `NorthwindCompiledQueryH2Test` с `$args[1]` сняты с `BlockedByPort`: ADO-слой
   привязывает элемент массива как отдельный скалярный параметр. Эталонный JVM тоже
   отвергает сырой `?1[1]` с `Unknown data type`, поэтому правка — не расширение SQL,
   а .NET-привязка параметров.

   Два decimal-теста `NorthwindSelectQueryH2Test` сняты с `BlockedByPort`. H2 делит
   `DECIMAL` до scale 36, поэтому провайдер оборачивает decimal-деление в `round(..., 28)`.
   На ADO-границе контракт другой: `GetDecimal()` округляет избыточный scale, а
   `GetValue()` возвращает строку для `NUMERIC`, чей объявленный precision/scale шире
   CLR `decimal` — решение принимается по типу колонки, а не по значению.
   `H2ComplianceTest.All_test_bases_must_be_implemented` зелёный: число missing spec
   bases упало со 114 до 0, трейт снят. В компиляцию включены suite
   `ConnectionInterception`, `MigrationsInfrastructure`, `OptimisticConcurrency`,
   `StoredProcedureUpdate`, `StoreValueGeneration`, bulk-updates, Northwind,
   AdHoc, inheritance/relationships, complex navigation, core query и JSON.
   PG-only bases (GearsOfWar, Northwind DbFunctions/Functions, Spatial) переведены
   в `IgnoredTestBases` и их H2-форки остались вне компиляции: они используют
   Npgsql-специфичные `DbFunctions`, `GlobalTypeMapper` и PostGIS.
   Заодно `H2RelationalConnection.CreateDbConnection()` перестал требовать DataSource
   и отдаёт не configured `H2Connection`, как ADO-провайдеры обычно делают до Open.
   В `H2SqlGenerationHelper` подавлены `START TRANSACTION`/`COMMIT` скриптов миграций:
   H2 их не читает, транзакцию держит соединение. Отказы
   `MigrationsInfrastructureH2Test` упали с 8 до 2: `H2Migrator` держит process-wide
   gate вокруг `Migrate`. Осталось `Can_apply_second_migration_in_parallel`
   (sync/async). `StoreValueGenerationH2Test` переведён на H2-базлайны
   (`FINAL TABLE`/`OLD TABLE`) и снят с `BlockedByPort`: 38/38 зелёных на net9 и net10.
   `MONEY` из PG-моделей отображается в `numeric(19,4)`, а предупреждение TPC identity
   подавлено как применимое к H2; отказы BulkUpdates упали с 357/356 до 330/341.
   `JsonTypesH2Test` выведен из компиляции и его bases добавлены в `IgnoredTestBases`:
   18 отказов на PG-специфичные jsonb-значения (`BitArray`, `inet`, GeoJson, infinity).
   `NorthwindMiscellaneousQueryH2Test` снят с `BlockedByPort`: 965/976 зелёных,
   H2-базлайны заменяют PG-формы (`::text`, `LIKE ANY/ALL`, `#`, `~`), BITNOT
   транслируется как функция, а LATERAL/вложенные коррелированные формы пропущены
   с эталонной причиной. `NorthwindGroupByQueryH2Test` полностью переведён на
   H2-базлайны и снят с `BlockedByPort`: 465/491 зелёных на framework.
   LATERAL-формы, GROUP BY по параметризованному ключу и несколько H2-ограничений
   агрегатов пропущены с эталонной причиной.
   Миграции, scaffolding, массивы, конвертеры и
   row value входят в зелёный гейт.

   Закрыто в этом прогоне:
   * `TIME` без точности в H2 2.5 — это `TIME(0)`, поэтому провайдер пишет `time(7)`:
     `TimeOnly`/`LocalTime` переживают round trip.
   * `FORMATDATETIME` в порте принимает .NET-шаблоны (утверждённое расхождение
     движка D-052), поэтому `DateTimeOffset.DateTime` и `.UtcDateTime` переводятся
     с `FFFFFFF`, а не с java-шаблоном `SSSSSSSSS`.
   * `UPDATE`/`DELETE` возвращают число строк через `FINAL TABLE`/`OLD TABLE`; пакет EF
     видит ноль и бросает `DbUpdateConcurrencyException`. Миграции остаются простыми
     `UPDATE`/`DELETE`, потому что их result set никто не читает.
   * Соединение, закрытое внутри ambient-транзакции, оставляет сессию своему enlistment;
     повторное открытие в другой области (nested `RequiresNew`) получает новую сессию.
   * `LazyLoadProxyH2Test` ждал `Charge: 1.0`; провайдер хранит decimal как `numeric(18,2)`,
     и ожидание приведено к `1.00`.
   * Снят устаревший `BlockedByPort` с 183 тестов: миграции, scaffolding и массивы проходят
     в общем прогоне, а не только поодиночке.
   * `ValueConvertersEndToEndH2Test` включён: CLR `string` теперь маппится поверх
     `date`/`time`/`timestamp`/`interval`, а ожидания типов переписаны с PostgreSQL на H2.
     151 тест зелёный.
   * Последний тест этого набора, `("Values")`, открылся после исправления движка:
     `IsDirectQuery` сравнивал текст токена, и quoted `"Values"` принимался за ключевое
     слово `VALUES`; теперь, как в эталоне, решает тип токена. Оракул —
     `FreshIssuesOracleTests.QuotedKeywordColumnNameIsNotTheValuesQuery`.
   * Границы `DateTime`/`DateOnly` больше не пишутся как PostgreSQL `-infinity`/`infinity`:
     H2 2.5 такие литералы отвергает. Провайдер пишет реальные `0001-01-01`/`9999-12-31`
     (и `+00:00` для timestamptz); `H2TemporalInfinityLiteralTest` держит формы, проверенные
     на эталонном jar.
   * `CustomConvertersH2Test` включён: его общая модель конвертировала `DateOnly` через
     `ToShortDateString` и `TimeOnly` в тики, а H2 понимает только ISO-формы. Фикстура
     переопределяет эти конвертеры на `yyyy-MM-dd` и `HH:mm:ss.FFFFFFF`; 56 тестов зелёные,
     4 пропущены по DateTimeOffset.
   * `H2CodeGeneratorTest`, `H2DbContextOptionsExtensionsTest` и
     `H2ModificationCommandBatchTest` вернулись в unit-сборку (+14): удалён тест несуществующего
     `H2NodaTimeCodeGeneratorPlugin`, `H2DataSource.Create` заменён на
     `H2DataSources.GetOrCreate`. Найден и исправлен дефект `H2NetTopologySuiteCodeGeneratorPlugin`:
     он искал несуществующую 5-аргументную перегрузку `UseNetTopologySuite` и падал в static
     initializer; теперь берёт единственный реальный метод. `H2RelationalConnectionTest`
     остаётся исключённым — он целиком построен на PG data source/admin API.
   * `NorthwindWhereQueryH2Test` вернулся (+484 зелёных): PG-базлайны переписаны на H2
     (`count(*)::int` → `CAST(count(*) AS integer)`, кавычки на алиасах,
     `date_part(...)::int` → `year/month/day/hour/minute/second`, `date_trunc` на `DATE`).
     `Where_bitwise_xor` переведён на эталонную функцию `BITXOR`: булевы операнды приводятся
     к 1/0, результат сравнивается с 1. Служебная UDF не нужна — H2 2.5 умеет BITXOR и
     сохраняет NULL. `H2BitwiseFunctionTests` проверяет три-valued семантику на NULL-операндах.
     Пять тестов, ранее помеченных `BlockedByPort`, теперь зелёные:
     `H2ArrayTypeMapping.CreateParameter` материализует object-список/массив в тип, который
     ждёт конвертер, а `H2RowValueTypeMapping` собирает `ValueTuple` из `object[]`.
   * `NorthwindSelectQueryH2Test` разбит и включён (+298): базлайн DayOfWeek переписан под
     `mod(iso_day_of_week(...), 7)`. 24 теории требуют `LATERAL`, 8 — квалифицированную
     внешнюю колонку внутри `DISTINCT`-проекции; обе формы отсутствуют в эталонном H2 2.5,
     поэтому они помечены `Skip` с проверенной причиной. Два decimal-теста позже сняты с
     `BlockedByPort`: провайдер добавил `round(..., 28)`, ADO округляет `GetDecimal()` и
     отдаёт строку из `GetValue()` для широкого `NUMERIC`.
   * Вернулся `NorthwindCompiledQueryH2Test` (+30 зелёных); два теста с `$args[1]` сняты с
     `BlockedByPort`: ADO привязывает элемент массива как скалярный параметр.
   * Вернулись `QueryNoClientEvalH2Test` (+13) и его Northwind-фикстура; набор проверяет,
     что провайдер не уходит в клиентскую оценку там, где EF Core 9 её запрещает.
   * `H2OperatorsProceduralQueryTest` добавлен в compliance-список: два процедурных теста в
     EF Core 9 закомментированы в базовом классе, поэтому у класса нет запускаемых методов.
   * Вернулись `QueryFilterFuncletizationH2Test` (+28) и `QueryBugsTest` (+1). Первый ушёл из
     compliance-списка отсутствующих баз; его переопределённый тест проверяет H2-перевод
     списка в query filter вместо клиентской ошибки EF.
   * Реализованы ещё два базовых compliance-теста: `H2ApiConsistencyTest` (+18) и `H2LoggingTest`
     (+13). Оба ушли из списка отсутствующих баз compliance-теста вместе с
     `UpdateSqlGeneratorTestBase`; сам `H2ComplianceTest` остаётся красным по замыслу.
   * Добавлен `H2UpdateSqlGeneratorTest` (+14) — реализован базовый тест
     `UpdateSqlGeneratorTestBase`, которого не хватало в compliance-списке. Базлайны показывают
     H2-форму без `OUTPUT`: `FINAL TABLE (INSERT/UPDATE ...)` и `COUNT(*) FROM OLD TABLE (DELETE ...)`.
     Compliance-тест остаётся красным, но список отсутствующих баз сократился.
   * Ещё три Northwind-набора (+305): `NorthwindSqlQueryH2Test`, `NorthwindSetOperationsQueryH2Test`
     и `NorthwindJoinQueryH2Test`. Базы SQL переписаны под H2 (`"o"."CustomerID"` вместо
     `o."CustomerID"`, `CAST(NULL AS integer)` вместо `NULL::int`); 8 теорий Join пропущены —
     им нужен `LATERAL`, которого в эталоне нет. Все включённые Northwind-классы объединены
     в xUnit-коллекцию `Northwind`: общий TestStore держит одно соединение, и параллельные
     классы отменяли чужие запросы (`Statement was canceled or the session timed out`).
   * Northwind больше не грузится из PostgreSQL-дампа `Northwind.sql` (`SET statement_timeout`,
     `CREATE EXTENSION`, `SERIAL`, `bytea`, 3202 `E'\\x…'`-литерала — эталон отказывает).
     `H2NorthwindTestStoreFactory` использует общий store из `H2TestStoreFactory` и даёт EF
     создать схему из `NorthwindH2Context`, после чего `NorthwindData.SeedAsync` грузит строки.
     Включены 6 наборов (+233): AsNoTracking, AsTracking, ChangeTracking, Navigations,
     QueryFilters, QueryTagging. Одна теория Navigations пропущена: H2 не разрешает
     квалифицированную внешнюю колонку внутри `DISTINCT`-подзапроса (проверено на jar).
     Остальные Northwind-наборы пока держат PG-базлайны (`::`, `date_part`, LATERAL, JSON).
   * `ExecutionStrategyTest` включён (+37): восстановлены stateful-fakes из истории репозитория —
     `TestRelationalCommand` внутри `TestRelationalCommandBuilderFactory` и
     `TestRelationalTransaction`/Factory. Мок-фреймворк не понадобился: тест кастует сервис к
     `TestH2Connection` и читает очереди отказов/счётчики, а команда должна делегировать
     настоящему `RelationalCommand`. Ожидаемый текст retry-сообщения заменён на
     `Simulated failure` вместо постгресового `PostgresException … XX000`.
   * Три дешёвых разблокирующих исправления: `H2SharpException` получил публичный
     конструктор (fake `TestH2Connection` теперь компилируется); `H2HistoryRepository`
     больше не пишет постгресовый `LOCK TABLE … IN ACCESS EXCLUSIVE MODE`, а берёт
     блокировку `MERGE` по строке служебной таблицы (H2 2.5 такого `LOCK TABLE` не знает);
     CHECK-ограничения читаются из `CHECK_CONSTRAINTS`, кладутся на таблицу аннотацией
     `H2:CheckConstraint:<name>` и превращаются в `HasCheckConstraint(...)` в сгенерированном
     коде. `MigrationsInfrastructureH2Test` включён: параллельные тесты теперь идут
     через отдельные соединения и process-wide gate в `H2Migrator`; два теста второй
     миграции ещё красные.
   * Каталог вынесен в `IH2CatalogReader`: 12 параметризованных запросов к `INFORMATION_SCHEMA` и
     типизированные строки живут в одном месте, скаффолдер и `H2DatabaseCreator` читают их,
     а не повторяют SQL. Три unit-теста `H2CatalogReaderTests` держат форму строк
     (колонки, identity, ключи, индексы, sequence, enum, FK), проверенную на эталонном jar.
   * Enum-колонка при scaffolding больше не пропадает молча: `H2DatabaseModelFactory` пишет
     `EnumColumnSkippedWarning` с именем `PUBLIC.table.column`, как это делает Npgsql.
     EF Core не умеет scaffold-ить тип базы как CLR-enum, поэтому свойство заводится вручную.
   * Возвращены в компиляцию и зелёные 1668 тестов: `FieldsOnlyLoadH2Test`,
     `IncludeOneToOneH2Test`, `NullKeysH2Test`, `CharacterQueryH2Test`,
     `ManyToManyLoadH2Test`, `ManyToManyTrackingH2Test`, `ManyToManyFieldsLoadH2Test`,
     `OwnedEntityQueryH2Test`, `OwnedQueryH2Test` и `SharedTypeQueryH2Test`. В
     `OwnedQueryH2Test` две теории пропущены: H2 2.5 отвергает qualified `ORDER BY`
     столбец, когда в `WHERE` есть коррелированный `DISTINCT`-подзапрос; проверено на
     эталонном jar, поэтому это правило H2, а не дефект порта.
   * Вторая партия возвращённых наборов — ещё 1061 тест: `ManyToManyQueryH2Test`,
     `ManyToManyNoTrackingQueryH2Test`, `CompositeKeysQueryH2Test`,
     `CompositeKeyEndToEndH2Test`, `TableSplittingH2Test`, `TPTTableSplittingH2Test`,
     `EntitySplittingH2Test`, `SeedingH2Test`, `MusicStoreH2Test`,
     `ConferencePlannerH2Test`, `ComputedColumnTest`, `DataAnnotationH2Test`,
     `MonsterFixupChangedChangingH2Test`, `NotificationEntitiesH2Test`,
     `OverzealousInitializationH2Test`, `CommandInterceptionH2Test`,
     `MaterializationInterceptionH2Test`, `SaveChangesInterceptionH2Test` и
     `NonSharedModelUpdatesH2Test`. В `ManyToMany*` шесть теорий пропущены: им нужен
     `LATERAL`, которого в H2 2.5 нет. `ManyToManyQueryH2Fixture` вызывает базовую
     настройку модели, иначе unidirectional-навигации не валидируются; `SeedingH2Test`
     явно конфигурирует провайдера в `OnConfiguring`.

   История: на движке `17c9f68` было 434 и 4888; коммит `f56ca4c` дал 350 переходов
   красное→зелёное и ни одного обратного. Правки провайдера добавили 288, перенос трейта
   на методы — 826, обход `Prepared.update` — 3799, эти прогоны — ещё 446,
   возвращённые наборы — 1668 и 1061.

Один тест красный по замыслу и помечен трейтом, а не спрятан.

| Тест | Причина | Кто чинит |
|---|---|---|
| `H2ComplianceTest.All_test_bases_must_be_implemented` | EF Core требует, чтобы провайдер реализовал общие базовые тесты. Не реализованы: `ApiConsistencyTestBase`, `BuiltInDataTypesTestBase`, `ComplexTypesTrackingTestBase`, `CompositeKeyEndToEndTestBase` и другие. Тест красный **по замыслу** — это список оставшейся работы | мы: портируем базовые тесты под H2 |

### Что было переписано, потому что провайдер прав, а тест ждал PostgreSQL

| Категория | Тестов | Что изменено |
|---|---:|---|
| `H2MigrationsSqlGeneratorTest` | 14 | ожидаемый DDL под H2: `integer` вместо `serial`, `varchar` вместо `character varying`, `char` вместо `character`, `clob` вместо `text`, схема тоже кавычится |
| `H2SqlGenerationHelperTest` | 6 | правило кавычения: у нас всегда кавычим, у Npgsql — выборочно |
| `H2HistoryRepositoryTest` | 4 | `varchar` вместо `character varying`, `CREATE SCHEMA "my"` |
| `H2MetadataExtensionsTest`, `H2ValueGenerationStrategyConventionTest` | 3 | дефолтная стратегия — `IdentityByDefaultColumn`, а не `SerialColumn`: в H2 нет псевдотипа `serial` |
| `H2ModificationCommandBatchFactoryTest` | 1 | батч считает строки обычным способом, а не из wire-протокола |
| `H2PostgresModelFinalizingConventionTest` | 1 | `xmin` — столбец PostgreSQL, в H2 такого нет; тест убран |
| `H2QuerySqlGenerator` | 1 | приведение типа пишется как `CAST(x AS y)`. Эталон также принимает `x::y`, включая `count(*)::int`; прежнее утверждение об отсутствии этой формы было неверным |
| `H2MigrationsSqlGenerator`, `H2HistoryRepository` | 6 | блок `DO $EF$` — анонимный блок PL/pgSQL — заменён на H2'овское `CREATE SCHEMA IF NOT EXISTS` и на условие внутри самой вставки: `INSERT … SELECT … WHERE NOT EXISTS (…)` |
| `H2IntervalTypeMapping` | — | литерал интервала без дней (`CAST('10:09:08.007' AS INTERVAL DAY TO SECOND)`) эталон отвергает: «невозможно преобразовать строку "10:09:08.007" в тип "INTERVAL"». Провайдер перешёл на фасад `Dialect.AppendInterval`: `INTERVAL '0 10:09:08.007' DAY TO SECOND` — типизированный литерал с числом дней (ноль включительно). Проверено на эталоне: формы с днями принимаются, включая `-0`, `-1`, `11 23:59:59.999999` |
| `TransactionH2Test.SaveChanges_can_be_used_with_no_savepoint` | 1 | тест ждал PostgreSQL: там неудачный оператор переводит транзакцию в aborted и COMMIT выбрасывает всё. H2 следует стандарту — оператор неудачен, транзакция нет, — проверено на эталонном jar: `COMMIT` проходит, `MAX(ID)` — 77. Ожидание переписано под это |
| `TransactionH2Test` (семейство ambient/enlisted) | 3 | **закрыто**: сессия закрытого enlisted-соединения остаётся у своего enlistment'а, а открытие в другой области получает отдельную сессию; `nested`, `independent commits` и `begin after enlisted close` зелёные |

## Плагин NodaTime: переводы членов даты и времени

У апстримного провайдера NodaTime — большой набор переводчиков; у форка долгое время были только
маппинги типов, и LINQ по NodaTime-свойствам уходил в клиентский вычисление. Теперь плагин приносит
и переводы: `H2NodaTimeMemberTranslator` подключается через `IMemberTranslatorPlugin` и переводит
члены `Year`, `Month`, `Day`, `DayOfYear`, `DayOfWeek`, `Hour`, `Minute`, `Second` в функции H2
(`YEAR`, `MONTH`, `DAY`, `DAY_OF_YEAR`, `ISO_DAY_OF_WEEK`, `HOUR`, `MINUTE`, `SECOND`).

Границы перевода — не прихоть, а семантика:

* **`LocalDate`, `LocalTime`, `LocalDateTime` переводятся**: их колонки (`date`, `time`,
  `timestamp`) несут поля такими, как их записали, и функции частей отвечают ровно это.
  `DayOfWeek` у NodaTime — это `IsoDayOfWeek`, неделя с понедельника, то есть ровно ответ
  `ISO_DAY_OF_WEEK` без арифметики (у BCL `DayOfWeek` неделя с воскресенья, и там берётся
  остаток по семи).
* **`OffsetDateTime` и `ZonedDateTime` не переводятся.** Их колонка — `timestamp with time zone`,
  и функции частей H2 читают такое значение в зоне сессии, а NodaTime спрашивает поля смещения
  самого значения. EF Core честно отвечает «не переводится», а не выдаёт поля чужой зоны.
* **Арифметика (`PlusDays`, `Period`), `Interval` и прочее** — не переведено; это работа под
  будущие примеры.

Материализация тоже поправлена: `H2NodaTimeTypeMapping` теперь читает `date`-колонку как
`GetDateTime` → `DateOnly.FromDateTime` и `time`-колонку как `GetFieldValue<TimeSpan>` →
`TimeOnly.FromTimeSpan` — так же, как это делают собственные маппинги провайдера, — потому что
движок отвечает `DateTime` из `date` и `TimeSpan` из `time`, а конвертер плагина ждёт
именно `DateOnly` и `TimeOnly`.

Живой пример — `ForumNodaTimeExamples` в `ModelTests`: расписание с колонками всех трёх
переводимых типов, запросы полей по живым строкам и тест, держащий EF за отказ по
`OffsetDateTime.Year`. Полный порт спецификационного набора NodaTime (`NodaTimeQueryH2Test`,
две тысячи строк) лежит непереписанным с диалектом PostgreSQL (`DATE '…'`, `INTERVAL 'P1M'`,
диапазоны) и в solution не входит.

## Спецификационный набор: возвращены ещё шесть семейств

Следующая партия исключённых файлов вернулась в компиляцию и зелёная:

| Семейство | Тестов | Что поправлено |
|---|---:|---|
| `WithConstructorsH2Test` | 41 | — |
| `KeysWithConvertersH2Test` | 47 | — |
| `BatchingTest` | 12 | `xid`/`xmin` (колонки PostgreSQL) убраны из модели; `Owner.Id` получил `ValueGeneratedOnAdd`; частичный индекс убран (в H2 его нет, а уникальность и так даёт NULL-distinct) |
| `TwoDatabasesH2Test` | 4 | `DummyConnectionString` — строка подключения H2 (`Data Source=...;Storage=Memory`) вместо `Host=...;Database=...` |
| `ConnectionSpecificationTest`, `ExistingConnectionTest` | 12 | северный ветер в этих тестах был только полигоном для проверки подключений; H2-скрипта Northwind всё ещё нет, поэтому тесты переведены на собственную заводскую базу с двумя клиентами. Соединения под тестом берутся из data source стора: отдельное соединение — отдельная база |

Красное из этой партии не спрятано, а переведено в трейт `BlockedByPort`. Из двух дефектов
первый закрыт обходом в провайдере, второй остался:

| Семейство | Тестов | Симптом порта |
|---|---:|---|
| `FieldMappingH2Test`, `PropertyValuesH2Test` | 340 | ~~«Not ported yet: Prepared.update»~~ — обойдено: пакет больше не смешивает виды операторов, оба набора зелёные. Сам дефект движка описан в `H2Command.ExecuteDbDataReader` (`Data/H2Command.cs:262`) и заведён как [h2sharp#67](https://github.com/dimzon/h2sharp/issues/67) |
| `ConvertToProviderTypesH2Test` | 32 | **закрыто**: причина была не в `ALTER SEQUENCE`, а в `TIME` без точности (`TIME(0)`) и java-шаблоне `FORMATDATETIME`; провайдер пишет `time(7)` и .NET-шаблон. Все 29 зелёные, 3 пропущены по DateTimeOffset |

`ValueConvertersEndToEndH2Test` и `CustomConvertersH2Test` остаются вне компиляции: их модели
назначают свойствам типа `string` store-тип `date`/`time`, а наш `H2TypeMappingSource` для CLR
`string` поверх `date`/`time` mapping не имеет — валидация модели отказывает до генерации SQL.
Это не пробел движка, а решение маппинга, которое надо принять отдельно.

## Транзакционное семейство вернулось в сборку

`TransactionH2Test` (111 тестов) и `TransactionInterceptionH2Test` исключались из компиляции
со времён форка. `SAVEPOINT` порт давно читает, и семейство вернулось: сто одиннадцать тестов
зелёные на обоих TFM. По пути исправлено в провайдере:
* **Откат к savepoint пишется формой H2.** EF Core 9 сам формирует текст savepoint-команд
  через `ISqlGenerationHelper`, и дефолт — `ROLLBACK TO "name"` (форма PostgreSQL, где слово
  `SAVEPOINT` необязательно). Грамматика H2 2.5 требует `ROLLBACK [WORK] TO SAVEPOINT name`:
  `H2SqlGenerationHelper.GenerateRollbackToSavepointStatement` переписан, и откат к точке
  сохранения доезжает до движка. `SAVEPOINT x` и `RELEASE SAVEPOINT x` дефолтные формы уже
  читает.
* **Reseed тестового стора.** `H2TestStore.CleanAsync` делал `DROP ALL OBJECTS` и возвращался;
  `ReseedAsync` спеки чистит и сеет одним вдохом, и посев падал на «Table not found (this
  database is empty)». Теперь `CleanAsync` пересоздаёт схему через `EnsureCreated` после
  очистки.
* **Ambient-транзакции.** ADO-слой порта зачисляет соединение в ambient-транзакцию
  (`H2Connection.EnlistTransaction`), и провайдер объявляет `AmbientTransactionsSupported =>
  true`, как у SQL Server: EF Core при ambient-транзакции не заводит свою, а работа идёт в той,
  что завели снаружи. #96 закрыт. Закрытое внутри области соединение оставляет свою сессию
  прежнему enlistment'у, а открытие в другой области (`RequiresNew`) получает новую сессию;
  поэтому nested/independent ambient и `BeginTransaction` после enlisted-закрытия зелёные.
* **Перехват транзакций** закрыт: ADO-слой отвечает фактический уровень изоляции и позволяет
  suppress-and-begin; `TransactionInterceptionH2Test` (72 теста) зелёный.

Заодно из генератора миграций убрана таблица системных столбцов PostgreSQL (`xmin`, `ctid` и
прочие): H2 системных столбцов не имеет, а колонка пользователя с таким именем молча
выпадала бы из миграций.

### Что нашёл набор `ModelTests` и что в провайдере из-за этого исправлено

Новые примеры приложения — `ForumSearchExamples`, `ForumCursorAndBulkExamples`,
`ForumPreferenceExamples`, `ForumStatisticsExamples`, `ForumKeyGenerationExamples`,
`ForumCalendarExamples`. Они показывают поиск с пользовательским текстом, пакетную
модерацию, составной курсор, подписки-массивы, JSON-настройки, отчёты, HiLo и календарь.
Запросы выполняются по данным приложения; там, где порт пока не поддерживает SQL,
`ReferenceForum` исполняет команду EF и проверяет реальные строки на H2 JVM.
Внутренние проверки коллекционных конвертеров перенесены в обычный модульный набор
`Storage/H2CollectionConversionTests.cs`, обе проверки сохранены.

Эти примеры выявили и исправили дефекты провайдера:

* `IndexOf(char)` — символ преобразуется к строковому аргументу `LOCATE`.
* HiLo — `NEXT VALUE FOR` вместо `nextval` со строкой кавыченного имени.
* Проекции `DateOnly`/`TimeOnly` — корректное чтение ADO-значений и учёт конвертера свойства.
* `DayNumber`/`FromDayNumber` — `DATEDIFF`/`TIMESTAMPADD`: вычитание дат в H2 возвращает интервал,
  а не целое число, как раньше предполагал переводчик.
* Член, читаемый с результата метода (`AddHours(1).Hour`) — перевод `TIMESTAMPADD` не нёс
  type mapping, и член-переводчик отказывал «could not be translated»; mapping теперь
  передаётся, и `AddYears/AddHours` с последующим членом работают.
* JSON-документы — UTF-8 binary параметр и JSON-литерал вместо двойного кодирования как строки;
  чтение документа после материализации выполняется в .NET, JSON path в SQL не добавлялся.
* `JSON_ARRAYAGG` — поддержан документированный `ORDER BY` внутри скобок;
  `JSON_OBJECTAGG` разделяет ключ и значение словом `VALUE`.
* `Skip`/`Take` массива — усечение границ и пустой массив; H2 `ARRAY_SLICE` с границей за
  пределами массива иначе возвращает `NULL`, что не соответствует LINQ.
* Разность меток времени под `.TotalDays/.TotalHours/.TotalMinutes/.TotalSeconds/.TotalMilliseconds` —
  `DATEDIFF` с тем же юнитом вместо `-`: порт не читает операции даты и времени, а `DATEDIFF`
  читает. `DATEDIFF` считает целые границы юнита, а не точную дробь: 90 минут — это 1 целый
  час, а не 1.5. На выровненных строках форума ответы совпадают.
* `string.Contains(x, OrdinalIgnoreCase)` — `ILIKE` с экранированным шаблоном; столбец-шаблон
  отклоняется, потому что экранировать его нечем.
* `EF.Functions.Equal` для кортежей — сравнение строк `(a, b) = (c, d)`; порт его пока не
  читает (dimzon/h2sharp#18).
* `Any(f => f == v)` над массивом — `ARRAY_CONTAINS`; кванторы без формы H2 (`All`, неравенство,
  `FirstOrDefault` на пустом) отклоняются с объяснением вместо SQL, который эталон отвергает.
* `First()` над массивом — `arr[1]`: выход за границу падает и там, и там.
* Тип массива в DDL, заданный скобками PostgreSQL (`smallint[]`), переписывается в форму H2
  (`smallint ARRAY`): нормализация в `H2ArrayTypeMapping` и в `H2MigrationsSqlGenerator`.
  Порт читает `ARRAY` (коммит движка «Arrays end to end»), и фикстура с `byte[]` + `[Column(TypeName="smallint[]")]`
  создаётся и работает; до этого DDL давал `smallint[][]` и падал.
* `CreateTables` — команды DDL исполняются напрямую, по одной, а не батчем в одной транзакции:
  порт не видит таблицу, созданную в той же транзакции (FK следующей команды отвечает
  «Table not found»). Соединение после создания не закрывается: память-база умирает с
  последним соединением, и закрытие единственного соединения стёрло бы только что
  созданную схему.

Набор `H2Sharp.EntityFrameworkCore.ModelTests` спрашивает у провайдера живую базу: модель форума,
наполнение, запросы, правку, типы, функции H2, сбор строк в один текст, сырые запросы. Он нашёл
в провайдере дефекты, которые спецификационный набор не видит: тот проверяет форму SQL, а набор
спрашивает результат.

| Что | Было | Стало |
|---|---|---|
| тип массива в запросе | `integer[]` — скобки PostgreSQL, и `H2ArrayTypeMapping` на этом падал со своей же проверкой | `integer ARRAY` — форма H2 2.5 |
| `IndexOf` | `LOCATE(строка, искомое)` — аргументы переставлены, и порт отвечает «-1» | `LOCATE(искомое, строка)`, как в эталоне: `LOCATE(searchString, string)` |
| отрицание | `(0 - x)` — порт не читал отрицание выражения вовсе: `SELECT -N FROM T` → «expected number» | `-x` — знак перед членом, и порт его читает |
| склейка строк | `a \|\| b` | `CONCAT(a, b)` — обе формы есть в H2 2.5, но смысл другой: трубы дают `NULL`, если часть `NULL`, а `CONCAT` части `NULL` игнорирует, как строковый `+` в .NET |
| склейка массивов | `a \|\| b` | `ARRAY_CAT(a, b)` — функция H2 2.5 |
| исключающее или | `a # b` | `BITXOR(a, b)` — функция H2 2.5 |
| строковый агрегат | `string_agg(x, ', ' ORDER BY y)` — порядок внутри скобок, и H2 2.5 такой формы не берёт | `LISTAGG(x, ', ') WITHIN GROUP (ORDER BY y)` — порядок отдельной фразой |
| агрегат массива | `array_agg(x)` | `ARRAY_AGG(x)` — имя H2 2.5 |
| агрегат json | `json_agg(x)`, `json_object_agg(k, v)` | `JSON_ARRAYAGG(x)`, `JSON_OBJECTAGG(k, v)` — имена H2 2.5 |
| псевдоним столбца | `AS` — потому что порт не читал псевдоним без него | `AS` — как в EF Core; писать без `AS` EF Core не умеет: разделитель он пишет без пробела |
| `UNLOGGED TABLE` | `CREATE UNLOGGED TABLE` — слова `UNLOGGED` у H2 2.5 нет, и таблица не создаётся | слова нет — и в API провайдера (`IsUnlogged` убран), и в DDL: провайдер пишет обычную `CREATE TABLE` |
| `INTERLEAVE IN PARENT` | `CREATE TABLE … INTERLEAVE IN PARENT …` — это фича CockroachDB, а не H2 | её нет и в API провайдера (`UseCockroachDbInterleaveInParent` убран), и в DDL |
| скаффолдинг: список таблиц | пустой список запрошенных таблиц не значил «все», и модель получалась пустой | пустой список значит «все», как в EF Core |
| скаффолдинг: номера столбцов каталога | номера столбцов читались со сдвигом, и `IS_NULLABLE` читался вместо `IS_IDENTITY` | номера соответствуют запросу |
| скаффолдинг: пустые столбцы каталога | `GENERATION_EXPRESSION` и `IDENTITY_GENERATION` читались как строки, а они пустые | читаются как то, что может быть пустым |
| скаффолдинг: ключи и индексы | у первичного ключа не читались столбцы, индексы не читались вовсе | читаются и то и другое |
| `EnsureCreated` | `HasTables()` для схемы по умолчанию всегда отвечал «нет», и второй `EnsureCreated` пытался создать таблицы заново | `HasTables()` смотрит в каталог схемы по умолчанию |

**Сбор строк в один текст.** Набор спрашивает у провайдера то, что спрашивает приложение:
тела ответов одного автора, длинные раньше коротких, и тела видимых ответов, короткие раньше
длинных. Это два сбора в одном запросе, у каждого свой порядок и свой фильтр:

```sql
SELECT "p"."TopicId",
       COALESCE(LISTAGG("p"."Body", ', ') WITHIN GROUP (ORDER BY "p"."Body" DESC) FILTER (WHERE "p"."AuthorId" = 1), '') AS "First",
       COALESCE(LISTAGG("p"."Body", ', ') WITHIN GROUP (ORDER BY "p"."Body")       FILTER (WHERE "p"."AuthorId" = 2), '') AS "Second"
FROM "Posts" "p"
GROUP BY "p"."TopicId"
```

Эталон H2 2.5 такую форму принимает и отвечает тем, что .NET. Порт с тех пор читает и
`WITHIN GROUP`, и `FILTER` у агрегата (пробелы 17 и 18 закрыты коммитом `69d522a`), и
`Two_gatherings_in_one_query_take_their_own_order_and_filter` живёт; остальные сборы — без
порядка и без фильтра — идут через `LISTAGG` и идут зелёными.

## Формы типов: что провайдер пишет и почему именно так

Провайдер унаследовал от Npgsql не только форму, но и *названия* вещей, которых в H2 2.5 нет.
Сверка с эталоном (`h2-2.5.259-SNAPSHOT.jar` на JVM) и с исходниками оригинала
(`reference/h2/h2/src/main/org/h2/command/Parser.java`, `value/DataType.java`) показала:

| Что | Было (PostgreSQL) | Стало (H2 2.5) | Что говорит эталон |
|---|---|---|---|
| тип массива | `integer[]` | `integer ARRAY` | `CREATE TABLE t (x integer[]);` → `Syntax error … expected ")"` |
| литерал массива | `ARRAY[1,2]::integer[]` | `ARRAY[1,2]` | `::` есть, но тип массива H2 — `integer ARRAY`; здесь тип следует из элементов |
| литерал времени с зоной | `TIMESTAMPTZ '…'` | `TIMESTAMP WITH TIME ZONE '…'` | `SELECT TIMESTAMPTZ '…'` → `Syntax error` |
| литерал времени с зоной (время) | `TIMETZ '…'` | `TIME WITH TIME ZONE '…'` | то же |
| литерал интервала | `INTERVAL '1 2:3:4'` | `CAST('1 2:3:4' AS INTERVAL DAY TO SECOND)` | `Cannot parse "INTERVAL" constant "1 2:3:4"`: у H2 литерал интервала несёт квалификатор, а приведение текста к тому же типу даёт то же значение |
| литерал перечисления | `'a'::my_enum` | `CAST('a' AS my_enum)` | обе формы принимает эталон; провайдер пишет стандартный `CAST` |
| часть даты | `date_part('year', x)` | `year(x)` | `Function "DATE_PART" not found`: часть даты в H2 берётся своей функцией |
| день недели | `floor(date_part('dow', x))::int` | `mod(iso_day_of_week(x), 7)` | ISO нумерует неделю с понедельника, .NET — с воскресенья |
| «содержит» | `strpos(x, y) > 0` | `locate(y, x) > 0` | `strpos` у H2 нет, а у `LOCATE` аргументы в обратном порядке |
| текст | `text` | `clob` | `TEXT` у H2 2.5 есть, и это синоним `CHARACTER VARYING` (`DataType.java`: `TINYTEXT`, `TEXT`, `MEDIUMTEXT`, `LONGTEXT`, `NTEXT` — в списке псевдонимов `VARCHAR`), а `CLOB` — отдельный тип `CHARACTER LARGE OBJECT`. Провайдер пишет `clob` намеренно: это тип H2 для текста без границы |
| перечисление | `CREATE TYPE "Mood" AS ENUM ('Happy', 'Sad')` | то же | перечисления у H2 2.5 есть, и форма именно такая: `ENUM` со списком значений (`Parser.parseEnumType`), и `CREATE TYPE` — это домен типа (`CREATE DOMAIN`), который потом берут как тип столбца. Эталон берёт и `CREATE TYPE "Mood" AS ENUM ('Happy','Sad')`, и `CREATE TABLE t (a "Mood")` |
| метка перечисления | `ALTER TYPE "Mood" ADD VALUE 'Angry'` | `ALTER TYPE "Mood" ADD VALUE 'Angry'` | эталон принимает эту форму, и метка начинает работать в `CAST` — в том числе когда тип уже занят столбцом с данными. Отвергает он всё остальное: `DROP VALUE`, `RENAME VALUE`, `RENAME TO`, `BEFORE`/`AFTER` (`expected "., ADD"`). `ALTER DOMAIN m ADD VALUE 'c'` тоже отвергается — домен и перечисление не одно и то же. Провайдер дописывает добавленную метку в конец и отказывает на удаление и переименование |
| склейка строк | `a \|\| b` | `CONCAT(a, b)` | обе формы есть, но `CONCAT` части `NULL` игнорирует: `CONCAT('a', NULL)` — это `a`, а `'a' \|\| NULL` — это `NULL` |
| склейка массивов | `a \|\| b` | `ARRAY_CAT(a, b)` | эталон принимает и оператор, и функцию; провайдер использует функцию |
| исключающее или | `a # b` | `BITXOR(a, b)` | решётки у H2 2.5 нет |
| строковый агрегат | `string_agg(x, ', ')` | `LISTAGG(x, ', ')` | `LISTAGG` — имя H2 2.5; `string_agg` — имя режима совместимости, и эталон переписывает его в `LISTAGG` |
| тип JSON | `jsonb` | `json` | у H2 2.5 один тип JSON, и `jsonb` у него нет вовсе. Провайдер раньше писал `jsonb` — и падал на своей же колонке |
| шаг в JSON | `jsonb_set(col, '{a,b}', value)` | **нет такой формы** | `jsonb_set` — функция PostgreSQL; у H2 2.5 нет способа менять часть документа JSON |
| внутри в JSON | `@>`, `<@`, `?`, `jsonb_typeof` | **нет таких функций** | у H2 2.5 есть только тип JSON и четыре конструктора (`JSON_ARRAY`, `JSON_ARRAYAGG`, `JSON_OBJECT`, `JSON_OBJECTAGG`) |
| шаг в JSON | `col -> 'a' ->> 'b'` | **нет такой формы** | операторы `->`, `->>`, `#>` и `#>>` — PostgreSQL'овские, а тип JSON у H2 2.5 хранит документ как текст |
| шаг в JSON как таблица | `jsonb_to_recordset(json)` | **нет такой функции** | `jsonb_to_recordset` — функция PostgreSQL; у H2 2.5 её нет |
| геометрия | `geography` | `geometry`, `GEOMETRY(POINT, 4326)` | у H2 2.5 нет `GEOGRAPHY`, но есть SRID: `Parser.parseGeometryType` читает его после запятой, а литерал EWKT `GEOMETRY 'SRID=4326;POINT (1 2)'` сохраняет систему координат |
| агрегат массива | `array_agg(x)` | `ARRAY_AGG(x)` | имя H2 2.5 |
| агрегат json | `json_agg(x)`, `json_object_agg(k, v)` | `JSON_ARRAYAGG(x)`, `JSON_OBJECTAGG(k, v)` | имена H2 2.5 |
| порядок строк в агрегате | `ORDER BY` внутри скобок | `WITHIN GROUP (ORDER BY …)` после скобок | `LISTAGG(x, ', ' ORDER BY y)` у H2 2.5 — синтаксическая ошибка; порядок идёт отдельной фразой |
| фильтр строк в агрегате | `FILTER (WHERE …)` | `FILTER (WHERE …)` | эталон берёт: `COUNT(*) FILTER (WHERE x = 1)` |
| сейчас | `now()` | `current_timestamp` | `NOW()` у H2 — другое имя `LOCALTIMESTAMP`, то есть время без зоны |

Проверено на эталоне: `SELECT YEAR(TIMESTAMP '2020-01-02 10:20:30')` → 2020,
`SELECT MOD(ISO_DAY_OF_WEEK(TIMESTAMP '2020-01-05 00:00:00'), 7)` → 0 (воскресенье),
`SELECT LOCATE('bc', 'abcd')` → 2, `SELECT EXTRACT(EPOCH FROM INTERVAL '1 2:3:4' DAY TO SECOND)` → 93784.

`EXTRACT` в H2 2.5 — синтаксис, а не вызов функции: `EXTRACT(field FROM value)`. Вызов
`extract('epoch', x)` эталон отвергает («expected "FROM"»). Поэтому генератор запросов
провайдера пишет `EXTRACT` синтаксисом, а полем может быть и слово, и строка в кавычках —
эталон берёт оба.

**Метка перечисления дописывается в конец.** У `ALTER TYPE … ADD VALUE` нет `BEFORE`/`AFTER` —
эталон их отвергает, — поэтому метка, вставленная в середину модели, окажется в конце: модель
говорит `Happy, Angry, Sad`, а база — `Happy, Sad, Angry`. Порядок меток в H2 значим для
сравнения, так что это расхождение, а не косметика: провайдер пишет документированную форму,
а порядок остаётся за базой (`Alter_enum_add_label_in_middle`; то же на уровне генератора —
`H2MigrationsSqlGeneratorTest.Alter_enum_add_label_in_middle_is_appended_at_end`, и он
зелёный, потому что движок для проверки текста команд не нужен).

**Что из этого провайдер может, а что ждёт движок.** Порт читает и тип `ARRAY`, и литерал
массива (коммит движка «Arrays end to end» `f24e67e`): таблица с массивом создаётся, а
`ForumCollectionExamples` проверяет списки по живым строкам. Остальное движок тоже читает:
`LOCATE`, `MOD`, `YEAR`, `MONTH`, `DAY`, `DAY_OF_YEAR`, `HOUR`, `MINUTE`, `SECOND`,
`ISO_DAY_OF_WEEK`, `EXTRACT` у него есть.

## Массивы: что провайдер пишет, а чего у H2 2.5 нет вовсе

Набор тестов массивов (`ArrayArrayQueryTest`, `ArrayListQueryTest`) вернулся в сборку: он был
исключён из неё вместе с файлами, которые «ещё не перенесены». Формы в нём переписаны на H2'овские,
а то, для чего у H2 ответа нет, из него убрано.

| Что | Форма PostgreSQL | Форма H2 2.5 | Что говорит эталон |
|---|---|---|---|
| «содержит элемент» | `arr @> ARRAY[item]` | `array_contains(arr, item)` | операторов-вместимости у H2 нет; `ARRAY_CONTAINS(ARRAY[1,NULL], NULL)` → TRUE, как и .NET'овский `Contains` |
| «содержит null» | `array_position(arr, NULL) IS NOT NULL` | `array_contains(arr, NULL)` | то же |
| «не содержит null» | `array_position(arr, NULL) IS NULL` | `not array_contains(arr, NULL)` | то же |
| элемент по номеру | `arr[1]` | `arr[1]` | у H2 такая же нумерация с единицы |
| байт двоичного по номеру | `get_byte(b, i)` | `BITAND(CAST(CAST(SUBSTRING(b, i + 1, 1) AS TINYINT) AS INTEGER), 255)` | `TINYINT` читает один байт со знаком; расширение до `INTEGER` и маска 255 сохраняют диапазон CLR `byte` 0–255. Проверено на всех 256 значениях в порту и сгенерированным SQL на JVM |
| срез массива | `arr[1:2]` | `array_slice(arr, 1, 2)` | синтаксиса среза у H2 нет: `expected "[, ., ::, AT, FORMAT, …"` |
| массив из подзапроса | `= ANY ((SELECT …)::integer[])` | `= ANY (CAST((SELECT …) AS integer ARRAY))` | без приведения H2 считает правую часть запросом, а не массивом: «Values of types "INTEGER" and "INTEGER ARRAY[3]" are not comparable» |
| квантованное сравнение | `x = ANY (arr)` | `x = ANY (arr)` | форма общая: `SELECT 1 = ANY(ARRAY[1,2,3])` → TRUE |
| элементы независимого массива строками | `unnest(arr)` | `UNNEST(arr) u(value)` | принимает литерал и параметр массива; имя элемента по умолчанию — `C1`. Порядок строк сохраняется, и `Independent_array_is_unnested_into_rows_on_the_port` живёт (коммит движка `f24e67e`, issue #23 закрыт) |
| номер элемента | `WITH ORDINALITY` | `WITH ORDINALITY` | нумерация с 1, дополнительный столбец `NORD` типа `INTEGER`; провайдер задаёт алиасы `"value", "ordinality"`. Порт это читает вместе с `UNNEST` |

Строка «содержит null» — это то, чем `H2SqlNullabilityProcessor` компенсирует
`item = ANY(array)`: он пишет `COALESCE(array_contains(arr, NULL), FALSE)`, потому что на
`NULL`-массиве функция отвечает `NULL`, а результат компенсации должен остаться non-nullable —
раньше это обеспечивало `array_position(arr, NULL) IS NOT NULL`. Ответы движка зафиксированы
тестом `H2ArrayContainsContractTest` в юнит-проекте: он прогоняет семь форм через порт и, если
доступен эталонный jar, через него тоже.

**Чего у H2 2.5 нет, и потому перевода нет:**
| Поведение | Форма PostgreSQL | Почему её нет у H2 |
|---|---|---|
| место элемента в массиве | `array_position(arr, item)` | `Function "ARRAY_POSITION" not found`; `ARRAY_CONTAINS` отвечает, есть ли элемент, а не где он |
| оператор пересечения двух массивов | `arr1 && arr2` | `&&` у H2 сравнивает bounding box геометрии, а не массивы; LINQ-запрос сохраняется как `EXISTS`/подзапрос вместо этого оператора |
| оператор включения массива | `arr1 <@ arr2` | оператора `<@` для массивов нет; сохраняется обычный квантованный запрос, а не запрещается операция LINQ |
| коррелированный UNNEST | `LEFT JOIN LATERAL unnest(t.arr) WITH ORDINALITY` | H2 поддерживает UNNEST, но не LATERAL и не разрешает внешние колонки в аргументах UNNEST: `42122 Column "t.Arr" not found`. Проверены настоящая таблица, CROSS/INNER JOIN, EXISTS, скалярный подзапрос и WITH ORDINALITY. Java `Parser.readTablePrimary()` создаёт `FunctionTable`, конструктор которого оптимизирует аргументы до связывания внешних колонок. Это ограничение эталона, а не дефект порта. Некоррелированный UNNEST порт читает (issue #23 закрыт) |

Отсутствие PostgreSQL-оператора не означает отсутствие операции: упрощения массивных
предикатов в `<@` и `&&` убраны, запросы передаются базовому переводу EF через подзапросы.
Полный перевод коллекций строками остаётся отдельной незавершённой задачей. Для извлечения
байта используются `SUBSTRING`, приведение и маска. Сам `get_byte` не пишется.
`H2ByteSubstringTests` проверяет `First()`, постоянный индекс, индекс-параметр и индекс из
столбца, включая 128 и 255. Всё время теста соединение остаётся открытым.

### Оставшаяся поверхность форка

NTS больше не публикует PostGIS `Transform`, `Force2D`, расстояние со сфероидом и KNN
и не переводит методы/свойства геометрии в отсутствующие `ST_*`. Маппинг `GEOMETRY`,
ограничения типа и SRID сохранены. `EnvelopeCombiner.CombineAsGeometry` переводится в H2
`ENVELOPE`, документированный в `functions-aggregate.html`, вместо PostGIS `ST_Extent`.
Геометрическое пересечение не подменяется оператором bounding-box `&&`.

`H2JoinedDeleteTests.Joined_delete_uses_a_key_subquery_on_the_reference` исполняет
сгенерированный `DELETE ... WHERE Id IN (SELECT ... JOIN ...)` на JVM и проверяет оставшиеся
строки. Парный `Joined_delete_removes_only_matching_rows_on_the_port` теперь зелёный:
`8d5fafa` перенёс `IN (subquery)`, trait снят после проверки строк.

`ExecuteUpdate` по запросу с соединением EF Core 9 сводит к виду «целевая таблица
`INNER JOIN` (исходный запрос) `ON` ключ», а формы `UPDATE … FROM` у H2 2.5 нет: эталон
отвергает её (`Syntax error … [*]FROM`). Провайдер пишет этот вид коррелированным
`UPDATE … WHERE EXISTS (SELECT 1 FROM (<запрос>) … WHERE target."Id" = source."Id")` и
отказывает, если выражение `SET` читает столбец присоединённой таблицы, — в `SET` у H2 нет
`FROM`, чтобы её назвать. `A_joined_update_is_reduced_to_a_subquery` проверяет и порт, и
принятие сгенерированного оператора эталоном.

Новые модельные примеры выявили два дефекта, воспроизведённые напрямую через ADO на `8d5fafa`
и сверенные с JVM: [#17](https://github.com/dimzon/h2sharp/issues/17) — `UPDATE T SET N = N + 1`
читает последнюю строку сканирования (10,20 → 81,81 при последней N=80),
[#18](https://github.com/dimzon/h2sharp/issues/18) — `(CreatedAt, Id) > (...)` не разбирается.
Оба закрыты портом: `View_counts_are_updated_with_a_database_expression`, `Composite_cursor_reads_the_next_page_without_offset`
и `Composite_key_lookup` разблокированы и зелёные.
[#19](https://github.com/dimzon/h2sharp/issues/19) — `POSITION(X'80' IN ...)` не разбирается
(`POSITION needs readConcat`), хотя извлечение байта через `SUBSTRING` работает.
`ForumCursorAndBulkExamples` сохраняет строгие `BlockedByPort` проверки значений и парные
рабочие запросы на эталоне.

`H2IdentitySeedTests` проверяет сгенерированный перезапуск identity на эталоне: обычный
и идемпотентный SQL, положительный и отрицательный шаг, наполнение перед текущим значением.
Identity не откатывается назад. Legacy-аннотация `SerialColumn` записывается как H2
`GENERATED BY DEFAULT AS IDENTITY`; смена её на `IdentityByDefaultColumn` не меняет DDL,
а смена режима на `ALWAYS` пишет `SET GENERATED ALWAYS`. PostgreSQL `setval`,
`pg_get_serial_sequence`, `PERFORM` и `ALTER SEQUENCE ... OWNED BY` удалены из генератора.

Публичные `Levenshtein`/`Metaphone` из PostgreSQL-расширения `fuzzystrmatch` сняты;
`SOUNDEX` и `DIFFERENCE` остаются в H2 API как `Soundex` и `SoundexDifference`.
Наследованные узлы бинарных выражений, для которых нет формы H2, отклоняются до отправки
команды вместо генерации PostgreSQL-операторов. `DELETE` над присоединёнными таблицами
передаётся стандартному переводу EF через ключевой подзапрос; `DELETE ... USING` не пишется.
Для JSON длина массива не переводится в отсутствующие `json_array_length`/`jsonb_array_length`.

Ранее реестр ошибочно утверждал, что у H2 нет SRID. Это исправлено по исходнику
`Parser.parseGeometryType` и живому эталону: SRID в типе и в EWKT есть. Оракул теперь
разделяет SQL командами H2 `ScriptReader`, а не строковым `split(";")`, которое разрывало
литерал `SRID=4326;POINT (...)` внутри кавычек.

**Что осталось в наборе.** 204 теста: 180 зелёных, 20 красных и 4 пропущенных. Приведение
`List<T>` починено (конвертер в `H2ArrayTypeMapping`), поэтому `H2DataReader.GetFieldValue<List<T>>`
больше не получает `int[]`. Красные — не SQL провайдера, а перезапись выражений в самих тестах:
`List<T>` нельзя подставить в `MemoryExtensions.Contains(ReadOnlySpan<T>, T)` (16), `SequenceEqual`
с параметром кладёт в дерево `ReadOnlySpan` или `op_Implicit` (4). Провайдер тут ни при чём;
трейт `BlockedByPort` стоит на этих методах, остальные 180 идут в гейте.

## Проверено против эталона

| Что | Провайдер | Эталон H2 2.5 | Порт H2Sharp |
|---|---|---|---|
| `CREATE TABLE` с кавыченными именами | `CREATE TABLE "Entries" ("Id" integer NOT NULL, …)` | принимает | принимает |
| `INSERT` с параметрами | `INSERT INTO "Entries" ("Id", …) VALUES (?, …)` | принимает | принимает |
| EF-форма `SELECT` | `SELECT "e"."Id", … FROM "Entries" AS "e" LIMIT 2` | принимает | принимает — с того момента, как порт перенёс `AS` у псевдонима и кавыченную ссылку на столбец |
| identity-колонка | `"Id" integer GENERATED BY DEFAULT AS IDENTITY` | принимает | принимает — с того момента, как порт перенёс identity-колонки |
| `INSERT`, который возвращает сгенерированный ключ | `SELECT "Id" FROM FINAL TABLE (INSERT INTO "Items" ("Name") VALUES (?))` | принимает | принимает — с того момента, как порт перенёс `FINAL TABLE` |
| `INSERT` без значений, который возвращает ключ | `SELECT "Id", … FROM FINAL TABLE (INSERT INTO "WithNullableBackingFields" DEFAULT VALUES)` | принимает, отвечает `Id = 1` и значениями по умолчанию | **падает**: `NULL not allowed for column "Id"` — пробел 22 |
| `UPDATE` по соединению | `UPDATE "Books" "b" SET "Shelf" = 'top' WHERE EXISTS (SELECT 1 FROM (…) AS "b0" WHERE "b"."Id" = "b0"."Id")` | принимает | принимает |
| `UPDATE … FROM` | не пишется | `Syntax error … [*]FROM` | — |
| `TIME WITH TIME ZONE` | `"Moment" timestamp with time zone NOT NULL` | принимает | **падает** |
| чтение большого объекта | `SELECT` из колонки `clob` | принимает | **падает** — контракт ADO у порта уже верный (`GetValue` отдаёт материализованное значение, поток — в `GetStream`/`GetTextReader`), не хватает одного шага: `ValueLob.CopyToResult` переносит содержимое LOB в результат, а он не портирован |

## Пробелы движка, из-за которых провайдер пока не проходит запросы

| № | Пробел | Симптом | Что нужно порту |
|---|---|---|---|
   | 1 | **Производная таблица** | **читается** (движок перенёс производные таблицы): `SELECT * FROM (SELECT …) "t"` принимается | — |
   | 2 | **Соединение в любой форме** | **читается** (коммит движка `49bccfc` «The join reads two tables as one»): `INNER`, `JOIN`, `LEFT`/`RIGHT [OUTER]`, `CROSS`, `NATURAL`, с условием `ON` и списком `USING` | — |
| 3 | **`FOREIGN KEY` в `CREATE TABLE`** | **читается**: внешний ключ внутри `CREATE TABLE` и `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY … ON DELETE CASCADE` принимаются (`ForumScaffoldingTests`, `ForumRelationshipExamples`) | — |
| 4 | **Типы `INTERVAL`** | `interval day to second` → `ожидалось "INTERVAL"` | интервальные типы в парсере; без квалификатора `INTERVAL` типом не является |
| 5 | **`DB_CLOSE_DELAY` для in-memory базы** | база исчезает вместе с последним соединением, настройка не действует | `DB_CLOSE_DELAY`, иначе in-memory база не переживает `EnsureCreated` |
   | 6 | **`CREATE SCHEMA`** | **читается** (движок перенёс схемы): `CREATE SCHEMA IF NOT EXISTS db2` принимается | — |
   | 7 | **`ALTER TABLE`** | читаются `ADD`/`DROP CONSTRAINT`, включая `FOREIGN KEY`; `ADD CONSTRAINT … UNIQUE` отвечает `expected "PRIMARY"`; `RENAME TO`, `ALTER COLUMN`, `DROP COLUMN`, `DROP DEFAULT` — `Not ported yet` | остальные действия `ALTER TABLE` и `UNIQUE`-ограничение |
   | 8 | **Колонка каталога без коллации** | `collation` колонки пуст, а у H2 2.5 там коллация базы | коллация в каталоге; имя каталога теперь имя базы (коммит движка `87a218a`) |
   | 9 | **`EXISTS` и `NOT EXISTS`** | **читается** (коммит движка `d86b2e1` «Ask subqueries whether a row exists») | — |
   | 10 | **Квантованное сравнение с массивом-параметром** | `= ANY ($__ids_0)` → `Not ported yet: Parser.parseQuerySelect: aggregate calls outside the select list are not ported` | `x = ANY (массив)`: эталон берёт эту форму (`SELECT 1 = ANY(ARRAY[1,2,3])` → TRUE), а порт читает `ANY` только как вызов агрегата |
   | 11 | **`SAVEPOINT` внутри транзакции** | **читается** (коммит движка `69d522a`): `SAVEPOINT`, `ROLLBACK TO SAVEPOINT` и `RELEASE SAVEPOINT` принимаются | — |
   | 12 | **`BTRIM` с набором символов** | ~~`Syntax error in SQL statement "SELECT btrim(""t"".""Title"", E'…')"; expected ")"`~~ | **закрыто**: порт читает второй аргумент; падала не функция, а литерал — провайдер писал `EU&'…'`, которого в H2 нет. Набор теперь `U&'…'`, его читают и эталон, и порт; `BTRIM` с таким набором режет и таб, и неразрывный пробел |
   | 13 | **Оператор склейки строк** | ~~`Syntax error in SQL statement "SELECT ""t"".""Title"" \|\| ' (a topic)'"; expected "statement"``~~ | **закрыто**: провайдер пишет `CONCAT(a, b)`, и порт её читает |
   | 14 | **`INTERSECT` и `EXCEPT`** | **читается** (коммит движка `69d522a` «Port set operations») | — |
   | 15 | **Запрос в списке столбцов** | **читается** (коммит движка `69d522a`): скалярный подзапрос с `CAST(агрегат)` принимается | — |
   | 16 | **Дата с параметром в `TIMESTAMPADD`** | `Value.getLong needs the concrete value type for NUMERIC, which is not ported yet` | `TIMESTAMPADD` с числом-параметром: с числом, написанным прямо, движок читает |
   | 17 | **Порядок строк в агрегате** | **читается** (коммит движка `69d522a`) | — |
   | 18 | **Фильтр строк в агрегате** | **читается** (коммит движка `69d522a`): `FILTER (WHERE …)` у агрегата принимается | — |
   | 19 | **Пространственный индекс** | `Not ported yet: Parser.parseCreate: HASH and SPATIAL indexes are not ported` | `CREATE SPATIAL INDEX`: H2 2.5 его читает, и провайдер его пишет |
   | 20 | **Пункт `INCLUDE` у индекса** | `Not ported yet: Parser.parseCreate: CREATE INDEX INCLUDE is not ported` | `INCLUDE (…)` у уникального индекса: H2 2.5 его читает, и провайдер его пишет |
   | 21 | **`UNIQUE` через `ALTER TABLE`** | `ALTER TABLE "Forums" ADD CONSTRAINT "UQ_Forums_Name" [*]UNIQUE ("Name")` → `expected "PRIMARY"` | `ADD CONSTRAINT … UNIQUE`: эталон принимает, и провайдер пишет эту форму из миграций (`A_unique_constraint_is_added_to_an_existing_table`) |
   | 22 | **`FINAL TABLE` над `INSERT … DEFAULT VALUES`** | `SELECT "Id", … FROM FINAL TABLE (INSERT INTO "WithNullableBackingFields" DEFAULT VALUES)` → `NULL not allowed for column "Id"` (23502); прямой `INSERT … DEFAULT VALUES` порт принимает и identity генерирует | эталон эту форму принимает и отвечает `Id = 1`. Дельта-таблица строит строку вставки через `Parser.PrepareInsert` (`Command/Parser.cs:1816`, `:6585`), а тот даёт по ячейке на столбец: у identity `Column.GetEffectiveDefaultExpression()` — `null`, и в ячейку кладётся `ValueExpression.Null`; обычная вставка идёт через `Parser.ParseInsert` (`Command/Parser.Columns.cs:438`) и кладёт пустую строку (`new Expression[0]`), после чего `Column.ValidateConvertUpdateSequence` видит `null` и берёт следующее значение identity. В дельта-таблице `ValueNull` — не `null`, и NOT NULL отвечает 23502. Дельта-таблице нужен тот же путь. Issue: dimzon/h2sharp#66 |
   | 23 | **Байт, который вмещает .NET** | **закрыто (#94)**: `H2DataReader.GetByte` отдаёт 255 для числа 255; три теста `H2ByteSubstringTests` сняты с трейта и зелёные | — |
   | 24 | **Каталог схем в сессии** | **закрыто (#95)**: `A_schema_of_its_own_is_made_and_found` в `ModelTests` — обычный `[Fact]` и зелёный | — |
   | 25 | **Открытие зачисленного соединения** | **закрыто (#96)**: все наборы `TransactionInterceptionH2Test` (72 теста) сняты с трейта и зелёные | — |

   Закрытые пробелы: DDL identity-столбца, значение identity обратно (`FINAL TABLE`),
   последовательности (`CREATE SEQUENCE`, `NEXT VALUE FOR`), типизированный литерал в
   `DEFAULT`, `AS` у псевдонима таблицы и столбца, кавыченная ссылка на столбец, знак перед
   членом (`-x`), производная таблица, склейка строк через `CONCAT`, `BTRIM` с набором
   символов, `VALUES`-таблица
   (`VALUES (1), (2)`), общее табличное выражение (`WITH c AS (…)`) и схемы
   (`CREATE SCHEMA IF NOT EXISTS`) — их порт перенёс, и
   провайдер пишет ровно то, что H2 2.5 требует. Предел в восемь параметров
   на команду тоже снят: порт держит растущее множество параметров и тот же предел, что
   эталон, `MAX_PARAMETER_INDEX = 100_000` (dimzon/h2sharp#15).

## Что из этого уже сделано провайдером

* **Кавычки везде.** H2 сворачивает некавыченное в UPPER, поэтому `title` доехал бы как
  `TITLE` и модель перестала бы описывать свою схему. Кавычки стоят два символа и убирают
  целый класс молчаливых расхождений.
* **`SerialColumn` — это identity.** В H2 нет псевдотипа `serial`; единственный
  авто-генерируемый столбец — `GENERATED BY DEFAULT AS IDENTITY`. Провайдер пишет именно
  это, и порт такое DDL теперь читает.
* **Значение identity возвращается через `FINAL TABLE`, а не через `RETURNING`.** В H2 2.5
  нет `RETURNING` — эталон его отвергает, — и нет ни `SCOPE_IDENTITY()`, ни `IDENTITY()`, ни
  `LAST_INSERT_ID()`. Строка, которую сделала вставка, читается через `FINAL TABLE`, как
  это делает грамматика H2 (`{ OLD | NEW | FINAL } TABLE`). Порт это читает, и значение
  доходит до вызывающего.
* **Приведение типа — `CAST(x AS y)`.** Эталон принимает и короткую форму `x::y`, в том
  числе `SELECT count(*)::int`. Провайдер использует стандартную запись `CAST` и типы H2.
* **`INTERVAL DAY TO SECOND`, а не `INTERVAL`.** Голый `INTERVAL` типом не является: H2 требует
  квалификатор, и движок сам кладёт `TimeSpan` именно в `DAY TO SECOND`. Пробел 4 — на стороне
  порта.

## Ограничения на стороне провайдера

* **`Period` и `OffsetTime` не маппятся.** `Period` считает месяцы и годы, чего не держит ни
  один BCL-тип; `OffsetTime` — время суток со смещением, а такого типа столбца в H2 нет.
  Свойство такого типа не получает маппинга, и EF Core об этом говорит, а не молча кладёт
  что-то другое.
* **Зона `ZonedDateTime` не хранится.** `TIMESTAMP WITH TIME ZONE` держит смещение, а не зону, —
  ровно как в PostgreSQL. Смещение сохраняется, значение после чтения отличается от
  записанного на зону.
* **Массивы.** `H2ArrayTypeMapping` разбирает тип элемента, но поддержка массивов в ADO-слое
  H2Sharp не проверена. Тест на массив нужен раньше, чем тип можно считать рабочим.
* **Коллации сняты из провайдера.** В H2 2.5 нет ни `CREATE COLLATION`, ни `COLLATE` у столбца,
  ни `COLLATE` в выражении: эталон читает `COLLATE` только в режиме совместимости, а каталог
  H2 2.5 не знает коллации столбца. Поэтому API провайдера (`HasCollation`, `UseCollation`,
  `GetCollation`, `CollationDefinitionPrefix`, `DefaultColumnCollation`, `IndexCollation`)
  убран, DDL коллаций больше не пишется, а попытка задать коллацию (у столбца, у индекса, у
  выражения, у базы данных) даёт `NotSupportedException` с объяснением, что H2 2.5 такого
  не умеет. `UNLOGGED` и «interleave in parent» CockroachDB сняты по той же причине.
* **Полнотекстовый поиск снят из провайдера.** В H2 2.5 нет типа `tsvector`, нет `tsquery`, нет
  функций `to_tsvector`/`plainto_tsquery`, нет индексов GIN/GIST и нет `CREATE TEXT SEARCH
  CONFIGURATION`. Поэтому API провайдера (`IsTsVectorExpressionIndex`, генерируемый столбец
  `tsvector`, `to_tsvector`/`plainto_tsquery`) убран вместе с аннотациями
  (`TsVectorConfig`, `TsVectorProperties`) и с DDL, который это писал.
* **Класс оператора столбца индекса снят.** В H2 2.5 у столбца индекса нет класса оператора:
  грамматика эталонного движка читает `readIdentifier()` и порядок сортировки, и ничего
  больше. Поэтому `HasOperators` и аннотация `IndexOperators` убраны, и DDL индекса больше не
  пишет класс оператора.
* **Метод индекса оставлен, но теперь это вид индекса H2 2.5.** У H2 2.5 есть индекс по
  умолчанию (btree) и пространственный индекс (`CREATE SPATIAL INDEX`); хеш-индекс H2 2.5
  читается только в режиме совместимости, а методы индексов PostgreSQL (`gin`, `gist`, `brin`)
  в H2 2.5 нет. Поэтому `HasMethod` принимает только `btree` (по умолчанию) и `spatial`, а
  остальное даёт `NotSupportedException` со списком того, что у H2 2.5 есть. Пробел движка:
  `CREATE SPATIAL INDEX` движок не читает («Parser.parseCreate: HASH and SPATIAL indexes are
  not ported»).
* **Пункт `INCLUDE` у индекса остаётся, но только у уникального.** Эталон читает `INCLUDE`
  в ветке уникального индекса, и `CREATE INDEX ... INCLUDE (...)` без `UNIQUE` у него — ошибка
  синтаксиса. Провайдер пишет `INCLUDE` только у уникального индекса, а у обычного даёт
  `NotSupportedException`. Пробел движка: `INCLUDE` движок не читает («Parser.parseCreate:
  CREATE INDEX INCLUDE is not ported»).
* **Расширения сняты из провайдера.** В H2 2.5 нет `CREATE EXTENSION`: эталон отвергает эту
  форму. Поэтому API провайдера (`HasPostgresExtension`, `EnsurePostgresExtension`, класс
  `PostgresExtension`) убран, DDL расширений больше не пишется, и скаффолдинг расширений не
  читает. То же — с табличным пространством: в H2 2.5 нет `TABLESPACE`, поэтому
  `UseTablespace` и `Tablespace` убраны.
* **Создание индекса не бывает одновременным.** В H2 2.5 нет `CREATE INDEX CONCURRENTLY`:
  эталон отвергает эту форму. Поэтому `IsCreatedConcurrently` убран, и DDL индекса больше не
  пишет `CONCURRENTLY`.
* **Параметров хранения нет.** В H2 2.5 нет `WITH (fillfactor = 70)` и `SET (...)`/`RESET (...)`:
  эталон отвергает эти формы. Поэтому `HasStorageParameter` (у сущности и у индекса) убран, и
  DDL больше не пишет `WITH (...)`.
* **Перечисления у H2 2.5 есть, и провайдер их пишет.** Тип `ENUM('a','b')` и домен
  `CREATE TYPE x AS ENUM(...)` эталон берёт, и провайдер пишет ровно это. API перечислений
  переименован в H2-овское: `HasPostgresEnum` стал `HasH2Enum`, класс `PostgresEnum` —
  `H2Enum`.
* **Частичного индекса у H2 2.5 нет.** Грамматика эталона читает у индекса список столбцов и
  всё: `CREATE INDEX ... (cols) WHERE ...` у H2 2.5 — ошибка синтаксиса. Поэтому фильтр
  индекса провайдер не пишет и отказывает, если его задали.
* **Сжатия столбца нет.** В H2 2.5 нет `COMPRESSION` у столбца: слово это есть только в
  `RUNSCRIPT`. Поэтому `UseCompressionMethod` убран, и DDL столбца больше не пишет
  `COMPRESSION`.
* **Внутренние имена провайдера больше не про PostgreSQL.** Классы выражений запроса,
  унаследованные от форка Npgsql, переименованы: `PgExpressionType` стал `H2ExpressionType`,
  `PgBinaryExpression` — `H2BinaryExpression`, и так далее. Это внутренние имена, но в
  провайдере, который пишет SQL для H2, им не место.

## Следствия для тестов

* In-memory база умирает вместе с соединением (пробел 5), поэтому тест обязан **держать
  соединение открытым** на всё время проверки — `context.Database.OpenConnection()`.
  Иначе `EnsureCreated` отработает по одной базе, а чтение пойдёт по другой и увидит её пустой.
  Тест-стор провайдера это делает сам: `H2TestStore` не закрывает своё соединение после
  команды.
* Тесты, требующие `SELECT`, останутся красными до пробелов 1 и 2. Это расхождение движка, а
  не провайдера, и реестр существует именно для того, чтобы их различать.

## Скаффолдинг: что мешает набору

Скаффолдинг читает каталог `INFORMATION_SCHEMA` — тот самый, который порт теперь отдаёт, и
соединения в нём порт читает с коммита `49bccfc` («The join reads two tables as one»).
Смоук-тест `Scaffolding/H2CatalogueJoinSmokeTest.cs` проверяет это прямо: он выполняет
внутреннее соединение, левое соединение и те три запроса каталога, которые пишет сам
скаффолдинг (`TABLE_CONSTRAINTS` с `KEY_COLUMN_USAGE`, `INDEXES` с `INDEX_COLUMNS`,
`TABLE_CONSTRAINTS` с `REFERENTIAL_CONSTRAINTS`) — все проходят.

Поэтому красное в наборе скаффолдинга — не соединения и не каталог:

* **Ограничения таблицы в `CREATE TABLE` порт читает не все**: первичный ключ и внешний ключ
  читаются (`The_foreign_keys_of_a_table_are_read_back`), а `CHECK` и `UNIQUE` — нет:
  «Not ported yet: Parser.readCreateTable: table constraints other than the primary key are
  not ported». Тесты с `CHECK`, `UNIQUE` и внешними ключами падают на своём DDL, кроме
  внешнего ключа — он проходит.
* **Данные тестов — PostgreSQL-овские.** `money`, `bytea`, `uuid`, `jsonb`, `inet`,
  `macaddr`, `point`, `line`, `bit(111)` и массив в форме `varchar(66)[]` H2 2.5 не знает:
  массив у него пишется как `ARRAY`, а не `[]`. Эти DDL надо переписать под H2 2.5.
* **Ожидания тестов — PostgreSQL-овские.** Схема по умолчанию у H2 2.5 — `PUBLIC`, а не
  `public`; первичный ключ без имени H2 2.5 называет `CONSTRAINT_3` (счётчик ограничений), а
  не `PK_<таблица>`, как PostgreSQL. Проверено на эталоне: `SELECT SCHEMA_NAME FROM
  INFORMATION_SCHEMA.SCHEMATA` даёт `PUBLIC`, а `CONSTRAINT_NAME` первичного ключа —
  `CONSTRAINT_3`.
* **Перечисления из каталога не скаффолдятся — движок.** Порт не читает ни `CREATE TYPE … AS
  ENUM` (домен перечисления), ни инлайн-тип колонки `ENUM('a','b')`: в его парсере нет слова
  `ENUM`, а представление `DOMAINS` каталога объявлено, но всегда пусто. Метки перечислений
  движок умеет отдавать только для колонок (`ENUM_VALUES`), но создать такую колонку через SQL
  нельзя. Поэтому скаффолдинг перечислений ждёт движок: когда `CREATE TYPE` и `DOMAINS`
  появятся, `H2DatabaseModelFactory` начнёт читать домены, а `H2CodeGenerator` — писать
  `HasH2Enum`. Провайдер при этом уже сегодня пишет правильное DDL, а `ForumEnumExamples`
  проверяет перечисления по эталону на JVM.

## Что движок умеет сегодня, и почему спецификационный набор ещё не гоняется по базе

> Раздел сохраняет историю первых проб движка. Часть перечисленного с тех пор закрыта;
> актуальный статус держат таблица пробелов выше и оба прогона гейта.

Спецификационный набор EF Core делится на две части: SQL-генерация (ToSql, модель, миграции) и
работа с живой базой. Первая идёт сейчас, вторая упирается в движок. Ниже — что проверено пробой
против самого движка (`C:\Work\h2sharp`), а не по документации.

### Умеет

| Что | Пример |
|---|---|
| `CREATE TABLE` с обычными столбцами | `CREATE TABLE T3 (Id INT PRIMARY KEY, Name VARCHAR(50))` |
| `INSERT`, `UPDATE`, `DELETE`, `SELECT` | проходят |
| `ORDER BY`, `LIMIT`, `CASE WHEN` | проходят |
| `CREATE INDEX`, `DROP TABLE` | проходят |
| несколько операторов в одной команде | `H2Command.ExecuteNonQuery` разбирает список команд |
| `ServerVersion` | `2.5.259` |

### Не умеет

| Чего нет | Симптом | Это |
|---|---|---|
| `GENERATED BY DEFAULT AS IDENTITY` | **читается** (движок перенёс identity-колонки): `CREATE TABLE T1 (Id INT GENERATED BY DEFAULT AS IDENTITY ...)` принимается | — |
| `AUTO_INCREMENT` (альтернативная запись) | та же ошибка | дефект порта |
| `INFORMATION_SCHEMA` | **читается** (коммит движка `17708ad`): каталог порта отдаёт таблицы, столбцы, ключи и ссылки; расходится только `collation` колонки, а имя каталога теперь имя базы (коммит движка `87a218a`) | расхождение — дефект порта |
| `CREATE SCHEMA` | **читается** (коммит движка): `CREATE SCHEMA IF NOT EXISTS db2` принимается | — |
| `DROP ALL OBJECTS` | **читается** (коммит движка `87a218a`): база пустеет, а главная схема и схема каталога остаются | — |
| `LEFT JOIN` | **читается** (коммит движка `49bccfc`): `LEFT JOIN`, `RIGHT [OUTER]`, `CROSS`, `NATURAL` | — |
| `IN (подзапрос)` | **читается** (коммит движка `8d5fafa`): `Delete` по соединению идёт через ключевой подзапрос | — |
| `EXISTS` | **читается** (коммит движка `d86b2e1`) | — |
| `x = ANY (массив)` | `Parser.parseQuerySelect: aggregate calls outside the select list are not ported` — эталон берёт `SELECT 1 = ANY(ARRAY[1,2,3])` → TRUE | дефект порта |
| `SAVEPOINT` | **читается** (коммит движка `69d522a`) | — |
| переменные в тексте SQL | `Parser.readTerm: variables (@name) are not ported` — но слой ADO переписывает `$name` в `$N` до разбора, так что через провайдер это не мешает | — |
| файловая база | `Engine.openSession: persistent databases need the file store` — хранилище в порте есть и формат его совместим с H2 2.5, но сессия не открывает файловую базу | дефект порта |
| `H2SharpException` с публичным конструктором | конструктор `internal`, тестовый набор не может сфабриковать отказ соединения | дефект порта |

### Следствия для тестового оборудования

- **База живёт ровно столько, сколько живёт её владелец.** База принадлежит data source: он её
  создаёт и держит (`SetCloseDelay(-1)`), а соединение — только способ до неё добраться. Тест-стор
  поэтому берёт своё соединение из data source и **не закрывает его** после команды, а база живёт
  вместе со стором.
- **Чистка базы идёт через `DROP ALL OBJECTS`.** `DROP ALL OBJECTS` движок читает: база пустеет,
  а главная схема и схема каталога остаются. Тест-стор чистит базу этой командой, и провайдер знать
  имён таблиц не должен. Стор, к тому же, emptying базу при своём создании: база стора — его
  собственная.
- **Northwind для EF-набора нужен свой.** Скрипт `Northwind.sql` из форка — PostgreSQL-овский:
  `serial`, `::`-касты, `SET`, ICU-колляция, `CREATE COLLATION`. Движок это не разбирает. Нужен
  H2-скрипт с тем же содержимым.
- **`IRelationalCommand` в EF Core 9 — шаблонный**, а не тот интерфейс, под который написан
  Npgsql-овский фейк `TestRelationalCommand`. Фейк придётся писать заново.
## Миграционный набор EF Core: 183 теста, все зелёные

> Историческая справка: разбор ниже — снимок 25 октября. Сейчас все 183 теста
> миграционного набора проходят в общем зелёном прогоне, `BlockedByPort` с них снят.

`Migrations\MigrationsH2Test.cs` — это 3000 строк общих миграционных тестов EF Core, и до сих пор
файл был целиком исключён. Он возвращён: из него вырезано то, что является PostgreSQL-овым, а не
EF-овым (полнотекстовый поиск `tsvector`, `CREATE COLLATION`, `CREATE EXTENSION`, версионно-зависимые
опции), и он даёт **183 теста**.

Из них 113 падают, 68 проходят и 2 пропущены. Разбор по тексту ошибки ниже снят 25 октября;
перемерены только строки, которые затронула правка перечислений, — суммы по строкам поэтому
не дают 113:

| Причина | Тестов | Чей |
|---|---:|---|
| таблица осталась от прошлого теста | 0 | закрыто: движок читает `DROP ALL OBJECTS`, и тест-стор чистит базу этой командой |
| `ALTER TABLE` — `RENAME TO`, `ALTER COLUMN`, `DROP COLUMN`, `DROP DEFAULT` | 54 | движок: «Not ported yet: Parser.readAlterTable: only ADD and DROP CONSTRAINT are ported» |
| модель каталога не совпала с моделью EF | 22 | движок: каталог не говорит, что сказано для индекса |
| identity: стратегия в каталоге, параметры identity | 9 | движок |
| перезапуск identity после наполнения | 6 | прежний `SETVAL` был дефектом провайдера: эта функция PostgreSQL отсутствует и в эталоне. Теперь пишется `ALTER TABLE ... ALTER COLUMN ... RESTART WITH` с `IDENTITY_BASE`/`IDENTITY_INCREMENT` из каталога H2; порт пока не читает эту форму `ALTER TABLE`, тесты сохранены |
| `CREATE TYPE … AS ENUM` не разбирается | 9 | движок; два из них — `Alter_enum_*_not_supported`, которые теперь упираются в `CREATE TYPE` первой миграции раньше, чем доходят до генератора |
| `CHECK` ограничение в `CREATE TABLE` | 0 | закрыто: с коммита движка `f56ca4c` табличные `UNIQUE` и `CHECK` читаются |
| `INCLUDE` у уникального индекса | 1 | движок: «Not ported yet: Parser.parseCreate: CREATE INDEX INCLUDE is not ported» |
| `COMMENT ON` не разбирается | 2 | движок |
| последовательные операции данных (INSERT, UPDATE, DELETE) | 2 | движок |
| последовательные ключи и параметры | 3 | движок |
| отказ проверки: коллации, `CREATE COLLATION` | 0 | снято: в H2 2.5 нет ни того, ни другого, провайдер коллации больше не пишет и не предлагает |
| фильтр индекса, `INCLUDE` у неуникального индекса | 0 | снято: частичного индекса у H2 2.5 нет, провайдер отказывает |

Внешний ключ через `ALTER TABLE … ADD CONSTRAINT … FOREIGN KEY` из списка ушёл: порт его читает
(`The_foreign_keys_of_a_table_are_read_back`). `UNIQUE`- и `CHECK`-ограничение через
`ALTER TABLE` тоже ушли: с коммита движка `f56ca4c` порт читает и
`ADD CONSTRAINT … UNIQUE (…)`, и `ADD CONSTRAINT … CHECK (…)` — тесты `Add_unique_constraint`,
`Add_check_constraint_with_name`, `Drop_*` и `Alter_check_constraint` проходят.

`CREATE SCHEMA` из списка ушла: схемы порт с тех пор читает. `UNLOGGED TABLE` из списка ушла
тоже: слова `UNLOGGED` в H2 2.5 нет, и провайдер его больше не пишет — ни в DDL, ни в своём API
(`IsUnlogged` убран). `CREATE TYPE … AS ENUM` остаётся: перечисления у
H2 2.5 **есть** (тип `ENUM('a','b')` и домен `CREATE TYPE x AS ENUM(...)`), форма провайдера
эталону подходит, и не читает её пока порт.

`GENERATED BY DEFAULT AS IDENTITY` и `CREATE SEQUENCE` в этом списке больше нет: движок перенёс
identity-колонки и последовательности, и DDL, который провайдер пишет, теперь принимается.
Блока `DO $EF$` в списке тоже нет: он был PostgreSQL'овским, и провайдер пишет H2'овскую
`CREATE SCHEMA IF NOT EXISTS` вместо него. Шестьдесят восемь тестов миграционного набора
проходят, 113 ждут движок и 2 пропущены.

Добавление метки перечисления из этого списка тоже ушло: `ALTER TYPE … ADD VALUE` H2 2.5
принимает, провайдер его пишет, и два теста `Alter_enum_add_label_*` теперь проверяют эту
форму, а не отказ. Падают они на том же `CREATE TYPE` первой миграции — то есть ровно на
пробеле движка, который и держит весь перечисленческий раздел.

Сама форма закрыта на уровне генератора, где движок не нужен: шесть тестов
`H2MigrationsSqlGeneratorTest.Alter_enum_*` собирают `AlterDatabaseOperation` с метками
перечисления и проверяют текст команд — `ALTER TYPE "Mood" ADD VALUE 'Angry';`, две такие
команды на две метки, дописывание метки из середины в конец, пустой текст при перестановке
меток и `NotSupportedException` при удалении и переименовании. Они зелёные на `net9.0` и
`net10.0`, так что расхождение с эталоном зафиксировано тестом, а не только абзацем ниже.

**Ни одного падения, которое было бы дефектом провайдера.** Три теста на код ошибки JSON были
поправлены: PostgreSQL сообщает `42804`, движок — `42001`.

### Что это значит для провайдера

Провайдер в этой части готов. Осталось — в движке. И вот конкретный разблокирующий запрос:

**`H2Connection.Session` — `internal`.** Движок отдаёт каталог нативно:
`CurrentDatabase.GetSchemas()` → `Schema.GetAllTablesAndViews(SessionLocal)`. Но `SessionLocal` нужно
получить, а `H2Connection.Session` внутренний, и `Schema.GetAllTablesAndViews` без него не вызвать.

Одна строка `internal` → `public` (или публичный метод, отдающий таблицы) снимает сразу три блокера:

| Что блокировано | Почему |
|---|---|
| `H2DatabaseModelFactory` (скаффолдинг) | каталога `INFORMATION_SCHEMA` нет, а нативный каталог недоступен |
| `H2DatabaseCleaner` и `H2DatabaseCreator.Delete()` | `DROP ALL OBJECTS` не разбирается, а перечислить таблицы нечем |
| 12 тестов «таблица осталась от прошлого теста» | следствие предыдущего |

Пока перечисления нет, провайдер удаляет таблицы по именам, которые знает из модели EF: это и в
`H2DatabaseCreator.Delete()`, и в `H2DatabaseCleaner`, и в тестовом сторе. Для тестов этого
достаточно — имена таблиц известны, — но для скаффолдинга не хватает: там таблицы перечисляет
**база**, а не модель.
## Встроенные функции H2: что выставлено провайдером

Список функций взят не из памяти, а из исходников эталонной H2
(`h2/src/main/org/h2/expression/function/BuiltinFunctions.java` — тот же список, что у
движка в `BuiltinFunctions.cs`).

| Множество | Сколько |
|---|---:|
| документировано в H2 2.5 | 146 |
| портировано в H2Sharp | 96 |
| выставлено провайдером через `H2DbFunctionsExtensions` | 54 метода / 56 функций |
| закрыто переводами самого EF Core (`System.Math`, строковые методы, `coalesce`, `nullif`, `greatest`) | ~40 |

Функция не выставляется, если движок её не читает: выставленная функция, которая падает на
сервере, хуже отсутствующей. Каждая из 56 проверена пробой против движка.

### Что выставлено

| Группа | Функции |
|---|---|
| даты и время | `DATEADD`, `DATEDIFF`, `DATE_TRUNC`, `EXTRACT` (через `YEAR`/`MONTH`/`DAY`/`DAY_OF_MONTH`/`DAY_OF_WEEK`/`DAY_OF_YEAR`/`HOUR`/`MINUTE`/`SECOND`/`QUARTER`/`WEEK`), `DAYNAME`, `MONTHNAME` |
| строки | `LOCATE`, `LPAD`, `RPAD`, `TRANSLATE`, `REPEAT`, `SPACE`, `QUOTE_IDENT`, `SOUNDEX`, `DIFFERENCE`, `INSERT`, `ASCII`, `CHAR`, `CHR`, `STRINGDECODE`, `STRINGENCODE`, `CONCAT_WS` |
| двоичные | `STRINGTOUTF8`, `UTF8TOSTRING`, `HEXTORAW`, `RAWTOHEX`, `COMPRESS`, `EXPAND` |
| числа | `SIGN`, `TRUNC`, `TRUNCATE`, `COT`, `ROUNDMAGIC` |
| биты | `BITAND`, `BITOR`, `BITXOR`, `BITNOT`, `BITNAND`, `BITNOR`, `BITXNOR`, `BITGET`, `BITCOUNT`, `LSHIFT`, `RSHIFT`, `ULSHIFT`, `URSHIFT`, `ROTATELEFT`, `ROTATERIGHT` |

Две особенности, о которых надо знать:

- **`LOCATE` берёт подстроку первой.** H2 — единственная из SQL, где `LOCATE(подстрока, строка)`,
  а не как везде. Переводчик меняет порядок аргументов; в C# подпись сделана как у EF —
  `Locate(value, substring)`.
- **Часть даты берётся своей функцией на каждое поле.** H2 не имеет `EXTRACT(field FROM x)` как
  единой формы, поэтому `Extract("YEAR", x)` переводится в `YEAR(x)`.

### Что движок ещё не портировал (50 функций)

`TO_CHAR`, `FORMATDATETIME`, `PARSEDATETIME`, `REGEXP_LIKE`, `REGEXP_REPLACE`, `REGEXP_SUBSTR`,
`XMLATTR`, `XMLCDATA`, `XMLCOMMENT`, `XMLNODE`, `XMLSTARTDOC`, `XMLTEXT`, `TRIM_ARRAY`,
`ARRAY_CONTAINS`, `ARRAY_SLICE`, `JSON_OBJECT`, `JSON_ARRAY`, `ENCRYPT`, `DECRYPT`, `HASH`,
`ORA_HASH`, `ABORT_SESSION`, `CANCEL_SESSION`, `AUTOCOMMIT`, `DATABASE_PATH`, `H2VERSION`,
`LOCK_MODE`, `LOCK_TIMEOUT`, `MEMORY_FREE`, `MEMORY_USED`, `READONLY`, `SESSION_ID`,
`TRANSACTION_ID`, `DISK_SPACE_USED`, `ESTIMATED_ENVELOPE`, `FILE_READ`, `FILE_WRITE`,
`DATA_TYPE_SQL`, `DB_OBJECT_ID`, `DB_OBJECT_SQL`, `CSVWRITE`, `SIGNAL`, `TRUNCATE_VALUE`,
`CURRVAL`, `NEXTVAL`, `CSVREAD`, `TABLE_DISTINCT`, `LINK_SCHEMA`, `ZERO`, `PI`.

Когда движок их дотянет, они belong в `H2DbFunctionsExtensions` — место для них там уже есть.

### Дефекты провайдера, которые вскрылись при этом

| Что | Было | Стало |
|---|---|---|
| `Guid.NewGuid()` | `gen_random_uuid()` — функции PostgreSQL, у H2 нет | `RANDOM_UUID()` |
| `string.IndexOf` | `strpos(...)` — функции PostgreSQL, у H2 нет | `LOCATE(...)` с обратным порядком аргументов |
| склейка строк | `a \|\| b` — есть в H2, но распространяет `NULL` | `CONCAT(a, b)` — игнорирует `NULL`, как строковый `+` .NET |
| склейка массивов | `a \|\| b` | `ARRAY_CAT(a, b)` |
| исключающее или | `a # b` | `BITXOR(a, b)` |
| строковый агрегат | `string_agg(x, ', ')` — имя PostgreSQL | `LISTAGG(x, ', ')` |
| агрегат json | `json_agg(x)`, `json_object_agg(k, v)` | `JSON_ARRAYAGG(x)`, `JSON_OBJECTAGG(k, v)` |
| `DateTime.AddDays` и прочая арифметика дат | `INTERVAL '1 days'` и `CAST($1 \|\| ' days' AS interval)` — движок не читает ни того, ни другого | `TIMESTAMPADD('DAY', 1, x)` |
| `EnsureDeleted()` при подключении строкой | `H2DatabaseCreator` открывал соединение сырой ADO-строкой, а движок ждёт URL | перевод через `H2RelationalConnection.H2Url` |
| `Reverse`, `StringToArray`, `ToDate`, `ToTimestamp`, `Distance` | методы в `DbFunctions`, которые никогда не переводились и всегда падали | убраны: таких функций в H2 нет |
| `JsonbAgg`, `JsonbObjectAgg` | методы в `DbFunctions` для типа `jsonb` | убраны: в H2 2.5 нет типа `jsonb` |

### Два новых пробела движка

- **`AS` перед алиасом таблицы не разбирался.** `SELECT a.Id FROM T3 a` — работало,
  `SELECT a.Id FROM T3 AS a` — нет: `Синтаксическая ошибка ... FROM T3 AS [*]a`. Движок это
  с тех пор прочитал, и провайдер пишет алиас без `AS` (см. ниже).
- **Литерал типа `TIMESTAMP '2020-01-01 00:00:00'` не разбирается.** Даты приходится брать
  из столбца, а не из литерала.
## Алиас таблицы: `AS` не нужен

Спросили, можно ли обойтись без `AS` перед алиасом таблицы. Можно, и это не уступка сломанному
движку: эталонная H2 2.5 на JVM была спрошена напрямую, и она принимает **обе** формы.

```
SELECT a."Id" FROM "Rows" a        -- OK
SELECT a."Id" FROM "Rows" AS a     -- OK
```

Поэтому провайдер пишет алиас без `AS`: `FROM "Rows" "r"`. Это обычный SQL, который принимают и
эталон, и порт, и из двух форм он короче. Псевдонимы **столбцов** свой `AS` сохраняют: «голый»
псевдоним столбца (`x y`) движок теперь читает, но EF Core пишет разделитель псевдонимов без
пробела, и голая форма через него не получается: `AS` остаётся.

Проверено и на эталоне: тест `H2GeneratedSqlReferenceTest.The_generated_query_is_accepted_by_the_reference_engine`
отдаёт сгенерированный провайдером запрос эталонной H2 и требует, чтобы она его приняла.

**Побочный эффект: EF-запросы теперь выполняются.** Первый запрос EF против порта прошёл:
`SELECT "r"."Name", "r"."Price" FROM "Rows" "r" WHERE "r"."Price" > $p0` — с фильтром,
проекцией и вызовом `EF.Functions.LeftPad`. До этого ни один запрос EF не мог выполниться.

### Что ещё вскрылось при этом

| Что | Было | Стало |
|---|---|---|
| `UseH2("jdbc:h2:mem:x")` | `H2Url` брал строку как key=value и падал: `Format of the initialization string does not conform to specification` | JDBC-URL передаётся движку как есть |
| `DefaultValuesTest` | значение по умолчанию — UTC `DateTime` в столбце `timestamp without time zone` | для H2 без часового пояса это `DateTimeKind.Unspecified`; тест поправлен |

Оба — настоящие дефекты провайдера, а не теста.

## Миграции: сверка с h2.jar (23 ноября 2026)

Все 119 тестов `MigrationsH2Test`, помеченных `BlockedByPort`, теперь проходят. Каждая DDL-форма
сначала проверялась на зафиксированном `h2-2.5.259-SNAPSHOT.jar`; там, где эталон формы не имеет,
провайдер её не пишет, а тест ожидает refusal — наоборот, где форма эталоном принята, порт обязан
её прочитать.

Что исправлено:

* **Каталог (`H2DatabaseModelFactory`)** читает все схемы через `INFORMATION_SCHEMA.SCHEMATA`,
  комментарии таблиц и столбцов (`REMARKS`), длины/точность типов, опции identity
  (`IDENTITY_START`/`IDENTITY_INCREMENT`/…), перечисления (`DOMAINS` + `ENUM_VALUES`),
  порядок сортировки индексов и покрывающие (`INCLUDE`) столбцы. Это убрало десятки ложных
  расхождений между моделью и сгенерированными миграциями.
* **Формы DDL.** Вместо постгресовых `serial`, `integer[]`, `ADD GENERATED … AS IDENTITY`,
  `text`, `TIMESTAMPTZ`, `pg_get_serial_sequence`/`setval` провайдер пишет формы H2 2.5:
  `integer GENERATED BY DEFAULT AS IDENTITY`, `integer ARRAY`, `varchar`, `timestamp`,
  `ALTER COLUMN … SET GENERATED BY DEFAULT`, `RESTART WITH` с подзапросом по `IDENTITY_BASE`.
* **Отказы вместо неверного SQL.** `ALTER SEQUENCE … RENAME TO`, `ALTER … SET SCHEMA`,
  фильтрованные индексы и колляция столбца отсутствуют в эталоне — провайдер кидает
  `NotSupportedException`, тесты ожидают отказ. `DROP IDENTITY` и `ALTER TABLE … RENAME TO`
  эталон принимает, и порт их читает.
* **Настоящий дефект движка.** `SequenceOptions.cycle` по умолчанию был `NO_CYCLE`, поэтому
  любое `ALTER SEQUENCE … START WITH` сбрасывало `CYCLE`. В эталоне поле не задано и сохраняет
  прежнее правило. Исправлено в H2Sharp 0.1.44-alpha.1 (`SequenceOptions`, `Sequence`),
  оракульный тест `Alter_sequence_all_settings` зелёный.
* **Схема `public`.** EF-модели с PostgreSQL-стилевой `public` теперь отображаются на `PUBLIC` H2 в
  `H2SqlGenerationHelper`; иначе порт создавал не ту схему.
* **JSON.** H2 2.5 умеет приводить строку к `json`, поэтому три теста, ожидавшие ошибку
  PostgreSQL `42001`, переписаны на проверку успешного приведения.
* **Системные столбцы (`xmin`, `oid`).** В H2 их нет: это обычные столбцы, и тесты теперь
  проверяют именно такое поведение.

Тогда зелёный гейт не изменился: 6271/129/317 на каждую TFM. Красный прогон функционального пакета
снизился с 164 до 79 падений; `ModelTests` — 4 падения.

### Долечивание провайдера после миграций (23 ноября 2026)

* `decimal` без явно заданной точности теперь отображается в `numeric(18,2)`: голый
  `NUMERIC` в H2 2.5 имеет scale 0 и молча превращал 1.99 в 2 (четыре теста `UpdatesH2Test`).
* Движок научился отдавать накопленные update counts через `RecordsAffected` смешанного
  пакета; это убрало бы четыре concurrency-теста, но полного эффекта не дало — шесть
  `UpdatesH2Test` всё ещё ждут `DbUpdateConcurrencyException` от EF, который не читает
  count в этом сценарии. Оставлены под трейтом.
* Обновлено ожидание `ForumScaffoldingTests`: провайдер возвращает DDL-написания типов
  (`integer`, `varchar(50)`, `numeric`) и длину/точность из каталога, а не верхний регистр
  псевдонимов H2.

Итог: green-gate 6271/129/317 на каждую TFM без изменений; красный functional-прогон
75 падений, `ModelTests` — 4.

### Scaffolding: каталог H2 и фильтры (23 ноября 2026)

* `PrincipalColumns` внешнего ключа теперь читаются из `KEY_COLUMN_USAGE` по ограничению,
  на которое ссылается FK; до этого коллекция всегда была пуста (шесть тестов).
* Фильтры таблиц разбирают quoted-имена (`"K.2"`) как идентификатор, а не делят строку по
  первой точке; фильтры схем сопоставляются с каталогом H2 без учёта регистра, при этом
  unquoted имя (`db2`) разрешается в upper-case схему (`DB2`), а quoted lower-case остаётся
  адресуемым.
* Комментарии/длины/точность/identity-опции/enums/сортировка индексов читаются из каталога;
  для `NUMERIC` точность берётся из `DECLARED_NUMERIC_PRECISION/SCALE`, иначе H2 отдаёт
  scale 0.
* Отсутствующие схема/таблица и отсутствующий principal-таблица FK теперь дают
  предупреждения EF (`MissingSchemaWarning`, `MissingTableWarning`,
  `ForeignKeyReferencesMissingPrincipalTableWarning`) вместо исключения.
* Тесты приведены к H2: quoted имена индексов/enum-типов, upper-case `PUBLIC`/`DB2` в
  сообщениях, H2-литералы default-значений и `CONSTRAINT_*` имена автогенерируемых
  ограничений.

Красный functional-прогон: 75 → 51 падений. Осталось 16 scaffolding-тестов, которые
упираются в неперенесённые DDL движка (`CREATE DOMAIN`, materialized view, enum-колонки,
serial-последовательности, частичные индексы) и PG-типы (`money`, `jsonb`, `varbit`).

### Проверка оставшихся «пробелов» на h2.jar (23 ноября 2026)

Эталонный `h2-2.5.259-SNAPSHOT.jar` принимает: `CREATE DOMAIN` (включая `NOT NULL`),
`CREATE VIEW`, `CREATE MATERIALIZED VIEW`, enum-колонки (`CREATE TYPE ... AS ENUM` +
`CREATE TABLE(... mood mood)`), `serial`, `varchar(66) ARRAY`, `time(1)`, `timestamp(3)`,
`bytea`, `int2/int4/int8`, `float4/float8`. Значит это не «нет в эталоне», а работа порта.

Эталон **не** принимает: `varbit`, `timetz`, `timestamptz`, `interval(5)`, `money`, `jsonb`,
`bit(111)`, `CREATE INDEX ... WHERE`; эти тесты остаются PG-специфичными или переписываются
под H2.

Провайдер уже читает домены, enum-колонки (пропускаются до enum-маппинга), views и
materialized views (view + скрытая backing-таблица `NAME$1`), а также сериализует
`RecordsAffected`; красный functional-прогон стал 19 (6 пропущено), scaffolding-кластер — 0 тестов.

### Дальнейшая сверка scaffolding с h2.jar (23 ноября 2026)

После проверки точных форм на эталонном jar:

* `CREATE DOMAIN ... NOT NULL`, `DEFAULT nextval(...)` в определении колонки, partial index
  (`CREATE INDEX ... WHERE`) эталон **не** принимает — эти тесты помечены `Skip` с причиной,
  это не дефекты порта.
* `CREATE VIEW`, `CREATE MATERIALIZED VIEW`, enum-колонки, `serial`, `time(2) with time zone`,
  `timestamp(4) with time zone`, `varchar(66) ARRAY` эталон принимает; провайдер читает их
  (views/materialized views как `DatabaseView`, backing-таблица `NAME$1` скрывается,
  enum-колонки пока пропускаются, time/timestamp с зоной получают precision в правильном
  месте).
* PG-only типы (`money`, `jsonb`, `varbit`, `timetz`, `interval(5)`, `bit(111)`) остаются
  только в тестах, которые ещё не переписаны; эталон их не знает.

Красный functional-прогон: 19 падений и 6 пропусков; scaffolding — 0 падений.

### Политика для невозможного в H2 (23 ноября 2026)

Тесты, которым нужна функциональность, отсутствующая в H2 2.5, не «воркэраундятся»
постгресовыми конструкциями: они либо переписаны на H2-эквивалент, либо помечены
`Skip` с точной причиной и ссылкой на проверку `h2.jar`. Это относится к
`Column_default_type_names_are_scaffolded`, `Store_types_without_any_facets`,
`Default_value_matching_clr_default_is_not_stored`, `Set_filter_for_filtered_index`,
`SequenceNonSerial`, `Column_nullability_is_set_with_domain`. Тесты на то, что H2 умеет
(enum-колонки, views, domains, serial), остаются в наборе и являются дефектами порта.

### Enum/FK/фильтры: сначала оракул (23 ноября 2026)

Перед разбором последних трёх scaffolding-падений добавлен JVM-оракул
`EnumDdlOracleTests.Enum_column_with_unique_and_foreign_key_matches_the_reference`:
enum-тип, колонка с `UNIQUE`, вторая `UNIQUE`-колонка и FK на неё. Оракул прошёл —
значит порт синтаксис понимает, и проблема была не в движке.

Причины падений оказались в провайдере/тестах:

* ключи и индексы, все колонки которых — enum-колонки, попадали в модель пустыми;
  теперь пустые PK/UNIQUE/индексы удаляются;
* `Bug453` создавал `"mood"` quoted, а колонку объявлял без кавычек (`mood mood`) —
  H2 искал тип `MOOD`; тест приведён к согласованным именам;
* `Complex_filtering_validation` ожидал, что фильтр `["db2"]` не влияет на таблицы
  из `db.2`/`PUBLIC`; H2 применяет `DatabaseModelFactoryOptions.Schemas` ко всем
  объектам, тест приведён к этому поведению.

Scaffolding-класс: 50 проходят, 6 PG-only пропущены, 0 падений. Красный functional:
39 падений и 6 пропусков; green-gate: 6272/129/317.

### Массивы: List vs array в тестовом rewrite (23 ноября 2026)

`ArrayListQueryTest` переписывает запросы, написанные против массивов, на свойства-`List<T>`.
После этого `MemoryExtensions.Contains`/`SequenceEqual` держались за span-перегрузки, и
`Expression.Call` падал ещё до SQL: `List<int>` нельзя передать в `ReadOnlySpan<int>`.
Rewrite теперь:

* убирает implicit array→span conversion;
* заново привязывает `Contains`/`SequenceEqual` к `Enumerable`;
* переводит массивы-параметры в `List<T>`;
* снимает `Convert`-обёртки, которые остаются от array-to-span.

Выражения дошли до SQL; 20 падений сократились до 10. Оставшиеся 10 — расхождения
SQL-базлайнов (`$left`/`$elem`, имена параметров), а не ошибки выполнения.

### Массивы: обновление SQL-базлайнов (24 ноября 2026)

После перевода выражения на `Enumerable` остались расхождения только в SQL-ожиданиях:
провайдер пишет `array_contains("s"."IntList", ...)` и `$__ToList_0`, тогда как старые
базлайны ждали `$left`/`$elem` и `$__arr_0`. Пять методов обновлены по фактическому SQL.
Осталось 6 падений: `Byte_array_parameter_contains_column` (конверсия SMALLINT/BINARY VARYING),
`SequenceEqual_over_nullable_with_parameter` (возвращает 0 строк) и
`ArrayArrayQueryTest.SequenceEqual_over_nullable_with_parameter` (span в параметре);
это уже поведение провайдера/движка, а не базлайны.

### Nullable SequenceEqual: IS NOT DISTINCT FROM (24 ноября 2026)

H2's plain `=` over arrays containing NULL yields UNKNOWN, so a row whose array has a NULL
element disappears from WHERE; C# `SequenceEqual` treats that NULL as equal. The SQL
generator now writes `left IS NOT DISTINCT FROM right` when either side is an array with a
nullable element. SQL is correct and the reference accepts it, but the four
`SequenceEqual_over_nullable_with_parameter` cases still return 0 rows -- the parameter/data
path needs further tracing. `ArrayArrayQueryTest` also got a span-to-Enumerable rewrite, so
its expression no longer fails before SQL. Green gate is unaffected (6271/129/317).

### byte[] с DbType.Byte больше не VARBINARY (24 ноября 2026)

`Byte_array_parameter_contains_column` уходил в `VARBINARY`: `H2ValueMapper` видел `byte[]`
раньше, чем `DbType.Byte`, который ставит array-маппинг. Теперь байтовый массив с
`DbType.Byte` разворачивается в SQL ARRAY of SMALLINT; `DbType.Binary` остаётся бинарной
строкой. Базлайн параметра обновлён на `0x14 (DbType = Byte)`. Остались 4 nullable
`SequenceEqual` (SQL `IS NOT DISTINCT FROM` корректен, но запрос возвращает 0 строк).

### Массивы: кластер зелёный (24 ноября 2026)

После ADO-исправления `byte[]`/`DbType.Byte`, `IS NOT DISTINCT FROM` для nullable-массивов и
выравнивания SQL-базлайнов (параметр `List<int?>` идёт как `DbType = Int32`, alias quoted)
весь `ArrayListQueryTest` + `ArrayArrayQueryTest` зелёный: 200 проходят, 4 пропущены,
0 падений.

### Провайдер долечен: TIME, affected count, ambient, JSON (24 ноября 2026)

Закрыты последние кластеры, не требующие новых возможностей SQL:

* **`TIME` без точности — `TIME(0)`.** В H2 2.5 колонка `time` хранит целые секунды, поэтому
  `TimeOnly`/NodaTime `LocalTime` теряли дробную часть. Конвенция и NodaTime-плагин пишут
  `time(7)` — семь цифр тиков .NET; проверено на jar (`TIME '12:30:45.123'` в `TIME` даёт
  `12:30:45`, в `TIME(7)` — `12:30:45.1234567`).
* **`FORMATDATETIME` и java-шаблон.** Переводчик `DateTimeOffset.DateTime`/`.UtcDateTime`
  писал `SSSSSSSSS`; порт `FORMATDATETIME` принимает .NET-шаблоны по утверждённому
  расхождению D-052, поэтому `S` оставался литералом и `CAST(… AS timestamp)` падал.
  Шаблон заменён на `FFFFFFF`; остальные date-time переводы не менялись.
* **Число строк `UPDATE`/`DELETE`.** Без `RETURNING` пакет EF не видел нуля и не бросал
  `DbUpdateConcurrencyException`: шесть `UpdatesH2Test` были красными. Теперь `UPDATE`
  оборачивается в `SELECT COUNT(*) FROM FINAL TABLE (…)`, `DELETE` — в `OLD TABLE`
  (`FINAL TABLE` не принимает `DELETE`); форма проверена на jar и на порте. Миграционные
  команды (без tracked entries) остаются простыми `UPDATE`/`DELETE`.
* **Ambient-транзакции и закрытие соединения.** `H2Enlistment` владеет своей сессией;
  закрытие соединения отвязывает сессию, но не отменяет enlistment. Повторное открытие в
  той же области возвращает ту же сессию, в новой (`RequiresNew`) — заводит отдельную.
  Три `TransactionH2Test` и два ADO-теста `H2TransactionScopeTests` это фиксируют.
* **`DateTime.SpecifyKind` и `ConvertTimeToUtc`.** `SpecifyKind(..., Utc)` теперь не пишет
  `AT TIME ZONE`: он меняет только .NET `Kind`, а timestamp без зоны уже хранит нужный
  wall clock. `ConvertTimeToUtc` сначала приводит timestamp к зоне сессии, затем пишет
  момент в UTC через `FORMATDATETIME` и читает его обратно timestamp'ом.
* **NUMERIC с масштабом больше 28 читается как double.** `H2DataReader.GetDouble`/`GetFloat`
  для `NUMERIC`/`DECFLOAT` идут в `BigDecimal` напрямую, а не через `decimal`; точные
  `GetDecimal`/целые геттеры по-прежнему отказывают, если значение не помещается.
* **`LazyLoadProxy` и decimal.** Ожидание `Charge: 1.0` было написано под старый numeric;
  провайдер хранит decimal как `numeric(18,2)`, поэтому ожидание — `1.00`.
* **Литералы — через `Dialect`.** `H2TimestampTzTypeMapping` для `DateTime` и
  `H2GeometryTypeMapping` больше не собирают SQL вручную: `Dialect.AppendTimestampWithTimeZone`
  и `Dialect.AppendGeometry`. Встроенные (embedded) строки JSON не трогались: это не SQL-литерал.

Итог: functional green gate **10925/0/11010** (85 пропущено), unit **151/151**, model
**325/325** на `net9.0` и `net10.0`; полный functional — 10925 прошло, 85 пропущено,
1 красный: `H2ComplianceTest` по замыслу. Последний тест конвертеров, `("Values")`,
открылся после исправления `IsDirectQuery` в движке: quoted keyword больше не читается
как ключевое слово. Остальные `BlockedByPort` сняты.
