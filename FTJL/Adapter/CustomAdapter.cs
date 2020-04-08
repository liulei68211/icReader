using System;
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;

namespace FTJL.Adapter
{
    public class TableItem
    {
        public string Carfactory { get; set; }//厂家
        public string CarNumber { get; set; }//车号
        public string GoodsName { get; set; }//货物名称

        public TableItem(string samfactory, string carnumber, string goodsname)
        {
            this.Carfactory = samfactory;
            this.CarNumber = carnumber;
            this.GoodsName = goodsname;
        }
    }
   
    public  class CustomAdapter : BaseAdapter<TableItem>
    {
        List<TableItem> items;
        Activity context;
        private Dictionary<int, bool> dictChk = new Dictionary<int, bool>();//键值对
        public static List<int> checkboxes = new List<int>();//存放选中的checkbox
        public bool isChecked = false;

        public CustomAdapter(Activity context, List<TableItem> items): base()
        {
            this.items = items;
            this.context = context;
            for (int i = 0; i < items.Count; i++)
            {
                dictChk.Add(i, false);
            }
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }
        public override long GetItemId(int position)
        {
            return position;
        }

        public override TableItem this[int position]
        {
            get { return items[position]; }
        }
        public override int Count
        {
            get { return items.Count; }
        }
        public class ViewHolder : Java.Lang.Object
        {
            public TextView text1;
            public TextView text2;
            public TextView text3;
            public CheckBox checkbox;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ViewHolder holder;
            convertView = context.LayoutInflater.Inflate(Resource.Layout.CustomView, null);
            TableItem item = items[position];
            if (convertView != null)
            {
                holder = new ViewHolder();
                holder.text1 = convertView.FindViewById<TextView>(Resource.Id.textView1);
                holder.text2 = convertView.FindViewById<TextView>(Resource.Id.textView2);
                holder.text3 = convertView.FindViewById<TextView>(Resource.Id.textView3);
                holder.checkbox = convertView.FindViewById<CheckBox>(Resource.Id.checkBox1);
                convertView.Tag = holder;
            }
            else
            {
                holder = (ViewHolder)convertView.Tag;
            }
            holder.text1.Text = item.Carfactory;
            holder.text2.Text = item.CarNumber;
            holder.text3.Text = item.GoodsName;
            holder.checkbox.Tag = position;//不能丢
            holder.checkbox.Checked = dictChk[(int)holder.checkbox.Tag];//解决listview滑动时checkbox状态改变
            //if (isChecked)
            //{
            //    holder.checkbox.Checked = true;
            //}
            //else
            //{
            //    holder.checkbox.Checked = false;
            //}


            holder.checkbox.CheckedChange += (s, e) =>
            {
                if (e.IsChecked)
                {
                    dictChk[(int)holder.checkbox.Tag] = e.IsChecked;//解决listview滑动时checkbox状态改变
                    //Android.Widget.Toast.MakeText(parent.Context, "点击的是" + item.CarNumber + item.GoodsName, ToastLength.Long).Show(); ;
                }
                
            };
                return convertView;
            #region 暂不用
            //var item = items[position];
            //View view = convertView;
            //if (view == null) // no view to re-use, create new
            //    view = context.LayoutInflater.Inflate(Resource.Layout.CustomView, null);
            //view.FindViewById<TextView>(Resource.Id.textView1).Text = item.SampID;
            //view.FindViewById<TextView>(Resource.Id.textView2).Text = item.CarNumber;
            //view.FindViewById<TextView>(Resource.Id.textView3).Text = item.GoodsName;
            //CheckBox checkBox = view.FindViewById<CheckBox>(Resource.Id.checkBox1);
            //checkBox.CheckedChange += (s, e) =>
            //{
            //    if (e.IsChecked)
            //    {
            //        dictChk[(int)checkBox.Tag] = e.IsChecked;//解决listview滑动时checkbox状态改变
            //        Android.Widget.Toast.MakeText(parent.Context, "点击的是" + item.CarNumber + item.GoodsName, ToastLength.Long).Show(); ;
            //    }
            //    else
            //    {

            //    }
            //};
            //    return view;
            #endregion
        }
    }
}