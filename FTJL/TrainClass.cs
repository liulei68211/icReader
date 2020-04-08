using System;
using System.Collections.Generic;
using System.Data.SqlClient;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using Android.Nfc;
using Android.Nfc.Tech;
using CN.Pda.Rfid.HF;
using CN.Pda.Serialport;
using FTJL.Adapter;
namespace FTJL
{
    [Activity(Label = "火车取样")]
    public class TrainClass : Activity
    {
        private Button btSelect;
        private string sqlstr;
        private List<string> tmGH = new List<string>();//过衡时间
        private string ghTm;//选中的过衡时间

        private string ghTime;//选中的过衡时间

        Boolean isSpinnerFirst = true;//下拉菜单是否为第一个选项
        private List<string> number = new List<string>();//序号
        private List<string> carNumber = new List<string>();//车号
        private List<string> gsInfo = new List<string>();//货物名称

        private string LoginSystemType;
        Tag tag;
        /// <summary>
        /// IC操作类
        /// </summary>
        IC ic = new IC();
        /// <summary>
        /// 车辆信息类
        /// </summary>
        CarInfo Car;

        /// <summary>
        /// SerialPort - 串口
        /// </summary>
        SerialPort serial;
        /// <summary>
        /// SerialPort - 读卡指令
        /// </summary>
        IHfConmmand hf;

        /// <summary>
        /// NFC模式
        /// </summary>
        NfcAdapter m_nfcAdapter;
        /// <summary>
        /// Android模式的Intent
        /// </summary>
        Intent m_intent;
        /// <summary>
        /// 标签或IC卡
        /// </summary>
        Tag m_tag;
        /// <summary>
        /// 
        /// </summary>
        private PendingIntent mPendingIntent;
        /// <summary>
        /// Intent过滤器    
        /// </summary> 
        private IntentFilter[] mFilters;
        /// <summary>
        /// technologies列表 
        /// </summary> 
        private string[][] mTechLists;

        //弹出列表
        //private bool[] checkItems;
        //private AlertDialog alertDialog = null;
        //private AlertDialog.Builder builder = null;

        private static JygtService.FoundationService Client = new JygtService.FoundationService();

        #region 创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.TrainSamp);

            LoginSystemType = Intent.GetStringExtra("LoginSystemType");

            btSelect = FindViewById<Button>(Resource.Id.btTm);

            btSelect.Click += tmSelect;

            //当前时间
            string xhdt = string.Format("{0:yyyy-MM-dd}", DateTime.Now);
            //btSelect.Text = "-- 请选择日期 --";
            btSelect.Text = xhdt;

            //过衡时间
            Spinner ghTm = FindViewById<Spinner>(Resource.Id.ghTm);

           // ghTm.ItemSelected += spinner_ItemSelected;
            ghTm.ItemSelected += delegate { spinner_ItemSelected(ghTm); };
            
            //过衡时间查询
            ghTmSelect();
        }
        #endregion      

        #region 日期选择按钮
        private void tmSelect(object sender, EventArgs e)
        {
            if (isSpinnerFirst == false)
            {
                isSpinnerFirst = true;
            }
            else
            {
                DatePickerFragment frag = DatePickerFragment.NewInstance(delegate (DateTime time)
                {
                     //btSelect.Text = time.ToLongDateString();//显示的是2017年1月1日；
                     btSelect.Text = time.ToString("yyyy-MM-dd");//显示的是2017-1-1；
                     ghTmSelect();
                 });
                frag.Show(FragmentManager, DatePickerFragment.TAG);
            }
        }
        #endregion

        #region 过衡时间查询
        private void ghTmSelect()
        {
                //数据库查询
                sqlstr = " SELECT distinct Passtime " +
                          " From [192.168.122.2].[GDH].[dbo].[Train_t_BaseData]  " +
                          " where (convert(varchar(10),Passtime,120) = '" + btSelect.Text + "') "+
                          " and (Balance like '%钢五%' or Balance like '%钢三%'  or  Balance like '%钢一%')";
                Sql sqlOpration = new Sql();
                //连接数据库
                sqlOpration.getcon2();           
                sqlOpration.getsqlcom(sqlstr);
                SqlDataReader sqlrd = sqlOpration.getrd(sqlstr);

                tmGH.Clear();
                while (sqlrd.Read())
                {
                    string str = sqlrd["Passtime"].ToString();
                    string str2 = Convert.ToDateTime(str).ToString("yyyy-MM-dd HH:mm:ss.000");
                    tmGH.Add(str2);
                }
                sqlOpration.closecon();

                #region 远程webservice
            //   Client.getsqlcom(sqlstr);
            //string[] tmGH = Client.getrd(sqlstr);
            //Array.Clear(tmGH, 0, tmGH.Length);//清空数组
            #endregion

                //过衡时间
                Spinner ghTm = FindViewById<Spinner>(Resource.Id.ghTm);
                //下拉框选择事件
                //ghTm.ItemSelected += tmChoose;
                //ghTm.ItemSelected += delegate { ItemSelected(ghTm); };

                //数据填充
                ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, tmGH);
                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                ghTm.Adapter = adapter;
        }
        #endregion       

        #region 过衡时间选择
        private void spinner_ItemSelected(View v)
        {
            if (isSpinnerFirst == true)
            {
                isSpinnerFirst = false;
            }
            else
            {
                Spinner ghTm = FindViewById<Spinner>(Resource.Id.ghTm);

                //下拉框选中的值
                ghTime = ghTm.SelectedItem.ToString();

                Intent intent = new Intent(this, typeof(TrainInfoList));
                intent.SetFlags(ActivityFlags.ClearTop);
                intent.PutExtra("ghTime", ghTime);
                intent.PutExtra("LoginSystemType", LoginSystemType);
                StartActivity(intent);

                #region 弹出多选列表
                //查询货物信息
                //var b = new AlertDialog.Builder(this);
                //string[] menu = new string[] { "麻婆豆腐 ", "羊蝎子", "驴肉火烧", "辣子鸡丁" };
                //checkItems = new bool[] { false, false, false, false };
                //b = b.SetMultiChoiceItems(menu, checkItems, (s, e) =>
                //{
                //    //Toast.MakeText(this, "you selected " + menu[e.Which], ToastLength.Short).Show();  
                //    checkItems[e.Which] = e.IsChecked;
                //})
                //  .SetPositiveButton("确定", (s, e) =>
                //  {
                //      string result = string.Empty;
                //      for (int i = 0; i < checkItems.Length; i++)
                //      {
                //          if (checkItems[i])
                //          {
                //              result += menu[i] + ",";
                //          }
                //      }
                //      Toast.MakeText(this, "you selected " + result, ToastLength.Short).Show();
                //  });
                //b.Create();
                //b.Show();
                #endregion
            }
        }
        #endregion

        #region 货物信息查询
        private void gsInfoSelect()
        {
            //数据库查询
            Sql sqlOpration = new Sql();
            //连接数据库
            sqlOpration.getcon2();
            sqlstr = " SELECT  No [序号], TrainNo [车号],  CarsType [车型], Gross [毛重]," +
                     " Tare[皮重], Selftare[标皮重], NetWeight[净重], CargoName[品种]," +
                     " SendUnit[供需方],convert(varchar(10),Passtime,120)[过衡时间],Passtime, TrainType[进出厂], Operator[操作员] " +
                     " From [GDH].[dbo].Train_t_BaseData  where Passtime = '" + ghTime + "')";

            sqlOpration.getsqlcom(sqlstr);
 
            SqlDataReader sqlrd = sqlOpration.getrd(sqlstr);
            tmGH.Clear();

            while (sqlrd.Read())
            {
                number.Add(sqlrd["序号"].ToString());
                carNumber.Add(sqlrd["车号"].ToString());
                gsInfo.Add(sqlrd["品种"].ToString());
            }
        }
        #endregion
    }
}