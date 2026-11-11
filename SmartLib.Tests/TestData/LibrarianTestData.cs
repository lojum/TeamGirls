using BusinessLogic.Services;
using Domain.Enums;

namespace SmartLib.Tests.TestData
{
    // Дані для LibrarianDashboardServiceTests і LibrarianDashboardViewModelTests
    public static class LibrarianTestData
    {
        public const long LibrarianId = 7;
        public const string LibrarianName = "Оксана Мандзювата";
        public const string SecondReaderEmail = "ivan.k@gmail.com";
        public const string SecondReaderName = "Іван Ковальчук";
        public const string InactiveReaderEmail = "inactive@gmail.com";

        public const string AvailableInventory = "INV-00101";
        public const string LoanedInventory = "INV-00301";
        public const string WrittenOffInventory = "INV-00501";

        public static readonly DateTime LoanedAt = new(2026, 10, 20, 10, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime ReturnedAt = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);
        public static readonly DateTime DueDate = new(2026, 11, 3);

        // Примірники для фільтра таблиці бібліотекаря
        public static List<CopySummary> Copies() =>
        [
            new(1, AvailableInventory, "Кобзар", "Тарас Шевченко", "978-966-03-8001-1", "Поезія", CopyStatus.Available, null),
            new(2, LoanedInventory, "1984", "Джордж Орвелл", "978-617-7538-01-0", "Антиутопія", CopyStatus.OnLoan, DueDate),
            new(3, WrittenOffInventory, "Перевтілення", "Франц Кафка", null, "Психологічна проза", CopyStatus.WrittenOff, null)
        ];

        public static List<ReaderOption> Readers() =>
        [
            new(1, UserTestData.ReaderName, UserTestData.ReaderEmail),
            new(2, SecondReaderName, SecondReaderEmail)
        ];

        // Пошукові запити й очікуваний інвентарний номер у результаті
        public static TheoryData<string, string> SearchCases => new()
        {
            { "кобзар", AvailableInventory },      // назва, інший регістр
            { "орвелл", LoanedInventory },         // автор
            { "INV-00501", WrittenOffInventory },  // інвентарний номер
            { "8001", AvailableInventory },        // частина ISBN
            { "  1984  ", LoanedInventory }        // пробіли навколо
        };

        public static TheoryData<CopyStatus, string> StatusCases => new()
        {
            { CopyStatus.Available, AvailableInventory },
            { CopyStatus.OnLoan, LoanedInventory },
            { CopyStatus.WrittenOff, WrittenOffInventory }
        };
    }
}
