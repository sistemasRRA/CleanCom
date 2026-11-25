using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanCom.IntegrationTests.Infrastructure;

/// <summary>JSON calls as the web and the apps make them (camelCase, enums as text), failing with the API's answer.</summary>
public static class Http
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string url, object? body = null) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static async Task<JsonElement> GetOkAsync(this HttpClient client, string url) => await ReadOkAsync(await client.GetAsync(url));

    public static async Task<JsonElement> PostOkAsync(this HttpClient client, string url, object? body = null) =>
        await ReadOkAsync(await client.PostJsonAsync(url, body));

    public static async Task<JsonElement> PutOkAsync(this HttpClient client, string url, object body) =>
        await ReadOkAsync(await client.PutJsonAsync(url, body));

    /// <summary>The JSON body of a 2xx answer (an empty object for 204); otherwise fails showing what the API said.</summary>
    public static async Task<JsonElement> ReadOkAsync(this HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode} {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: {text}");
        return response.StatusCode == HttpStatusCode.NoContent || text.Length == 0 ? JsonDocument.Parse("{}").RootElement : JsonDocument.Parse(text).RootElement;
    }

    /// <summary>The ProblemDetails of an error answer, after checking its status.</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode status)
    {
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"Expected {(int)status}, got {(int)response.StatusCode}: {text}");
        return text.Length == 0 ? JsonDocument.Parse("{}").RootElement : JsonDocument.Parse(text).RootElement;
    }
}
