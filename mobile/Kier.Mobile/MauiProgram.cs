#if ANDROID
using Android.Webkit;
using Microsoft.Maui.Handlers;
#endif

namespace Kier.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder.UseMauiApp<App>();

#if ANDROID
        WebViewHandler.Mapper.AppendToMapping("KierWebViewPermissions", (handler, view) =>
        {
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.DomStorageEnabled = true;
            handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            handler.PlatformView.SetWebViewClient(new Platforms.Android.KierWebViewClient());
            handler.PlatformView.SetWebChromeClient(new Platforms.Android.KierWebChromeClient());
        });
#endif

        return builder.Build();
    }
}
