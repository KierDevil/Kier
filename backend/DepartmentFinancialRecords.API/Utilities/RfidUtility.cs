namespace DepartmentFinancialRecords.API.Utilities
{
    public static class RfidUtility
    {
        public static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().Replace(" ", string.Empty).ToUpperInvariant();
        }
    }
}
