using System;
using System.Data;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace FTJL
{
    [Activity(Label = "LimeStoneActivity")]
    public class LimeStoneActivity : Activity
    {
        #region 变量
        List<string> sampModeName = new List<string>();//取样方式
        List<string> sampAdressName = new List<string>();//取样地点
        List<string> sampGoodsName = new List<string>();//取样货物
        List<string> sampUtile = new List<string>();//货物单位
        List<int> goodsIdList = new List<int>();

        ToggleButton readIc;
        private string LoginSystemType;

        Spinner samp_goodsname;//货物名称
        Spinner samp_mode;//取样方式
        EditText samp_adress;//取样地点

        private string sampAdressId;
        private string nvGoodCode;//nc存货编码

        Sql sql = new Sql();
        BasicInfo bsinf = new BasicInfo();
        DataTable dt;

        Boolean isSpinnerFirst = true;//下拉菜单是否为第一个选项
        #endregion

        #region 创建activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Limestone);
            // Create your application here
            sampAdressId = "21";
            samp_adress = FindViewById<EditText>(Resource.Id.sampadress);
            samp_adress.Text = "综合料场北";
            sql.getcon2();
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
            samp_goodsname = FindViewById<Spinner>(Resource.Id.goodsName);
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
    }
}