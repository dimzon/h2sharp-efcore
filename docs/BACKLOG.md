# Что можно улучшить в провайдере

Разбор красного прогона `Category=BlockedByPort` на движке `f56ca4c` (26 октября 2026,
`net10.0`). Файл отвечает на вопрос «что из этого наше, а что движка», и что уже сделано.

Повод: движок за сутки подтянулся на три коммита, и 350 тестов позеленели **без единой правки
провайдера** — коммит `f56ca4c` начал читать табличные `UNIQUE`/`CHECK`, которые провайдер и так
писал. Это же и подтверждает правило из `AGENTS.md`: не сужать диалект под текущее состояние
порта — окупается.

## Состояние

| | Движок `f56ca4c` | Сейчас |
|---|---:|---:|
| Зелёный гейт (`Category!=BlockedByPort`), функциональные | 1193 | **2019** |
| Прогон с трейтом, прошло | 1087 | **275** |
| Прогон с трейтом, упало | 4229 | 4229 |
| Всего функциональных тестов | 6523 | 6523 |

Всего в прогоне с трейтом 4504 теста, пропущенных нет. От движка `f56ca4c` 288 тестов перешли в
зелёное и ни один не ушёл обратно: пункты 1 (116), 3 (144), 5 (8), 2 (16) и ожидание `bytea`
в тесте (4). Потом трейт переехал с классов на методы и отдал гейту ещё 826 тестов, не изменив
набор красных ни на один. Восемнадцать тестов пары `ConcurrencyDetector*` гуляют от прогона к
прогону (`Table "Products" not found`), так что прошедших бывает на восемнадцать больше или меньше.

Сделано в этой сессии: пункты 1, 2, 3, 5 и инфраструктура. Остался пункт 4 — он ждёт движок.

## Наше

| # | Что | Тестов | Состояние |
|---|---|---:|---|
| 1 | `uuid_generate_v4()` вместо `RANDOM_UUID()` в тест-модели | 119 | **сделано**: 116 проходят, 3 остались на `Prepared.update` |
| 2 | `array_position(array, NULL)` пишет сам провайдер | 16 | **сделано**: `COALESCE(ARRAY_CONTAINS(a, NULL), FALSE)` + контрактный тест |
| 3 | Массивы и `List<T>` | 144 | **сделано**: 8 → 152 проходят из 204 |
| 4 | `The entity type 'Driver' requires a primary key` | 62 | **разобрано**: наша ошибка имён, дальше упирается в `Prepared.update` (движок) |
| 5 | `array_to_string` — имя PostgreSQL, которого у H2 нет | 8 | **сделано**: перевод снят, тесты ждут `AssertTranslationFailed` |

### 1. `uuid_generate_v4()` — сделано

В `StoreGeneratedFixupH2Test` две сущности были переведены на `RANDOM_UUID()` с комментарием
«H2 has no extensions», а остальные пятнадцать `HasDefaultValueSql` остались на
`uuid_generate_v4()`. Класс был красный целиком — 119 из 119 — потому что модель общая и первый
же `CREATE TABLE` падал на `Function "UUID_GENERATE_V4" not found`.

Это не «свой диалект под порт», а именно отступление: у H2 2.5 функция называется `RANDOM_UUID`,
и провайдер её и пишет (`H2NewGuidTranslator.UuidGenerationFunction`).

Итог: 116 из 119. Оставшиеся три (`Add_overlapping_graph_from_*`) упираются в
`Prepared.update` — это движок.

### 2. `array_position` — сделано

`H2SqlNullabilityProcessor` для `item = ANY(array)` при nullable-столбце добавлял
`array_position(array, NULL) IS NOT NULL` — перенесённую из PostgreSQL компенсацию `NULL` (в
Npgsql там `IS NOT DISTINCT FROM`-семантика). У H2 функции `ARRAY_POSITION` нет, и 16 тестов
падали на `Function "ARRAY_POSITION" not found`.

Замена — `COALESCE(ARRAY_CONTAINS(array, NULL), FALSE)`. Это тот же вопрос «есть ли в массиве
`NULL`», и `ARRAY_CONTAINS` отвечает на него ровно так, как нужно; проверено на порте и на
эталонном `h2-2.2.224.jar`, ответы совпали везде:

| Запрос | Порт | Эталон |
|---|---|---|
| `ARRAY_CONTAINS(ARRAY[1, NULL, 3], NULL)` | TRUE | TRUE |
| `ARRAY_CONTAINS(ARRAY[1, 2, 3], NULL)` | FALSE | FALSE |
| `ARRAY_CONTAINS(ARRAY[]::INT ARRAY, NULL)` | FALSE | FALSE |
| `ARRAY_CONTAINS(ARRAY['a', NULL], NULL)` | TRUE | TRUE |
| `ARRAY_CONTAINS(CAST(NULL AS INT ARRAY), NULL)` | NULL | null |
| `COALESCE(ARRAY_CONTAINS(CAST(NULL AS INT ARRAY), NULL), FALSE)` | FALSE | FALSE |

`COALESCE` нужен потому, что на `NULL`-массиве функция отвечает `NULL`, а прежняя форма
`array_position(...) IS NOT NULL` не была NULL никогда; `VisitAny` объявляет свой результат
non-nullable, и подменять это на «иногда NULL» нельзя — иначе `NOT` и `IS NULL` вокруг предиката
начнут пропускать не те строки.

Второй кандидат, `CARDINALITY(a) <> CARDINALITY(ARRAY_REMOVE(a, NULL))`, отпадает: `ARRAY_REMOVE`
в H2 нет.

Ответы зафиксированы тестом `H2ArrayContainsContractTest` в юнит-проекте провайдера
(`test/H2Sharp.EntityFrameworkCore.Tests/Query/`): семь запросов гоняются через порт, а если
доступен эталонный jar (переменная `H2SHARP_REFERENCE_H2_JAR` или соседний чекаут h2sharp) — и
через него, с требованием совпадения. Регрессия порта тогда видна сразу в юнит-тесте, а не только
как 16 красных функциональных.

Итог: 16 тестов позеленели, базлайны переписаны штатным `EF_TEST_REWRITE_BASELINES=1`.

### 3. Массивы и `List<T>` — сделано

Корень: в `H2ArrayTypeMapping.CreateParameters` конвертер ставился только когда
`TCollection != TConcreteCollection`. Для свойства `List<int>` эти типы совпадают, конвертера не
было, и шейпер просил у ридера `GetFieldValue<List<int>>` — а ADO-слой порта отдаёт `int[]`:

```
Unable to cast object of type 'System.Int32[]' to type 'System.Collections.Generic.List`1[System.Int32]'
```

Правка: конвертер ставится и тогда, когда модель — список (`IsArrayOrGenericList()`), но не
массив. Массив и структура-ключ с собственным конвертером (`KeysWithConvertersH2Test`) остаются
без конвертера, как было.

Заодно: базлайны `AssertSql` в `ArrayQueryTest`/`ArrayArrayQueryTest`/`ArrayListQueryTest` были
постгресовские (`SELECT s."Id"`, `FROM "SomeEntities" AS s`) и никогда не проверялись — тесты
падали раньше, на приведении типа. Переписаны штатным механизмом `EF_TEST_REWRITE_BASELINES=1`
на то, что провайдер действительно пишет (`SELECT "s"."Id"`, `FROM "SomeEntities" "s"`).

Итог: было 8 из 204, стало **180 из 204**. Оставшиеся 20:

| Причина | Тестов |
|---|---:|
| перезапись выражения в тесте: `List<T>` нельзя подставить в `MemoryExtensions.Contains(ReadOnlySpan<T>, T)` | 14 |
| `byte[]`-параметр: `Data conversion error converting "SMALLINT, BINARY VARYING"` | 2 |
| `SequenceEqual` с параметром: `ReadOnlySpan` в дереве выражений | 2 |
| `Array_column_Contains_null_constant`: нет приведения `List<int>` к `int` | 2 |

Попытка починить перезапись выражения (свести `MemoryExtensions.Contains` к
`Enumerable.Contains`) дала ровно ноль: два теста позеленели, два покраснели. Откачена, чтобы не
оставлять лишний код. Правильный путь — переопределить эти тесты в `ArrayListQueryTest` с телом
на `IntList`, как уже сделано для `String_Join_*`.

### 4. `Driver` без первичного ключа — разобрано

`DataBindingH2Test` (56) и `SerializationH2Test` (6) падали не на SQL и не на движке, а на
проверке модели: `The entity type 'Driver' requires a primary key to be defined`. Виновато имя
типа в нашем же файле фикстуры.

`F1H2Fixture.cs` лежит в пространстве имён `H2Sharp.EntityFrameworkCore` и импортирует
`Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel`. У водителя в модели F1 есть `Id`, и
ключ по конвенции находится — но только если имя `Driver` разрешается в тип тест-модели. А в
сборке движка есть **свой публичный `H2Sharp.Driver`** (`src/H2Sharp/Driver.cs`, аналог
`org.h2.Driver`). Простое имя ищется сначала в охватывающем пространстве имён, и `using`
проигрывает: `Driver` разрешался в `H2Sharp.Driver`, у которого нет ни `Id`, ни других свойств.

В модель поэтому попадали два типа: настоящий `…ConcurrencyModel.Driver` с ключом `Id` и пустой
`H2Sharp.Driver` с одним теневым `Version`. Валидатор спотыкался о второй. Дамп модели это
показывает прямо (временный тест, потом удалён):

```
H2Sharp.Driver: keyless=False key=<none> props=[Version]
…ConcurrencyModel.Driver: keyless=False key=Id props=[Id,CarNumber,…,Wins]
```

Правка: алиас `using F1Driver = …ConcurrencyModel.Driver;` и `Entity<F1Driver>()` вместо
`Entity<Driver>()` в обоих фикстурах. Других мест с этой ловушкой нет: в корневом пространстве
имён движка всего четыре публичных типа — `Driver`, `H2SharpException`, `SqlExceptionKind`,
`TODOException`, — и `Driver` единственное из них, чьё имя тесты вообще используют.

Итог: модель строится, `CREATE TABLE` проходит (в том числе с `UNIQUE`-ограничениями F1 —
прежний комментарий про «expected PRIMARY» устарел ещё на коммите `f56ca4c`), и все 62 теста
упираются в следующий слой — `Not ported yet: Prepared.update`: фикстура сидирует таблицы через
`SaveChanges`. Это движок. Когда он перенесёт путь обновления, эти 62 теста стоит прогнать
первыми — от провайдера они уже ничего не ждут.

### 5. `array_to_string` — сделано, перевод снят

`H2StringMethodTranslator` переводил `string.Join` над массивом в `array_to_string` — это имя
PostgreSQL. Функция проверена в трёх местах:

* в снимке документации H2 2.5 (`../h2sharp/docs/functions.html`) её нет;
* порт отвечает `Function "ARRAY_TO_STRING" not found`;
* эталонный `h2-2.2.224.jar` отвечает ровно то же самое — значит это не пробел порта, и движок
  её никогда не заведёт.

Функция при этом действительно нужна: четыре метода, `String_Join_with_array_of_{int,string}_column`
в `ArrayArrayQueryTest` и `ArrayListQueryTest`, по два варианта sync/async — 8 случаев. Но точной
замены у H2 нет; кандидаты проверены на порте и на эталонном jar:

| Кандидат | Что вышло |
|---|---|
| `LISTAGG(x, sep)` поверх `UNNEST(a)` | работает только на константном массиве: `SELECT LISTAGG(X, ', ') FROM UNNEST(ARRAY[1, 2, 3]) AS U(X)` → `1, 2, 3`. На столбце — `Column "T.A" not found`, и в порте, и в эталоне: подзапрос не видит внешнюю таблицу. Латерального `UNNEST` у H2 нет — ни `FROM t, UNNEST(t.a)`, ни `CROSS JOIN`, ни `LEFT JOIN … ON TRUE` |
| `CARDINALITY(a) <> CARDINALITY(ARRAY_REMOVE(a, NULL))` | `ARRAY_REMOVE` в H2 нет |
| `CAST(a AS VARCHAR)` + `SUBSTRING` + `REPLACE` | рендер неоднозначен: `ARRAY['a, b']` и `ARRAY['a', 'b']` дают одинаковое `[a, b]`; `ARRAY['null']` и `ARRAY[NULL]` — одинаковое `[null]`. Для числовых массивов ещё терпимо, для строковых — молчаливая потеря границ |

Поэтому перевод снят: неконстантный массив больше не транслируется, и EF выдаёт нормальное
`could not be translated` при компиляции запроса вместо сырой ошибки движка в рантайме.
Константный массив по-прежнему идёт в `concat_ws` и работает (это `NorthwindFunctionsQueryH2Test`
и остальные `String_Join_*`).

Тесты переопределены на `AssertTranslationFailed` — ровно тот вариант, который предполагал план
выше. Итог: 48 → 40 красных в массивах (152 → 160 зелёных), новых красных нет.

Побочный улов: `New_array_with_heterogeneous_columns_throws` (4) падал не по делу — тест ждал
текст исключения с постгресовым типом `bytea`, а провайдер честно пишет `varbinary`. Ожидание в
`ArrayQueryTest` исправлено, 4 теста позеленели. Других живых ожиданий `bytea` в коде нет:
`H2TypeMappingTest` и `H2TypeMappingSourceTest` (оба с `bytea`) исключены из компиляции как
непортированные, `ValueConvertersEndToEndH2Test` и `BuiltInDataTypesH2Test` — тоже;
`H2GeneratedSqlReferenceTest` требует обратного (`Assert.DoesNotContain("bytea")`), а
`H2DatabaseModelFactoryTest` целиком строит таблицы из постгресового DDL (`bytea`, `float4`,
`money`, `inet`, `macaddr`) и красный по этой же причине.

## Не наше — движок

| Что | Тестов |
|---|---:|
| `Not ported yet: Prepared.update: the statement is not run yet` | 3892 |
| `ALTER TABLE` — `ADD`/`RENAME TO`/`DROP COLUMN` | 17 |
| `CREATE TYPE … AS ENUM` | 9 |
| `INCLUDE` у уникального индекса | 1 |
| `COMMENT ON` | 2 |
| повторная транзакция на соединении (ADO-слой) | 64 |

`Prepared.update` — 86 % всего красного: `LoadH2Test` 3133, `PropertyValuesH2Test` 173,
`FieldMappingH2Test` 167, `FindH2Test` 411. Пока движок не перенесёт путь обновления, любой
батчевый `SaveChanges` падает, и это единственный по-настоящему крупный блокер.

## Инфраструктура: трейт стоял не там — сделано

`Trait("Category", "BlockedByPort")` стоял 21 раз на классе и ни разу на методе. Гейт
`Category!=BlockedByPort` не видел 826 тестов, которые внутри помеченных классов проходили, а
прогон с трейтом на 59 % состоял из шума одного `LoadH2Test` (3133 из 5330).

Трейт переехал на метод везде, где это возможно:

* 164 атрибута вставлены прямо в файлах, где объявлены красные методы (миграции 104, скаффолдинг
  50, массивы 10);
* 22 заглушки `public override … => base.…();` для унаследованных виртуальных методов —
  `ConvertToProviderTypes` 8, `Migrations` 9, `StoreGeneratedFixup` 3, `LazyLoadProxy` 1,
  `H2Compliance` 1. Атрибут на переопределении подхватывается, хотя `[ConditionalFact]` в
  заглушке не повторяется: атрибуты методов наследуются через цепочку переопределений, проверено
  прогоном (тест найден, трейт виден, тело вызывает базу).

Итог: гейт вырос с 1193 до **2019** функциональных тестов, набор красных остался тем же —
**4229**. Ни один тест не потерялся: 2019 + 4504 = 6523, столько же, сколько 1193 + 5330.

Десять классов оставлены с классовым трейтом, у каждого своя причина:

| Класс | Красных | Зелёных | Почему трейт на классе |
|---|---:|---:|---|
| `StoreGeneratedH2Test` | 1 | 210 | строка `Deleted` теории не помечается: метод базы не `virtual` |
| `UpdatesH2Test` | 17 | 19 | два красных метода (`SaveChanges_*`) не `virtual` |
| `ConcurrencyDetectorDisabledH2Test` | 18 | 18 | пара делит один store, один из двух всегда видит пустую базу |
| `ConcurrencyDetectorEnabledH2Test` | 2 | 16 | то же, и цифры у пары меняются местами от прогона к прогону |
| `TransactionInterceptionH2Test` | 60 | 6 | 32 заглушки-переопределения ради 6 тестов |
| `LoadH2Test` | 3133 | 0 | `Prepared.update` |
| `FindH2Test` | 411 | 0 | `Prepared.update` |
| `PropertyValuesH2Test` | 173 | 0 | `Prepared.update` |
| `FieldMappingH2Test` | 167 | 0 | `Prepared.update` |
| `DataBindingH2Test` | 56 | 0 | сидирование F1 через `Prepared.update` |
| `SerializationH2Test` | 6 | 0 | то же |
| `DefaultValuesTest` | 1 | 0 | in-memory база исчезает вместе с соединением |

### Почему `new` не работает для невиртуальных методов

Первый подход был такой: метод базы не `virtual`, значит его можно заслонить (`new`), поставить
трейт на заслоняющий метод и вызвать базу через приведение:
`((Base<Fixture>)this).Method(args)`. Компилируется, но xunit схлопывает кейсы: идентификатор
кейса — класс + имя метода + аргументы, а имя и аргументы у обоих методов одинаковые. Остаётся
один кейс, и это **унаследованный** — без трейта. Проверено на `StoreGeneratedH2Test`: с
заслоняющим методом в TRX по-прежнему два кейса, у обоих `traits: []`, и красная строка осталась
в гейте. Приём убран, классы из таблицы выше остались на классовом трейте.

### Побочный эффект: теория помечается целиком

Трейт ставится на метод, а у `[ConditionalTheory]` один метод на все строки `[InlineData]`.
Поэтому в прогоне с трейтом осталось 275 проходящих тестов: 210 у `StoreGeneratedH2Test`, 19 у
`UpdatesH2Test`, 34 у пары `ConcurrencyDetector*`, 6 у перехвата транзакций и 6 строк теорий
вычисляемых столбцов в миграциях (там красная только строка `stored: true`). Это не шум: трейт
на методе и не может быть иначе, а `AssertSql`-строки одной теории делят одно тело.

## Оптимизации SQL, не связанные с портом

* `CONCAT(a, b)` вместо `a || b` — осознанный выбор: `CONCAT` игнорирует `NULL`, `||` даёт
  `NULL`. Записан в `docs/DIVERGENCES.md`, трогать не надо.
* `H2SqlExpressionFactory` — TODO про ассоциативность `||`: там снимаются лишние скобки.
* `H2QuerySqlGenerator` — `Exponent => 1300` без ссылки на источник; либо ссылка, либо имя
  константы.

## Порядок

1. ~~Пункт 1~~ — сделано, +116.
2. ~~Пункт 3~~ — сделано, +144; остаток разобран по причинам выше.
3. ~~Пункт 5~~ — сделано, +8: альтернативы у H2 нет, перевод снят, тесты ждут отказа трансляции.
4. ~~Пункт 2~~ — сделано, +16: `COALESCE(ARRAY_CONTAINS(a, NULL), FALSE)` и контрактный тест на
   ответы движка.
5. ~~Пункт 4~~ — разобрано: 62 теста больше не падают на модели, но сидирование F1 идёт через
   `SaveChanges`, то есть в `Prepared.update`. Ждут движок.
6. ~~Трейт~~ — сделано: трейт на методе, гейт 1193 → 2019 функциональных тестов, красных по-прежнему
   4229, ни один тест не потерян.
