using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using FTJL.Adapter;
namespace FTJL
{
    [Activity(Label = "生过烧标记")]
    public class BaihuiListInfo : Activity
    {
        Sql sql = new Sql();
        private List<string> carNum_list = new List<string>();
        private List<string> goodsName_list = new List<string>();
        private List<string> factoryName_list = new List<string>();
        private List<string> measureId_list = new List<string>();
        private List<int> pkId_list = new List<int>();

        private List<TableItem1> data;//listview绑定的数据
        private MatalAdapter adapter;
        private ListView mylist;
        private ToggleButton btSign,btSignExtr,btSignExtr2;//样车标记

        private TextView text_sampfactory;
        private TextView text_carnumber;
        private TextView text_goodsname;
        private CheckBox checkbox;

        private List<string> rdmList = new List<string>();
        private string tmnw,firstSignTm;//第一次样车标记时间;
        private string update_sql;

        #region 创建activity
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetContentView(Resource.Layout.MetalListInfo);

            mylist = FindViewById<ListView>(Resource.Id.listView1);
            tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            //连接远程数据库 查询已经取过的白灰样
            sql.getcon2();

            string select_sql = "";
            measureId_list.Clear();
            carNum_list.Clear();
            goodsName_list.Clear();
            factoryName_list.Clear();

            // 判断是否标记过
            select_sql = "select count(isSign) as count,dSignTime from QQQ_Sampe_h where dStart like '%" + tmnw.Substring(0, 10) + "%' and iPlace_pk = 22 and isSign = 1 group by dSignTime ";
            DataTable mydt = sql.GetTable(select_sql);
            int isSignCount = 0;

            if (mydt.Rows.Count == 0)
            {
                isSignCount = 0;
                firstSignTm = "";
            }
            else
            {
                isSignCount = Convert.ToInt32(mydt.Rows[0]["count"].ToString().Trim());
                firstSignTm = mydt.Rows[0]["dSignTime"].ToString().Trim();
            }
            

            if (isSignCount == 1)
            {
                //只查询第一次标记后的样
                select_sql = "select b.cCarCode as carCode,b.cSupplierName as factoryName,b.cMeasure_ID as measureId,b.cInvName as goodsName  from QQQ_Sampe_h a " +
                         "left join QQQ_Sample_b b on a.pk_h = b.pk_h " +
                         "where a.dStart like '%" + tmnw.Substring(0, 10) + "%' and a.iPlace_pk = 22 and b.bSample = 1 and a.isSign = 0 and dReadIC > '" + firstSignTm + "'";
            }
            if(isSignCount == 2)
            {
                //查询当天所有标记过的样
                select_sql = "select b.cCarCode as carCode,b.cSupplierName as factoryName,b.cMeasure_ID as measureId,b.cInvName as goodsName  from QQQ_Sampe_h a " +
                        "left join QQQ_Sample_b b on a.pk_h = b.pk_h " +
                        "where a.dStart like '%" + tmnw.Substring(0, 10) + "%' and a.iPlace_pk = 22 and b.bSample = 1 and a.isSign = 1 ";
            }
            if(isSignCount == 0)
            {
                //查询当天所有未标记的样
                select_sql = "select b.cCarCode as carCode,b.cSupplierName as factoryName,b.cMeasure_ID as measureId,b.cInvName as goodsName  from QQQ_Sampe_h a " +
                        "left join QQQ_Sample_b b on a.pk_h = b.pk_h " +
                        "where a.dStart like '%" + tmnw.Substring(0, 10) + "%' and a.iPlace_pk = 22 and b.bSample = 1 and a.isSign = 0 ";
            }


            DataTable dt = sql.GetTable(select_sql);

            if (dt.Rows.Count == 0)
            {
                measureId_list.Add("");
                carNum_list.Add("");
                goodsName_list.Add("");
                factoryName_list.Add("");
            }
            else
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    measureId_list.Add(dt.Rows[i]["measureId"].ToString());
                    carNum_list.Add(dt.Rows[i]["carCode"].ToString());
                    goodsName_list.Add(dt.Rows[i]["goodsName"].ToString());
                    factoryName_list.Add(dt.Rows[i]["factoryName"].ToString());
                }
            }
           
            sql.closecon();
            data = new List<TableItem1>();
            data.Clear();
            for (int i = 0; i < measureId_list.Count; i++)
            {
                data.Add(new TableItem1(factoryName_list[i], carNum_list[i]+measureId_list[i], goodsName_list[i]));
            }
            adapter = new MatalAdapter(this, data);
            mylist.Adapter = adapter;

            btSign = FindViewById<ToggleButton>(Resource.Id.btSamp);
            btSign.Click += signClick;
            btSignExtr = FindViewById<ToggleButton>(Resource.Id.btSampExtra);
            btSignExtr.Click += signExtrClick;
            btSignExtr2 = FindViewById<ToggleButton>(Resource.Id.btSamp2);
            btSignExtr2.Click += afterSampClick;

            btSign.Enabled = true;
            btSign.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));
          
            btSignExtr2.Enabled = true;
            btSignExtr2.SetBackgroundColor(Android.Graphics.Color.ParseColor("#BD2B32"));

            btSignExtr.Enabled = true;
            btSignExtr.SetBackgroundColor(Android.Graphics.Color.ParseColor("#3F91DA"));
        }
        #endregion

        #region 抽取标记样车（第二次）按钮
        private void afterSampClick(object sender,EventArgs e)
        {          
            carNum_list.Clear();
            //需要先判断中午是否标记过生过烧

            //判断是否已经有样车标记记录 
            sql.getcon2();
            int isSignCount = 0;
            string select_sql = "select count(isSign) as count from QQQ_Sampe_h where  dStart like '%" + tmnw.Substring(0, 10) + "%' and iPlace_pk = 22 and isSign = 1";
            DataTable mydt = sql.GetTable(select_sql);

            for (int i=0;i<mydt.Rows.Count;i++)
            {
                isSignCount = Convert.ToInt32(mydt.Rows[i]["count"].ToString().Trim());
            }

            //抽取标记样车 >=2车 标记两个样
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
                    carNum_list.Add(text_carnumber.Text.Substring(0, 7));
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取checkbox数据失败", this, true);
                }
            }

            Random rdm = new Random();
            string rdmNumber = "";

            if (carNum_list.Count >= 2)
            {
                if (isSignCount > 0)
                {
                    //已经做过标记 只抽取一个
                    rdmNumber = carNum_list[rdm.Next(0, carNum_list.Count)];
                    rdmList.Add(rdmNumber);
                    CommonFunction.ShowMessage("需要标记的样车车号为" + rdmList[0] + "", this, true);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        //没有标记 抽两个
                        rdmNumber = carNum_list[rdm.Next(0, carNum_list.Count)];
                        //抽取两个不一样的车号
                        while (rdmList.Contains(rdmNumber))
                        {
                            rdmNumber = carNum_list[rdm.Next(0, carNum_list.Count)];
                        }
                        rdmList.Add(rdmNumber);//将生成的随机数添加到集合对象中
                    }
                    CommonFunction.ShowMessage("需要标记的样车车号为" + rdmList[0] +"，" +rdmList[1], this, true);
                }              
            }
        }
        #endregion

        #region 抽取标记样车(第一次)按钮
        private void signClick(object sender,EventArgs e)
        {           
            //第一次抽取 >=2车的抽一个样
            Random rdm = new Random();
            string rdmNumber = "";

            if (carNum_list.Count == 1)
            {
                rdmList.Add(carNum_list[0]);
                CommonFunction.ShowMessage("请点击蓝色保存按钮",this,true);
            }
            if (carNum_list.Count >= 2)
            {
                rdmNumber = carNum_list[rdm.Next(0, carNum_list.Count)];
                rdmList.Add(rdmNumber);
                CommonFunction.ShowMessage("需要标记的样车车号为" + rdmList[0], this, true);
            }
        }
        #endregion

        #region 样车标记按钮
        private void signExtrClick(object sender,EventArgs e)
        {
            tmnw = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            sql.getcon2();
            string rdmstr = "";
            string pkSelect = "";

            if (rdmList.Count == 1)
            {
                pkSelect = "select A.pk_h as pkId from QQQ_Sample_b A WHERE A.cCarCode = " + rdmList[0] + " and dReadIC like '%" + tmnw.Substring(0, 10) + "%'";
                DataTable dt = sql.GetTable(pkSelect);
                pkId_list.Add(Convert.ToInt32(dt.Rows[0]["pkId"].ToString()));

                string strs = "update QQQ_Sampe_h set isSign = 1,dSignTime = '" + tmnw + "' where iPlace_pk = 22 and dStart like '%" + tmnw.Substring(0, 10) + "%' and pk_h='" + pkId_list[0] + "'";

                if (!sql.getsqlcom(strs))
                {
                    CommonFunction.ShowMessage("更新失败，请重新操作", this, true);
                }
                else
                {
                    CommonFunction.ShowMessage("操作成功", this, true);
                    sql.closecon();
                }
            }
            else
            {
                //根据抽取的车号 在表QQQ_Samp_b中查询对应的pk_h
                for (int i = 0; i < rdmList.Count; i++)
                {
                    rdmstr += "'" + rdmList[i] + "'" + ",";
                }
                rdmstr = rdmstr.TrimEnd(',');
                pkSelect = "select A.pk_h as pkId from QQQ_Sample_b A WHERE A.cCarCode IN (" + rdmstr + ") and dReadIC like '%" + tmnw.Substring(0, 10) + "%'";
                DataTable dt = sql.GetTable(pkSelect);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    pkId_list.Add(Convert.ToInt32(dt.Rows[i]["pkId"].ToString()));
                }

                update_sql = "";
                string str = "";
                for (int i = 0; i < pkId_list.Count; i++)
                {
                    str = "update QQQ_Sampe_h set isSign = 1,dSignTime = '" + tmnw + "' where iPlace_pk = 22 and dStart like '%" + tmnw.Substring(0, 10) + "%' and pk_h='" + pkId_list[i] + "'";
                    update_sql += str + ";";
                }

                if (!sql.getsqlcom(update_sql))
                {
                    CommonFunction.ShowMessage("更新失败，请重新操作", this, true);
                }
                else
                {
                    CommonFunction.ShowMessage("操作成功", this, true);
                    sql.closecon();
                }
            }    
        }
        #endregion

    }
}