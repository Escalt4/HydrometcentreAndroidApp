using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HydrometcentreApp
{
    [Activity(Label = "ChangeForecastAreaActivity")]
    public class ChangeForecastAreaActivity : Activity
    {
        private HttpClientHandler handler;
        private HttpClient httpClient;
        private CancellationTokenSource cancellationTokenSource;

        EditText editTextSearch;
        ListView listViewSearch;
        ArrayAdapter<string> arrayAdapter;
        private ProgressBar progressBarSearch;

        List<string> items = new List<string>();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_change_forecast_area);

            editTextSearch = FindViewById<EditText>(Resource.Id.editTextSearch);
            editTextSearch.TextChanged += UpdateSearchResult;

            listViewSearch = FindViewById<ListView>(Resource.Id.listViewSearch);
            arrayAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, items);
            listViewSearch.Adapter = arrayAdapter;

            progressBarSearch = FindViewById<ProgressBar>(Resource.Id.progressBarSearch);

            CookieContainer cookieContainer = ForecastParser.LoadCookiesFromSharedPreferences();
            handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };

            httpClient = new HttpClient(handler);
            cancellationTokenSource = new CancellationTokenSource();

            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("Host", "meteoinfo.ru");
            httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://meteoinfo.ru/");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not/A)Brand\";v=\"99\", \"Microsoft Edge\";v=\"115\", \"Chromium\";v=\"115\"");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
            httpClient.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            //var resultIntent = new Intent();
            //resultIntent.PutExtra("result_key", "Some data"); 
            //SetResult(Result.Ok, resultIntent);
            //Finish();

        }

        private async void UpdateSearchResult(object sender, System.EventArgs e)
        {
            string searchQuery = editTextSearch.Text;

            if (searchQuery.Length < 2)
            {
                items.Clear();
                arrayAdapter.Clear();
                arrayAdapter.AddAll(items);
                arrayAdapter.NotifyDataSetChanged();
                return;
            };
            listViewSearch.Visibility = ViewStates.Gone;
            progressBarSearch.Visibility = ViewStates.Visible;

            string url = $"https://meteoinfo.ru/hmc-output/select/select_s2.php?q[term]={WebUtility.UrlEncode(searchQuery)}&q[_type]=query&id_lang=1";

            cancellationTokenSource.Cancel();
            cancellationTokenSource = new CancellationTokenSource();
            string responseBody = "";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url, cancellationTokenSource.Token);


                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                {
                    if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        using (var gzipStream = new GZipStream(contentStream, CompressionMode.Decompress))
                        using (var reader = new StreamReader(gzipStream))
                        {
                            responseBody = reader.ReadToEnd();
                        }
                    }
                    else
                    {
                        using (var reader = new StreamReader(contentStream))
                        {
                            responseBody = reader.ReadToEnd();
                        }
                    }
                }

                RootObject result = JsonConvert.DeserializeObject<RootObject>(responseBody);

                List<Result> results = result.Results;
                items.Clear();
                foreach (var item in results)
                {
                    items.Add(item.Text);
                }

                RunOnUiThread(() =>
                {
                    arrayAdapter.Clear();
                    arrayAdapter.AddAll(items);
                    arrayAdapter.NotifyDataSetChanged();

                    progressBarSearch.Visibility = ViewStates.Gone;
                    listViewSearch.Visibility = ViewStates.Visible;
                });
            }
            catch (TaskCanceledException)
            {
                // Запрос был отменен
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    progressBarSearch.Visibility = ViewStates.Gone;
                    Toast.MakeText(this, "Ошибка", ToastLength.Short).Show();
                });
                Console.WriteLine(url);
                Console.WriteLine(responseBody);
                Console.WriteLine(ex.ToString());
            }
        }
    }

    public class Result
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    public class RootObject
    {
        public List<Result> Results { get; set; }
    }
}