using Android.Webkit;

namespace Kier.Mobile.Platforms.Android;

public class KierWebChromeClient : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest? request)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            request?.Grant(request.GetResources());
        });
    }
}
