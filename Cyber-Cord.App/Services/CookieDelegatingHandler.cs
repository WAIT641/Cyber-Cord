using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Cyber_Cord.App.Services;

public class CookieDelegatingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return await base.SendAsync(request, cancellationToken);
    }
}
