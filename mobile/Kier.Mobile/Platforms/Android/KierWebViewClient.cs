using Android.Net.Http;
using Android.Webkit;

namespace Kier.Mobile.Platforms.Android;

public class KierWebViewClient : WebViewClient
{
    public override void OnReceivedSslError(global::Android.Webkit.WebView? view, SslErrorHandler? handler, SslError? error)
    {
#if DEBUG
        handler?.Proceed();
#else
        handler?.Cancel();
#endif
    }
}
