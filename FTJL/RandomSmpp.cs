using System;
using System.IO;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

using CN.Pda.Rfid.HF;
using CN.Pda.Serialport;

using Android.Nfc;
using Android.Nfc.Tech;
using Mono.Data.Sqlite;

namespace FTJL
{
    [Activity(Label = "RandomSmpp")]
    public class RandomSmpp : Activity
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
        /// 车辆信息类
        /// </summary>
        CarInfo Car;

        /// <summary>
        /// 保存按钮
        /// </summary>
        ToggleButton btSamp;

        /// <summary>
        /// 取样按钮
        /// </summary>
        ToggleButton btSave;

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
        Tag m_tag, tag;
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
        /// 返回的自增值
        /// </summary>

        Sql sql = new Sql();
        DataTable dt;
        BasicInfo bsif = new BasicInfo();

        EditText extra_bags;//抽取包数
        EditText samp_bags;//总包数
        EditText broken_bag;//总包数
        EditText samp_person;//取样人
        EditText samp_time;//取样时间
        EditText samp_factory;//供应商名称
        EditText samp_goodsName;//货物名称
        EditText samp_carNum;//车号
        EditText samp_Mode;//业务类型
        EditText samp_adress;//取样地点

        private string cmeasureID;//计量通行证号
        private string get_extrstr;//破包号
        private string get_extrstrlist;//参与摇号包数
        private int bags;//实际取袋数
        private string tmnw;
        private string LoginSystemType;
        #endregion

        #region 创建Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.RandomSmpp);

            cmeasureID = Intent.GetStringExtra("cmeasure_Id");
            get_extrstr = Intent.GetStringExtra("sextra_one");//破包号
            get_extrstrlist = Intent.GetStringExtra("rmlist_Str");//抽取号
            LoginSystemType = Intent.GetStringExtra("LoginSystemType");
            bags = Convert.ToInt32(Intent.GetStringExtra("sampbags"));

            //供应商名称
            samp_factory = FindViewById<EditText>(Resource.Id.supplier);
            samp_factory.Text = Intent.GetStringExtra("sampFactory");
            //货物名称
            samp_goodsName = FindViewById<EditText>(Resource.Id.goodsName);
            samp_goodsName.Text = Intent.GetStringExtra("sampGoodsName");
            //车号
            samp_carNum = FindViewById<EditText>(Resource.Id.carNumber);
            samp_carNum.Text = Intent.GetStringExtra("sampCarNum") + "||"+ cmeasureID;
            //地点
            samp_adress = FindViewById<EditText>(Resource.Id.sampAdress);
            samp_adress.Text = Intent.GetStringExtra("sampAdress");
            //业务类型
            samp_Mode = FindViewById<EditText>(Resource.Id.sampMode);
            samp_Mode.Text = Intent.GetStringExtra("sampMode");
            //总包数
            samp_bags = FindViewById<EditText>(Resource.Id.bagCount);
            samp_bags.Text = Convert.ToString( bags);
            //取样包数
            extra_bags = FindViewById<EditText>(Resource.Id.extractBags);
            extra_bags.Text = Convert.ToString(get_extrstrlist);
            //破包
            broken_bag = FindViewById<EditText>(Resource.Id.extractNumber);
            broken_bag.Text = get_extrstr;
            //取样人
            samp_person = FindViewById<EditText>(Resource.Id.sampName);
            samp_person.Text = Intent.GetStringExtra("username");
            //取样时间
            tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:ss:mm");
            samp_time = FindViewById<EditText>(Resource.Id.sampTime);
            samp_time.Text = tmnw;

            //保存按钮
            btSamp = FindViewById<ToggleButton>(Resource.Id.btSamp);
            btSamp.Click += OperateDialogBox;
            btSamp.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E21918"));
            btSamp.Enabled = true;

            //取样按钮
            btSave = FindViewById<ToggleButton>(Resource.Id.btSave);
            btSave.Click += OnClick_Save;
            btSave.SetBackgroundColor(Android.Graphics.Color.ParseColor("#E21918"));
            btSave.Enabled = true;
        }
        #endregion

        #region 保存按钮
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
                    if (button.Text == "保存")
                    {
                        if (btSamp.Checked)
                        {
                            bool result = false;
                            //将信息保存到取样临时表中
                            sql.getcon2();
                            string insert_sql = "insert into QTM_SampleTemp(cmeasureId,carNumber,factoryName,goodsName,isSamp,addTime,sampbags ,extrabags,getbag) values " +
                                             "('" + cmeasureID + "','" + samp_carNum.Text.Substring(0,7) + "','" + samp_factory.Text + "','" + samp_goodsName.Text + "','" + 0 + "','" + tmnw + "','"+ bags + "','"+ get_extrstrlist + "','"+ get_extrstr + "')";

                            if (sql.getsqlcom(insert_sql) != true)
                            {
                                result = false;
                                CommonFunction.ShowMessage("写入数据失败", this, true);
                            }
                            sql.closecon();
                            result = true;
                            WriteDataOK(result);
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

        #region 取样按钮
        private void OnClick_Save(object sender,EventArgs e)
        {
            Intent intent = new Intent(this, typeof(TrainInfoList));
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.PutExtra("LoginSystemType", LoginSystemType);
            intent.PutExtra("get_extrstrlist", extra_bags.Text);//抽取包号
            intent.PutExtra("get_extrstr", broken_bag.Text);//破包号
            intent.PutExtra("bags", samp_bags.Text);//总包数
            intent.PutExtra("username", samp_person.Text);//总包数

            StartActivity(intent);
        }
        #endregion
    }
}