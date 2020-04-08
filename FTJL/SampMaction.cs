using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FTJL
{
    [Activity(Label = "SampMaction")]
    public class SampMaction : Activity
    {
        #region 变量
        Sql sql = new Sql();
        Spinner sampMation;
        Spinner feedOpen;
        Spinner boxCode;
        public string select_sql;
        DataTable dt;

        List<string> feedOpen_list = new List<string>();//下料口
        List<string> equiName_list = new List<string>();//取样机名称
        List<int> equiId_list = new List<int>();//取样机名称
        List<string> boxCode_list = new List<string>();//罐号
        #endregion
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.SampMaction);

            sampMation = FindViewById<Spinner>(Resource.Id.sampMaction);
            feedOpen = FindViewById<Spinner>(Resource.Id.feedOpen);
            boxCode = FindViewById<Spinner>(Resource.Id.boxCode);

            //远程数据表 
            sql.getcon2();
            select_sql = "select [cPlaceName] from [QSB_Place] where cPlaceName like '%取样机%'";

            dt = sql.GetTable(select_sql);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                //equiId_list.Add(Convert.ToInt32(dt.Rows[i]["pk"].ToString().Trim()));
                equiName_list.Add(dt.Rows[i]["cPlaceName"].ToString().Trim());//取样机
                //feedOpen_list.Add(dt.Rows[i]["EquiName"].ToString().Trim());
            }
            sql.closecon();

            //填充数据库数据 到取样机下拉框
            ArrayAdapter<string> adapter_equiName = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, equiName_list);
            adapter_equiName.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            sampMation.Adapter = adapter_equiName;
            sampMation.ItemSelected += equiName_ItemSelected;
        }

        #region 取样机选择事件
        private void equiName_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            feedOpen_list.Clear();
            sql.getcon2();
            //根据选中的取样机名称 查询对应得id
            select_sql = "select pk from QSB_Place where cPlaceName='" + CurSpinner.SelectedItem.ToString()+"'";
            dt = sql.GetTable(select_sql);

            int equiId = Convert.ToInt32(dt.Rows[0]["pk"].ToString().Trim());

            //根据得到的 id 从表 QQB_SamplePoint(取样机样点表) 中查询下料口
            select_sql = "select [cEquiName] from [QSB_Equipment] where [iPlace_pk]='" + equiId + "'";

            dt = sql.GetTable(select_sql);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                feedOpen_list.Add(dt.Rows[i]["cEquiName"].ToString().Trim());
            }

            //填充数据库数据 到下料口下拉框
            ArrayAdapter<string> adapter_feedOpen = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, feedOpen_list);
            adapter_feedOpen.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            feedOpen.Adapter = adapter_feedOpen;
            feedOpen.ItemSelected += sampMation_ItemSelected;
        }
        #endregion

        #region 下料口选择事件
        private void sampMation_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            //根据选中的下料口名称 查询对应得id  [QSB_Equipment]
            select_sql = "select pk from [QSB_Equipment] where [cEquiName]='" + CurSpinner.SelectedItem.ToString() + "'";
            dt = sql.GetTable(select_sql);

            int feedOpenId = Convert.ToInt32(dt.Rows[0]["pk"].ToString().Trim());

            //根据得到的下料口id 在表 [QQB_PotStatus] 中查询 罐号
            string boxCode_str = "";
            select_sql = "select [iPotCode] from [QQB_PotStatus] where [iEquipMent_pk]='" + feedOpenId + "'";

            dt = sql.GetTable(select_sql);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                boxCode_list.Add(dt.Rows[i]["iPotCode"].ToString().Trim());        
            }

            //填充数据库数据 到罐号下拉框
            ArrayAdapter<string> adapter_boxCode = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, boxCode_list);
            adapter_boxCode.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            boxCode.Adapter = adapter_boxCode;
          //  feedOpen.ItemSelected += sampMation_ItemSelected;
        }
        #endregion
    }
}