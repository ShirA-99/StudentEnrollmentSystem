namespace StudentEnrollmentSystem.Data;

public static class SeedDataDefaults
{
    public const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=StudentEnrollmentSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public const string DemoPassword = "Pass123$";

    public static readonly string[] DemoEmails =
    [
        "alice@student.demo",
        "bob@student.demo",
        "chloe@student.demo"
    ];
}
