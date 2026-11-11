using Domain.Enums;

namespace SmartLib.Tests.TestData
{
    public static class LoanTestData
    {
        public const long LibrarianId = 100;
        public const string InventoryNumber = "INV-00101";
        public const string UnknownInventoryNumber = "INV-99999";
        public const string BookTitle = "Кобзар";
        public const string BookAuthor = "Тарас Шевченко";
        public const string BookGenre = "Поезія";

        // Фіксований «зараз» і очікуваний термін повернення через 14 днів
        public static readonly DateTimeOffset Now = new(2026, 10, 28, 15, 30, 0, TimeSpan.Zero);
        public static readonly DateTime ExpectedDueDate = new(2026, 11, 11);

        // Фрагменти повідомлень LoanException
        public const string ReaderNotFoundMessage = "Reader with this email was not found";
        public const string InactiveReaderMessage = "inactive";
        public const string CopyNotFoundMessage = "No book copy found";

        // Статуси, у яких примірник видати не можна
        public static TheoryData<CopyStatus> UnavailableStatuses => new()
        {
            CopyStatus.OnLoan,
            CopyStatus.Reserved,
            CopyStatus.WrittenOff
        };

        // Нестандартний термін видачі (днів) і очікувана дата повернення від Now
        public static TheoryData<int, DateTime> CustomDurations => new()
        {
            { 7, new DateTime(2026, 11, 4) },
            { 21, new DateTime(2026, 11, 18) },
            { 30, new DateTime(2026, 11, 27) }
        };

        // Термін поза межами 1..60 днів
        public static TheoryData<int> InvalidDurations => new()
        {
            0,
            -1,
            61
        };

        public const string InvalidDurationMessage = "Loan duration must be between 1 and 60 days";

        // Порожній інвентарний номер або email
        public static TheoryData<string, string> EmptyInputs => new()
        {
            { "", UserTestData.ReaderEmail },
            { "   ", UserTestData.ReaderEmail },
            { InventoryNumber, "" },
            { InventoryNumber, "   " }
        };
    }
}
