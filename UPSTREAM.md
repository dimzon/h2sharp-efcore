# Происхождение форка

Этот репозиторий — форк **EFCore.PG** (провайдер PostgreSQL/Npgsql для Entity Framework Core),
адаптированный под движок **H2Sharp** (порт H2 2.5 на C#, `https://github.com/dimzon/h2sharp`).

## Точка отсчёта

| | |
|---|---|
| Upstream | https://github.com/npgsql/efcore.pg |
| Тег | `v9.0.5` |
| Коммит | `bd7de7b71e9c0dd135cccc47bc7ea1c1ab48c0c9` |
| Дата клона | 2026-10-19 |
| Entity Framework Core | 9.0.x (`[9.0.14,10.0.0)`) |
| Target framework | `net8.0` |

Ветка разработки — `h2-provider`. Ветка `main` форка не существует: клон снят с тега.

## Что было изменено при клонировании

Только идентичность сборки, без изменения поведения:

* каталоги и файлы проектов переименованы
  (`src/EFCore.PG` → `src/H2Sharp.EntityFrameworkCore` и далее по аналогии);
* пространство имён `Npgsql.EntityFrameworkCore.PostgreSQL` → `H2Sharp.EntityFrameworkCore`
  (вместе со всеми подпространствами);
* `AssemblyName` / `RootNamespace` / `CustomToolNamespace` переписаны тем же правилом.

Изначально пакет `Npgsql` (ADO.NET) использовался как есть: на этом шаге форк ещё работал
с PostgreSQL. Сейчас ADO.NET-слой заменён на `H2Sharp.Data`, пакет `Npgsql` удалён из зависимостей
(как и объявленные для него версии), а подключение идёт через `H2DataSource`.

Всё, что не переименовано, осталось именами upstream: имена типов внутри провайдера
(`NpgsqlSqlGenerationHelper`, `NpgsqlMigrationsSqlGenerator`, …), идентификаторы аннотаций
(`Npgsql:ValueGenerationStrategy`, …), тексты сообщений. Это сделано намеренно: переименование
подожит до тех пор, пока провайдер заработает, иначе при отладке несовпадение расхождений
диалекта с расхождением имён невозможно отличить.

## Лицензия

Upstream распространяется по [PostgreSQL License](LICENSE) — разрешающей. Этот форк остаётся
под той же лицензией. Ядро H2Sharp (`h2sharp/src/H2Sharp`) распространяется отдельно, под
MPL-2.0 OR EPL-1.0, и в этот репозиторий не входит.
