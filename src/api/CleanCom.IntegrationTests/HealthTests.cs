using System.Net;
using CleanCom.IntegrationTests.Infrastructure;

namespace CleanCom.IntegrationTests;

[Collection("api")]
public class HealthTests(ApiFactory api)
{
    [Fact]
    public async Task Health_answers_without_session_when_the_database_is_reachable()
    {
        var response = await api.Anonymous().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
