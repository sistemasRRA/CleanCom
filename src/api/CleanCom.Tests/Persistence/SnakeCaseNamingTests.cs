using CleanCom.Infrastructure.Persistence;
using CleanCom.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CleanCom.Tests.Persistence;

public class SnakeCaseNamingTests
{
    [Theory]
    [InlineData("Rutas", "rutas")]
    [InlineData("ZonaVisitDays", "zona_visit_days")]
    [InlineData("CupoParadas", "cupo_paradas")]
    [InlineData("PK_Rutas", "pk_rutas")]
    [InlineData("IX_Settings_Key_EffectiveFrom", "ix_settings_key_effective_from")]
    [InlineData("RFC", "rfc")]
    [InlineData("UserRoles", "user_roles")]
    public void Converts_names_to_snake_case(string name, string expected)
    {
        Assert.Equal(expected, SnakeCaseNaming.ToSnakeCase(name));
    }

    [Fact]
    public void The_whole_model_uses_snake_case()
    {
        // Building the model does not open a connection.
        var options = new DbContextOptionsBuilder<CleanComDbContext>()
            .UseSqlServer("Server=.;Database=x;", sql => sql.UseNetTopologySuite())
            .Options;
        using var db = new CleanComDbContext(options);

        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            Assert.Equal(table.ToLowerInvariant(), table);
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName();
                Assert.Equal(column.ToLowerInvariant(), column);
            }
        }
        Assert.Equal("users", db.Model.FindEntityType(typeof(User))!.GetTableName());
    }
}
