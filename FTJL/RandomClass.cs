using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Nfc;
using Android.Nfc.Tech;
using CN.Pda.Rfid.HF;
using CN.Pda.Serialport;


using Mono.Data.Sqlite;

namespace FTJL
{
    [Activity(Label = "一车一抽")]
    public class RandomClass : Activity
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
        /// 确认取样
        /// </summary>
        ToggleButton btExtra,btNoExtra;

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

        /// <summary>
        /// 取样编号
        /// </summary>
        private string sampCode;

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
        /// 返回的自增值
        /// </summary>
        private int pk;

        private EditText samp_factory;
        private EditText samp_goodsname;
        private Spinner sp_mode;
        private EditText samp_adress;
        private EditText samp_carNumber;
        private EditText cmeasure_Id;
        private EditText extra_bags;//抽取包数
        private EditText samp_bags;//包数

        Sql sql = new Sql();
        SqlDataReader sqlrd;
        BasicInfo bsif = new BasicInfo();

        private string sampbags;
        private string extrabags;

        private string car_goodsCode;
        private string car_goodsName;
        private string car_factoryName;
        private string car_factoryCode;
        private string purchaseOrderID;//采购订单号
        private string cMeasureID;
        private string car_adress;
        #endregion

        #region Activity 启动
        protected override void OnStart()
        {
            base.OnStart();
        }
        #endregion

        #region Activity 唤醒
        protected override void OnResume()
        {
            base.OnResume();
            if (m_nfcAdapter == null)
            {
                m_nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
            }
            m_nfcAdapter.EnableForegroundDispatch(this, mPendingIntent, null, null);// mFilters, mTechLists);
        }
        #endregion

        #region Activity 暂停
        protected override void OnPause()
        {
            base.OnPause();
            if (m_nfcAdapter != null)
            {
                m_nfcAdapter.DisableForegroundDispatch(this);
            }
        }
        #endregion

        #region Activity 退出
        protected override void OnStop()
        {
            //m_nfcAdapter = null;
            base.OnStop();
        }
        #endregion

        #region 获取新的intent
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            ProcessAdapterAction(intent);
        }
        #endregion

        #region 获取Intent后的处理事件
        public void ProcessAdapterAction(Intent intent)
        {
            //当系统检测到tag中含有NDEF格式的数据时，且系统中有activity声明可以接受包含NDEF数据的Intent的时候，系统会优先发出这个action的intent。
            //得到是否检测到ACTION_NDEF_DISCOVERED触发                           序号1
            if (NfcAdapter.ActionNdefDiscovered.Equals(intent.Action))
            {
                if (!btExtra.Checked)
                {
                    ReadIcData(intent);
                    m_intent = intent;
                }
                else
                {
                    
                }
            }
            //当没有任何一个activity声明自己可以响应ACTION_NDEF_DISCOVERED时，系统会尝试发出TECH的intent.即便你的tag中所包含的数据是NDEF的，但是如果这个数据的MIME type或URI不能和任何一个activity所声明的想吻合，系统也一样会尝试发出tech格式的intent，而不是NDEF.
            //得到是否检测到ACTION_TECH_DISCOVERED触发                           序号2
            if (NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {
                //System.out.println("ACTION_TECH_DISCOVERED");
                //处理该intent   
                if (!btExtra.Checked)
                {
                    m_intent = intent;
                    ReadIcData(intent);
                }
                else
                {
                  
                }
            }
            //当系统发现前两个intent在系统中无人会接受的时候，就只好发这个默认的TAG类型的
            //得到是否检测到ACTION_TAG_DISCOVERED触发                           序号3
            if (NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            {
                if (!btExtra.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                   // m_intent = intent;
                
                }
            }
        }
        #endregion

        #region 读卡事件
        private void ReadIcData(Intent intent)
        {
            m_intent = intent;
            ic.icserial = ic.FindIC(m_intent);

            Car = new CarInfo();    //初始化一个CarInfo类

            //取出封装在intent中的TAG 
            tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;
            m_tag = tag;

            //清空文本
            extra_bags.Text = "";
            samp_bags.Text = "";
            samp_factory.Text="";
             samp_goodsname.Text="";
             samp_carNumber.Text="";
            cmeasure_Id.Text="";
        MifareClassic mfc = MifareClassic.Get(m_tag);
            if (mfc != null)
            {
                #region 获取到tag
                try
                {
                    mfc.Connect();
                    var type = mfc.GetType();
                    if (type.Equals(typeof(Android.Nfc.Tech.MifareClassic)))
                    {
                        #region 循环读取5个扇区数据
                        for (int i = 0; i < ic.sectors.Length; i++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                int sec = mfc.BlockToSector(ic.sectors[i] * 4 + k);
                                if (sec < mfc.SectorCount)
                                {
                                    if (mfc.AuthenticateSectorWithKeyB(sec, ic.ToDigitsBytes(ic.mb)))
                                    {
                                        int c = mfc.SectorCount;
                                        c = mfc.BlockCount;

                                        byte[] data = new byte[16];
                                        data = mfc.ReadBlock(ic.sectors[i] * 4 + k);
                                        if (data != null)
                                        {
                                            ic.IcDatas[i, k] = ic.ToHexString(data);
                                            ic.isReadIcOK = true;
                                        }
                                        else
                                        {
                                            CommonFunction.ShowMessage("读取" + ic.sectors[i].ToString() + "扇区" + Convert.ToString(ic.sectors[i] * 4 + k) + "块失败！", this, true);
                                            ic.isReadIcOK = false;
                                            return;
                                        }
                                    }
                                    else
                                    {
                                        CommonFunction.ShowMessage("密码验证失败！", this, true);
                                        ic.isReadIcOK = false;
                                        return;
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage(ex.Message, this, true);
                }
                finally
                {
                    mfc.Close();
                }

                if (ic.isReadIcOK)
                {
                    try
                    {
                        //解析扇区数据
                        Car = ic.GetCarInfo(ic.IcDatas);

                        if (Car != null)
                        {
                            // CommonFunction.ShowMessage("", this, false);

                            //将摇号信息写入本地数据表
                            // isEqualFactory();

                            #region 读卡正确，显示在窗体文本内,按钮复活 
                            //是否为同一张卡 重复卡不得重复刷
                            if (!isEqualFactory())
                            {
                                samp_carNumber.Text = Car.CarPlate;

                                // bussnise.Text = Car.BusinessType;

                                // measuremode.Text = Car.MeasureMode;

                                cmeasure_Id.Text = Car.MeasureID;

                                //根据车号和计量单号 从计量系统中查询 采购订单号 供应商编号 供应商名称 货物编号 货物名称等信息
                                bsif.selectCmInfo(Car.CarPlate, Car.MeasureID, out cMeasureID,out purchaseOrderID, out car_factoryCode, out car_factoryName, out car_goodsCode, out car_goodsName);
                                samp_factory.Text = car_factoryName;
                                samp_goodsname.Text = car_goodsName;
                            }
                            #endregion
                        }
                        else
                        {
                            #region 读卡异常，按钮不可用
                            ToggleButton samp = FindViewById<ToggleButton>(Resource.Id.btExtract);
                            samp.Enabled = false;
                            CommonFunction.ShowMessage("卡内信息错误！", this, true);
                            #endregion
                        }
                    }
                    catch (System.ArgumentOutOfRangeException ax)
                    {
                        CommonFunction.ShowMessage("读卡信息错误！", this, true);
                    }
                    catch (Exception ex)
                    {
                        CommonFunction.ShowMessage(ex.Message, this, true);
                    }
                }

                #endregion
            }
            else
            {
                #region 判断卡类型
                btExtra.Enabled = false;

                btExtra.Checked = false;
                CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                ic.isReadIcOK = false;
                return;
                #endregion

            }
        }
        #endregion    

        #region 创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.RandomSamp);

            LoginSystemType = Intent.GetStringExtra("LoginSystemType");
            username = Intent.GetStringExtra("username");

            //摇号抽样按钮
            btExtra = FindViewById<ToggleButton>(Resource.Id.btExtract);
            btExtra.Click += extractSamp;
            btExtra.Enabled = false;
            btExtra.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));

            //不摇号取样按钮
            btNoExtra = FindViewById<ToggleButton>(Resource.Id.btNoExtra);
            btNoExtra.Click += noExtraSamp;
            btNoExtra.Enabled = true;
            btNoExtra.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));

            //计量通行证号
            cmeasure_Id = FindViewById<EditText>(Resource.Id.cMeasureId);
            //地点
            samp_adress = FindViewById<EditText>(Resource.Id.sampAdress);
            samp_adress.Text = "合金库";
           
            //供应商
            samp_factory = FindViewById<EditText>(Resource.Id.sampFactory);
            //存货名称
            samp_goodsname = FindViewById<EditText>(Resource.Id.sampGoodsname);
            //车号
            samp_carNumber = FindViewById<EditText>(Resource.Id.sampCarNum);
            //取样包数
            samp_bags = FindViewById<EditText>(Resource.Id.bagCount);
            //抽取包数
            extra_bags = FindViewById<EditText>(Resource.Id.extractBags);
            //业务类型下拉菜单
            sp_mode = FindViewById<Spinner>(Resource.Id.sampMode);

            //填充数据
            sp_mode.ItemSelected += selectMode_ItemSelected;
            //数据库查询 暂时不用
            //bsif.sampModeNameList = bsif.selectSampMode();
            //填充数据 数据库数据
            //ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, bsif.sampModeNameList);
            //adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            //sp_mode.Adapter = adapter;

            //填充xml文件数据
            ArrayAdapter adapter_mode = ArrayAdapter.CreateFromResource(this, Resource.Array.SampMode, Android.Resource.Layout.SimpleSpinnerItem);
            adapter_mode.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sp_mode.Adapter = adapter_mode;
            sp_mode.Prompt = "请选择";

            //取样包数文本框改变事件
            //editText没有焦点
            samp_bags.TextChanged += TextChange;

            if (CommonFunction.mode == "NFC")
            {
                #region NFC 模式
                try
                {
                    m_nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
                    if (m_nfcAdapter == null)
                    {
                        CommonFunction.ShowMessage("设备不支持NFC！", this, true);
                        return;
                    }
                    if (!m_nfcAdapter.IsEnabled)
                    {
                        CommonFunction.ShowMessage("请在系统设置中先启用NFC功能！", this, true);
                        return;
                    }

                    //m_nfcAdapter.SetNdefPushMessage(CreateNdefMessageCallback(), this, this);

                    mTechLists = new string[][] { new string[] { "Android.Nfc.Tech.MifareClassic" }, new string[] { "Android.Nfc.Tech.NfcA" } };
                    IntentFilter tech = new IntentFilter(NfcAdapter.ActionTechDiscovered);
                    mFilters = new IntentFilter[] { tech, };     //存放支持technologies的数组      

                    mPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(RandomClass)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.UpdateCurrent);  //intent过滤器，过滤类型为NDEF_DISCOVERED    


                    //Mifare卡和Desfare卡都是ISO - 14443 - A卡
                    ProcessAdapterAction(this.Intent);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage(ex.Message, this, true);
                }
                #endregion
            }
            else if (CommonFunction.mode == "SerialPort")
            {
                #region SerialPort模式
                try
                {
                    Stream im;
                    Stream om;
                    serial = new SerialPort(13, 115200, 0);
                    serial.Power_5Von();

                    im = serial.MFileInputStream;
                    om = serial.MFileOutputStream;

                    hf = new HfReader(im, om);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage(ex.Message, this, true);
                }
                #endregion
            }
        }
        #endregion

        #region 取样数 文本框改变事件
        private void TextChange(object sender, Android.Text.TextChangedEventArgs e)
        {
            double nn;

            if (samp_bags.Text != "")
            {
                nn = Convert.ToDouble(samp_bags.Text);

                if (nn % 4 == 0)
                {
                    extra_bags.Text = Convert.ToString(nn / 4);
                }
                else
                {
                    extra_bags.Text = Convert.ToString(Math.Floor(nn / 4)+1);
                }
                btExtra.Enabled = true;
                btExtra.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));
            }
        }
        #endregion

        #region 摇号抽样按钮
        private void extractSamp(object sender, EventArgs e)
        {
            extrabags = extra_bags.Text;
            sampbags = samp_bags.Text;

            Bundle bundle = new Bundle();
            Intent intent = new Intent(this, typeof(ExtraBags));
            intent.SetFlags(ActivityFlags.ClearTop);
            //传递int型数据
            //bundle.PutInt("extrabags", extrabags);
            //intent.PutExtras(bundle);
            //bundle.PutInt("sampbags",sampbags);
            //intent.PutExtras(bundle);

            intent.PutExtra("extrabags",extrabags);//抽取包数
            intent.PutExtra("sampbags", sampbags);//取样包数
            intent.PutExtra("sampMode", sp_mode.SelectedItem.ToString());//取样方式

            intent.PutExtra("cmeasure_Id", Car.MeasureID);//计量通行证号
            intent.PutExtra("purchaseOrderID", purchaseOrderID);//采购编号
            intent.PutExtra("sampFactory", car_factoryName);//供应商 名称
            intent.PutExtra("sampFactoryCode", car_factoryCode);//供应商编号

            intent.PutExtra("sampGoodsName", car_goodsName);//存货名称
            intent.PutExtra("sampGoodsCode", car_goodsCode);//存货编号
            intent.PutExtra("sampCarNum", Car.CarPlate);//车号
            intent.PutExtra("sampAdress", samp_adress.Text);//取样地点
            intent.PutExtra("LoginSystemType", LoginSystemType);

            intent.PutExtra("username", username);
            StartActivity(intent);

            btExtra.Enabled = false;
            btExtra.Checked = false;
            btExtra.SetBackgroundColor(Android.Graphics.Color.ParseColor("#e6e6e6"));

        }
        #endregion

        #region 不摇号取样按钮
        public void noExtraSamp(object sender,EventArgs e)
        {
            Intent intent = new Intent(this, typeof(CommonSampClass));
            intent.SetFlags(ActivityFlags.ClearTop);
            string LoginSystemType = "汽车取样";
            intent.PutExtra("username", username);
            intent.PutExtra("LoginSystemType", LoginSystemType);
            StartActivity(intent);
        }
        #endregion

        #region 写入数据库
        
        #endregion

        #region 根据计量通行证号判断是否是同一张卡 并绑定数据 将数据写入本地取样信息数据库
        private bool isEqualFactory()
        {
            bool  resultt = false;
            //string tmnw = System.DateTime.Now.ToString("yyyy-MM-dd");

            ////找到数据库
            //string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            //string db = Path.Combine(documents, MainActivity.DataBaseName);

            //bool isExit = File.Exists(db);
            //if (isExit)
            //{
            //    var conn = new SqliteConnection("Data Source=" + db);
            //    try
            //    {
            //        //查询当天所有的厂家信息
            //        string select_sql = "select cmeasureId,factoryName,carNumber,goodsName,isSamp from ExtraInfo where  addTime = '" + tmnw + "'";
            //        if (Bool_QueryInfo(conn, select_sql))
            //        {
            //            //数据库不为空 判断是否为同一张卡
            //            select_sql = "select factoryName,carNumber,goodsName,isSamp from ExtraInfo where cmeasureId = '" + Car.MeasureID + "' and  addTime = '" + tmnw + "'";
            //            if (Bool_QueryInfo(conn, select_sql))
            //            {
            //                //为同一张卡
            //                CommonFunction.ShowMessage("该卡信息已经存入数据表中", this, true);
            //                resultt = true;
                            
            //            }
            //            else
            //            {
            //                resultt =  false;
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        CommonFunction.ShowMessage("获取货物信息失败！", this, true);
            //    }
            //    finally
            //    {
            //        if (conn.State != System.Data.ConnectionState.Closed)
            //        {
            //            conn.Clone();
            //        }
            //        conn.Dispose();
            //    }

            //}

            return resultt;
        }
        #endregion

        #region 查询数据库返回真假值
        private bool Bool_QueryInfo(SqliteConnection SqlConn, string SqlStr)
        {
            bool result = false;
            try
            {
                if (SqlConn.State != System.Data.ConnectionState.Open)
                {
                    SqlConn.Open();
                }
                if (SqlConn.State == System.Data.ConnectionState.Open)
                {
                    SqliteCommand cmd = new SqliteCommand(SqlStr, SqlConn);
                    SqliteDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        reader.Close();
                        result = true;
                    }
                    else
                    {
                        reader.Close();
                        result = false;
                    }
                }
                else result = false;
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }
        #endregion

        #region 取样方式下拉菜单选择事件 获取对应得id
        private void selectMode_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            string select_modeId = "select pk from QQB_BusiType where cBusiName = '" + CurSpinner.SelectedItem.ToString() + "'";
            sql.getcon2();
           // sql.getsqlcom(select_modeId);

            sqlrd = sql.getrd(select_modeId);
            if (sqlrd.Read())
            {
                bsif.sampModeId = Convert.ToInt32(sqlrd["pk"].ToString());
            }
            sqlrd.Close();
            sql.closecon();
        }
        #endregion
    }
}