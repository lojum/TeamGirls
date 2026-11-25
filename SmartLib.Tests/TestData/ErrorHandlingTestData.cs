namespace SmartLib.Tests.TestData
{
    // Дані для тестів глобального перехоплювача і журналу збоїв панелей
    public static class ErrorHandlingTestData
    {
        // Технічний текст винятку: у журналі він має бути, у повідомленні користувачу — ні
        public const string TechnicalMessage = "Npgsql: connection refused at aws-0-eu-west-1.pooler.supabase.com:5432";

        // Методи панелі бібліотекаря, які читають дані з бази
        public static TheoryData<string> LibrarianDashboardQueries => new()
        {
            "copies",
            "readers",
            "activity"
        };
    }
}
