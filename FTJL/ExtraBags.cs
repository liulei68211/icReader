using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;

using Android.Widget;
using System.Threading;
using Mono.Data.Sqlite;
using Android.Util;
namespace FTJL
{
    [Activity(Label = "ExtraBags")]
    public class ExtraBags : Activity
    {
        #region 变量
        private string sampbags;
        private string extrabags;
        private List<TextView> txtList = new List<TextView>();
        ArrayList rmlist = new ArrayList();
        private string rmlist_Str;
        private string sextra_one;
        TextView txt;
        Random rdm;
        Button bt_samp;

        private string purchaseOrderID;//采购订单号
        private string cmeasureID;//计量通行证号
        private int rdmNumber;
        private string sampMode;
        private string username;
        private string sampFactory;
        private string sampFactoryCode;
        private string sampGoodsCode;
        private string sampGoodsName;
        private string sampCarNum;
        private string sampAdress;
        private string LoginSystemType;
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
           
            base.OnCreate(savedInstanceState);

            // Create your application here
            
            SetContentView(Resource.Layout.ExtraBags);

            extrabags = Intent.GetStringExtra("extrabags");//抽样包数
            sampbags = Intent.GetStringExtra("sampbags");//取样包数    
            sampMode = Intent.GetStringExtra("sampMode");//取样方式
            sampAdress = Intent.GetStringExtra("sampAdress");//取样地点
          
            sampGoodsCode = Intent.GetStringExtra("sampGoodsCode");
            sampGoodsName = Intent.GetStringExtra("sampGoodsName");
            cmeasureID = Intent.GetStringExtra("cmeasure_Id");
            sampFactory = Intent.GetStringExtra("sampFactory");
            sampFactoryCode = Intent.GetStringExtra("sampFactoryCode");
            sampCarNum = Intent.GetStringExtra("sampCarNum");
            purchaseOrderID= Intent.GetStringExtra("purchaseOrderID");
            LoginSystemType = Intent.GetStringExtra("LoginSystemType");
            username = Intent.GetStringExtra("username");

            //生成随机数
            rdm = new Random();

            for (int i=0;i< Convert.ToInt32(extrabags);i++)
            {
                rdmNumber = rdm.Next(1, Convert.ToInt32(sampbags)+1);
               // rmlist.Add(rdmNumber);

                //判断集合里有没有生成的随机数，如果有，重新生成一个，知道生成的随机数list集合
                //中没有才退出循环
                while (rmlist.Contains(rdmNumber))
                {
                    rdmNumber = rdm.Next(1, Convert.ToInt32(sampbags)+1);
                }
                rmlist.Add(rdmNumber);//将生成的随机数添加到集合对象中
            }

            //抽取随机数
            LinearLayout layout = FindViewById<LinearLayout>(Resource.Id.linearLayout1);

            //通过timer控制 2秒付一个值
            //随机数赋值给textview
            System.Diagnostics.Debug.Write("主线程" + Thread.CurrentThread.ManagedThreadId);
            //实例化Timer 并初始化
            System.Timers.Timer timer = new System.Timers.Timer(1000);
            int s = 0;

            timer.Elapsed += delegate
                {
                    System.Diagnostics.Debug.Write("timer线程" + Thread.CurrentThread.ManagedThreadId);
                        RunOnUiThread(() => {
                            if (s < Convert.ToInt32(extrabags))
                            {
                                txtList[s].Text = rmlist[s].ToString();
                                layout.AddView(txtList[s]);
                                s++;
                            }
                            else
                            {
                                bt_samp.Enabled = true;
                                bt_samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));
                                bt_samp.SetTextColor(Android.Graphics.Color.ParseColor("#ffffff"));
                                timer.Stop();//暂停
                                timer.Dispose();//释放控件
                                //timer.Enabled = false;      
                            }
                    });
                };
            timer.Enabled = true;

            for (int i = 0; i < Convert.ToInt32(extrabags); i++)
            {
                txt = new TextView(this);
                
                txt.Id = i;

                txt.SetTextColor(Android.Graphics.Color.Blue);
                txt.SetTextSize(ComplexUnitType.Dip, 22);
                // txt.SetTextColor(Android.Graphics.Color.Coral);
                txt.SetBackgroundColor(Android.Graphics.Color.ParseColor("#ffffff"));
                //设置固定大小
                LinearLayout.LayoutParams lp = new LinearLayout.LayoutParams(250, 250);
                txt.SetPadding(30, 5, 30, 5);
                txtList.Add(txt);
            }
            bt_samp = FindViewById<Button>(Resource.Id.btSamp);
            bt_samp.Enabled = false;
            bt_samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
            bt_samp.Click += OnSamp;      
        }

        #region 确定按钮
        private void OnSamp(object sender,EventArgs e)
        {
                //从数组rmlist中随机抽取一个数
                int num = rmlist.Count;
                int chooes = rdm.Next(num);
                sextra_one = rmlist[chooes].ToString();

                Intent intent = new Intent(this, typeof(RandomSmpp));
                intent.SetFlags(ActivityFlags.ClearTop);

                for (int i=0;i<rmlist.Count;i++)
                {
                    rmlist_Str += rmlist[i].ToString() + ",";
                }
                //去掉逗号
                rmlist_Str = rmlist_Str.TrimEnd(',');

                intent.PutExtra("purchaseOrderID", purchaseOrderID);//采购订单号
                intent.PutExtra("cmeasure_Id", cmeasureID);
                intent.PutExtra("sextra_one", sextra_one);
                intent.PutExtra("sampMode", sampMode);
                intent.PutExtra("rmlist_Str", rmlist_Str);
                intent.PutExtra("sampbags", sampbags);

                intent.PutExtra("sampFactory", sampFactory);//供应商
                intent.PutExtra("sampFactoryCode", sampFactoryCode);//供应商编号
                intent.PutExtra("sampGoodsCode", sampGoodsCode);//存货编号
                intent.PutExtra("sampGoodsName", sampGoodsName);//存货名称
                intent.PutExtra("sampCarNum", sampCarNum);//车号
                intent.PutExtra("sampAdress", sampAdress);//地点

                intent.PutExtra("username", username);
                intent.PutExtra("LoginSystemType", LoginSystemType);
                StartActivity(intent);
        }
        #endregion





    }
}