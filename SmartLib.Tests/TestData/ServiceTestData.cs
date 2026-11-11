using BusinessLogic.Services;

namespace SmartLib.Tests.TestData
{
    // Дані для AdminDashboardServiceTests, UserServiceTests і DependencyInjectionTests
    public static class ServiceTestData
    {
        // Фіктивний рядок підключення: сервіси лише створюються, до бази ніхто не підключається
        public const string FakeConnectionString = "Host=localhost;Database=smartlib_unit_tests;Username=test;Password=test";

        public const string FirstBookTitle = "1984";
        public const string FirstBookAuthor = "Джордж Орвелл";
        public const string SecondBookTitle = "Кобзар";
        public const string SecondBookAuthor = "Тарас Шевченко";
        public const string Genre = "Класика";
        public const short PublicationYear = 2020;

        public const string OverdueInventory = "INV-00301";
        public const string ActiveInventory = "INV-00101";
        public const string ReturnedInventory = "INV-00102";
        public const int DaysOverdue = 5;
        public const int DaysUntilDue = 3;

        public const string NewReaderName = "  Нова Читачка  ";
        public const string NewReaderEmail = "  New.Reader@Gmail.com ";
        public const string NewReaderPassword = "Secret123!";

        // Сервіси, які AddBusinessLogic має зареєструвати: інтерфейс і реалізація
        public static TheoryData<Type, Type> RegisteredServices => new()
        {
            { typeof(IAuthService), typeof(AuthService) },
            { typeof(IBookService), typeof(BookService) },
            { typeof(ILoanService), typeof(LoanService) },
            { typeof(IUserService), typeof(UserService) },
            { typeof(IAdminDashboardService), typeof(AdminDashboardService) },
            { typeof(ILibrarianDashboardService), typeof(LibrarianDashboardService) }
        };

        // AdminDashboardService рахує прострочку від поточної дати
        public static DateTime Today => DateTime.UtcNow.Date;
    }
}
