using System.Text;
using Newtonsoft.Json;

namespace BackbonePackagesService.Api.Temporary;

/// <summary>
/// Provides extension methods for http requests.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Reads the <see cref="HttpContent"/> as a string and deserializes it into an object of type <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The type to which the JSON content should be deserialized.</typeparam>
    /// <param name="content">The HTTP content to read.</param>
    /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure deserialization behavior.</param>
    /// <returns>The deserialized instance of <see cref="TModel"/>.</returns>
    public static async ValueTask<TModel?> ReadFromNewtonsoftJsonAsync<TModel>(this HttpContent content, JsonSerializerSettings settings)
    {
        var contentString = await content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TModel>(contentString);
    }

    /// <summary>
    /// Sends a POST request with a JSON payload serialized using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="TModel">The type of the object to serialize as the JSON payload.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> instance used to make the request.</param>
    /// <param name="requestUri">The URI to which the request is sent.</param>
    /// <param name="value">The object to serialize and include in the JSON payload.</param>
    /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure serialization behavior.</param>
    /// <returns>The response message</returns>
    public static Task<HttpResponseMessage> PostAsNewtonsoftJsonAsync<TModel>(
        this HttpClient client,
        string requestUri,
        TModel value,
        JsonSerializerSettings settings)
    {
        var content = new StringContent(
            JsonConvert.SerializeObject(value, settings),
            Encoding.UTF8,
            "application/json");

        return client.PostAsync(requestUri, content);
    }
    
    /// <summary>
    /// Sends a PUT request with a JSON payload serialized using Newtonsoft.Json.
    /// </summary>
    /// <typeparam name="TModel">The type of the object to serialize as the JSON payload.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/> instance used to make the request.</param>
    /// <param name="requestUri">The URI to which the request is sent.</param>
    /// <param name="value">The object to serialize and include in the JSON payload.</param>
    /// <param name="settings">The <see cref="JsonSerializerSettings"/> to configure serialization behavior.</param>
    /// <returns>The response message</returns>
    public static Task<HttpResponseMessage> PutAsNewtonsoftJsonAsync<TModel>(
        this HttpClient client,
        string requestUri,
        TModel value,
        JsonSerializerSettings settings)
    {
        var content = new StringContent(
            JsonConvert.SerializeObject(value, settings),
            Encoding.UTF8,
            "application/json");

        return client.PutAsync(requestUri, content);
    }
}