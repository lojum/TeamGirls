# Тестування та забезпечення якості SmartLib

## 1. Стратегія тестування

| Рівень | Що перевіряємо | Інструмент | Де |
|---|---|---|---|
| Модульні (unit) | `PasswordHasher`, `AuthService`, `BookService`, `LoanService` | xUnit + EF Core InMemory (для `BookService` — список у пам'яті) | `SmartLib.Tests/Services` |
| Перевірка моделі БД | Відповідність моделі EF до `schema.sql`: назви таблиць, колонок і представлення `v_book_catalog`, типи `date` | xUnit + провайдер Npgsql (без підключення до бази) | `SmartLib.Tests/DataAccess` |
| Інтеграційні (наступний крок) | Тригер `trg_loans_sync_copy`, `DOMAIN`, унікальні індекси на справжній PostgreSQL | xUnit + Testcontainers.PostgreSql | — |
| Ручні | Вікна WPF: вхід, реєстрація, адмін-панель | Чек-лист (розділ 5) | — |

Принципи:
- Основна увага — логіці (BusinessLogic, DataAccess). Тестовий проєкт має ціль `net8.0-windows` і посилається на `Presentation`, щоб можна було тестувати й ViewModel. Тому тести запускаються на Windows (і в GitHub Actions на `windows-latest`).
- Кожен тест створює власну in-memory базу, тож тести незалежні й можуть іти в будь-якому порядку.
- Поточний час передається через `TimeProvider`. У тестах дата «заморожена», тому результати не залежать від дня запуску.
- EF InMemory не виконує SQL, тригери й обмеження. Тому відповідність моделі до схеми перевіряють окремі тести `DbContextMappingTests`, а поведінку бази — інтеграційні тести.

Критерій готовності: усі тести зелені, покриття BusinessLogic не менше 70%. Фактично покриття 100% рядків і гілок.

## 2. Як запустити

```bash
dotnet test SmartLib.Tests/SmartLib.Tests.csproj
```

З покриттям коду:

```bash
dotnet test SmartLib.Tests/SmartLib.Tests.csproj --collect:"XPlat Code Coverage"
```

У Visual Studio Code (розширення C# Dev Kit): вкладка Testing (іконка колби) → Run Tests.
Тести також запускаються автоматично на кожен pull request (`.github/workflows/tests.yml`).

## 3. Автоматизовані тести (70)

| Клас | Що перевіряє |
|---|---|
| `PasswordHasherTests` | правильний і неправильний пароль; різні хеші одного пароля (сіль); формат хешу; підтримка BCrypt; 9 варіантів зіпсованого хешу повертають `false` без винятку |
| `AuthServiceTests` | успішний вхід; email в іншому регістрі та з пробілами; неправильний пароль; невідомий email; неактивний користувач; порожні поля |
| `BookServiceTests` | пошук за назвою, автором, жанром, точним роком; частина року не збігається; без урахування регістру і зайвих пробілів; частина слова; порожній запит повертає всі книги; немає збігів; спецсимволи та SQL-ін'єкція; книга без авторів і жанрів |
| `LoanServiceTests` | успішна видача і термін повернення +14 днів; подія `BookLoaned`; невідомий читач; email бібліотекаря; неактивний читач; невідомий примірник; статуси `OnLoan`, `Reserved`, `WrittenOff`; повторна видача того самого примірника; подія не спрацьовує при помилці; порожні поля |
| `DbContextMappingTests` | колонки `book_authors` і `book_genres`; колонки представлення `v_book_catalog`; тип `date` для `due_date`, `acquired_at`, `written_off_at`; назви таблиць; колонки `loans` |

## 4. Перевірка крайових ситуацій

- Email у різному регістрі та з пробілами: `OLENA.B@GMAIL.COM`, `"  olena.b@gmail.com  "`.
- Порожні рядки й рядки з пробілів у пошуку, при вході та при видачі.
- Зіпсовані хеші паролів: порожній рядок, невідомий алгоритм, нечислова кількість ітерацій, 0 і 9 999 999 ітерацій, невалідний base64, неправильна довжина солі.
- Спецсимволи в пошуку: `%` і `'; DROP TABLE books; --`. LINQ передає їх як параметри, тому ін'єкція неможлива.
- Примірник у кожному недоступному статусі.
- Повторна видача того самого примірника.
- Неактивний обліковий запис при вході та при видачі.
- Термін повернення рахується від дати, а не від моменту: видача 28.10 о 15:30 → повернути до 11.11.

## 5. Ручне тестування інтерфейсу (чек-лист)

- [ ] Реєстрація з email у верхньому регістрі, потім вхід з email у нижньому регістрі.
- [ ] Повторна реєстрація з тим самим email дає повідомлення «already exists».
- [ ] Пароль із 7 символів не приймається, із 8 — приймається.
- [ ] Вхід адміністратора: панель показує кількість книг, читачів, прострочених видач.
- [ ] Пошук у каталозі, читачах і боржниках фільтрує списки.
- [ ] Кнопки «Previous» і «Next» у боржників неактивні на першій та останній сторінках.
- [ ] Без інтернету: замість падіння з'являється повідомлення про помилку бази.

## 6. Виконаний рефакторинг

| Проблема (до) | Що змінено (після) | Тест, який це підтверджує |
|---|---|---|
| EF шукав колонки `authors_author_id` і `books_book_id`, яких немає в `schema.sql`. Запити з `Include(Authors)` та `Include(Genres)` падали на реальній базі | У `SmartLibDbContext` явно задано колонки `book_id`, `author_id`, `genre_id` | `DbContextMappingTests.JoinTables_UseColumnNamesFromSchema`: на старому коді падає, на новому проходить |
| `due_date`, `acquired_at` мапились як `timestamptz`, а в базі вони `DATE` | `HasColumnType("date")` | `DbContextMappingTests.DateOnlyColumns_AreMappedAsDate` |
| Логіка входу була у `LoginViewModel` і порівнювала email з урахуванням регістру, хоча індекс у базі на `lower(email)` | Новий `AuthService` з `NormalizeEmail`; ViewModel лише викликає сервіс; реєстрація зберігає email у нижньому регістрі | `AuthServiceTests.Authenticate_EmailInDifferentCaseOrWithSpaces_ReturnsUser` |
| `LoanService` кидав загальний `Exception`, не перевіряв `IsActive`, повертав завжди `true`, брав час з `DateTime.UtcNow` | Власний `LoanException`; перевірка активності читача і наявності активної видачі; повертає створений `Loan`; час через `TimeProvider`; константа `LoanPeriodDays` | `LoanServiceTests` (16 тестів) |
| `AdminDashboardViewModel.LoadAsync()` ніхто не викликав, тому панель адміністратора була порожня | Виклик у події `Loaded` вікна з обробкою помилки | ручний чек-лист, п. 4 |
| Фільтр пошуку був усередині `SearchBooksAsync` і читав представлення бази, тож без бази його не можна було перевірити | Фільтр винесено в `BookService.ApplySearch(IQueryable<BookCatalogView>, query)`; додано обрізання пробілів у запиті | `BookServiceTests` (16 тестів) |
| `BookCatalogView.ISBN` і `Publisher` були непорожніми рядками, хоча в базі ці колонки можуть бути `NULL`: книга без ISBN зламала б завантаження каталогу | Тип змінено на `string?` | `BookServiceTests.Search_BookWithoutAuthorsAndGenres_IsFoundByTitle`, `DbContextMappingTests.BookCatalogView_MapsToViewColumns` |
| Тестовий проєкт не мав інструментів для бази й покриття | Додано `coverlet.collector`, EF InMemory і глобальний `using Xunit` | запуск у GitHub Actions |

## 7. Що лишається

- Інтеграційні тести на справжній PostgreSQL (Testcontainers): тригер синхронізації статусу, `DOMAIN` (ISBN, рік), унікальний email без урахування регістру, одна активна видача на примірник.
- Винести фільтрацію і сторінки боржників з `AdminDashboardViewModel` в окремий клас без WPF і покрити тестами.
- Перенести рядок підключення з паролем з `appsettings.json` у user-secrets і змінити пароль бази.
