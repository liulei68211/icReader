using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

using FTJL.Adapter;
using Android.Nfc;
using CN.Pda.Serialport;
using CN.Pda.Rfid.HF;
using Android.Nfc.Tech;
using Mono.Data.Sqlite;


namespace FTJL
{
    [Activity(Label = "trainInfoList")]
    public class TrainInfoList : Activity
    {
        #region 变量
        //取样类型 10为不取样；11为磅前取样；12为磅后取样；13为代取样

        //IcDatas[0,0] - 1扇区 4块  从第一位开始：8位卡号 + 24位车号（解密后为正常车牌照）
        //IcDatas[0,1] - 1扇区 5块  从第一位开始：5个标志位（计量类型/业务类型/计重方式/退货标识/理重实重标识 + 1个待用标志 + 24位计量单号（不读取）
        //IcDatas[0,2] - 1扇区 6块  从第一位开始：12位卸货时间 + 6位扣吨数 + 12位装货时间（卸货时不操作改数据，取样时写入）

        //IcDatas[1,0] - 2扇区 8块  从第一位开始：26位采购订单号（解密后为13位）
        //IcDatas[1,1] - 2扇区 9块  从第一位开始：6位毛重 + 10位毛重时间 / 6位皮重 + 10位皮重时间 + 16位计量员（卸货时写入操作员名字）
        //IcDatas[1,2] - 2扇区 10块 

        //IcDatas[2,0] - 3扇区 12块 从第一位开始：（卸货：客商名称，占用3个数据块）/（取样：2位取样代码，12位取样时间，18位取样人员名字,占用1个数据块）
        //IcDatas[2,1] - 3扇区 13块 第一位 取样卡/计量卡 标识位 1为取样卡
        //IcDatas[2,2] - 3扇区 14块

        //IcDatas[3,0] - 4扇区 16块 从第一位开始：存货名称，占用3个数据块
        //IcDatas[3,1] - 4扇区 17块
        //IcDatas[3,2] - 4扇区 18块

        //IcDatas[4,0] - 5扇区 20块 从第一位开始：规格、型号，占用3个数据块（格式：规格!型号）
        //IcDatas[4,1] - 5扇区 21块
        //IcDatas[4,2] - 5扇区 22块

        /// <summary>
        /// IC操作类
        /// </summary>
        IC ic = new IC();
        /// <summary>
        /// 车辆信息类
        /// </summary>
        CarInfo Car;
        /// <summary>
        /// 取样
        /// </summary>
        ToggleButton samp,supplort,save,sampSure;
   
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
        Tag m_tag,tag;
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
        /// 系统类型
        /// </summary>
        public string systemType;
        private string sqlstr;

        //自定义adapter
        private List<TableItem> data;//listview绑定的数据
        private CustomAdapter adapter;
        private DataTable dt;
         Sql sql = new Sql();
         BasicInfo bsif = new BasicInfo();

        private int isYshi;
        private string sampYingshi;

        private TextView text_sampfactory;
        private TextView text_carnumber;
        private TextView text_goodsname;
        private CheckBox checkbox;

        private ListView mylist;//listview控件

        private string ghTime;//过衡时间
        private string tmnw;//当前时间

        private List<string> number = new List<string>();//火车序号
        private List<string> trainNumber = new List<string>();//火车车号
        private List<string> gsInfo = new List<string>();//火车货物名称
        private List<string> groupDb = new List<string>();//组装数据

        /* 接收数据
         * 取样地点 samp_adressName 取样地点id samp_adressId
         * 货物名称 samp_goodsName  货物编号 samp_goodsCode
         * 取样方式 samp_modeName  取样方式id samp_modeId
         * 取样数量 samp_weight  
         * 货物单位 samp_utileName  
         */

        private string samp_goodsName;
        private string samp_modeName;
        private string samp_adressName;
        private string strError;//判断事务是否为空

        private string get_extrstr;//破包号
        private string get_extrstrlist;//参与摇号包数
        private int bags;//实际取袋数

        //子表
        private List<string> cmeasureID_list = new List<string>();//计量通行证号
        private List<string> sampCarNum_list = new List<string>();//车号
        private List<string> purchaseOrderID_list = new List<string>();//采购订单号
        private List<string> sampFactoryCode_list = new List<string>();//发货单位编码
        private List<string> sampFactory_list = new List<string>();//发货单位名称
        private List<string> sampGoodsCode_list = new List<string>();//货物编码
        private List<string> sampGoodsName_list = new List<string>();//存货名称
        private int isSampsub = 0;//是否取样    
        private List<string> sampMode_list=new List<string>();
        private List<int> bSample_list=new List<int>();//取样子表是否取样
        private string isSamp = "0";//取样临时表 是否取样
        private List<string> sampIsSamp_list = new List<string>();//取样临时表是否取样
        private List<int> sumbags_list = new List<int>();//总包数
        private List<string> extrabags_list = new List<string>();//抽取包数
        private List<string> getbag_list = new List<string>();//破包号

        //主表
        private string seqId;//取样编号
        private double samp_weight;//取样数量
        private string samp_utileName;//计量单位
        private int samp_adressId;//取样地点主键
        private int samp_modeId;
        private string nvgoodsCode;//nc存货编码
        private string username;//取样人
        private string isWriteDb;//是否上传成功
        private int isSampFinish = 0;//取样主表取样是否完成
        private int isSign = 0;
        private string cMeasureID,purchaseOrderId, sampFactoryCode, sampFactoryName, sampGoodsCode, sampGoodsName;
        #endregion

        #region 创建 Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.trainInfoList);

            tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
           // string sampTime = DateTime.ParseExact(tmnw, "MMddHHmmss", System.Globalization.CultureInfo.CurrentCulture).ToString();

            systemType = Intent.GetStringExtra("LoginSystemType");
            username = Intent.GetStringExtra("username");
            samp_adressName = Intent.GetStringExtra("samp_adressName");//取样地点
            samp_adressId = Convert.ToInt32(Intent.GetStringExtra("samp_adressId"));//取样地点id
            samp_goodsName = Intent.GetStringExtra("samp_goodsName");//货物名称
            nvgoodsCode = Intent.GetStringExtra("samp_goodsCode");//nc存货编码 
            samp_modeId = Convert.ToInt32(Intent.GetStringExtra("samp_modeId"));//取样方式id
            samp_modeName = Intent.GetStringExtra("samp_modeName");//取样方式名称
            samp_weight = Convert.ToDouble(Intent.GetStringExtra("samp_weight"));//取样数量
            samp_utileName = Intent.GetStringExtra("samp_utileName");//取样单位

            get_extrstr = Intent.GetStringExtra("get_extrstr");//破包号
            get_extrstrlist = Intent.GetStringExtra("get_extrstrlist");//抽取包号
            bags = Convert.ToInt32(Intent.GetStringExtra("bags"));//总包数
            sampYingshi = Intent.GetStringExtra("sampYingshi");//萤石取样位置

            mylist = FindViewById<ListView>(Resource.Id.listView1);
            // mylist.ItemClick += OnListItemClick;

            //取样按钮
            samp = FindViewById<ToggleButton>(Resource.Id.btSamp);
            samp.Enabled = false;
            samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#e6e6e6"));
            if (systemType == "白灰取样")
            {
                samp.Text = "白灰取样";
            }
            else
            {
                samp.Text = "取样";
            }
            samp.Click += OperateDialogBox;

            //补添记录按钮
            supplort = FindViewById<ToggleButton>(Resource.Id.btSupplement);
            supplort.Enabled = false;
            supplort.SetBackgroundColor(Android.Graphics.Color.ParseColor("#e6e6e6"));
            supplort.Click += SupplortClick;

            //抽样按钮
            save = FindViewById<ToggleButton>(Resource.Id.btSave);
            save.Click += SaveClick;
            //取样确认按钮
            sampSure = FindViewById<ToggleButton>(Resource.Id.btSampSure);
            if (samp_adressName == "综合料厂北")
            {
                sampSure.Text = "确认卸货";
                sampSure.Enabled = true;
                sampSure.SetBackgroundColor(Android.Graphics.Color.ParseColor("#4D96DA"));
                sampSure.Click += unLoadSureClick;
            }
            if (samp_adressName == "原料厂白灰厂" || samp_adressName=="合金库")
            {
                sampSure.Enabled = true;
                sampSure.Text = "取样确认";
                sampSure.SetBackgroundColor(Android.Graphics.Color.ParseColor("#4D96DA"));
                sampSure.Click += sampSureClick;
            }
            
            if (systemType == "火车取样")
            {
                save.Enabled = false;
                save.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));

                ghTime = Intent.GetStringExtra("ghTime");
                //数据库查询
                Sql sqlOpration = new Sql();
                //连接数据库
                sqlOpration.getcon2();
                sqlstr = " SELECT  No [序号],Balance," +
                         " (case when TrainNo ='' then '0' else TrainNo end)车号," +
                         " (case when CargoName ='' then NULL else CargoName end)货物," +
                         " Passtime " +
                         " From [192.168.122.2].[GDH].[dbo].[Train_t_BaseData] "+
                         " where Passtime = '" + ghTime + "' and (Balance like '%钢五%' or Balance like '%钢三%'  or  Balance like '%钢一%' or  Balance like '%钢四%')";

                dt = sqlOpration.GetTable(sqlstr);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    number.Add(dt.Rows[i]["序号"].ToString());
                    trainNumber.Add(dt.Rows[i]["车号"].ToString());
                    gsInfo.Add(dt.Rows[i]["货物"].ToString());
                }
                data = new List<TableItem>();
                for (int i = 0; i < number.Count; i++)
                {
                    data.Add(new TableItem(number[i],trainNumber[i],gsInfo[i]));
                }
                adapter = new CustomAdapter(this, data);
                mylist.Adapter = adapter;

                samp = FindViewById<ToggleButton>(Resource.Id.btSamp);
                samp.Enabled = false;
                samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#e6e6e6"));//#DB3E3E
                samp.Click += OperateDialogBox;
            }
            else if(systemType == "汽车取样")
            {
                save.Enabled = false;
                save.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
                data = new List<TableItem>();
            }
            else if (systemType == "合金取样")
            {
                save.Enabled = false;
                save.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
                data = new List<TableItem>();
            }
            else if (systemType == "白灰取样")
            {
                save.Enabled = true;
                save.SetBackgroundColor(Android.Graphics.Color.ParseColor("#DB3E3E"));
                data = new List<TableItem>();
            }
           
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

                    mPendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(TrainInfoList)).AddFlags(ActivityFlags.SingleTop), PendingIntentFlags.UpdateCurrent);  //intent过滤器，过滤类型为NDEF_DISCOVERED    

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

        #region 卸货确认按钮
        private void unLoadSureClick(object sender,EventArgs e)
        {
            Intent intent = new Intent(this, typeof(Unload));
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.PutExtra("username", username);
            StartActivity(intent);
        }
        #endregion

        #region 取样确认按钮
        private void sampSureClick(object sender,EventArgs e)
        {
            Intent intent = new Intent(this, typeof(SampCm));
            intent.SetFlags(ActivityFlags.ClearTop);

            intent.PutExtra("username", username);
            StartActivity(intent);
        }
        #endregion

        #region 未取样点击取样按钮 获取listview 以及checkbox选中的数据
        private void getListInfo()
        {
            clearList();
            //获取选中checkbox 的数据
            for (int i = 0; i < adapter.Count; i++)
            {
                LinearLayout layout = (LinearLayout)mylist.Adapter.GetView(i, null, null);//layout 是listview的父布局
                text_sampfactory = (TextView)layout.FindViewById(Resource.Id.textView1);
                text_carnumber = (TextView)layout.FindViewById(Resource.Id.textView2);
                text_goodsname = (TextView)layout.FindViewById(Resource.Id.textView3);
                checkbox = (CheckBox)layout.FindViewById(Resource.Id.checkBox1);

                try
                {
                  
                    //从子表中查询 已经选中checkbox的值是否 上传到子表中 如果上传 只添加未选中的checkbox的值 ；如果未上传，则全部添加到子表中
                    if (checkbox.Checked)
                    {
                        isSampFinish = 1;//主表取样完成
                        isSampsub = 1;//子表是否取样
                    }
                    else
                    {
                        isSampsub = 0;
                    }
                    //根据adapter中的车号 厂家名称和货物名称 
                    sampFactory_list.Add(text_sampfactory.Text);
                    sampCarNum_list.Add(text_carnumber.Text.Substring(0, 7));
                    cmeasureID_list.Add(text_carnumber.Text.Substring(7));
                    sampGoodsName_list.Add(text_goodsname.Text);

                    ////根据车号和计量单号 从计量系统中查询 采购订单号 供应商编号 供应商名称 货物编号 货物名称等信息  
                    bsif.selectCmInfo(sampCarNum_list[i], cmeasureID_list[i],out cMeasureID, out purchaseOrderId, out sampFactoryCode, out sampFactoryName, out sampGoodsCode, out sampGoodsName);

                    bSample_list.Add(isSampsub);
                    purchaseOrderID_list.Add(purchaseOrderId);
                    sampFactoryCode_list.Add(sampFactoryCode);
                    sampGoodsCode_list.Add(sampGoodsCode);

                    bsif.selectGoodsNum(sampGoodsName_list[i].ToString(), out bsif.iTheoryNum, out bsif.iJoinSampleNum);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取checkbox数据失败", this, true);
                }
            }
            //将数据写入数据库
            writeDb();
        }
        #endregion

        #region  判断抽取的样车车号是否与text_carnumber的值相等
        private bool isSame(string str)
        {
            bool isResult = false;
            Random rdm = new Random();
            string aa = sampCarNum_list[rdm.Next(0, sampCarNum_list.Count)];
            if (aa == str)
            {
                isResult = true;
            }
            return isResult;
        }
        #endregion

        #region 已经取过样 点击补添记录按钮 获取listview 以及checkbox选中的数据
        private void sup_getListInfo()
        {
            bool result = true;

            clearList();

            sql.getcon2();
            //判断取样车数是否 达到标准(石灰石 6车一取，是否够6车)
            string str_sql = "select max(pk_h)as pk_h from QQQ_Sampe_h where iPlace_pk = '" + samp_adressId + "'and dStart like'%" + tmnw.Substring(0,10) + "%'";
            dt = sql.GetTable(str_sql);

            //需要获得未选中checkbox的 计量通行证号
            //获取选中checkbox 的数据
            for ( int i=0; i < adapter.Count; i++)
            {
                LinearLayout layout = (LinearLayout)mylist.Adapter.GetView(i, null, null);//layout 是listview的父布局
                text_sampfactory = (TextView)layout.FindViewById(Resource.Id.textView1);
                text_carnumber = (TextView)layout.FindViewById(Resource.Id.textView2);
                text_goodsname = (TextView)layout.FindViewById(Resource.Id.textView3);
                checkbox = (CheckBox)layout.FindViewById(Resource.Id.checkBox1);

                try
                {
                    if (!checkbox.Checked)
                    {
                        isSampsub = 0;

                        //根据adapter中的车号 厂家名称和货物名称 
                        sampFactory_list.Add(text_sampfactory.Text);
                        sampCarNum_list.Add(text_carnumber.Text.Substring(0,7));
                        cmeasureID_list.Add(text_carnumber.Text.Substring(7));
                        sampGoodsName_list.Add(text_goodsname.Text);

                        //根据车号和计量单号 从计量系统中查询 采购订单号 供应商编号 供应商名称 货物编号 货物名称等信息  
                        bsif.selectCmInfo(sampCarNum_list[i], cmeasureID_list[i],out cMeasureID, out purchaseOrderId, out sampFactoryCode, out sampFactoryName, out sampGoodsCode, out sampGoodsName);

                        bSample_list.Add(isSampsub);
                        purchaseOrderID_list.Add(purchaseOrderId);
                        sampFactoryCode_list.Add(sampFactoryCode);
                        sampGoodsCode_list.Add(sampGoodsCode);

                        string insert_sampb = "insert into QQQ_Sample_b " +
                                   "(pk_h,cMeasure_ID,cCarCode,cOrderID,CSupplierCode,CSupplierName,cInvCode,cInvName,dReadIC,iCarBagNum,iSampleBagNum,getbag,bSample,cSampYshi) " +
                                   " values " +
                                   "('" + Convert.ToInt32(dt.Rows[0]["pk_h"].ToString()) + "', '" + cmeasureID_list[i] + "','" + sampCarNum_list[i] + "','" + purchaseOrderID_list[i] + "', '" + sampFactoryCode_list[i] + "', '" + sampFactory_list[i] + "', '" + sampGoodsCode_list[i] + "', '" + sampGoodsName_list[i] + "','" + tmnw+ "','"+sumbags_list[i]+"','"+extrabags_list[i]+"','"+ getbag_list[i]+"','"+ isSampsub +"','"+sampYingshi+"')";

                        if (sql.getsqlcom(insert_sampb) != true)
                        {
                            result = false;
                            CommonFunction.ShowMessage("操作失败请重新操作",this,true);
                        }
                        result = true;
                    }
                    else
                    {
                        sampFactory_list.Add("");
                        sampCarNum_list.Add("");
                        sampGoodsName_list.Add("");
                        purchaseOrderID_list.Add("");
                        sampFactoryCode_list.Add("");
                        sampGoodsCode_list.Add("");
                       cmeasureID_list.Add("");
                        //bSample_list.Add(isSampsub);
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取checkbox数据失败", this, true);
                }
            }

            //更新取样临时表 isSamp = 1
            string update_sql = "";
            for (int i = 0; i < cmeasureID_list.Count; i++)
            {
                string ssss = "update QTM_SampleTemp set [isSamp] = '1' where [cmeasureId] = '" + cmeasureID_list[i] + "'";
                update_sql += ssss + ";";
            }
            if (sql.getsqlcom(update_sql) != true)
            {
                CommonFunction.ShowMessage("更新临时表数据失败", this, true);
            }
            sql.closecon();
            WriteDataOK(result);
        }
        #endregion

        #region 写入数据库
        private void writeDb()
        {
            sql.getcon2();
            
            //根据 数据表QSS_SeqID 中的地点主键 iPlace_pk = adressID 查询取样编号cSeqID
            string sql_pkplace = "select dbo.Q_GetSeqID('" + samp_adressId + "') as seqId";
            //获取seqId 并判断是否为空
            dt = sql.GetTable(sql_pkplace);
            seqId = dt.Rows[0]["seqId"].ToString();
            //取样时间
            //sampTime = Convert.ToDateTime(tmnw);
           // string sampTime = DateTime.ParseExact(tmnw, "MMddHHmmss", System.Globalization.CultureInfo.CurrentCulture).ToString();
            if (seqId == "")
            {
                //取样编号为空的时候 提示联系生产部人员添加地点
                //Toast.MakeText(this, "暂无该地点信息,请联系生产部相关人员进行设置", ToastLength.Long).Show();
                CommonFunction.ShowMessage("暂无该地点信息,请联系生产部相关人员进行设置", this,true);
            }
            else
            {
                #region
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
                #endregion
                //更新取样临时表 取样状态为1 ；不为空时 调用事务 更新seqId(+1) 插入主表信息 插入子表信息     
                sql.getcon2();
               // seqId += "1";
                string update_sql = "";
                for (int i = 0; i < cmeasureID_list.Count; i++)
                {
                    string ssss = "update QTM_SampleTemp set [isSamp] = '1' where [cmeasureId] = '" + cmeasureID_list[i] + "'";
                    update_sql += ssss + ";";
                }
                if (sql.getsqlcom(update_sql) != true)
                {
                    CommonFunction.ShowMessage("更新临时表数据失败", this, true);
                }

                bsif.InsertSampInfo(samp_adressId, seqId,samp_weight,samp_utileName,samp_adressId,samp_modeId,nvgoodsCode,bsif.iTheoryNum,bsif.iJoinSampleNum,tmnw,username, isSampFinish,isSign,
                cmeasureID_list, purchaseOrderID_list, sampCarNum_list, sampFactoryCode_list, sampFactory_list, sampGoodsCode_list, sampGoodsName_list, tmnw,sumbags_list,extrabags_list,getbag_list, bSample_list,sampYingshi, out strError);

                isWriteDb = strError;
                bool result = true;
                WriteDataOK(result);
                cmeasureID_list.Clear();
                getbag_list.Clear();
                extrabags_list.Clear();
                sumbags_list.Clear();
                purchaseOrderID_list.Clear();
                sampFactoryCode_list.Clear();
                sampGoodsCode_list.Clear();
                bSample_list.Clear();
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
            //m_nfcAdapter NFC模式
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
                if (!samp.Checked)
                {
                    ReadIcData(intent);
                    m_intent = intent;
                }
                else
                {
                    //写卡函数
                    //UnloadFunction(intent);
                }
            }
            //当没有任何一个activity声明自己可以响应ACTION_NDEF_DISCOVERED时，系统会尝试发出TECH的intent.即便你的tag中所包含的数据是NDEF的，但是如果这个数据的MIME type或URI不能和任何一个activity所声明的想吻合，系统也一样会尝试发出tech格式的intent，而不是NDEF.
            //得到是否检测到ACTION_TECH_DISCOVERED触发                           序号2
            if (NfcAdapter.ActionTechDiscovered.Equals(intent.Action))
            {
                //System.out.println("ACTION_TECH_DISCOVERED");
                //处理该intent   
                if (!samp.Checked)
                {
                    m_intent = intent;
                    ReadIcData(intent);
                }
                else
                {
                    // UnloadFunction(intent);
                }
            }
            //当系统发现前两个intent在系统中无人会接受的时候，就只好发这个默认的TAG类型的
            //得到是否检测到ACTION_TAG_DISCOVERED触发                           序号3
            if (NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            {
                if (!samp.Checked)
                {
                    ReadIcData(intent);
                }
                else
                {
                    m_intent = intent;
                    // UnloadFunction(intent);
                }
            }
        }
        #endregion

        #region 读卡事件
        private void ReadIcData(Intent intent)
        {
            m_intent = intent;
            ic.icserial = ic.FindIC(m_intent);
            #region
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
                    try
                    {
                        //解析扇区数据
                        Car = ic.GetCarInfo(ic.IcDatas);
                        if (Car != null)
                        {
                            // CommonFunction.ShowMessage("", this, false);
                            #region 读卡正确，显示在窗体文本内,按钮复活
                            //根据 Car.MeasureID 判断所读的卡是计量卡还是取样卡
                            if (Car.isSampIc != "1")
                            {
                                purchaseOrderID_list.Clear();
                                sampFactoryCode_list.Clear();
                                sampGoodsCode_list.Clear();
                                //计量卡
                               
                                //根据车号和计量单号 从计量系统中查询 采购订单号 供应商编号 供应商名称 货物编号 货物名称等信息  
                                bsif.selectCmInfo(Car.CarPlate, Car.MeasureID, out  cMeasureID, out purchaseOrderId, out sampFactoryCode, out sampFactoryName, out sampGoodsCode, out sampGoodsName);

                                cmeasureID_list.Add(Car.MeasureID);
                                purchaseOrderID_list.Add(purchaseOrderId);
                                sampFactoryCode_list.Add(sampFactoryCode);
                                sampGoodsCode_list.Add(sampGoodsCode);

                                //判断是否为同一厂家 并绑定数据到listview
                                if (isEqualFactory() == true)
                                {
                                    if (isSamped())
                                    {
                                        //存在取样记录 补添记录按钮可用，取样按钮不可用
                                        samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
                                        samp.Enabled = false;

                                        supplort.SetBackgroundColor(Android.Graphics.Color.ParseColor("#4D96DA"));
                                        supplort.Enabled = true;
                                    }
                                    else
                                    {
                                        //不存在取样记录 按钮都不可用
                                        samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
                                        samp.Enabled = false;

                                        supplort.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));
                                        supplort.Enabled = false;
                                    }

                                    //循环adapter数据 如果isSamp = 1 则checkbox为选中状态
                                    for (int i = 0; i < adapter.Count; i++)
                                    {
                                        LinearLayout layout = (LinearLayout)mylist.Adapter.GetView(i, null, null);//layout 是listview的父布局
                                        text_sampfactory = (TextView)layout.FindViewById(Resource.Id.textView1);
                                        text_carnumber = (TextView)layout.FindViewById(Resource.Id.textView2);
                                        text_goodsname = (TextView)layout.FindViewById(Resource.Id.textView3);
                                        checkbox = (CheckBox)layout.FindViewById(Resource.Id.checkBox1);

                                        if (sampIsSamp_list.Count != 0)
                                        {
                                            if (sampIsSamp_list[i].ToString() == "1")
                                            {
                                                //已经取过样
                                                checkbox.Checked = true;
                                            }
                                        }
                                    }
                                } 
                            }
                            else
                            {
                                //所读的卡为取样卡 
                                samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#DB3E3E"));
                                samp.Enabled = true;
                            }
                            #endregion
                        }
                        else
                        {
                            #region 读卡异常，按钮不可用
                            ToggleButton samp = FindViewById<ToggleButton>(Resource.Id.btSamp);
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
                samp.Enabled = false;

                samp.Checked = false;
                CommonFunction.ShowMessage("不支持该类型的卡", this, true);
                ic.isReadIcOK = false;
                return;
                #endregion
            }
            #endregion
        }
        #endregion

        #region 判断取样临时表 中是否存在取样记录 isSamp = 1
        private bool isSamped()
        {
            bool result = false;

            if (sampCarNum_list.Count == 0)
            {
                result = false;
            }
            else
            {
                for (int i = 0; i < sampIsSamp_list.Count; i++)
                {
                    if (sampIsSamp_list[i].ToString() == "1")
                    {
                        result = true;
                        break;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }
            return result;
        }
        #endregion

        #region 取样按钮 点击取样按钮时 在刷过的车号中 随机选择一辆车 作为取样车
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
                if (button.Text == "白灰取样" || button.Text == "取样")
                {
                    if (samp.Checked)
                    {
                        //获取listview信息
                        getListInfo();
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

        #region 补添记录按钮 事件 
        private void SupplortClick(object sender, EventArgs e)
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
                if (button.Text == "补添记录")
                {
                    if (supplort.Checked)
                    {
                        //获取listview信息 
                        sup_getListInfo();
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

        #region 判断是否是同一厂家 并绑定数据 将数据写入本地数据库（摇号 与 不摇号）
        private bool isEqualFactory()
        {
            bool result = false;//true:取样按钮不可用 补添记录按钮可用 ；false 取样按钮可用 补添记录按钮不可用
            //从远程数据库QQQ_Sample_b中查询
            SqlConnection conn =  sql.getcon2();
            try
            {
                 //查询读到的卡中 的厂家信息 的所有记录  
                 string insert_sql = "";
                 string select_sql = "select cmeasureId from QTM_SampleTemp where  addTime like '%"+tmnw.Substring(0,10)+"%'";
                 if (Bool_QueryInfo(conn,select_sql))
                 {
                     //数据库不为空 根据计量 通行证号判断是否为同一张卡
                     select_sql = "select cmeasureId from QTM_SampleTemp where cmeasureId ='" + Car.MeasureID + "' and  addTime like '%" + tmnw.Substring(0, 10) + "%'";
                     if (Bool_QueryInfo(conn,select_sql))
                     {
                        result = false;

                        if (samp_goodsName == "高品位萤石" || samp_goodsName =="萤石")
                        {
                            //再添加一条记录 isYshi = 2
                            isYshi = 2;
                            insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags,extrabags,getbag,isYshi) values " +
                                "('" + Car.MeasureID + "','" + Car.CarPlate + "','" + sampFactoryName + "','" + sampGoodsName + "','" + isSamp + "','" + tmnw + "','" + bags + "','" + get_extrstr + "','" + get_extrstrlist + "','" + isYshi + "')";

                            if (sql.getsqlcom(insert_sql) != true)
                            {
                                result = false;
                                CommonFunction.ShowMessage("写入数据失败", this, true);
                            }
                        }
                        //如果货物名称为萤石或者高品位萤石 则允许再次刷卡
                        //将与该厂家 货物信息相同的且isSamp = 0 的展示出来
                        select_sql = "select cmeasureId,carNumber,goodsName,factoryName,isSamp ,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = '0'";
                        showListDb(select_sql);
                        //为同一张卡
                        CommonFunction.ShowMessage("该卡信息已经存入数据表中 请换卡", this, true);
                    }
                     else
                     {
                         //不是同一张卡 判断是否为同一厂家  
                         select_sql = "select factoryName,goodsName from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName = '" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                         if (Bool_QueryInfo(conn,select_sql))
                         {
                            //是同一厂家 判断是否车号相同 
                            select_sql = "select factoryName,goodsName,carNumber from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and  carNumber ='"+Car.CarPlate+ "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                            if (Bool_QueryInfo(conn,select_sql))
                            {
                                result = true;
                                //是同一厂家 车号相同 同一辆车二次进厂 将该信息insert数据表  查询出该厂家所有信息 展示listview 
                                insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags,extrabags,getbag) values " +
                                 "('" + Car.MeasureID + "','" + Car.CarPlate + "','" + sampFactoryName + "','" + sampGoodsName + "','" + isSamp + "','" + tmnw + "','" + bags + "','" + get_extrstr + "','" + get_extrstrlist + "')";

                                if (sql.getsqlcom(insert_sql) != true)
                                {
                                    result = false;
                                    CommonFunction.ShowMessage("写入数据失败", this, true);
                                }
                                //判断是否为摇号取样 如果是 
                                if (systemType == "合金取样"  )
                                {
                                    //是摇号取样 先insert 查出isSamp = 0 的所有数据
                                    samp_adressId = 5;
                                    samp_modeId = 1;
                                    select_sql = "select cmeasureId, factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                }
                                if (systemType == "白灰取样")
                                {
                                    //是摇号取样 先insert 查出isSamp = 0 的所有数据
                                    samp_adressId = 22;
                                    samp_modeId = 1;
                                    select_sql = "select cmeasureId, factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                }
                                else
                                {
                                    //统计已经取样记录是否够数 石灰石、云石粉3车一取；石灰石粉6车一取                                
                                    select_sql = "select count(*)count from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                                    dt = sql.GetTable(select_sql);
                                    int counts = 0;
                                    for (int i=0;i<dt.Rows.Count;i++)
                                    {
                                        counts = Convert.ToInt32(dt.Rows[0]["count"].ToString());
                                    }
                                    sql.closecon();
                                    if (sampGoodsName == "石灰石" || sampGoodsName == "云石粉")
                                    {
                                        if (counts == 3)
                                        {
                                            //只查询isSamp = 0的数据
                                            select_sql = "select cmeasureId, factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                        }
                                        else
                                        {
                                            //查出所有数据
                                            select_sql = "select cmeasureId,carNumber,goodsName,factoryName,isSamp ,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                                        }
                                    }
                                    if (sampGoodsName == "石灰石粉")
                                    {
                                        if (counts == 6)
                                        {
                                            //只查询isSamp = 0的数据
                                            select_sql = "select cmeasureId, factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                        }
                                        else
                                        {
                                            //查出所有数据
                                            select_sql = "select cmeasureId,carNumber,goodsName,factoryName,isSamp ,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                                        }
                                    }
                                    //不是摇号取样 查出所有数据(isSamp = 1 and isSamp = 0)
                                   // select_sql = "select cmeasureId,carNumber,goodsName,factoryName,isSamp ,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                                }
                                showListDb(select_sql);
                            }
                            else
                            {
                                result = true;
                                //是同一厂家 车号不同  执行insert 
                                insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags,extrabags,getbag) values " +
                                 "('" + Car.MeasureID + "','" + Car.CarPlate + "','" + sampFactoryName + "','" + sampGoodsName + "','" + isSamp + "','" + tmnw + "','" + bags + "','" + get_extrstr + "','" + get_extrstrlist + "')";

                                if (sql.getsqlcom(insert_sql) != true)
                                {
                                    result = false;
                                    CommonFunction.ShowMessage("写入数据失败", this, true);
                                }
                                sampFactory_list.Clear();
                                sampCarNum_list.Clear();
                                sampGoodsName_list.Clear();
                                sampIsSamp_list.Clear();
                                cmeasureID_list.Clear();
                                sumbags_list.Clear();
                                extrabags_list.Clear();
                                getbag_list.Clear();

                                //判断是否为摇号取样 如果是 
                                if (systemType == "合金取样")
                                {
                                    samp_adressId = 5;
                                    samp_modeId = 1;
                                    //是摇号取样 先insert 查出isSamp = 0 的所有数据 请包数 破包 抽取包数 存入list
                                    select_sql = "select cmeasureId,factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                }
                                if (systemType == "白灰取样")
                                {
                                    //是摇号取样 先insert 查出isSamp = 0 的所有数据
                                    samp_adressId = 22;
                                    samp_modeId = 1;
                                    select_sql = "select cmeasureId, factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                                }
                                else
                                {
                                    //不是摇号取样 查出所有数据(isSamp = 1 and isSamp = 0)
                                    select_sql = "select cmeasureId,carNumber,goodsName,factoryName,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and addTime like '%" + tmnw.Substring(0, 10) + "%'";
                                }
                                showListDb(select_sql);
                            }
                        }
                         else
                         {
                            result = true;
                            //不是同一厂家  执行insert data 清空 绑定数据listview
                            insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags,extrabags,getbag) values " +
                                 "('" + Car.MeasureID + "','" + Car.CarPlate + "','" + sampFactoryName + "','" + sampGoodsName + "','" + isSamp + "','" + tmnw + "','" + bags + "','" + get_extrstr + "','" + get_extrstrlist + "')";

                            if (sql.getsqlcom(insert_sql) != true)
                            {
                                result = false;
                                CommonFunction.ShowMessage("写入数据失败", this, true);
                            }
                            select_sql = "select factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and  carNumber ='" + Car.CarPlate + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";
                            dt = sql.GetTable(select_sql);
                            //判断 是否摇号取样
                            if (systemType == "合金取样")
                            {
                                //是摇号取样 先insert 查出isSamp = 0 的所有数据
                                samp_adressId = 5;
                                samp_modeId = 1;
                               
                                sumbags_list.Add(Convert.ToInt32(dt.Rows[0]["sampbags"].ToString().Trim()));
                                extrabags_list.Add(dt.Rows[0]["extrabags"].ToString().Trim());
                                getbag_list.Add(dt.Rows[0]["getbag"].ToString().Trim());
                            }
                            else
                            {
                                sumbags_list.Add(0);
                                extrabags_list.Add("0");
                                getbag_list.Add("0");
                            }
                                data.Clear();
                                data.Add(new TableItem(sampFactoryName,Car.CarPlate+Car.MeasureID,sampGoodsName));
                                mylist = FindViewById<ListView>(Resource.Id.listView1);
                                adapter = new CustomAdapter(this, data);
                                mylist.Adapter = adapter;
                            }
                        }
                    }
                 else
                 {
                    result = true;
                    if (samp_goodsName == "高品位萤石" || samp_goodsName == "萤石")
                    {
                        isYshi = 1;
                    }
                    else
                    {
                        isYshi = 0;
                    }
                    //数据库为空 数据写入数据库 绑定listview
                    insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags,extrabags,getbag,isYshi) values " +
                                 "('" + Car.MeasureID + "','" + Car.CarPlate + "','" + sampFactoryName + "','" + sampGoodsName + "','" + isSamp + "','" + tmnw + "','"+bags+"','"+get_extrstr+"','"+get_extrstrlist+"','"+isYshi+"')";

                     if (sql.getsqlcom(insert_sql) != true)
                     {
                        result = false;
                        CommonFunction.ShowMessage("写入数据失败", this, true);
                     }
                    select_sql = "select factoryName,goodsName,carNumber,isSamp,sampbags,extrabags,getbag from QTM_SampleTemp where factoryName = '" + sampFactoryName + "' and goodsName='" + sampGoodsName + "' and  carNumber ='" + Car.CarPlate + "' and addTime like '%" + tmnw.Substring(0, 10) + "%' and isSamp = 0";

                    dt = sql.GetTable(select_sql);
                    //判断 是否摇号取样
                    if (systemType == "合金取样")
                    {
                        //是摇号取样 先insert 查出isSamp = 0 的所有数据
                        samp_adressId = 5;
                        samp_modeId = 1;
                       
                        sumbags_list.Add(Convert.ToInt32(dt.Rows[0]["sampbags"].ToString().Trim()));
                        extrabags_list.Add(dt.Rows[0]["extrabags"].ToString().Trim());
                        getbag_list.Add(dt.Rows[0]["getbag"].ToString().Trim());
                    }
                    else
                    {
                        sumbags_list.Add(0);
                        extrabags_list.Add("0");
                        getbag_list.Add("0");
                    }
                    if (data.Count != 0)
                    {
                        data.Clear();
                    }
                    data.Add(new TableItem(sampFactoryName, Car.CarPlate+Car.MeasureID, sampGoodsName));
                    mylist = FindViewById<ListView>(Resource.Id.listView1);
                    adapter = new CustomAdapter(this, data);
                     mylist.Adapter = adapter;
                 }
            }
            catch (Exception ex)
            {
                 CommonFunction.ShowMessage("获取货物信息失败！", this, true);
            }
            finally
            {
                 if (conn.State != ConnectionState.Closed)
                 {
                    conn.Close();
                 }
                 conn.Dispose();
            }
            return result;
        }
        #endregion

        #region listview展示数据
        private void showListDb( string sql)
        {
            Sql sql1= new Sql();
            sql1.getcon2();
            dt = sql1.GetTable(sql);
           
            sampFactory_list.Clear();
            sampCarNum_list.Clear();
            sampGoodsName_list.Clear();
            sampIsSamp_list.Clear();
            cmeasureID_list.Clear();
            sumbags_list.Clear();
            extrabags_list.Clear();
            getbag_list.Clear();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                //该车未刷卡  并且为同一厂家
                sampFactory_list.Add(dt.Rows[i]["factoryName"].ToString().Trim());
                sampCarNum_list.Add(dt.Rows[i]["carNumber"].ToString().Trim());
                sampGoodsName_list.Add(dt.Rows[i]["goodsName"].ToString().Trim());
                sampIsSamp_list.Add(dt.Rows[i]["isSamp"].ToString().Trim());
                cmeasureID_list.Add(dt.Rows[i]["cmeasureId"].ToString().Trim());
                if (dt.Rows[i]["sampbags"].ToString().Trim() == "")
                {
                    sumbags_list.Add(0);
                    extrabags_list.Add("0");
                    getbag_list.Add("0");
                }
                else
                {
                    sumbags_list.Add(Convert.ToInt32(dt.Rows[i]["sampbags"].ToString().Trim()));
                    extrabags_list.Add(dt.Rows[i]["extrabags"].ToString().Trim());
                    getbag_list.Add(dt.Rows[i]["getbag"].ToString().Trim());
                }
            }
            sql1.closecon();
            data.Clear();

            //绑定listview
            for (int i = 0; i < sampCarNum_list.Count; i++)
            {
                data.Add(new TableItem(sampFactory_list[i], sampCarNum_list[i] + cmeasureID_list[i], sampGoodsName_list[i]));
            }
            mylist = FindViewById<ListView>(Resource.Id.listView1);
            adapter = new CustomAdapter(this, data);
            mylist.Adapter = adapter;
        }
        #endregion

        #region 查询数据库返回真假值
        private bool Bool_QueryInfo(SqlConnection SqlConn, string SqlStr)
        {
            bool result = false;
            try
            {
                if (SqlConn.State != ConnectionState.Open)
                {
                    SqlConn.Open();
                }
                if (SqlConn.State == ConnectionState.Open)
                {
                    SqlCommand cmd = new SqlCommand(SqlStr, SqlConn);
                    SqlDataReader reader = cmd.ExecuteReader();
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

        #region 操作成功
        /// <summary>
        /// 操作成功
        /// </summary>
        private void WriteDataOK(bool ok)
        {
            if (ok)
            {
                //取样按钮
                samp.Checked = false;

                samp.Enabled = false;
                samp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E6E6E6"));

                CommonFunction.ShowMessage("操作成功！", this, true);
            }
            else CommonFunction.ShowMessage("写卡失败！", this, true);
        }
        #endregion

        #region 抽样车
        private void SaveClick(object sender,EventArgs e)
        {
            // 白灰取样时 刷够车数时(5车一个样)，先点击保存按钮，后台程序随机选择一个车号，并保存一条取样记录，当需要 标记生过烧时 (>=2个取样编号时，标记两个样)
            clearList();
           
            //获取选中checkbox 的数据
            for (int i = 0; i < adapter.Count; i++)
            {
                LinearLayout layout = (LinearLayout)mylist.Adapter.GetView(i, null, null);//layout 是listview的父布局
                text_sampfactory = (TextView)layout.FindViewById(Resource.Id.textView1);
                text_carnumber = (TextView)layout.FindViewById(Resource.Id.textView2);
                text_goodsname = (TextView)layout.FindViewById(Resource.Id.textView3);
                checkbox = (CheckBox)layout.FindViewById(Resource.Id.checkBox1);
                try
                {
                    //根据adapter中的车号 厂家名称和货物名称     
                    sampFactory_list.Add(text_sampfactory.Text);
                    sampCarNum_list.Add(text_carnumber.Text.Substring(0, 7));
                    cmeasureID_list.Add(text_carnumber.Text.Substring(7));
                    sampGoodsName_list.Add(text_goodsname.Text);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取checkbox数据失败", this, true);
                }
            }

            Random rdm = new Random();
            string aa = sampCarNum_list[rdm.Next(0, sampCarNum_list.Count)];

            CommonFunction.ShowMessage( "样车车号为" + aa + "请选择样车车号并取样",this,true);
            //Toast.MakeText(this,"样车车号为"   + aa +   "请选择样车车号并取样", ToastLength.Short).Show();
            // adapter.isChecked = true;
            #region listview更新
            //for (int i = 0; i < adapter.Count; i++)
            //{
            //    if (aa == sampCarNum_list[i])
            //    {
            //        adapter.isChecked = true;
            //    }
            //    else
            //    {
            //        adapter.isChecked = false;
            //    }
            //    LinearLayout layout = (LinearLayout)mylist.Adapter.GetView(i, null, null);//layout 是listview的父布局
            //    text_sampfactory = (TextView)layout.FindViewById(Resource.Id.textView1);
            //    text_carnumber = (TextView)layout.FindViewById(Resource.Id.textView2);
            //    text_goodsname = (TextView)layout.FindViewById(Resource.Id.textView3);
            //    checkbox = (CheckBox)layout.FindViewById(Resource.Id.checkBox1);
            //}
            //adapter.NotifyDataSetChanged();//更新listview
            #endregion
        }
        #endregion

        #region list清除数据
        private void clearList()
        {
            sampFactory_list.Clear();
            sampCarNum_list.Clear();
            sampGoodsName_list.Clear();
            purchaseOrderID_list.Clear();
            sampFactoryCode_list.Clear();
            sampGoodsCode_list.Clear();
            cmeasureID_list.Clear();
        }
        #endregion
    }
}