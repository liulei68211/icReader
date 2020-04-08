using System;
using System.IO;

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

namespace FTJL
{
    [Activity(Label = "SampCm")]
    public class SampCm : Activity
    {
        #region 变量
        /// <summary>
        /// IC操作类
        /// </summary>
        IC ic = new IC();
        /// <summary>
        /// 车辆信息类
        /// </summary>
        CarInfo Car;

        //IcDatas[0,0] - 1扇区 4块  从第一位开始：8位卡号 + 24位车号（解密后为正常车牌照）
        //IcDatas[0,1] - 1扇区 5块  从第一位开始：5个标志位（计量类型/业务类型/计重方式/退货标识/理重实重标识 + 1个待用标志 + 24位计量单号（不读取）
        //IcDatas[0,2] - 1扇区 6块  从第一位开始：12位卸货时间 + 6位扣吨数 + 12位装货时间（卸货时不操作改数据，取样时写入）

        //IcDatas[1,0] - 2扇区 8块  从第一位开始：26位采购订单号（解密后为13位）
        //IcDatas[1,1] - 2扇区 9块  从第一位开始：6位毛重 + 10位毛重时间 / 6位皮重 + 10位皮重时间 + 16位计量员（卸货时写入操作员名字）
        //IcDatas[1,2] - 2扇区 10块 

        //IcDatas[2,0] - 3扇区 12块 从第一位开始：（卸货：客商名称，占用3个数据块）/（取样：2位取样代码，12位取样时间，18位取样人员名字,占用1个数据块）
        //IcDatas[2,1] - 3扇区 13块
        //IcDatas[2,2] - 3扇区 14块

        //IcDatas[3,0] - 4扇区 16块 从第一位开始：存货名称，占用3个数据块
        //IcDatas[3,1] - 4扇区 17块
        //IcDatas[3,2] - 4扇区 18块

        //IcDatas[4,0] - 5扇区 20块 从第一位开始：规格、型号，占用3个数据块（格式：规格!型号）
        //IcDatas[4,1] - 5扇区 21块
        //IcDatas[4,2] - 5扇区 22块


        /// <summary>
        /// 操作员名字
        /// </summary>
        string UserName;
        /// <summary>
        /// 磅前取样
        /// </summary>
        ToggleButton beforequality;
        /// <summary>
        /// 磅后取样
        /// </summary>
        ToggleButton afterquality;


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
        #endregion

        #region 创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.SampCm);

            beforequality = FindViewById<ToggleButton>(Resource.Id.bBeforeQuality);
            beforequality.Checked = false;
            beforequality.Enabled = false;
            beforequality.Click += OperateDialogBox;

            afterquality = FindViewById<ToggleButton>(Resource.Id.bAfterQuality);
            afterquality.Checked = false;
            afterquality.Enabled = false;
            afterquality.Click += OperateDialogBox;

            UserName = Intent.GetStringExtra("username");
            EditText person = FindViewById<EditText>(Resource.Id.tPerson);
            person.Text = UserName;

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

                    mPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(SampCm)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.UpdateCurrent);  //intent过滤器，过滤类型为NDEF_DISCOVERED    


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
            base.OnStop();
        }
        #endregion

        #region 获取到新的intent
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            ProcessAdapterAction(intent);
        }
        #endregion

        #region 处理新的intent事件
        public void ProcessAdapterAction(Intent intent)
        {
            //当系统检测到tag中含有NDEF格式的数据时，且系统中有activity声明可以接受包含NDEF数据的Intent的时候，系统会优先发出这个action的intent。
            //得到是否检测到ACTION_NDEF_DISCOVERED触发                           序号1
            if (NfcAdapter.ActionNdefDiscovered.Equals(intent.Action))
            {
                if (!afterquality.Checked && !beforequality.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    QualityFunction(intent);
                }
            }
            //当没有任何一个activity声明自己可以响应ACTION_NDEF_DISCOVERED时，系统会尝试发出TECH的intent.即便你的tag中所包含的数据是NDEF的，但是如果这个数据的MIME type或URI不能和任何一个activity所声明的想吻合，系统也一样会尝试发出tech格式的intent，而不是NDEF.
            //得到是否检测到ACTION_TECH_DISCOVERED触发                           序号2
            if (NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {
                //System.out.println("ACTION_TECH_DISCOVERED");
                if (!afterquality.Checked && !beforequality.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    QualityFunction(intent);
                }
            }
            //当系统发现前两个intent在系统中无人会接受的时候，就只好发这个默认的TAG类型的
            //得到是否检测到ACTION_TAG_DISCOVERED触发                           序号3
            if (NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            {
                if (!afterquality.Checked && !beforequality.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    QualityFunction(intent);
                }
            }
        }
        #endregion

        #region 读卡函数
        private void ReadIcData(Intent intent)
        {
            m_intent = intent;
            ic.icserial = ic.FindIC(m_intent);

            //取出封装在intent中的TAG 
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;
            m_tag = tag;

            #region 清空文本
            EditText carinfo = FindViewById<EditText>(Resource.Id.tCarInfo);
            carinfo.Text = "";

            EditText bussnise = FindViewById<EditText>(Resource.Id.tBussnise);
            bussnise.Text = "";

            EditText measuretype = FindViewById<EditText>(Resource.Id.tMeasureType);
            measuretype.Text = "";

            EditText measuremode = FindViewById<EditText>(Resource.Id.tMeasureMode);
            measuremode.Text = "";

            EditText measureid = FindViewById<EditText>(Resource.Id.tMeasureID);
            measureid.Text = "";

            EditText goodweight = FindViewById<EditText>(Resource.Id.tGoodWeight);
            goodweight.Text = "";

            EditText measuredt = FindViewById<EditText>(Resource.Id.tMeasureDT);
            measuredt.Text = "";

            EditText qualitytype = FindViewById<EditText>(Resource.Id.tQualityType);
            qualitytype.Text = "";

            #endregion

            MifareClassic mfc = MifareClassic.Get(m_tag);
            if (mfc != null)
            {
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

                            carinfo.Text = Car.CarPlate;

                            bussnise.Text = Car.BusinessType;

                            measuremode.Text = Car.MeasureMode;

                            measureid.Text = Car.CardID;

                            if (Car.QualityType == "0")
                            {
                                qualitytype.Text = "不取样";

                                beforequality.Checked = false;
                                beforequality.Enabled = false;

                                afterquality.Checked = false;
                                afterquality.Enabled = false;
                            }
                            else if (Car.QualityType == "1")
                            {
                                qualitytype.Text = "磅前取样";

                                beforequality.Enabled = true;
                                beforequality.Checked = true;
                                beforequality.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));
                                beforequality.SetTextColor(Android.Graphics.Color.ParseColor("#ffffff"));

                                afterquality.Enabled = false;
                                afterquality.Checked = false;
                            }
                            else if (Car.QualityType == "2")
                            {
                                qualitytype.Text = "磅后取样";

                                measuretype.Text = Car.MeasurementType;
                                goodweight.Text = Car.Gross.ToString();
                                measuredt.Text = Car.GrossDateTime;

                                beforequality.Enabled = false;
                                beforequality.Checked = false;

                                afterquality.Enabled = true;
                                afterquality.Checked = true;
                                afterquality.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));
                                afterquality.SetTextColor(Android.Graphics.Color.ParseColor("#ffffff"));
                            }
                            else if (Car.QualityType == "3")
                            {
                                qualitytype.Text = "代取样";

                                beforequality.Checked = false;
                                beforequality.Enabled = false;

                                afterquality.Checked = false;
                                afterquality.Enabled = false;
                            }
                            else
                            {
                                qualitytype.Text = "不明";

                                beforequality.Checked = false;
                                beforequality.Enabled = false;

                                afterquality.Checked = false;
                                afterquality.Enabled = false;
                            }

                            #endregion
                        }
                        else
                        {
                            #region 读卡异常，按钮不可用

                            beforequality.Enabled = false;
                            beforequality.Checked = false;
                            afterquality.Enabled = false;
                            afterquality.Checked = false;

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
            }
            else
            {
                #region 判断卡类型

                beforequality.Enabled = false;

                afterquality.Enabled = false;

                CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                ic.isReadIcOK = false;
                return;
                #endregion
            }
        }
        #endregion

        #region 取样写入函数
        /// <summary>
        /// 取样写入函数
        /// </summary>
        private void QualityFunction(Intent intent)
        {
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
                //写卡
                MifareClassic mfc = MifareClassic.Get(m_tag);
                if (mfc != null)
                {
                    try
                    {
                        mfc.Connect();
                        var type = mfc.GetType();
                        if (type.Equals(typeof(MifareClassic)))
                        {
                            try
                            {
                                if (afterquality.Checked)
                                {
                                    #region 1-先写入操作人员名字，位于2扇区9块后16位
                                    try
                                    {
                                        string tmp = ic.IcDatas[1, 1].Substring(0, 16); //获取2扇区9块前16位
                                        string writedata = tmp + CommonFunction.Add20ToUserName(ic.GetHexFromChs(UserName), 16);
                                        bool auth = mfc.AuthenticateSectorWithKeyB(2, ic.ToDigitsBytes(ic.mb));

                                        if (auth)
                                        {
                                            mfc.WriteBlock(9, ic.ToDigitsBytes(writedata));//16位卸货人员名字写入IC卡2扇区9块
                                        }
                                        else result = false;
                                    }
                                    catch (Exception ex)
                                    {
                                        result = false;
                                        CommonFunction.ShowMessage(ex.Message, this, true);
                                        return;
                                    }
                                    #endregion
                                    #region 2-取样类型位于3扇区12块前两位，如果取样类型为12，写入取样时间和取样人员 3扇区12块  2位取样代码，12位取样时间，16位取样人员名字
                                    try
                                    {
                                        //string quyangbiaozhi = ic.IcDatas[2, 0].ToString().Substring(0, 2);
                                        string quyangbiaozhi = ic.IcDatas[0, 1].ToString().Substring(4,1);
                                        if (quyangbiaozhi == "2")
                                        {
                                            //判断取样代码。为"12"时，磅后取样，写入IC卡取样时间和取样人员（即货场操作人员）；不为"12"时，不对IC卡操作，并且提示。
                                            string qydt = string.Format("{0:yyyyMMddhhmmss}", DateTime.Now);
                                            string writedata = quyangbiaozhi + qydt + CommonFunction.Add20ToUserName(ic.GetHexFromChs(UserName), 16);
                                            bool auth = mfc.AuthenticateSectorWithKeyB(3, ic.ToDigitsBytes(ic.mb));

                                            if (auth)
                                            {
                                                mfc.WriteBlock(12, ic.ToDigitsBytes(writedata));//12位取样时间和16位卸货人员名字写入IC卡3扇区12块
                                            }
                                            else result = false;
                                        }
                                        else
                                        {
                                            result = false;
                                            CommonFunction.ShowMessage("取样类型非'磅后取样'", this, true);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        result = false;
                                        CommonFunction.ShowMessage(ex.Message, this, true);
                                        return;
                                    }
                                    #endregion
                                }
                                else if (beforequality.Checked)
                                {

                                }
                            }
                            finally
                            {
                                mfc.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result = false;
                        CommonFunction.ShowMessage(ex.Message, this, true);
                    }
                    finally
                    {
                        mfc.Close();
                    }
                }
                else
                {
                    #region 判断卡类型
                    result = false;
                    CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                    return;
                    #endregion
                }

            }
            else if (CommonFunction.mode == "SerialPort")
            {
                CommonFunction.ShowMessage("磅后取样", this, true);
            }
            if (result)
            {
                beforequality.Checked = false;
                beforequality.Enabled = false;

                afterquality.Checked = false;
                afterquality.Enabled = false;

                CommonFunction.ShowMessage("操作成功！", this, true);
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
                if (button.Text == "磅前取样")
                {
                    if (beforequality.Checked) afterquality.Checked = false;
                    beforequality.Checked = false;
                    CommonFunction.ShowMessage("暂时无磅前取样业务", this, true);
                }
                else if (button.Text == "磅后取样")
                {
                    beforequality.Checked = false;
                    afterquality.Checked = true;
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
    }
}