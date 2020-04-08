using System;
using System.Data;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using FTJL.Extensions;
using System.Threading.Tasks;
namespace FTJL
{
    [Activity(Label = "通用取样")]
    public class CommonSampClass : Activity
    {
        #region 变量
        List<string> sampModeName = new List<string>();//取样方式
        List<string> sampAdressName = new List<string>();//取样地点
        List<string> sampGoodsName = new List<string>();//取样货物
        List<string> sampUtile = new List<string>();//货物单位
        List<int> goodsIdList = new List<int>();

        ToggleButton readIc;
        private string LoginSystemType;
        private string sampYingshi;

        Spinner samp_goodsname;//货物名称
        Spinner sp_adresssname;//地点
        Spinner samp_mode;//取样方式
        EditText samp_weight;//取样数量
        Spinner samp_util;//单位
        Spinner samp_yshi;//萤石取样位置(前/后半车)


        private string sampAdressId;
        private string nvGoodCode;//nc存货编码
        private string sampModeId;
        private double sampWeight;//数量
        private string username;//取样人
        Sql sql = new Sql();
        BasicInfo bsinf = new BasicInfo();
        DataTable dt;

        Boolean isSpinnerFirst = true;//下拉菜单是否为第一个选项
        CustomProgressDialog dialog;
        #endregion

        #region 创建 Activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
           

           //使用加载等待页面
            dialog = CustomProgressDialog.CreateDialog(this);
            dialog.OnWindowFocusChanged(true);


            LoginSystemType = Intent.GetStringExtra("LoginSystemType");
            username = Intent.GetStringExtra("username");

            EditText sampCount = FindViewById<EditText>(Resource.Id.sampCount);

            readIc = FindViewById<ToggleButton>(Resource.Id.btReadic);
            readIc.Click += icRead;

            dialog.Show();

            //地点下拉菜单  
            sampAdressName = bsinf.selectAdress();
            sp_adresssname = FindViewById<Spinner>(Resource.Id.sampAdress);
            //填充数据库数据
            ArrayAdapter<string> adapter_adressName = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, sampAdressName);
            adapter_adressName.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sp_adresssname.Adapter = adapter_adressName;
            sp_adresssname.ItemSelected += adress_ItemSelected;

            //业务类型
            samp_mode = FindViewById<Spinner>(Resource.Id.sampMode);
            //填充xml文件数据
            ArrayAdapter adapter_mode = ArrayAdapter.CreateFromResource(this, Resource.Array.SampMode, Android.Resource.Layout.SimpleSpinnerItem);
            adapter_mode.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            samp_mode.Adapter = adapter_mode;
            samp_mode.Prompt = "请选择";
            samp_mode.ItemSelected += selectMode_ItemSelected;

            //数量
            samp_weight = FindViewById<EditText>(Resource.Id.sampCount);
            if (samp_weight.Text == "")
            {
                 samp_weight.Text = "0.00";
            }
            else
            {
                sampWeight = Convert.ToDouble(samp_weight.Text);
            }

            //单位
            samp_util = FindViewById<Spinner>(Resource.Id.sampunilt);
            sampUtile = bsinf.selectUtil();
            //填充数据
            ArrayAdapter adapter_utile = ArrayAdapter.CreateFromResource(this, Resource.Array.SampUtile, Android.Resource.Layout.SimpleSpinnerItem);
            adapter_utile.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            samp_util.Adapter = adapter_utile;
            //sp_goodsname.ItemSelected += TargetSpinner_ItemSelected;

            //萤石取样位置
            samp_yshi = FindViewById<Spinner>(Resource.Id.sampYshi);
            ArrayAdapter adpter_yshi = ArrayAdapter.CreateFromResource(this, Resource.Array.SampYshi, Android.Resource.Layout.SimpleSpinnerItem);
            adpter_yshi.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            samp_yshi.Adapter = adpter_yshi;
         
        }
        #endregion

        protected override void OnResume()
        {
            base.OnResume();
            if (dialog.IsShowing)
            {
                dialog.Dismiss();
            }
        }

        #region 地点下拉菜单选择 获取地点id
        private void adress_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;

            sampGoodsName.Clear();//清空数据
            goodsIdList.Clear();//清空数据

            //isSpinnerFirst = true;
            sql.getcon2();
            string select_adressId = "select pk from QSB_Place where cPlaceName = '" + CurSpinner.SelectedItem.ToString() + "'";
            dt = sql.GetTable(select_adressId);
            sampAdressId = dt.Rows[0]["pk"].ToString();

            //根据地点id从表QSI_PlaceGoodsRel 查找对应得货物名称 填充到货物下拉菜单
            string select_goodsId = "select iGoods_pk from QSI_PlaceGoodsRel where iPlace_pk ='" + sampAdressId + "'";
            dt = sql.GetTable(select_goodsId);

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                goodsIdList.Add(Convert.ToInt32(dt.Rows[i]["iGoods_pk"].ToString()));
             }
            //根据货物id 再表QSB_Goods中查找货物名称
            //将list 数据转为数组

            string tmp = "";
            for (int j = 0; j < goodsIdList.Count; j++)
            {
                tmp += goodsIdList[j].ToString() + ",";
            }
           tmp = tmp.TrimEnd(',');
           string sql_goodsName = "select cInvName from QSB_Goods where pk in (" + tmp + ")";
                dt = sql.GetTable(sql_goodsName);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    sampGoodsName.Add(dt.Rows[i]["cInvName"].ToString());
                }
                sql.closecon();
                //绑定货物下拉框
                samp_goodsname = FindViewById<Spinner>(Resource.Id.sampGoods);
                //填充数据
                ArrayAdapter adapter_goodsname = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, sampGoodsName);
                adapter_goodsname.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                samp_goodsname.Adapter = adapter_goodsname;
                samp_goodsname.ItemSelected += goodsName_ItemSelected;
        }
        #endregion

        #region 货物下拉框选择 获取货物id
        private void goodsName_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            if (isSpinnerFirst == true)
            {
                isSpinnerFirst = false;
            }
            else
            {
                sql.getcon2();
                string sql_goodsId = "select cInvCode from QSB_Goods  where cInvName = '" + CurSpinner.SelectedItem.ToString() + "'";
                dt = sql.GetTable(sql_goodsId);
                nvGoodCode = dt.Rows[0]["cInvCode"].ToString();

                sql.closecon();
            } 
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
            intent.PutExtra("samp_adressId", sampAdressId);
            intent.PutExtra("samp_adressName", sp_adresssname.SelectedItem.ToString());
            intent.PutExtra("samp_goodsName", samp_goodsname.SelectedItem.ToString());
            intent.PutExtra("samp_goodsCode", nvGoodCode);//nc存货编码
            intent.PutExtra("samp_modeName", samp_mode.SelectedItem.ToString());
            intent.PutExtra("samp_modeId",sampModeId);
            intent.PutExtra("samp_weight", sampWeight);
            intent.PutExtra("samp_utileName", samp_util.SelectedItem.ToString());
            intent.PutExtra("sampYingshi", samp_yshi.SelectedItem.ToString());//萤石取样位置(前/后半车)
 
            StartActivity(intent);
        }
        #endregion

        #region 取样方式下拉菜单选择事件 获取对应得id
        private void selectMode_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            sql.getcon2();
            string select_modeId = "select pk from QQB_BusiType where cBusiName = '" + CurSpinner.SelectedItem.ToString() + "'";
            dt = sql.GetTable(select_modeId);
            sampModeId = dt.Rows[0]["pk"].ToString();
            sql.closecon();
        }
        #endregion
    }
}