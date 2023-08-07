using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace HydrometcentreApp.Controls
{
    public class CustomControl : LinearLayout
    {
        private TextView textViewDate;
        private LinearLayout linearLayoutNigth;
        private ImageView imageViewNigth;
        private TextView textViewNigthTemp;
        private TextView textViewNigthDescription;
        private LinearLayout linearLayoutDay;
        private ImageView imageViewDay;
        private TextView textViewDayTemp;
        private TextView textViewDayDescription;
               
        public CustomControl(Context context) : base(context)
        {
            Initialize(context);
        }

        public CustomControl(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialize(context);
        }

        public CustomControl(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialize(context);
        }

        public void UpdateForecastData(ForecastOnDate forecastOnDate, MainActivity mainActivity)
        {
            textViewDate.Text = forecastOnDate.date;

            if (forecastOnDate.forecastNight != null)
            {
                Bitmap bitmap = BitmapFactory.DecodeResource(Resources, Resources.GetIdentifier(forecastOnDate.forecastNight.img, "drawable", mainActivity.PackageName));
                if (bitmap is null)
                {
                    bitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.image_not_found);
                }
                imageViewNigth.SetImageBitmap(bitmap);
                textViewNigthTemp.Text = forecastOnDate.forecastNight.temp;
                textViewNigthDescription.Text = forecastOnDate.forecastNight.description.Replace(". ", "\n");
            }
            else
            {
                linearLayoutNigth.Visibility = ViewStates.Gone;
            }

            if (forecastOnDate.forecastDay != null)
            {
                Bitmap bitmap = BitmapFactory.DecodeResource(Resources, Resources.GetIdentifier(forecastOnDate.forecastDay.img, "drawable", mainActivity.PackageName));
                if (bitmap is null)
                {
                    bitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.image_not_found);
                }
                imageViewDay.SetImageBitmap(bitmap);
                textViewDayTemp.Text = forecastOnDate.forecastDay.temp;
                textViewDayDescription.Text = forecastOnDate.forecastDay.description.Replace(". ", "\n");
            }
            else
            {
                linearLayoutDay.Visibility = ViewStates.Gone;
            }

            Invalidate();
        }


        private void Initialize(Context context)
        {
            LayoutInflater inflater = LayoutInflater.FromContext(context);
            inflater.Inflate(Resource.Layout.custom_layout, this, true);

            textViewDate = FindViewById<TextView>(Resource.Id.textViewDate);

            linearLayoutNigth = FindViewById<LinearLayout>(Resource.Id.linearLayoutNigth);

            imageViewNigth = FindViewById<ImageView>(Resource.Id.imageViewNigth);
            textViewNigthTemp = FindViewById<TextView>(Resource.Id.textViewNigthTemp);
            textViewNigthDescription = FindViewById<TextView>(Resource.Id.textViewNigthDescription);

            linearLayoutDay = FindViewById<LinearLayout>(Resource.Id.linearLayoutDay);

            imageViewDay = FindViewById<ImageView>(Resource.Id.imageViewDay);
            textViewDayTemp = FindViewById<TextView>(Resource.Id.textViewDayTemp);
            textViewDayDescription = FindViewById<TextView>(Resource.Id.textViewDayDescription);
        }

        protected override void OnDraw(Android.Graphics.Canvas canvas)
        {
            base.OnDraw(canvas);


        }
    }
}