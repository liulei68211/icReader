using System;
using System.IO;
using System.Data;
using System.Data.SqlClient;

using Android.App;
using Android.Content;
using Android.OS;

using Android.Widget;

using Android.Nfc;
using Android.Nfc.Tech;

using CN.Pda.Rfid.HF;
using CN.Pda.Serialport;

namespace FTJL
{
    [Activity(Label = "YugaSamp")]
    public class YugaSamp : Activity
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
        ToggleButton btSamp;

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
        /// 流水号
        /// </summary>
        private string serialNumber = "0001";

        /// <summary>
        /// 计量通行证号
        /// </summary>
        private string iMeasureId;

        /// <summary>
        /// 计量通行证号
        /// </summary>
        private string iMeasureId2;

        /// <summary>
        /// 客商名称
        /// </summary>
        private string cSupplierName;
        /// <summary>
        /// 货物地点编号及地点
        /// </summary>
        public string adressCode,adressName;
        public int adressID;
        /// <summary>
        /// 货物名称和编号
        /// </summary>
        private string cNCGoodsName,cNCGoodsCode;
        /// <summary>
        /// 取样时间
        /// </summary>
        public DateTime  sampTime;

        /// <summary>
        /// 操作员名字
        /// </summary>
        private string username;

        /// <summary>
        /// 是否成功写入数据库
        /// </summary>
        private string isWriteDb ;
        public string strError;//判断事务是否为空

        EditText samp_factory;
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

        #region  创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.YugaSamp);

            username = Intent.GetStringExtra("username");

            //厂家
            samp_factory = FindViewById<EditText>(Resource.Id.sampFactory);
            samp_factory.Text = "豫港";

            //数据库查询地点
            //bsif.selectAdress( out bsif.sampAdress, out bsif.adressCode);
            samp_adress = FindViewById<EditText>(Resource.Id.sampAdress);
            samp_adress.Text = "豫港焦炭皮带称";
            
            adressID =11;
            adressCode = "11";

            //货物名称 
            samp_goodsname = FindViewById<EditText>(Resource.Id.sampGoodsname);
            cNCGoodsCode = "0700000602";
            samp_goodsname.Text = "焦炭";

            //业务类型
            //业务类型下拉菜单
            sp_mode = FindViewById<Spinner>(Resource.Id.sampMode);
            sp_mode.ItemSelected += selectMode_ItemSelected;

            //数据库查询 暂不用
            //bsif.sampModeNameList = bsif.selectSampMode();

            //填充xml数据
            ArrayAdapter adapter_mode = ArrayAdapter.CreateFromResource(this, Resource.Array.SampMode, Android.Resource.Layout.SimpleSpinnerItem);
            adapter_mode.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            sp_mode.Adapter = adapter_mode;
            sp_mode.Prompt = "请选择";

            ////填充数据 数据库数据
            //ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, bsif.sampModeNameList);
            //adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            //sp_mode.Adapter = adapter;

            //日期
            string tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:ss:mm");
            samp_time = FindViewById<EditText>(Resource.Id.sampTime);
            samp_time.Text = tmnw;

            //取样人
            samp_people = FindViewById<EditText>(Resource.Id.sampPeople);
            samp_people.Text = username;

            btSamp = FindViewById<ToggleButton>(Resource.Id.btSamp);
            btSamp.Enabled = false;
            btSamp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#e6e6e6"));
            btSamp.Click += OperateDialogBox;

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

                    mPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(YugaSamp)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.UpdateCurrent);  //intent过滤器，过滤类型为NDEF_DISCOVERED    


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
                if (!btSamp.Checked)
                {
                    ReadIcData(intent);
                    m_intent = intent;
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
                if (!btSamp.Checked)
                {
                    m_intent = intent;
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
                if (!btSamp.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    m_intent = intent;
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
            tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;
            m_tag = tag;

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
                    ToggleButton samp = FindViewById<ToggleButton>(Resource.Id.btSamp);
                    //Button samp = FindViewById<Button>(Resource.Id.btSamp);
                    samp.Enabled = true;
                    btSamp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#DB3E3E"));
                }
                else
                {
                    #region 读卡异常，按钮不可用
                    Button samp = FindViewById<Button>(Resource.Id.btSamp);
                    samp.Enabled = false;
                    CommonFunction.ShowMessage("卡内信息错误！", this, true);
                    #endregion
                }
                #endregion
            }
            else
            {
                #region 判断卡类型
                btSamp.Enabled = false;

                btSamp.Checked = false;
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
            //var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
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
                                if (btSamp.Checked) //取样
                                {
                                    #region  写入取样编号 IcDatas[0,0] - 1扇区 4块
                                    try
                                    {
                                        //当前时间
                                        string xhdt = string.Format("{0:yyyyMMddhhmmss}", DateTime.Now);

                                        //写入取样编号  YYYYMMDD+0001 
                                        //sampCode = xhdt + serialNumber;
                                       // string writedata = xhdt + serialNumber + ic.IcDatas[0, 2].Substring(18);

                                        //写入取样卡标识
                                        string writedata = "12018041100100000000000000000000";
                                        //serialNumber = Convert.ToString( Convert.ToInt32(serialNumber)+1);

                                        bool auth = mfc.AuthenticateSectorWithKeyB(1, ic.ToDigitsBytes(ic.mb));
                                        if (auth)
                                        {
                                            mfc.WriteBlock(4, ic.ToDigitsBytes(writedata));
                                        }
                                    }
                                    catch (Java.IO.IOException ex)
                                    {
                                        result = false;
                                        CommonFunction.ShowMessage(ex.Message, this, true);
                                    }
                                    #endregion
                                    
                                }
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

        #region 写入数据库
        private void writeDb()
        {
            sql.getcon2();
            //查询地点id
            string sql_placeId = "select pk from QSB_Place where cPlaceName = '" + samp_adress.Text + "'";
            //根据 数据表QSS_SeqID 中的地点主键 iPlace_pk = adressID 查询取样编号cSeqID
            string sql_pkplace = "select dbo.Q_GetSeqID('" + adressID + "') as seqId";
            //获取seqId 并判断是否为空
            dt = sql.GetTable(sql_pkplace);

            seqId = dt.Rows[0]["seqId"].ToString();
            //取样时间
            sampTime = Convert.ToDateTime(samp_time.Text);
            //客商名称
            cSupplierName = samp_factory.Text;
            //货物名称
            cNCGoodsName = samp_goodsname.Text;

            if (seqId == "")
            {
                //取样编号为空的时候 提示联系生产部人员添加地点
                Toast.MakeText(this,"暂无该地点信息,请联系生产部相关人员进行设置",ToastLength.Long).Show();
                #region 暂不操作
                ////当前时间
                //xhdt = string.Format("{0:yyyyMMdd}", DateTime.Now);
                //seqId = adressCode + xhdt + "001";

                //string insert_sqlId = "insert into QSS_SeqID(cBusiEquiName,iPlace_pk,cSeqID)values('手持终端','" + adressID + "','" + seqId + "')SELECT @@cSeqID AS seqId";

                //dt = sql.GetTable(insert_sqlId);

                //seqId = dt.Rows[0]["seqId"].ToString();
                //sql.getsqlcom(insert_sqlId);

                //isWriteDb = true;
                #endregion
            }
            else
             {
                // 不为空时 调用事务     更新seqId(+1) 插入主表信息 插入子表信息                                                                
                /*
                 * 插入主表信息
                 * cSampleCode          取样号                       ----  sampleCode
                 * nSampleQuantity      取样数量(可为空)             ----  sampCount
                 * cMeasUnit            计量单位(可为空)             ----  sampUnit
                 * iPlace_pk            取样地点主键                 ----  adressCode
                 * iBusiType_pk         业务类型主键                 ----  sampModeId 
                 * cInVCode             Nc存货编码                   ----  cNCGoodsCode
                 * iTheorySampleNum     理论取样车（来样数量）       ----  theoryNum
                 * iTheoryJoinSampleNum 理论参与车辆数据             ----  joinSampleNum
                 * dStart               取样开始时间(可为空)         ----  sampTime
                 * cNewUser             取样人(可为空)               ----  username
                 * bFinish              是否取样完成(可为空)
                 */
                 
                 /*
                  * 插入子表信息
                  * pk_h                 主表主键                 ----  pk
                  * cMeasure_ID          计量通行证号             ----  measureId
                  * cCarCode             车号                     ----  sampCarNum
                  * cOrderID             采购订单号               ----  orderId
                  * CSupplierCode        发货单位编号             ----  supplieCode
                  * cSuplierName         供货商名称               ----  supplieName
                  * cInvCode             存货编号                 ----  goodsCode
                  * cInvName             货物名称                 ----  goodsName
                  * dReadIC              取样开始时间             ----  sampTime
                  * iCarBagNum           参与摇号的包数           ----  carBags
                  * iSampleBagNum        实际取袋数               ----  sampCarBags
                 */
                bsif.InsertSampInfo2(adressID,seqId,0.00,"",adressID,bsif.sampModeId, cNCGoodsCode, bsif.iTheoryNum,bsif.iJoinSampleNum,sampTime,username, 1,
                    "", "","","",cSupplierName, "",cNCGoodsName, sampTime,0,"",1,out strError);

                isWriteDb = strError;
            }
        }
        #endregion

        #region 操作提示对话框
        /// <summary>
        /// 操作提示对话框
        /// </summary>
        private void OperateDialogBox(object sender, EventArgs e)
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
                if (button.Text == "取样")
                {
                    if (btSamp.Checked)
                    {
                        writeDb();

                        if (isWriteDb == "")
                        {
                            UnloadFunction(m_intent);
                        }
                        else
                        {
                            Toast.MakeText(this,"写入数据库失败，不能写入ic卡",ToastLength.Short).Show();
                        }
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

        #region 操作成功
        /// <summary>
        /// 操作成功
        /// </summary>
        private void WriteDataOK(bool ok)
        {
            if (ok)
            {
                //取样按钮
                btSamp.Checked = false;

                btSamp.Enabled = false;
                btSamp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));

                CommonFunction.ShowMessage("操作成功！", this, true);
            }
            else CommonFunction.ShowMessage("写卡失败！", this, true);
        }
        #endregion
    }
}