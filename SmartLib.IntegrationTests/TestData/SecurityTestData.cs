using Xunit;

namespace SmartLib.IntegrationTests.TestData
{
    public static class SecurityTestData
    {
        public static IReadOnlyList<string> SqlInjectionPayloads { get; } =
        [
            "'; DROP TABLE books; --",
            "' OR '1'='1",
            "%' OR 1=1--",
            "\\'; DELETE FROM users; --"
        ];

        public static TheoryData<string> SqlInjectionAttempts
        {
            get
            {
                var data = new TheoryData<string>();
                foreach (var payload in SqlInjectionPayloads)
                {
                    data.Add(payload);
                }

                return data;
            }
        }
    }
}