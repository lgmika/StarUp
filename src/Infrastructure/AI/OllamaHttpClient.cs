using Microsoft.Extensions.Options;

namespace StartupConnect.Infrastructure.AI;

public sealed class OllamaHttpClient : IDisposable
{
    public OllamaHttpClient(IOptions<AIOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value.Ollama;
        Client = new HttpClient
        {
            BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
    }

    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
    }
}
