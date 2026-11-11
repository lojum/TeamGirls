# SmartLib — система управління бібліотекою

[![CI](https://github.com/lojum/TeamGirls/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/lojum/TeamGirls/actions/workflows/ci.yml)
[![CodeQL](https://github.com/lojum/TeamGirls/actions/workflows/codeql.yml/badge.svg?branch=main)](https://github.com/lojum/TeamGirls/actions/workflows/codeql.yml)

SmartLib — це настільний застосунок для автоматизації роботи бібліотекаря та обліку книжкового фонду.

## Проблема та мета проєкту

Проблема: втрата часу на ручний облік книжкового фонду, відсутність швидкого доступу до інформації про наявність книг та складність ведення статистики видачі.

Мета: створити автоматизовану систему для швидкого пошуку, бронювання та обліку бібліотечного фонду.

Цільові користувачі: бібліотекарі, адміністратори книжкового фонду та читачі.

## Основні можливості

- Каталог книг із пошуком за автором, жанром та роком видання.
- CRUD-операції для управління фондом (додавання, редагування, списання книг).
- Система реєстрації видачі та повернення книг читачами.
- Формування аналітичних звітів про роботу бібліотеки.

## Технологічний стек

- База даних: PostgreSQL
- Бекенд: C#, .NET, LINQ
- Інтерфейс: Desktop UI 

## Підключення до бази даних (Database connection)

Пароль від бази **не зберігається в репозиторії**. У `Presentation/appsettings.json` рядок підключення порожній, а справжній кожна учасниця зберігає у себе через **user-secrets**. Це файл на комп'ютері розробника, поза папкою проєкту, тож потрапити в Git він не може.

**Налаштування (один раз на кожному комп'ютері):**

1. Отримайте рядок підключення в команди особисто, не через GitHub чи загальний чат. Формат:
   ```
   Host=<host>.pooler.supabase.com;Port=5432;Database=postgres;Username=<user>;Password=<password>;Ssl Mode=Require;Trust Server Certificate=true;
   ```
2. У корені проєкту виконайте (рядок підключення — у лапках):
   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=...;" --project Presentation
   ```
3. Перевірте:
   ```bash
   dotnet user-secrets list --project Presentation
   ```

Секрет зберігається у `%APPDATA%\Microsoft\UserSecrets\smartlib-presentation-4c7e2f1a-9b3d-4e8a-a6f0-2d5c8b1e7f93\secrets.json`. Замість user-secrets можна задати змінну середовища `SMARTLIB_ConnectionStrings__DefaultConnection`.

Якщо рядок не налаштовано, застосунок під час запуску покаже повідомлення з цією командою.

Модульним тестам (`SmartLib.Tests`) база не потрібна. Інтеграційні тести (`SmartLib.IntegrationTests`) працюють з окремою тестовою PostgreSQL (Docker або `SMARTLIB_TEST_DB`) і **ніколи не підключаються до робочої бази**.

Якщо пароль потрапив у репозиторій або чат, його треба змінити в Supabase (Project Settings → Database → Reset database password) і оновити секрет у кожної учасниці.

## Архітектура

Застосунок побудований як чотиришарова N-Tier система. Напрямок залежностей:

`Presentation -> BusinessLogic -> DataAccess -> Domain`

- **Domain** містить сутності та переліки предметної області й не залежить від інших проєктів.
- **DataAccess** містить `SmartLibDbContext`, налаштування Entity Framework Core та доступ до PostgreSQL; залежить від `Domain`.
- **BusinessLogic** містить сервіси та правила роботи з даними; використовує `DataAccess` і `Domain`. Реєстрація сервісів та налаштування підключення зосереджені в `AddBusinessLogic`.
- **Presentation** містить WPF Views і MVVM ViewModels. ViewModels викликають сервіси бізнес-логіки, не звертаючись до `DbContext` чи Entity Framework Core. Вікна та сервіси створюються через Dependency Injection.

Отже, проєкт `Presentation` посилається лише на `BusinessLogic`, а не напряму на `DataAccess` чи `Domain`.

## Склад команди та відповідальність

Софія Шелеп — Data & Architecture
Проєктування реляційної схеми у PostgreSQL, налаштування доменних обмежень (DOMAIN) та зовнішніх ключів, написання SQL-скриптів для створення бази даних.

Юлія Назарова — Core Backend
Ініціалізація репозиторію, реалізація бізнес-логіки, обробка даних, робота з колекціями, застосування LINQ-запитів для пошуку книг та створення структури класів.

Оксана Мандзювата — Frontend 
Розробка графічного інтерфейсу застосунку, створення вікон для користувачів, застосування делегатів та подій для оновлення інтерфейсу.
