# Тестування та забезпечення якості SmartLib

## 1. Стратегія тестування

| Рівень | Що перевіряємо | Інструмент | Де |
|---|---|---|---|
| Модульні (unit) | `PasswordHasher`, `AuthService`, `BookService`, `LoanService` | xUnit + EF Core InMemory (для `BookService` — список у пам'яті) | `SmartLib.Tests/Services` |
| Перевірка моделі БД | Відповідність моделі EF до `schema.sql`: назви таблиць, колонок і представлення `v_book_catalog`, типи `date` | xUnit + провайдер Npgsql (без підключення до бази) | `SmartLib.Tests/DataAccess` |
| Інтеграційні | Тригер `trg_loans_sync_copy`, `DOMAIN`, CHECK, унікальні індекси, представлення `v_book_catalog`, сервіси разом зі справжньою PostgreSQL | xUnit + PostgreSQL (Testcontainers або сервер з `SMARTLIB_TEST_DB`) | `SmartLib.IntegrationTests` (розділ 8) |
| Ручні | Вікна WPF: вхід, реєстрація, адмін-панель | Чек-лист (розділ 5) | — |

Принципи:
- Основна увага — логіці (BusinessLogic, DataAccess). Тестовий проєкт має ціль `net8.0-windows` і посилається на `Presentation`, щоб можна було тестувати й ViewModel. Тому тести запускаються на Windows (і в GitHub Actions на `windows-latest`).
- Кожен тест створює власну in-memory базу, тож тести незалежні й можуть іти в будь-якому порядку.
- Поточний час передається через `TimeProvider`. У тестах дата «заморожена», тому результати не залежать від дня запуску.
- EF InMemory не виконує SQL, тригери й обмеження. Тому відповідність моделі до схеми перевіряють окремі тести `DbContextMappingTests`, а поведінку бази — інтеграційні тести.
- Кожен тест має структуру Arrange — Act — Assert (підготовка, дія, перевірка), позначену коментарями.
- Тестові дані винесено з тестів в окремі файли в папці `SmartLib.Tests/TestData` (див. розділ 3.1). Тести отримують набори даних через `[MemberData]` замість довгих списків `[InlineData]`.
- Замінники залежностей: in-memory база EF — це fake (спрощена робоча реалізація сховища), `FixedTimeProvider` — stub (повертає заздалегідь заданий час).

Критерій готовності: усі тести зелені, покриття BusinessLogic не менше 70%. Фактично покриття 100% рядків і гілок.

## 2. Як запустити

```bash
dotnet test SmartLib.Tests/SmartLib.Tests.csproj
```

З покриттям коду:

```bash
dotnet test SmartLib.Tests/SmartLib.Tests.csproj --collect:"XPlat Code Coverage"
```

Лише певна категорія тестів (атрибут `[Trait("Category", ...)]`):

```bash
dotnet test SmartLib.Tests/SmartLib.Tests.csproj --filter "Category=Unit"
dotnet test SmartLib.Tests/SmartLib.Tests.csproj --filter "Category=Mapping"
```

У Visual Studio Code (розширення C# Dev Kit): вкладка Testing (іконка колби) → Run Tests.
Тести також запускаються автоматично на кожен pull request (`.github/workflows/ci.yml`, опис — у `CI.md`).

## 3. Автоматизовані тести (106)

| Клас | Що перевіряє |
|---|---|
| `PasswordHasherTests` | правильний і неправильний пароль; різні хеші одного пароля (сіль); формат хешу; підтримка BCrypt; 9 варіантів зіпсованого хешу повертають `false` без винятку |
| `AuthServiceTests` | успішний вхід; email в іншому регістрі та з пробілами; неправильний пароль; невідомий email; неактивний користувач; порожні поля |
| `BookServiceTests` | пошук за назвою, автором, жанром, точним роком; частина року не збігається; без урахування регістру і зайвих пробілів; частина слова; порожній запит повертає всі книги; немає збігів; спецсимволи та SQL-ін'єкція; книга без авторів і жанрів |
| `LoanServiceTests` | успішна видача і термін повернення +14 днів; нестандартний термін (7, 21, 30 днів) і неприпустимий (0, −1, 61); подія `BookLoaned`; невідомий читач; email бібліотекаря; неактивний читач; невідомий примірник; статуси `OnLoan`, `Reserved`, `WrittenOff`; повторна видача того самого примірника; подія не спрацьовує при помилці; порожні поля; активні видачі читача без повернених і чужих |
| `LibrarianDashboardServiceTests` | статус і термін повернення виданого примірника; повернена видача не позначає примірник виданим; лише активні читачі; історія від найновішої дії; фільтр таблиці за назвою, автором, ISBN, інвентарним номером і статусом |
| `LibrarianDashboardViewModelTests` | завантаження таблиці й списків; помилка бази; пошук у таблиці; видача передає інвентарний номер, email, id бібліотекаря і термін; повідомлення з `LoanException`; видача без вибору читача й книги; дата повернення за обраним терміном |
| `ReaderBookStatusTests` | статус книги в кабінеті читача: видана цьому читачеві, є вільні примірники, немає вільних |
| `RegistrationViewModelTests` (Оксана) | валідація форми реєстрації; кнопка реєстрації стає активною |
| `DbContextMappingTests` | колонки `book_authors` і `book_genres`; колонки представлення `v_book_catalog`; тип `date` для `due_date`, `acquired_at`, `written_off_at`; назви таблиць; колонки `loans` |

### 3.1. Тестові дані

Дані, які повторювались у кількох тестах або займали довгі списки `[InlineData]`, винесено в окремі статичні класи. Тест лише бере потрібний набір, а самі значення змінюються в одному місці.

| Файл | Що містить | Де використовується |
|---|---|---|
| `TestData/UserTestData.cs` | email, ім'я та пароль тестового читача, email бібліотекаря, варіанти email у різному регістрі, порожні облікові дані | `AuthServiceTests`, `LoanServiceTests`, `Helpers/TestDb.cs` |
| `TestData/PasswordTestData.cs` | паролі, хеш тестових користувачів зі `schema.sql`, 9 зіпсованих хешів | `PasswordHasherTests` |
| `TestData/BookTestData.cs` | тестовий каталог із 4 книг, запити для перевірки регістру, порожні запити, запити без збігів (зокрема SQL-ін'єкція) | `BookServiceTests` |
| `TestData/LoanTestData.cs` | інвентарні номери, тестова книга, фіксована дата і очікуваний термін повернення, фрагменти повідомлень про помилки, недоступні статуси примірника, порожні вхідні дані | `LoanServiceTests` |
| `TestData/SchemaTestData.cs` | назви таблиць і колонок зі `schema.sql` | `DbContextMappingTests` |
| `TestData/LibrarianTestData.cs` | бібліотекар, другий і неактивний читач, інвентарні номери, дати видачі й повернення, примірники й читачі для панелі, пошукові запити й статуси | `LibrarianDashboardServiceTests`, `LibrarianDashboardViewModelTests`, `ReaderBookStatusTests` |
| `RegistrationTestData.cs` (Оксана) | коректні й некоректні дані форми реєстрації | `RegistrationViewModelTests` |

Приклад:

```csharp
// TestData/PasswordTestData.cs
public static TheoryData<string> MalformedHashes => new()
{
    "",               // порожній рядок
    "not-a-hash",     // немає роздільників
    ...
};

// Services/PasswordHasherTests.cs
[Theory]
[MemberData(nameof(PasswordTestData.MalformedHashes), MemberType = typeof(PasswordTestData))]
public void Verify_MalformedHash_ReturnsFalse(string badHash) { ... }
```

Допоміжний клас `Helpers/TestDb.cs` лишився окремо: він не зберігає дані, а створює тестову базу й додає в неї записи.

Замінники сервісів для тестів ViewModel лежать у `Helpers/Fakes.cs`: `FakeLoanService` (fake: запам'ятовує виклик видачі, піднімає подію `BookLoaned` або кидає заданий виняток), `FakeLibrarianDashboardService` (stub: повертає підготовлені примірники й читачів) і `FakeNavigationService`.

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
- [ ] Вхід бібліотекаря: таблиця показує примірники з бази, статус «Checked Out» і дату повернення для виданих.
- [ ] Видача: обрати читача, доступний примірник і термін 21 день → «Confirm Loan» (або F5) → повідомлення «… issued to …», примірник стає «Checked Out», у «Recent System Activity» з'являється запис ISSUED.
- [ ] Видача списаного або вже виданого примірника (вибір у таблиці) → червоне повідомлення з причиною.
- [ ] Вхід читача: каталог з бази, категорії — жанри; книга, яку видали цьому читачеві, у розділі «My Loans» зі статусом «Borrowed · due …».

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
| Тестові дані (email, паролі, хеші, каталог книг, назви колонок) повторювались у різних тестах і займали довгі списки `[InlineData]` | Дані винесено в `SmartLib.Tests/TestData` і передаються через `[MemberData]`; додано коментарі Arrange — Act — Assert і категорії `[Trait]` | усі 70 тестів зелені після перенесення |
| Після злиття гілок вікна бібліотекаря й читача показували зашиті приклади даних: видача книги і каталог не працювали з базою, `UserId` бібліотекаря губився під час навігації | Новий `LibrarianDashboardService` (примірники, читачі, історія); `LibrarianDashboardViewModel` видає книгу через `LoanService` з обраним терміном і показує повідомлення з `LoanException`; кабінет читача бере каталог через `BookService`, а «My Loans» — через `LoanService.GetActiveLoansAsync`; навігація передає `UserId` | `LibrarianDashboardServiceTests`, `LibrarianDashboardViewModelTests`, `ReaderBookStatusTests`, нові тести `LoanServiceTests` |
| Тестовий проєкт не мав інструментів для бази й покриття | Додано `coverlet.collector`, EF InMemory і глобальний `using Xunit` | запуск у GitHub Actions |

## 7. Що лишається

- Винести фільтрацію і сторінки боржників з `AdminDashboardViewModel` в окремий клас без WPF і покрити тестами.
- Перенести рядок підключення з паролем з `appsettings.json` у user-secrets і змінити пароль бази.

## 8. Інтеграційні тести (`SmartLib.IntegrationTests`, 30)

Окремий проєкт без WPF (`net8.0`). Перевіряє те, чого не бачать модульні тести з EF InMemory: SQL, тригери, `DOMAIN`, CHECK-обмеження, індекси й представлення.

**Як влаштовано (за схемою «тест → сервіс → справжня база»):**
- `Infrastructure/PostgresServer.cs` — сервер PostgreSQL, спільний для всіх тестів. Якщо задано змінну `SMARTLIB_TEST_DB`, використовується цей сервер (так працює CI). Інакше Testcontainers запускає `postgres:16-alpine` у Docker.
- `Infrastructure/TestDatabase.cs` — для **кожного тесту** окрема база `smartlib_tests_<guid>`, створена тим самим `schema.sql`, що й робоча (разом із його тестовими даними). Після тесту база видаляється, навіть якщо тест упав.
- `Infrastructure/IntegrationTestBase.cs` — `IAsyncLifetime`: `InitializeAsync` створює базу, `DisposeAsync` видаляє.
- `DbContext` у тестах створюється тим самим методом `DependencyInjection.CreateDataSource` / `UseSmartLibDatabase`, що й у застосунку, тож перевіряється справжня конфігурація підключення.
- Результат перевіряється окремим SQL-запитом до бази, а не лише викликом методу.
- Дані — у `TestData/SeedData.cs` (тестові дані зі `schema.sql`) і `TestData/ConstraintTestData.cs` (значення для `DOMAIN` і обмежень).

| Клас | Що перевіряє |
|---|---|
| `SchemaConstraintTests` | `schema.sql` можна запустити повторно без помилок і дублів; ISBN: 4 коректні та 4 некоректні формати; рік видання: межі 1000 і 2100 приймаються, 999 і 2101 — ні; некоректний email; той самий email в іншому регістрі (індекс `ux_users_email`); списаний примірник без дати списання (`ck_writeoff_consistent`) |
| `LoanTriggerTests` | видача через `LoanService` → тригер ставить примірнику `on_loan`, `due_date` збережено як дата; повернення → `available`; видача списаного примірника → помилка тригера; друга активна видача того самого примірника → індекс `ux_loans_one_active_per_copy`; `LoanService` не створює видачу для виданого примірника |
| `ServiceIntegrationTests` | `BookService` читає `v_book_catalog` (автори, жанри, кількість примірників, списані не враховуються); зареєстрований через `UserService` читач входить через `AuthService` з email в іншому регістрі; повторна реєстрація з тим самим email → `DuplicateEmailException`; `AdminDashboardService` правильно рахує книги, читачів і боржників |

**Як запустити локально** (один із варіантів):

1. Запустити Docker Desktop, потім:
   ```bash
   dotnet test SmartLib.IntegrationTests/SmartLib.IntegrationTests.csproj
   ```
2. Без Docker — на власному тестовому сервері PostgreSQL (**ніколи не на робочій базі Supabase**):
   ```powershell
   $env:SMARTLIB_TEST_DB = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=..."
   dotnet test SmartLib.IntegrationTests/SmartLib.IntegrationTests.csproj
   ```

Якщо немає ні Docker, ні змінної, тести падають із поясненням, що робити. У CI вони запускаються окремим завданням «Integration tests (PostgreSQL)» на `ubuntu-latest` з PostgreSQL 16.

**Помилка, яку знайшли інтеграційні тести:** у `schema.sql` у гілці `main` лишилися маркери конфлікту злиття (`<<<<<<<`, `>>>>>>>`) і дубльований блок тестових даних. Модульні тести цього не помітили, а всі 30 інтеграційних тестів упали з `syntax error at or near "<<<<<<<"`. Після виправлення скрипта всі тести зелені.

