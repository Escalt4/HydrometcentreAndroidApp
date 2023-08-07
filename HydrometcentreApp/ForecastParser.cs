using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Widget;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HydrometcentreApp
{
    public class ForecastParser
    {
        static readonly List<string> weekDays = new List<string>() { "Пн ", "Вт ", "Ср ", "Чт ", "Пт ", "Сб ", "Вс " };
        static readonly List<string> weekDaysFull = new List<string>() { "Понедельник, ", "Вторник, ", "Среда, ", "Четверг, ", "Пятница, ", "Суббота, ", "Воскресенье, " };
        static readonly List<string> months = new List<string>() { " янв", " фев", " мар", " апр", " май", " июн", " июл", " авг", " сен", " окт", " ноя", " дек" };
        static readonly List<string> monthsFull = new List<string>() { " Января", " Февраля", " Марта", " Апреля", " Мая", " Июня", " Июля", " Августа", " Сентября", " Октября", " Ноября", " Декабря" };

        static async public Task<bool> UpdateForecast(Activity activity)
        {
            string htmlString = null;

            CookieContainer cookieContainer = LoadCookiesFromSharedPreferences();

            using (HttpClient httpClient = new HttpClient(new HttpClientHandler() { CookieContainer = cookieContainer }))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36 Edg/115.0.1901.188");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru,en;q=0.9,en-GB;q=0.8,en-US;q=0.7");
                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                httpClient.DefaultRequestHeaders.Add("Host", "meteoinfo.ru");
                httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not/A)Brand\";v=\"99\", \"Microsoft Edge\";v=\"115\", \"Chromium\";v=\"115\"");
                httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                httpClient.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync("https://meteoinfo.ru/forecasts/russia/moscow-area/moscow");
                    response.EnsureSuccessStatusCode();

                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                        {
                            using (var gzipStream = new GZipStream(contentStream, CompressionMode.Decompress))
                            using (var reader = new StreamReader(gzipStream))
                            {
                                htmlString = reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            using (var reader = new StreamReader(contentStream))
                            {
                                htmlString = reader.ReadToEnd();
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    activity.RunOnUiThread(() =>
                    {
                        Toast.MakeText(activity, "Ошибка подключения к meteoinfo.ru", ToastLength.Short).Show();
                    });
                    return false;
                }


                var forecast = new List<ForecastOnDate>();
                DateTime dateTimeNow = DateTime.Now;
                DateTime forecastDateTime;

                try
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(htmlString);

                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//br"))
                    {
                        node.ParentNode.ReplaceChild(doc.CreateTextNode(" "), node);
                    }

                    var forecastString = new List<string>();
                    var table = doc.GetElementbyId("div_4_1").SelectSingleNode("./div/table");
                    foreach (var node in table.ChildNodes)
                    {
                        foreach (var qwe in node.ChildNodes)
                        {
                            string str = qwe.InnerText.Trim();
                            if (str != "")
                            {
                                forecastString.Add(str);
                            }
                            else
                            {
                                forecastString.Add($"{qwe.SelectSingleNode(qwe.XPath + "//img").Attributes["src"].Value}");
                            }
                        }
                    }

                    int i = 0;
                    while (i != forecastString.Count)
                    {
                        if (weekDays.Contains(forecastString[i].Substring(0, 3)))
                        {
                            forecast.Add(new ForecastOnDate(DoFullName(DoFullName(forecastString[i], weekDays, weekDaysFull), months, monthsFull)));
                            i++;
                        }
                        else if (forecastString[i] == "День")
                        {
                            forecast.Last().forecastDay = new ForecastOnHalhDate(forecastString[i + 1], forecastString[i + 2], forecastString[i + 3]);
                            i += 4;
                        }
                        else if (forecastString[i] == "Ночь")
                        {
                            forecast.Last().forecastNight = new ForecastOnHalhDate(forecastString[i + 1], forecastString[i + 2], forecastString[i + 3]);
                            i += 4;
                        }
                    }

                    string zametka = doc.GetElementbyId("div_4_1").SelectSingleNode("./div/div").InnerText.Trim();
                    string forecastTimeString = zametka.Substring(zametka.IndexOf("обновлена в ") + "обновлена в ".Length, 5);
                    DateTime forecastTime = DateTime.ParseExact(forecastTimeString, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    DateTime currentTime = DateTime.ParseExact(DateTime.Now.ToString("HH:mm"), "HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                    if (forecastTime <= currentTime)
                    {
                        forecastDateTime = DateTime.ParseExact($"{dateTimeNow.Year}-{dateTimeNow.Month}-{dateTimeNow.Day} {forecastTimeString}", "yyyy-M-d HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        dateTimeNow = dateTimeNow.AddDays(-1);
                        forecastDateTime = DateTime.ParseExact($"{dateTimeNow.Year}-{dateTimeNow.Month}-{dateTimeNow.Day} {forecastTimeString}", "yyyy-M-d HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                        dateTimeNow = dateTimeNow.AddDays(1);
                    }

                }
                catch (Exception)
                {
                    activity.RunOnUiThread(() =>
                    {
                        Toast.MakeText(activity, "Ошибка обработки данных", ToastLength.Short).Show();
                    });
                    return false;
                }

                string filePath = System.IO.Path.Combine(Application.Context.FilesDir.AbsolutePath, "forecast_data.json");
                System.IO.FileInfo fileInf = new System.IO.FileInfo(filePath);
                if (fileInf.Exists)
                {
                    fileInf.Delete();
                }
                string jsonForecast = JsonConvert.SerializeObject(forecast);
                File.WriteAllText(filePath, jsonForecast);

                ISharedPreferences sharedPreferences = Application.Context.GetSharedPreferences("HydrometcentreApp", FileCreationMode.Private); ;
                ISharedPreferencesEditor editor = sharedPreferences.Edit();

                editor.PutString("forecastDateTime", forecastDateTime.ToString("O"));
                editor.PutString("dateTimeNow", dateTimeNow.ToString("O"));
                editor.Apply();

            }

            SaveCookiesToSharedPreferences(cookieContainer);

            return true;
        }

        static private async void DownloadAndSaveImg(string imageUrl, Activity activity, HttpClient httpClient)
        {
            string filePath = System.IO.Path.Combine(Application.Context.FilesDir.AbsolutePath, imageUrl.Replace(@"https://meteoinfo.ru/images/ico/", ""));
            System.IO.FileInfo fileInf = new System.IO.FileInfo(filePath);

            if (fileInf.Exists)
            {
                fileInf.Delete();
            }

            Exception e = null;
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync(imageUrl);
                    response.EnsureSuccessStatusCode();

                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    Android.Graphics.Bitmap imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

                    Matrix matrix = new Matrix();
                    float scale = (float)60 / Math.Max(imageBitmap.Width, imageBitmap.Height);
                    matrix.SetScale(scale, scale);
                    imageBitmap = Bitmap.CreateBitmap(imageBitmap, 0, 0, imageBitmap.Width, imageBitmap.Height, matrix, true);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        imageBitmap.Compress(Bitmap.CompressFormat.Png, 100, stream);
                    }

                    return;
                }
                catch (Exception ex)
                {
                    e = ex;
                    Thread.Sleep(250);
                }
            }

            activity.RunOnUiThread(() =>
            {
                Toast.MakeText(activity, $"Ошибка загрузки {imageUrl.Replace(@"https://meteoinfo.ru/images/ico/", "")}", ToastLength.Short).Show();
            });

            Console.WriteLine(e.ToString());
        }

        static private string DoFullName(string str, List<string> shorts, List<string> full)
        {
            string s = str;

            for (int i = 0; i < shorts.Count; i++)
            {
                s = s.Replace(shorts[i], full[i]);
            }
            return s;
        }

        static public CookieContainer LoadCookiesFromSharedPreferences()
        {
            CookieContainer cookieContainer;
            string serializedCookieContainer = Application.Context.GetSharedPreferences("HydrometcentreApp", FileCreationMode.Private).GetString("serializedCookieContainer", null);
            if (!string.IsNullOrEmpty(serializedCookieContainer))
            {
                cookieContainer = DeserializeCookieContainer(serializedCookieContainer);
            }
            else
            {
                cookieContainer = new CookieContainer();
            }

            return cookieContainer;
        }

        static public void SaveCookiesToSharedPreferences(CookieContainer cookieContainer)
        {
            ISharedPreferencesEditor editor = Application.Context.GetSharedPreferences("HydrometcentreApp", FileCreationMode.Private).Edit();

            List<SerializableCookie> serializableCookies = ConvertToSerializableCookies(cookieContainer);
            string serializedCookieContainer = JsonConvert.SerializeObject(serializableCookies);
            editor.PutString("serializedCookieContainer", serializedCookieContainer);
            editor.Apply();
        }

        static private List<SerializableCookie> ConvertToSerializableCookies(CookieContainer container)
        {
            List<SerializableCookie> serializableCookies = new List<SerializableCookie>();

            foreach (Cookie cookie in container.GetCookies(new Uri("https://meteoinfo.ru")))
            {
                serializableCookies.Add(new SerializableCookie
                {
                    Name = cookie.Name,
                    Value = cookie.Value,
                    Domain = cookie.Domain,
                    Path = cookie.Path,
                    Expires = cookie.Expires,
                    HttpOnly = cookie.HttpOnly,
                    Secure = cookie.Secure
                });
            }

            return serializableCookies;
        }

        static private CookieContainer DeserializeCookieContainer(string serializedCookies)
        {
            List<SerializableCookie> serializableCookies = JsonConvert.DeserializeObject<List<SerializableCookie>>(serializedCookies);
            CookieContainer container = new CookieContainer();

            foreach (SerializableCookie serializableCookie in serializableCookies)
            {
                container.Add(new Cookie(serializableCookie.Name, serializableCookie.Value, serializableCookie.Path, serializableCookie.Domain)
                {
                    Expires = serializableCookie.Expires,
                    HttpOnly = serializableCookie.HttpOnly,
                    Secure = serializableCookie.Secure
                });
            }

            return container;
        }
    }

    public class ForecastOnDate
    {
        public string date;
        public ForecastOnHalhDate forecastDay;
        public ForecastOnHalhDate forecastNight;

        public ForecastOnDate(string date)
        {
            this.date = date;
        }
    }

    public class ForecastOnHalhDate
    {
        public string img;
        public string temp;
        public string description;

        public ForecastOnHalhDate(string img, string temp, string description)
        {
            this.img = img.Replace(@"/images/ico/", "weather_").Replace(@".png", "");
            this.temp = temp.Replace("&deg;", "°");
            this.description = description.Replace("- ", "-");
        }
    }

    [Serializable]
    public class SerializableCookie
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Domain { get; set; }
        public string Path { get; set; }
        public DateTime Expires { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
    }
}