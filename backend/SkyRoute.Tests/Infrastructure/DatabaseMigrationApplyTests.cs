using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkyRoute.Infrastructure.Data;

namespace SkyRoute.Tests.Infrastructure;

public sealed class DatabaseMigrationApplyTests
{
    private const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=SkyRouteDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    [Fact]
    public void Database_CanConnect_AndAuthTablesExistWithExpectedIndexes()
    {
        var options = new DbContextOptionsBuilder<SkyRouteDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new SkyRouteDbContext(options);
        context.Database.Migrate();
        Assert.True(context.Database.CanConnect());

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bookings'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RefreshTokens'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM sys.indexes WHERE name = 'IX_Bookings_ReferenceCode'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM sys.indexes WHERE name = 'IX_Bookings_UserId'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM sys.indexes WHERE name = 'IX_Users_Email'"));
        Assert.Equal(1, ExecuteCount(connection, "SELECT COUNT(1) FROM sys.indexes WHERE name = 'IX_RefreshTokens_TokenHash'"));
    }

    private static int ExecuteCount(SqlConnection connection, string sql)
    {
        using var command = new SqlCommand(sql, connection);
        return Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
