using System.Net.Http.Json;

namespace Villainous.Extensions;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostJson<T>(this HttpClient httpClient, string url, T content)
    {
        var response = await httpClient.PostAsJsonAsync(url, content);
        if (!response.IsSuccessStatusCode)
            throw new Exception("");

        return response;
    }

    public static async Task<HttpResponseMessage> PostJson(this HttpClient httpClient, string url)
    {
        var response = await httpClient.PostAsync(url, null);
        if (!response.IsSuccessStatusCode)
            throw new Exception("");

        return response;
    }

    public static async Task<string> GetString(this Task<HttpResponseMessage> httpResponseTask)
    {
        return await (await httpResponseTask).Content.ReadAsStringAsync();
    }

    public static async Task<T> GetAsJson<T>(this Task<HttpResponseMessage> httpResponseTask)
    {
        return (await (await httpResponseTask).Content.ReadFromJsonAsync<T>())!;
    }

    public static void AuthenticateWithBearer(this HttpClient httpClient, string accessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }
}