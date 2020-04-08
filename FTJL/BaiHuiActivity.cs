using System;
using System.Data;
using System.Data.SqlClient;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;


namespace FTJL
{
    [Activity(Label = "白灰取样")]
    public class BaiHuiActivity : Activity
    {
        #region 变量
        //IcDatas[0,0] - 1扇区 4块  从第一位开始：24位车号（解密后为正常车牌照）
        //IcDatas[0,1] - 1扇区 5块  从第一位开始：14位取样编号
        //IcDatas[0,2] - 1扇区 6块  从第一位开始：取样地点(2位) 

        //IcDatas[1,0] - 2扇区 8块  取样时间
        //IcDatas[1,1] - 2扇区 9块  取样人（6位）
        //IcDatas[1,2] - 2扇区 10块 取样方式(4位)

        //IcDatas[2,0] - 3扇区 12块 货物主键（2位）
        //IcDatas[2,1] - 3扇区 13块 
        //IcDatas[2,2] - 3扇区 14块 

        //IcDatas[3,0] - 4扇区 16块 
        //IcDatas[3,1] - 4扇区 17块
        //IcDatas[3,2] - 4扇区 18块

        //IcDatas[4,0] - 5扇区 20块 
        //IcDatas[4,1] - 5扇区 21块 
        //IcDatas[4,2] - 5扇区 22块

        /// <summary>
        /// IC操作类
        /// </summary>
        IC ic = new IC();

        /// <summary>
        /// 确认取样
        /// </summary>
        ToggleButton readIc, btSamplecar;
        /// <summary>
        /// 货物地点编号及地点
        /// </summary>
        public string adressCode, adressName;
        public string adressID;
        /// <summary>
        /// 货物名称和编号
        /// </summary>
        private string cNCGoodsName, cNCGoodsCode;
        /// <summary>
        /// 取样时间
        /// </summary>
        public DateTime sampTime;

        /// <summary>
        /// 操作员名字
        /// </summary>
        private string username;
        private string LoginSystemType;
        /// <summary>
        /// 是否成功写入数据库
        /// </summary>
        public string strError;//判断事务是否为空

        EditText samp_goodsname;
        Spinner sp_mode;
        EditText samp_adress;
        EditText samp_time;
        EditText samp_people;

        Sql sql = new Sql();
        SqlDataReader sqlrd;
        BasicInfo bsif = new BasicInfo();
        public DataTable dt;
        public string seqId;//取样编号
        #endregion

        #region 创建activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Create your application here
            SetContentView(Resource.Layout.baiHui);

            username = Intent.GetStringExtra("username");
            LoginSystemType = Intent.GetStringExtra("LoginSystemType");
            //数据库查询地点
            //bsif.selectAdress( out bsif.sampAdress, out bsif.adressCode);
            samp_adress = FindViewById<EditText>(Resource.Id.sampAdress);
            samp_adress.Text = "原料厂白灰厂";

            adressID = "22";
            adressCode = "16";

            //货物名称 
            samp_goodsname = FindViewById<EditText>(Resource.Id.sampGoodsname);
            cNCGoodsCode = "0700000301";
            samp_goodsname.Text = "冶金石灰";

            //业务类型
            //业务类型下拉菜单
            sp_mode = FindViewById<Spinner>(Resource.Id.sampMode);
            sp_mode.ItemSelected += selectMode_ItemSelected;

            //填充xml数据
            ArrayAdapter adapter_mode = ArrayAdapter.CreateFromResource(this, Resource.Array.SampMode, Android.Resource.Layout.SimpleSpinnerItem);
            adapter_mode.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            sp_mode.Adapter = adapter_mode;
            sp_mode.Prompt = "请选择";

            //日期
            string tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:ss:mm");
            samp_time = FindViewById<EditText>(Resource.Id.sampTime);
            samp_time.Text = tmnw;

            //取样人
            samp_people = FindViewById<EditText>(Resource.Id.sampPeople);
            samp_people.Text = username;

            //读取计量卡按钮
            readIc = FindViewById<ToggleButton>(Resource.Id.btReadic);
            readIc.Click += icRead;
            //读取计量卡按钮
            btSamplecar = FindViewById<ToggleButton>(Resource.Id.btSamplecar);
            btSamplecar.Click += extraCar;
        }
        #endregion

        #region 取样方式下拉菜单选择事件 获取对应得id
        private void selectMode_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            string select_modeId = "select pk from QQB_BusiType where cBusiName = '" + CurSpinner.SelectedItem.ToString() + "'";
            sql.getcon2();

            sqlrd = sql.getrd(select_modeId);
            if (sqlrd.Read())
            {
                bsif.sampModeId = Convert.ToInt32(sqlrd["pk"].ToString());
            }
            sqlrd.Close();
            sql.closecon();
        }
        #endregion

        #region 读取计量卡按钮
        private void icRead(object sender, EventArgs e)
        {
            Intent intent = new Intent(this, typeof(TrainInfoList));
            intent.SetFlags(ActivityFlags.ClearTop);
            /* 传递数据
             * 取样地点 samp_adressName 取样地点id samp_adressId
             * NV货物编号 nvgoodsCode
             * 取样方式 samp_modeName  取样方式id samp_modeId
             * 取样数量 samp_weight  
             * 货物单位 samp_utileName  
             */
            intent.PutExtra("LoginSystemType", LoginSystemType);
            intent.PutExtra("username", username);
            intent.PutExtra("samp_adressId", adressID);
            intent.PutExtra("samp_adressName", samp_adress.Text);
            intent.PutExtra("samp_goodsName", samp_goodsname.Text);
            intent.PutExtra("samp_goodsCode", cNCGoodsCode);//nc存货编码
            intent.PutExtra("samp_modeName", sp_mode.SelectedItem.ToString());
            intent.PutExtra("samp_modeId", bsif.sampModeId);
            intent.PutExtra("samp_weight", 0);
            intent.PutExtra("samp_utileName", "");
            StartActivity(intent);
        }
        #endregion

        #region 生过烧标记按钮
        private void extraCar(object sender,EventArgs e)
        {
            Intent intent = new Intent(this, typeof(BaihuiListInfo));
            intent.SetFlags(ActivityFlags.ClearTop);
            StartActivity(intent);
        }
        #endregion
    }
}