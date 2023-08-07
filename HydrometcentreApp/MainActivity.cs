using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.SwipeRefreshLayout.Widget;
using HydrometcentreApp.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace HydrometcentreApp
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private SwipeRefreshLayout swipeRefreshLayout;
        private LinearLayout linearLayoutMain;
        private TextView textViewLastUpdate;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Resources resources = Resources;
            string packageName = PackageName;

            linearLayoutMain = FindViewById<LinearLayout>(Resource.Id.linearLayoutMain);

            textViewLastUpdate = FindViewById<TextView>(Resource.Id.textViewLastUpdate);

            swipeRefreshLayout = FindViewById<SwipeRefreshLayout>(Resource.Id.swipeRefreshLayout);
            swipeRefreshLayout.Refresh += OnSwipeRefreshLayout_Refresh;

            SupportActionBar.Title = "Москва(ВДНХ)";

            ShowForecast();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (requestCode == 200)
            {
                if (resultCode == Android.App.Result.Ok && data != null)
                {
                    string resultData = data.GetStringExtra("result_key");
                }
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }


        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            int id = item.ItemId;

            Intent intent;
            switch (item.ItemId)
            {
                case Resource.Id.changeRegion:
                     intent = new Intent(this, typeof(ChangeForecastAreaActivity));
                    StartActivityForResult(intent, 200);
                    //Toast.MakeText(this, "Смена региона временно не доступна", ToastLength.Short).Show();
                    return true;

                case Resource.Id.nowcastingMap:
                     intent = new Intent(this, typeof(NowcastingActivity));
                    StartActivityForResult(intent, 200);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }


        private void OnSwipeRefreshLayout_Refresh(object sender, System.EventArgs e)
        {
            Task.Run(async () =>
            {
                if (await ForecastParser.UpdateForecast(this))
                {
                    RunOnUiThread(() =>
                    {
                        ShowForecast();
                        Toast.MakeText(this, "Прогноз обновлен", ToastLength.Short).Show();
                    });
                }

                swipeRefreshLayout.Refreshing = false;
            });
        }

        private void ShowForecast(bool qwe = false)
        {
            string filePath = Path.Combine(Application.Context.FilesDir.AbsolutePath, "forecast_data.json");

            List<ForecastOnDate> forecast;
            if (File.Exists(filePath))
            {
                string jsonForecast = File.ReadAllText(filePath);
                forecast = JsonConvert.DeserializeObject<List<ForecastOnDate>>(jsonForecast);
            }
            else
            {
                swipeRefreshLayout.Refreshing = true;

                OnSwipeRefreshLayout_Refresh(null, null);

                return;
            }

            var monthsFull = new List<string>() { "Января", "Февраля", "Марта", "Апреля", "Мая", "Июня", "Июля", "Августа", "Сентября", "Октября", "Ноября", "Декабря" };

            ISharedPreferences sharedPreferences = Application.Context.GetSharedPreferences("HydrometcentreApp", FileCreationMode.Private); ;
            ISharedPreferencesEditor editor = sharedPreferences.Edit();

            string textViewLastUpdateText = "Прогноз:";

            string forecastDateTime = sharedPreferences.GetString("forecastDateTime", null);
            if (!string.IsNullOrEmpty(forecastDateTime) && DateTime.TryParse(forecastDateTime, out DateTime result))
            {
                textViewLastUpdateText += $"\n\t\t\tсоздан {result.ToString($"d {monthsFull[result.Month - 1]}")} в {result.ToString($"HH:mm")}";
            }
            else
            {
                textViewLastUpdateText += "\n\t\t\tсоздан -- ---- в --:--";
            }

            string dateTimeNow = sharedPreferences.GetString("dateTimeNow", null);
            if (!string.IsNullOrEmpty(dateTimeNow) && DateTime.TryParse(dateTimeNow, out result))
            {
                textViewLastUpdateText += $"\n\t\t\tзагружен {result.ToString($"d {monthsFull[result.Month - 1]}")} в {result.ToString($"HH:mm")}";
            }
            else
            {
                textViewLastUpdateText += $"\n\t\t\tзагружен -- ---- в --:--";
            }

            textViewLastUpdate.Text = textViewLastUpdateText;

            linearLayoutMain.RemoveAllViews();
            foreach (var item in forecast)
            {
                CustomControl customControl = new CustomControl(this);
                customControl.LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,     // Width
                    ViewGroup.LayoutParams.WrapContent      // Height
                );

                linearLayoutMain.AddView(customControl);
                customControl.UpdateForecastData(item, this);
            }
        }
    }
}