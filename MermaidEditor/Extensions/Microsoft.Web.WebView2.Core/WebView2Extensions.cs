using WebView = Microsoft.Web.WebView2.Wpf.WebView2;

namespace Microsoft.Web.WebView2.Core
{
    internal static class WebView2Extensions
    {
        public static bool IsWebViewAvailable(this WebView webView) => (webView.CoreWebView2 != null);
    }
}