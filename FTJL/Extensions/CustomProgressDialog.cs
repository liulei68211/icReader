using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;


namespace FTJL.Extensions
{
    class CustomProgressDialog:Dialog
    {
        private static CustomProgressDialog customProgressDialog;

        public CustomProgressDialog(Context context) : base(context)
        {

        }
        public CustomProgressDialog(Context context, int themeResId) : base(context, themeResId)
        {
        }

        public static CustomProgressDialog CreateDialog(Context context)
        {
            customProgressDialog = new CustomProgressDialog(context, Resource.Style.CustomProgressDialog);


            customProgressDialog.SetContentView(Resource.Layout.loading);
            customProgressDialog.Window.Attributes.Gravity = GravityFlags.Center;

            return customProgressDialog;
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);

            if (customProgressDialog == null)
            {
                return;
            }
            ImageView imageView = customProgressDialog.FindViewById<ImageView>(Resource.Id.loadingImageView);
            AnimationDrawable animationDrawable = (AnimationDrawable)imageView.Background;
            //start the animation
            animationDrawable.Start();
        }
    }
}