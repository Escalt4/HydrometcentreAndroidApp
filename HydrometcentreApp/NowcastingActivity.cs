using Android.App;
using Android.OS;
using Android.Webkit;
using System.IO;

namespace HydrometcentreApp
{
    [Activity(Label = "NowcastingActivity")]
    public class NowcastingActivity : Activity 
    {
        WebView webViewMap;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.nowcasting_map_layout);

            using (StreamReader sr = new StreamReader(Assets.Open("map.html")))
            {
                string htmlContent = sr.ReadToEnd();
                webViewMap = FindViewById<WebView>(Resource.Id.mapWebView);
                webViewMap.Settings.JavaScriptEnabled = true;
                webViewMap.SetInitialScale(275);
                webViewMap.LoadDataWithBaseURL("file:///android_asset/", htmlContent, "text/html", "utf-8", null);
            }
        }
    }
}
