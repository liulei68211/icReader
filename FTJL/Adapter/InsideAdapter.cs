
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace FTJL.Adapter
{
    //收/发货信息构造函数
    public class SendInfo
    {
        public string Info { get; set; }
        public SendInfo(string info)
        {
            this.Info = info;
        }
    }
    public class InsideAdapter:BaseAdapter
    {
        private List<SendInfo> data;
        private Context context;

        public override int Count
        {
            get
            {
                return data.Count;
            }
        }

        public InsideAdapter(List<SendInfo> data, Context context)
        {
            this.data = data;
            this.context = context;
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            convertView = LayoutInflater.From(context).Inflate(Resource.Layout.InsideViewaxml, parent, false);
            TextView title = convertView.FindViewById<TextView>(Resource.Id.Text1);
            title.Text = data[position].Info;

            return convertView;
        }
    }
}