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

## Измерение

   | Набор | Прошло | Упало | Всего |
   |---|---:|---:|---:|
   | `H2Sharp.EntityFrameworkCore.FunctionalTests` | 940 | 0 | 940 |
   | `H2Sharp.EntityFrameworkCore.Tests` | 111 | 0 | 111 |
   | **Итого** | **1051** | **0** | **1051** |

   Набор `H2Sharp.EntityFrameworkCore.Tests` собирается по-разному в Debug и в Release: четыре
   теста `LegacyH2TypeMappingTest` написаны под `#if DEBUG`. В Debug их 115, и два из них
   красные — см. ниже; в Release их 111, и все зелёные.

   Разделение по трейту:

   | Прогон | Прошло | Упало | Всего |
   |---|---:|---:|---:|
   | `Category!=BlockedByPort` | 940 | 0 | 940 |
   | `Category=BlockedByPort` | 17 | 442 | 465 |

   Два теста пропущены в обоих прогонах. Числа выше — на `net10.0`, против движка с
   `FINAL TABLE`, с последовательностями и с `AS` у псевдонима.

   **Прогон с трейтом больше не обрывается.** Раньше тестовое оборудование скаффолдинга
   выполняло блок `DO … IF NOT EXISTS (SELECT …)`, движок его не читал, и тестовый хост падал
   вместе с ним — оставшиеся тесты не доходили. Блок `DO` заменён на H2'овскую форму, и
   прогон идёт целиком, поэтому числа ниже измерены одним прогоном: миграционный набор
   (194 теста: 16 прошло, 176 упало, 2 пропущено), набор скаффолдинга (9 тестов, 13 случаев
   — все красные), наборы `DefaultValuesTest` и `H2ComplianceTest` (2 теста, оба красные).

   Два теста в `H2Sharp.EntityFrameworkCore.Tests` красные и не связаны с портом: они
   проверяют `LegacyTimestampBehavior`, и их результат зависит от порядка, в котором
   отработали тесты функционального набора. В Release их нет вовсе (см. выше).

Оставшиеся два — не сбой провайдера.

| Тест | Причина | Кто чинит |
|---|---|---|
| `DefaultValuesTest.Can_use_H2_default_values` | in-memory база исчезает вместе с последним соединением, и тестовое оборудование открывает новое соединение на каждом шаге: `Table "Chips" not found (this database is empty)` | дефект порта |
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
| `H2QuerySqlGenerator` | 1 | приведение типа пишется как `CAST(x AS y)`, а не как PostgreSQL'овское `x::y`: короткой записи в H2 2.5 нет, и `count(*)::int` эталон не берёт |
| `H2MigrationsSqlGenerator`, `H2HistoryRepository` | 6 | блок `DO $EF$` — анонимный блок PL/pgSQL — заменён на H2'овское `CREATE SCHEMA IF NOT EXISTS` и на условие внутри самой вставки: `INSERT … SELECT … WHERE NOT EXISTS (…)` |

## Формы типов: что провайдер пишет и почему именно так

Провайдер унаследовал от Npgsql не только форму, но и *названия* вещей, которых в H2 2.5 нет.
Сверка с эталоном (`h2-2.5.259-SNAPSHOT.jar` на JVM) и с исходниками оригинала
(`reference/h2/h2/src/main/org/h2/command/Parser.java`, `value/DataType.java`) показала:

| Что | Было (PostgreSQL) | Стало (H2 2.5) | Что говорит эталон |
|---|---|---|---|
| тип массива | `integer[]` | `integer ARRAY` | `CREATE TABLE t (x integer[]);` → `Syntax error … expected ")"` |
| литерал массива | `ARRAY[1,2]::integer[]` | `ARRAY[1,2]` | короткой записи `::` в H2 2.5 нет вовсе |
| литерал времени с зоной | `TIMESTAMPTZ '…'` | `TIMESTAMP WITH TIME ZONE '…'` | `SELECT TIMESTAMPTZ '…'` → `Syntax error` |
| литерал времени с зоной (время) | `TIMETZ '…'` | `TIME WITH TIME ZONE '…'` | то же |
| литерал интервала | `INTERVAL '1 2:3:4'` | `CAST('1 2:3:4' AS INTERVAL DAY TO SECOND)` | `Cannot parse "INTERVAL" constant "1 2:3:4"`: у H2 литерал интервала несёт квалификатор, а приведение текста к тому же типу даёт то же значение |
| литерал перечисления | `'a'::my_enum` | `CAST('a' AS my_enum)` | короткой записи `::` нет |
| часть даты | `date_part('year', x)` | `year(x)` | `Function "DATE_PART" not found`: часть даты в H2 берётся своей функцией |
| день недели | `floor(date_part('dow', x))::int` | `mod(iso_day_of_week(x), 7)` | ISO нумерует неделю с понедельника, .NET — с воскресенья |
| «содержит» | `strpos(x, y) > 0` | `locate(y, x) > 0` | `strpos` у H2 нет, а у `LOCATE` аргументы в обратном порядке |
| сейчас | `now()` | `current_timestamp` | `NOW()` у H2 — другое имя `LOCALTIMESTAMP`, то есть время без зоны |

Проверено на эталоне: `SELECT YEAR(TIMESTAMP '2020-01-02 10:20:30')` → 2020,
`SELECT MOD(ISO_DAY_OF_WEEK(TIMESTAMP '2020-01-05 00:00:00'), 7)` → 0 (воскресенье),
`SELECT LOCATE('bc', 'abcd')` → 2, `SELECT EXTRACT(EPOCH FROM INTERVAL '1 2:3:4' DAY TO SECOND)` → 93784.

`EXTRACT` в H2 2.5 — синтаксис, а не вызов функции: `EXTRACT(field FROM value)`. Вызов
`extract('epoch', x)` эталон отвергает («expected "FROM"»). Поэтому генератор запросов
провайдера пишет `EXTRACT` синтаксисом, а полем может быть и слово, и строка в кавычках —
эталон берёт оба.

**Что из этого провайдер может, а что ждёт движок.** Таблицу с массивом создать нельзя:
движок не читает тип `ARRAY` (в `TypeInfo` порта нет места для типа элемента), и литерал
массива не разбирается. Остальное движок читает: `LOCATE`, `MOD`, `YEAR`, `MONTH`, `DAY`,
`DAY_OF_YEAR`, `HOUR`, `MINUTE`, `SECOND`, `ISO_DAY_OF_WEEK`, `EXTRACT` у него есть. Пробел
движка с типом `ARRAY` записан в issue #14 движка.

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
| срез массива | `arr[1:2]` | `array_slice(arr, 1, 2)` | синтаксиса среза у H2 нет: `expected "[, ., ::, AT, FORMAT, …"` |
| массив из подзапроса | `= ANY ((SELECT …)::integer[])` | `= ANY (CAST((SELECT …) AS integer ARRAY))` | без приведения H2 считает правую часть запросом, а не массивом: «Values of types "INTEGER" and "INTEGER ARRAY[3]" are not comparable» |
| квантованное сравнение | `x = ANY (arr)` | `x = ANY (arr)` | форма общая: `SELECT 1 = ANY(ARRAY[1,2,3])` → TRUE |

**Чего у H2 2.5 нет, и потому перевода нет:**

| Поведение | Форма PostgreSQL | Почему её нет у H2 |
|---|---|---|
| место элемента в массиве | `array_position(arr, item)` | `Function "ARRAY_POSITION" not found`; `ARRAY_CONTAINS` отвечает, есть ли элемент, а не где он |
| байт двоичного по номеру | `get_byte(b, i)` | функции `get_byte` у H2 нет, и двоичное не приводится к числу («Data conversion error converting "BINARY VARYING to INTEGER"») |
| пересечение двух массивов | `arr1 && arr2` | оператора `&&` у H2 нет |
| один массив в другом | `arr1 <@ arr2` | оператора `<@` у H2 нет |
| элементы массива строками | `LEFT JOIN LATERAL unnest(arr) WITH ORDINALITY` | и `UNNEST`, и `LATERAL` у H2 нет; форма H2 — таблица-диапазон и элемент по номеру: `SELECT t.id, r.x, array_get(t.arr, r.x) FROM t, system_range(1, 65536) r WHERE r.x <= array_length(t.arr)`, но элемент там — выражение над строкой, а в модели EF коллекция это столбец присоединённой таблицы ⇒ форму H2 в модель EF не записать |

Первые три поведения переводятся на клиентскую сторону (EF читает значение и считает на клиенте),
последние два — у H2 ответа нет, и тесты на них из набора убраны.

**Что осталось в наборе.** 204 теста, и все они красные по одной причине: движок не читает тип
`ARRAY`, таблица фикстуры не создаётся, и тест падает, не начавшись. Набор помечен трейтом
`BlockedByPort` — это пробел движка, а не провайдера (issue #14 движка).

## Проверено против эталона

| Что | Провайдер | Эталон H2 2.5 | Порт H2Sharp |
|---|---|---|---|
| `CREATE TABLE` с кавыченными именами | `CREATE TABLE "Entries" ("Id" integer NOT NULL, …)` | принимает | принимает |
| `INSERT` с параметрами | `INSERT INTO "Entries" ("Id", …) VALUES (?, …)` | принимает | принимает |
| EF-форма `SELECT` | `SELECT "e"."Id", … FROM "Entries" AS "e" LIMIT 2` | принимает | принимает — с того момента, как порт перенёс `AS` у псевдонима и кавыченную ссылку на столбец |
| identity-колонка | `"Id" integer GENERATED BY DEFAULT AS IDENTITY` | принимает | принимает — с того момента, как порт перенёс identity-колонки |
| `INSERT`, который возвращает сгенерированный ключ | `SELECT "Id" FROM FINAL TABLE (INSERT INTO "Items" ("Name") VALUES (?))` | принимает | принимает — с того момента, как порт перенёс `FINAL TABLE` |
| `TIME WITH TIME ZONE` | `"Moment" timestamp with time zone NOT NULL` | принимает | **падает** |
| чтение большого объекта | `SELECT` из колонки `clob` | принимает | **падает** — контракт ADO у порта уже верный (`GetValue` отдаёт материализованное значение, поток — в `GetStream`/`GetTextReader`), не хватает одного шага: `ValueLob.CopyToResult` переносит содержимое LOB в результат, а он не портирован |

## Пробелы движка, из-за которых провайдер пока не проходит запросы

| № | Пробел | Симптом | Что нужно порту |
|---|---|---|---|
| 1 | **Производная таблица** | `SELECT * FROM (SELECT …) "t"` → `Table "(" not found` | `FROM (SELECT …) [AS] alias`: подзапрос в `FROM` — а без него не работает ни один `FromSql` над сущностью с ключом и ни один `FromSql` в linq2db-провайдере |
| 2 | **Соединение в любой форме** | ни запятое соединение (`FROM T t, SYSTEM_RANGE(1, 3) r`), ни `CROSS JOIN`, ни `JOIN … ON` не читаются: `ожидалось "statement"` | соединения в `FROM` |
| 3 | **`FOREIGN KEY` в `CREATE TABLE`** | ошибка `ожидалось "PRIMARY"` | констрейнты (фаза 7 ROADMAP h2sharp) |
| 4 | **Типы `INTERVAL`** | `interval day to second` → `ожидалось "INTERVAL"` | интервальные типы в парсере; без квалификатора `INTERVAL` типом не является |
| 5 | **`DB_CLOSE_DELAY` для in-memory базы** | база исчезает вместе с последним соединением, настройка не действует | `DB_CLOSE_DELAY`, иначе in-memory база не переживает `EnsureCreated` |
   | 6 | **`CREATE SCHEMA`** | `CREATE [*]SCHEMA IF NOT EXISTS db2` | схемы; скаффолдинг делает схему себе перед тем, как спросить о её таблицах |
   | 7 | **`ALTER TABLE`** | `ALTER TABLE … ALTER COLUMN …` → `Not ported yet: Parser.readAlterTable: only ADD and DROP CONSTRAINT are ported` | `ALTER TABLE` целиком: пока читаются только `ADD` и `DROP CONSTRAINT` |
   | 8 | **Каталог отличается от эталонного** | имя каталога — путь базы в URL, а не имя базы; collation колонки пуст | каталог как в H2 2.5 |

   Закрытые пробелы: DDL identity-столбка, значение identity обратно (`FINAL TABLE`),
   последовательности (`CREATE SEQUENCE`, `NEXT VALUE FOR`), типизированный литерал в
   `DEFAULT`, `AS` у псевдонима таблицы и столбца и кавыченная ссылка на столбец — их
   порт перенёс, и провайдер пишет ровно то, что H2 2.5 требует.

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
* **Приведение типа — `CAST(x AS y)`, а не `x::y`.** Короткая запись — PostgreSQL'овская, и
  H2 2.5 её не берёт: `SELECT count(*)::int` эталон отвергает. Провайдер пишет стандартную
  форму и типы H2 (`integer`, а не PostgreSQL'овское `int`).
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

## Следствия для тестов

* In-memory база умирает вместе с соединением (пробел 5), поэтому тест обязан **держать
  соединение открытым** на всё время проверки — `context.Database.OpenConnection()`.
  Иначе `EnsureCreated` отработает по одной базе, а чтение пойдёт по другой и увидит её пустой.
* Тесты, требующие `SELECT`, останутся красными до пробелов 1 и 2. Это расхождение движка, а
  не провайдера, и реестр существует именно для того, чтобы их различать.

## Что движок умеет сегодня, и почему спецификационный набор ещё не гоняется по базе

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
| `INFORMATION_SCHEMA` | **читается** (коммит движка `17708ad`): каталог порта отдаёт таблицы, колонки, ключи и ссылки; расходятся имя каталога (путь базы в URL вместо имени базы) и `collation` колонки | расхождение — дефект порта |
| `CREATE SCHEMA` | `CREATE [*]SCHEMA IF NOT EXISTS db2` — ожидалось `TABLE` | дефект порта |
| `DROP ALL OBJECTS` | `DROP [*]ALL OBJECTS` — ожидалось `TABLE` | дефект порта |
| `LEFT JOIN` | `FROM T3 a [*]LEFT JOIN T3 b` — ожидалось `statement` | дефект порта |
| `IN (подзапрос)` | `Parser.parseQuerySelect: IN with a subquery is not ported` | дефект порта |
| переменные в тексте SQL | `Parser.readTerm: variables (@name) are not ported` — но слой ADO переписывает `@имя` в `?` до разбора, так что через провайдер это не мешает | — |
| файловая база | `Engine.openSession: persistent databases need the file store` — хранилище в порте есть и формат его совместим с H2 2.5, но сессия не открывает файловую базу | дефект порта |
| `H2SharpException` с публичным конструктором | конструктор `internal`, тестовый набор не может сфабриковать отказ соединения | дефект порта |

### Следствия для тестового оборудования

- **База живёт ровно столько, сколько живёт соединение.** `mem:`-база исчезает, когда закрывается
  последнее соединение, а `DB_CLOSE_DELAY` в порте нет и сессия не открывает файловую базу. Тест-стор обязан
  **держать одно соединение открытым** всё время жизни стора, иначе база исчезнет между тестами.
- **Чистка базы не может идти через каталог.** `INFORMATION_SCHEMA` нет, поэтому перечислить таблицы
  нечем. Два пути: `DROP ALL OBJECTS` (движок не умеет) или удаление строк по таблицам, которые
  провайдер знает из модели EF.
- **Northwind для EF-набора нужен свой.** Скрипт `Northwind.sql` из форка — PostgreSQL-овский:
  `serial`, `::`-касты, `SET`, ICU-колляция, `CREATE COLLATION`. Движок это не разбирает. Нужен
  H2-скрипт с тем же содержимым.
- **`IRelationalCommand` в EF Core 9 — шаблонный**, а не тот интерфейс, под который написан
  Npgsql-овский фейк `TestRelationalCommand`. Фейк придётся писать заново.
## Миграционный набор EF Core: 199 тестов, 177 из них ждут движок

`Migrations\MigrationsH2Test.cs` — это 3000 строк общих миграционных тестов EF Core, и до сих пор
файл был целиком исключён. Он возвращён: из него вырезано то, что является PostgreSQL-овым, а не
EF-овым (полнотекстовый поиск `tsvector`, `CREATE COLLATION`, `CREATE EXTENSION`, версионно-зависимые
опции), и он даёт **199 новых тестов**.

Из них 176 падают, и разбор по тексту ошибки даёт точную картину:

| Причина | Тестов | Чей |
|---|---:|---|
| таблица осталась от прошлого теста: `EnsureDeleted` не удаляет, а `DROP ALL OBJECTS` движок не читает | 127 | следствие: движок |
| `COMMENT ON` не разбирается | 6 | движок |
| `CREATE TYPE … AS ENUM` не разбирается | 8 | движок |
| `LEFT JOIN` не разбирается | 7 | движок |
| `UNLOGGED TABLE`, `COLLATE`, `CREATE COLLATION` | 5 | движок |
| `CREATE SCHEMA` не разбирается | 7 | движок |
| `ALTER TABLE` — `RENAME TO`, `ALTER COLUMN`, `DROP COLUMN`, `ADD CONSTRAINT … FOREIGN KEY` | 4 | движок |
| `CREATE TABLE` с массивом, с генерируемым столбцом или с внешним ключом | 8 | движок |
| прочее: схема в имени таблицы | 1 | движок |

`GENERATED BY DEFAULT AS IDENTITY` и `CREATE SEQUENCE` в этом списке больше нет: движок перенёс
identity-колонки и последовательности, и DDL, который провайдер пишет, теперь принимается.
Блока `DO $EF$` в списке тоже нет: он был PostgreSQL'овским, и провайдер пишет H2'овскую
`CREATE SCHEMA IF NOT EXISTS` вместо него. Шестнадцать тестов миграционного набора проходят,
и осталось 176.

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
| `DateTime.AddDays` и прочая арифметика дат | `INTERVAL '1 days'` и `CAST($1 \|\| ' days' AS interval)` — движок не читает ни того, ни другого | `TIMESTAMPADD('DAY', 1, x)` |
| `EnsureDeleted()` при подключении строкой | `H2DatabaseCreator` открывал соединение сырой ADO-строкой, а движок ждёт URL | перевод через `H2RelationalConnection.H2Url` |
| `Reverse`, `StringToArray`, `ToDate`, `ToTimestamp`, `Distance` | методы в `DbFunctions`, которые никогда не переводились и всегда падали | убраны: таких функций в H2 нет |

### Два новых пробела движка

- **`AS` перед алиасом таблицы не разбирается.** `SELECT a.Id FROM T3 a` — работает,
  `SELECT a.Id FROM T3 AS a` — нет: `Синтаксическая ошибка ... FROM T3 AS [*]a`. EF строит
  `FROM "Rows" AS "r"` в каждом запросе, то есть **ни один запрос EF не выполняется**, пока
  движок это не прочитает. Это самый дорогой пробел из всех.
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
эталон, и порт. Псевдонимы **столбцов** свой `AS` сохраняют: «голый» псевдоним столбца (`x y`)
движок не читает, а `x AS y` читает.

Проверено и на эталоне: тест `H2GeneratedSqlReferenceTest.The_generated_query_is_accepted_by_the_reference_engine`
отдаёт сгенерированный провайдером запрос эталонной H2 и требует, чтобы она его приняла.

**Побочный эффект: EF-запросы теперь выполняются.** Первый запрос EF против порта прошёл:
`SELECT "r"."Name", "r"."Price" FROM "Rows" "r" WHERE "r"."Price" > @p0` — с фильтром,
проекцией и вызовом `EF.Functions.LeftPad`. До этого ни один запрос EF не мог выполниться.

### Что ещё вскрылось при этом

| Что | Было | Стало |
|---|---|---|
| `UseH2("jdbc:h2:mem:x")` | `H2Url` брал строку как key=value и падал: `Format of the initialization string does not conform to specification` | JDBC-URL передаётся движку как есть |
| `DefaultValuesTest` | значение по умолчанию — UTC `DateTime` в столбце `timestamp without time zone` | для H2 без часового пояса это `DateTimeKind.Unspecified`; тест поправлен |

Оба — настоящие дефекты провайдера, а не теста.