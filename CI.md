# Неперервна інтеграція (CI) SmartLib

Кожна зміна коду автоматично збирається й перевіряється на GitHub Actions. Зміни потрапляють у `main` лише через pull request, коли всі перевірки зелені та є схвалення від іншої учасниці.

```
Зміна коду → локальний запуск тестів → push у feature-гілку → pull request
    → CI: збирання, модульні тести, інтеграційні тести, покриття, стиль коду, аналіз безпеки
    → зелений: перегляд коду і злиття в main   /   червоний: виправити й запушити знову
```

## 1. Коли запускається

| Workflow | Файл | Коли запускається |
|---|---|---|
| **CI** | `.github/workflows/ci.yml` | кожен pull request; кожен push у `main`; вручну (Actions → CI → Run workflow) |
| **CodeQL** | `.github/workflows/codeql.yml` | pull request у `main`; push у `main`; щопонеділка за розкладом |
| **Dependabot** | `.github/dependabot.yml` | щотижня перевіряє оновлення пакетів NuGet і версій GitHub Actions та відкриває для них PR |

Якщо в ту саму гілку надходить новий push, попередній запуск CI скасовується, щоб не чекати застарілого результату.

## 2. Завдання (jobs) workflow CI

| Завдання | Раннер | Що робить | Коли червоне |
|---|---|---|---|
| **Build & unit tests** | `windows-latest` | `dotnet restore` і `dotnet build` усього рішення в конфігурації Release (разом із WPF); модульні тести `SmartLib.Tests`; `dotnet publish` застосунку | помилка компіляції або хоча б один модульний тест упав |
| **Integration tests (PostgreSQL)** | `ubuntu-latest` + контейнер PostgreSQL 16 | інтеграційні тести `SmartLib.IntegrationTests` на справжній базі, створеній зі `schema.sql` | помилка в схемі бази, тригері, обмеженнях або в запитах сервісів |
| **Coverage report & threshold** | `ubuntu-latest` | об'єднує покриття модульних та інтеграційних тестів, публікує звіт на сторінці запуску (Job Summary), перевіряє поріг | покриття `BusinessLogic` нижче **85%** |
| **Code style (dotnet format)** | `ubuntu-latest` | перевіряє відступи, пробіли й порожні рядки за `.editorconfig` | код відформатовано не за правилами |

Чому два раннери: проєкт `Presentation` (WPF) збирається лише на Windows, а контейнер PostgreSQL GitHub запускає лише на Linux.

## 3. Додаткові перевірки якості

- **Поріг покриття.** Об'єднане покриття бізнес-логіки має бути не менше 85% (зараз близько 94% рядків і 97% гілок). Якщо нові сервіси з'являться без тестів, CI це помітить.
- **Стиль коду.** `dotnet format` за правилами `.editorconfig`.
- **CodeQL.** Статичний аналіз безпеки C#-коду від GitHub. Знахідки видно у вкладці **Security → Code scanning**.
- **Dependabot.** PR з оновленнями залежностей. Кожен такий PR проходить той самий CI, тож оновлення, яке ламає збирання чи тести, не потрапить у `main`. Мажорні версії пакетів (наприклад, EF Core 9/10, які потребують нового .NET) Dependabot не пропонує, їх оновлюємо вручну.
- **Секрети.** Робочий рядок підключення до бази в CI не потрібен і не зберігається: інтеграційні тести використовують тимчасовий PostgreSQL у контейнері.

## 4. Результати запуску

На сторінці запуску (вкладка **Actions** → потрібний запуск):

- **Summary** — таблиця покриття за проєктами і рядок із перевіркою порогу;
- **Artifacts** (унизу сторінки):
  - `SmartLib-app` — зібраний застосунок (Release). Після розпакування запускається `Presentation.exe`; рядок підключення береться з user-secrets, як описано в README;
  - `coverage-report` — HTML-звіт покриття (відкрити `index.html`);
  - `unit-test-results`, `integration-test-results` — результати тестів (`.trx`) і сирі файли покриття.

## 5. Як перевірити те саме локально (перед push)

```bash
dotnet build SmartLib.sln --configuration Release
dotnet test SmartLib.Tests/SmartLib.Tests.csproj
dotnet test SmartLib.IntegrationTests/SmartLib.IntegrationTests.csproj   # потрібен Docker Desktop
dotnet format whitespace . --folder --verify-no-changes
```

Якщо перевірка стилю впала, виправити все автоматично можна тією самою командою без `--verify-no-changes`:

```bash
dotnet format whitespace . --folder
```

## 6. Захист гілки `main` (налаштовується один раз на GitHub)

**Settings → Branches → Add branch ruleset** (або **Add classic branch protection rule**) для гілки `main`:

1. **Require a pull request before merging**, **Required approvals: 1**: жодна зміна не потрапляє в `main` без перегляду іншою учасницею.
2. **Require status checks to pass**: додати перевірки `Build & unit tests`, `Integration tests (PostgreSQL)`, `Coverage report & threshold`, `Code style (dotnet format)`. Назви з'являються в пошуку після першого запуску нового CI.
3. **Block force pushes**: заборонити переписувати історію `main`.

Якщо в **Settings → Code security** увімкнено CodeQL «Default setup», його треба вимкнути, бо аналіз уже налаштовано власним файлом `codeql.yml`. **Dependabot alerts** там же варто увімкнути.

## 7. Сценарій демонстрації

1. Відкрити зелений pull request і показати всі перевірки та Job Summary з покриттям.
2. Завантажити артефакт `SmartLib-app` і показати, що зібраний CI застосунок запускається.
3. **Червоний запуск:** створити тестову гілку, навмисно зламати один тест (або додати зайві пробіли в кінці рядка), відкрити PR. CI стає червоним, а кнопка злиття заблокована захистом гілки. Виправити, і CI знову зелений. Тестову гілку після демонстрації видалити.
4. Показати вкладку **Security** (CodeQL) і PR від Dependabot, якщо вже з'явилися.
