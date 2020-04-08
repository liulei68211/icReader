using System;
using System.IO;
using System.Data;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Nfc;
using Android.Nfc.Tech;
using CN.Pda.Rfid.HF;
using CN.Pda.Serialport;
using Android.Widget;
using Mono.Data.Sqlite;
using FTJL.Adapter;
namespace FTJL
{
    [Activity(Label = "InsideDish")]
    public class InsideDish : Activity
    {
        #region 变量
        //IcDatas[0,0] - 1扇区 4块  从第一位开始：8位卡号 + 24位车号（解密后为正常车牌照）
        //IcDatas[0,1] - 1扇区 5块  从第一位开始：5个标志位（计量类型/业务类型/计重方式/退货标识/理重实重标识 + 1个待用标志 + 24位计量单号（不读取）
        //IcDatas[0,2] - 1扇区 6块  从第一位开始：12位卸货时间 + 6位扣吨数 + 12位装货时间（卸货时不操作改数据，取样时写入）

        //IcDatas[1,0] - 2扇区 8块  从第一位开始：26位采购订单号（解密后为13位）
        //IcDatas[1,1] - 2扇区 9块  从第一位开始：6位毛重 + 10位毛重时间 / 6位皮重 + 10位皮重时间 + 16位计量员（卸货时写入操作员名字）
        //IcDatas[1,2] - 2扇区 10块  第一位 写入收发货标识(0:未收发货,1:确认收发货)，2-5位 写入 对应得内盘信息主键

        //IcDatas[2,0] - 3扇区 12块 从第一位开始：（卸货：客商名称，占用3个数据块）/（取样：2位取样代码，12位取样时间，18位取样人员名字,占用1个数据块）
        //IcDatas[2,1] - 3扇区 13块 
        //IcDatas[2,2] - 3扇区 14块 

        //IcDatas[3,0] - 4扇区 16块 从第一位开始：存货名称，占用3个数据块
        //IcDatas[3,1] - 4扇区 17块 写入 
        //IcDatas[3,2] - 4扇区 18块

        //IcDatas[4,0] - 5扇区 20块 从第一位开始：规格、型号，占用3个数据块（格式：规格!型号）
        //IcDatas[4,1] - 5扇区 21块 
        //IcDatas[4,2] - 5扇区 22块

        //adapter数据
        private List<SendInfo> data;
        private Context context;
        private InsideAdapter adapter;
        private ListView list;
        private List<string> listData = new List<string>();//存放数据表CM_Other查出的字段

        EditText carNumber;//车牌号
        EditText username;
        ToggleButton btSender;//发货按钮
        ToggleButton btGet;//收货按钮
        ToggleButton btSave;//同步远程服务器数据

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
        /// <summary>
        /// 操作员名字
        /// </summary>
        string UserName;

        Sql sql = new Sql();

        //接收内盘同步数据
        private List<int> pkId_list = new List<int>();//编号
        private List<string> carNo_list = new List<string>();//车号
        private List<string> sendFacID_list = new List<string>();//发货公司编号
        private List<string> sendFacName_list = new List<string>();//发货公司名称
        private List<string> recFacID_list = new List<string>();//接收公司编号
        private List<string> recFacName_list = new List<string>();//接收公司名称
        private List<string> materielID_list = new List<string>();//存货编码
        private List<string> materielName_list = new List<string>();//存货名称
        private List<string> jzType_list = new List<string>();//定期皮 /普通

        private int pkid;//写入卡中的主键
        private int isOK = 0;//收发货标识
        #endregion

        #region 创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.InsideDish);

            //操作人
            UserName = Intent.GetStringExtra("username");
            username = FindViewById<EditText>(Resource.Id.username);
            username.Text = UserName;

            btGet = FindViewById<ToggleButton>(Resource.Id.btGet);
            btGet.Enabled = false;
            btGet.Click += getDb;

            btSender = FindViewById<ToggleButton>(Resource.Id.btSet);
            btSender.Enabled = false;
            btSender.Click += sendDb;

            btSave = FindViewById<ToggleButton>(Resource.Id.btSave);
            btSave.Enabled = true;
            btSave.Checked = true;
            btSave.Click += dbSave;


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

                    mPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(InsideDish)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.UpdateCurrent);  //intent过滤器，过滤类型为NDEF_DISCOVERED    


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
                if (!btGet.Checked && !btSender.Checked )
                {
                    ReadIcData(intent);
                }
                else
                {
                    UnloadFunction(intent);
                }
            }
            //当没有任何一个activity声明自己可以响应ACTION_NDEF_DISCOVERED时，系统会尝试发出TECH的intent.即便你的tag中所包含的数据是NDEF的，但是如果这个数据的MIME type或URI不能和任何一个activity所声明的想吻合，系统也一样会尝试发出tech格式的intent，而不是NDEF.
            //得到是否检测到ACTION_TECH_DISCOVERED触发                           序号2
            if (NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {
                //System.out.println("ACTION_TECH_DISCOVERED");
                //处理该intent   
                if (!btGet.Checked && !btSender.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    UnloadFunction(intent);
                }
            }
            //当系统发现前两个intent在系统中无人会接受的时候，就只好发这个默认的TAG类型的
            //得到是否检测到ACTION_TAG_DISCOVERED触发                           序号3
            if (NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            {
                if (!btGet.Checked && !btSender.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    UnloadFunction(intent);
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
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;
            m_tag = tag;

            #region 清空文本
            carNumber = FindViewById<EditText>(Resource.Id.carNumber);
            carNumber.Text = "";
            #endregion

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
                            CommonFunction.ShowMessage("", this, false);

                            #region 读卡正确，显示在窗体文本内,按钮复活

                             carNumber.Text = Car.CarPlate ;
                             // carNumber.Text = "GT030108";//暂用

                            //根据车号查询 相关收发货路线信息
                            goodsInfo();
                            #endregion
                        }
                        else
                        {
                            #region 读卡异常，按钮不可用
                            btSender.Enabled = false;

                            btGet.Enabled = false;
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
                btGet.Enabled = false;
                btSender.Enabled = false;

                btGet.Checked = false;
                btSender.Checked = false;
                CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                ic.isReadIcOK = false;
                return;
                #endregion

            }
        }
        #endregion

        #region 卸货写卡函数
        /// <summary>
        /// 卸货写卡函数
        /// </summary>
        private void UnloadFunction(Intent intent)
        {
            //业务类型置"1"
            //计量类型置"1"
            //计重方式置"1"
            //退货标识置"1"
            //写入卸货时间

            bool result = true;

            if (ic.icserial != ic.FindIC(intent))
            {
                result = false;
                CommonFunction.ShowMessage("要写入的卡和读取的卡不符，请检查是否被更换", this, true);
                return;
            }
            //取出封装在intent中的TAG 
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;
            m_tag = tag;

            if (CommonFunction.mode == "NFC")
            {
                MifareClassic mfc = MifareClassic.Get(m_tag);
                if (mfc != null)
                {
                    try
                    {
                        mfc.Connect();
                        var type = mfc.GetType();
                        if (type.Equals(typeof(Android.Nfc.Tech.MifareClassic)))
                        {
                            try
                            {
                                #region 不点击按钮的写卡方法
                                try
                                {
                                    string pkidd = Convert.ToString(pkid);
                                    isOK = 1;
                                    string writedata = "";
                                    if (pkidd.Length == 1)
                                    {
                                        writedata = Convert.ToString(isOK) + pkidd + "000000000000000000000000000000";
                                    }
                                    else if (pkidd.Length == 2)
                                    {
                                        writedata = Convert.ToString(isOK) + pkidd + "00000000000000000000000000000";
                                    }
                                    else if (pkidd.Length == 3)
                                    {
                                        writedata = Convert.ToString(isOK) + pkidd + "0000000000000000000000000000";
                                    }


                                    bool auth = mfc.AuthenticateSectorWithKeyB(2, ic.ToDigitsBytes(ic.mb));
                                    if (auth)
                                    {
                                        mfc.WriteBlock(10, ic.ToDigitsBytes(writedata));
                                    }
                                }
                                catch (Java.IO.IOException ex)
                                {
                                    result = false;
                                    CommonFunction.ShowMessage(ex.Message, this, true);
                                }
                                #endregion

                                #region 点击按钮的写卡方法
                                //0业务类型 1计量类型 2计重方式 3 退货标识
                            //    if (btGet.Checked) //收货
                            //    {
                            //    #region 1-写入收发货标识 位于2区10块 [1,2] 第一位 
                            //    try
                            //    {
                            //        //string tmp = ic.IcDatas[0, 1].Remove(0, 1);
                            //        //string writedata = tmp.Insert(0, "1");

                            //        // string writedata = ic.IcDatas[1, 2].Substring(0, 1) ;
                            //        string pkidd = Convert.ToString(pkid);
                            //        string writedata = "";
                            //        if (pkidd.Length == 1)
                            //        {
                            //            writedata = Convert.ToString(isOK) + pkidd + "000000000000000000000000000000";
                            //        }
                            //        else if (pkidd.Length == 2)
                            //        {
                            //            writedata = Convert.ToString(isOK) + pkidd + "00000000000000000000000000000";
                            //        }
                            //        else if (pkidd.Length == 3)
                            //        {
                            //            writedata = Convert.ToString(isOK) + pkidd + "0000000000000000000000000000";
                            //        }


                            //        bool auth = mfc.AuthenticateSectorWithKeyB(2, ic.ToDigitsBytes(ic.mb));
                            //        if (auth)
                            //        {
                            //            mfc.WriteBlock(10, ic.ToDigitsBytes(writedata));
                            //        }
                            //    }
                            //    catch (Java.IO.IOException ex)
                            //    {
                            //        result = false;
                            //        CommonFunction.ShowMessage(ex.Message, this, true);
                            //    }
                            //    #endregion
                            //}
                                #endregion
                        }
                            finally
                            {
                                mfc.Close();
                            }
                        }
                    }
                    catch (Java.IO.IOException ex)
                    {
                        CommonFunction.ShowMessage("请放卡！", this, true);
                        return;
                    }
                }
                else
                {
                    #region 判断卡类型

                    CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                    return;
                    #endregion
                }
            }
            else if (CommonFunction.mode == "SerialPort")
            {
                //
            }

            WriteDataOK(result);

        }
        #endregion

        #region 操作成功
        /// <summary>
        /// 操作成功
        /// </summary>
        private void WriteDataOK(bool ok)
        {
            if (ok)
            {
                btSender.Enabled = false;
                btGet.Enabled = false;
                CommonFunction.ShowMessage("操作成功！", this, true);
            }
            else CommonFunction.ShowMessage("写卡失败！", this, true);
        }
        #endregion

        #region 发货按钮
        private void sendDb(object sender,EventArgs e)
        {
            var button = (ToggleButton)sender;
            if (!button.Checked)
            {
                button.Checked = false;
                return;
            }

            //对话框
            var callDialog = new AlertDialog.Builder(this);

            //对话框内容
            callDialog.SetMessage("确定要进行" + button.Text + "吗?");

            //确定按钮
            callDialog.SetNeutralButton("确定", delegate {
                if (button.Text == "发货")
                {
                    if (btSender.Checked)
                    {
                        btGet.Checked = false;
                    }
                } 
            });

            //取消按钮
            callDialog.SetNegativeButton("取消", delegate {
                button.Checked = false;
            });

            //显示对话框
            callDialog.Show();
        }
        #endregion

        #region 收货按钮
        private void getDb(object sender, EventArgs e)
        {
            var button = (ToggleButton)sender;
            if (!button.Checked)
            {
                button.Checked = false;
                return;
            }

            //对话框
            var callDialog = new AlertDialog.Builder(this);

            //对话框内容
            callDialog.SetMessage("确定要进行" + button.Text + "吗?");

            //确定按钮
            callDialog.SetNeutralButton("确定", delegate {
                if (button.Text == "收货")
                {
                    if (btGet.Checked)
                    {
                        btSender.Checked = false;
                        UnloadFunction(m_intent);//写卡
                    }
                }
            });

            //取消按钮
            callDialog.SetNegativeButton("取消", delegate {
                button.Checked = false;
            });

            //显示对话框
            callDialog.Show();
        }
        #endregion

        #region 根据读到的车牌号查询货物名称id 货物名称 发货方id 发货方名 收货方id 收货方名称 计重方式
        private void goodsInfo()
        {
            //找到数据库
            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);

            bool isExit = File.Exists(db);
            if (isExit)
            {
                var conn = new SqliteConnection("Data Source=" + db);
                try
                {
                    conn.Open();
                    //查询读到的卡中 的厂家信息 的所有记录   
                    string str_sql = "select I_ClassID ||'，'|| C_SedFactoryDes ||'，'|| C_MaterielDes ||'，'||C_RecFactoryDes  as '发/收货路线' FROM CMOtherInfo where C_CarryNo='" + carNumber.Text + "'";
                    var cmd = new SqliteCommand(str_sql, conn);
                    SqliteDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        listData.Add(sdr["发/收货路线"].ToString());
                    }
                    data = new List<SendInfo>();
                    for (int i=0;i<listData.Count;i++)
                    {
                        data.Add(new SendInfo(listData[i]));
                    }
                    adapter = new InsideAdapter(data,this);
                    list = FindViewById<ListView>(Resource.Id.listView1);
                    list.Adapter = adapter;

                    list.ItemClick += (s, e) =>
                    {
                        OnClick(e.Position);
                    };
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取货物信息失败！", this, true);
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                    {
                        conn.Clone();
                    }
                    conn.Dispose();
                }
            }
        }
        #endregion

        #region listview 选择item事件
        private void OnClick(int position)
        {
           // position--;
            Toast.MakeText(this, $"这条新闻有" + data[position].Info + "次浏览量", ToastLength.Short).Show();
            string getData = data[position].Info;
            //获取第一个逗号前的主键id
            pkid = Convert.ToInt32(getData.Split('，')[0].ToString());
            //提示确定要发货吗
            //对话框
            var callDialog = new AlertDialog.Builder(this);

            //对话框内容
            callDialog.SetMessage("确定要进行 收货吗?");
            //确定按钮
            callDialog.SetNeutralButton("确定", delegate {
                UnloadFunction(m_intent); 
            });

            //取消按钮
            callDialog.SetNegativeButton("取消", delegate {
            });

            //显示对话框
            callDialog.Show();

        }
        #endregion

        #region 同步远程数据
        private void saveDb()
        {
            #region 同步数据库数据
            sql.getcon();
            //接收远程数据
            string select_sql = "select * from [CM_OtherInfo] order by I_ClassID";
            DataTable dt = sql.GetTable(select_sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                pkId_list.Add(Convert.ToInt32(dt.Rows[i]["I_ClassID"].ToString().Trim()));
                carNo_list.Add(dt.Rows[i]["C_CarryNo"].ToString().Trim());
                sendFacID_list.Add(dt.Rows[i]["C_SedFactoryID"].ToString().Trim());
                sendFacName_list.Add(dt.Rows[i]["C_SedFactoryDes"].ToString().Trim());
                materielID_list.Add(dt.Rows[i]["C_MaterielID"].ToString().Trim());
                materielName_list.Add(dt.Rows[i]["C_MaterielDes"].ToString().Trim());
                recFacID_list.Add(dt.Rows[i]["C_RecFactoryID"].ToString().Trim());
                recFacName_list.Add(dt.Rows[i]["C_RecFactoryDes"].ToString().Trim());
                jzType_list.Add(dt.Rows[i]["C_JZType"].ToString().Trim());
            }
            sql.closecon();
            //存入本地数据库
            //找到数据库
            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);

            bool isExit = File.Exists(db);
            if (isExit)
            {
                var insert_sql = "";
                var conn = new SqliteConnection("Data Source=" + db);
              
                try
                {
                    conn.Open();
                    bool result = false;
                    //string delete_sql = "delete  from CMOtherInfo";
                    //var cmdd = new SqliteCommand(delete_sql, conn);
                    //cmdd.CommandType = System.Data.CommandType.Text;
                    //cmdd.ExecuteNonQuery();

                    //查询读到的卡中 的厂家信息 的所有记录
                    for (int i = 0; i < jzType_list.Count; i++)
                    {
                        insert_sql = "insert into CMOtherInfo(I_ClassID,C_CarryNo,C_SedFactoryID,C_SedFactoryDes,C_MaterielID,C_MaterielDes,C_RecFactoryID,C_RecFactoryDes,C_JZType)values('"+ pkId_list[i] +"','" + carNo_list[i] + "','" + sendFacID_list[i] + "','" + sendFacName_list[i] + "','" + materielID_list[i] + "','" + materielName_list[i] + "','" + recFacID_list[i] + "','" + recFacName_list[i] + "','" + jzType_list[i] + "')";
                        var cmd = new SqliteCommand(insert_sql, conn);
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                    result = true;
                    if (result)
                    {
                        CommonFunction.ShowMessage("数据更新成功", this, true);
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取货物信息失败！", this, true);
                }
                finally
                {
                    if (conn.State != System.Data.ConnectionState.Closed)
                    {
                        conn.Clone();
                    }
                    conn.Dispose();
                }
            }
            #endregion
        }
        #endregion

        #region 同步远程数据按钮
        private void dbSave(object sender,EventArgs e)
        {
            saveDb();
        }
        #endregion
    }
}