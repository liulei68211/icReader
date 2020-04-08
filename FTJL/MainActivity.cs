using Android.App;
using Android.Widget;
using Android.OS;

using System;
using Android.Content;
using System.IO;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using Android.Nfc;


namespace FTJL
{
    [Activity(Name = "icReader.icReader.MainActivity", Label = "济钢取样", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public static string LoginSystemType;
        string username;

        /// <summary>
        /// 数据库完整路径和名称
        /// </summary>
        string db;

        /// <summary>
        /// 数据库是否存在
        /// </summary>
        bool IsExistsSqlDB = false;

        /// <summary>
        /// NFC模式
        /// </summary>
        NfcAdapter m_nfcAdapter;

        /// <summary>
        /// 数据库
        /// </summary>
        //string DataBaseName = @"\Android\data\IcReader.IcReader\files\UserDB.db3";
        public static string DataBaseName = "UserDB.db3";
      
        #region Activity创建
        protected override void OnCreate(Bundle bundle)
        {
            try
            {
                base.OnCreate(bundle);

                //展示页面
                SetContentView(Resource.Layout.Main);

                CommonFunction.mode = "NFC";

                //数据填充
                Spinner TargetSpinner = FindViewById<Spinner>(Resource.Id.mSystemType);
                ArrayAdapter adapter = ArrayAdapter.CreateFromResource(this, Resource.Array.SystemType, Android.Resource.Layout.SimpleSpinnerItem);
                adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

                TargetSpinner.Adapter = adapter;
                TargetSpinner.Prompt = "请选择";
                TargetSpinner.ItemSelected += TargetSpinner_ItemSelected;

                //数据库操作
                string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                db = Path.Combine(documents, DataBaseName);
                bool exists = File.Exists(db);
                //判断文件中是否存在 数据表db 如果存在 获取数据，如果不存在 重置系统创建数据表
                if (!exists)
                {
                    InitSqlDB(db);
                }
                else
                {
                    #region 获取数据 
                    IsExistsSqlDB = true;
                    var conn = new SqliteConnection("Data Source=" + db);
                    var cmd = new SqliteCommand("select * from LoginTB limit 1", conn);
                    cmd.CommandType = System.Data.CommandType.Text;
                    try
                    {
                        conn.Open();
                        if (conn.State == System.Data.ConnectionState.Open)
                        {
                            SqliteDataReader sdr = cmd.ExecuteReader();
                            if (sdr.Read())
                            {
                                int id = 0;
                                Spinner SystemSpinner = FindViewById<Spinner>(Resource.Id.mSystemType);
                                for (int i = 0; i < SystemSpinner.Count; i++)
                                {
                                    if (SystemSpinner.GetItemAtPosition(i).ToString() == sdr["SystemType"].ToString())
                                    {
                                        id = i;
                                        break;
                                    }
                                }
                                SystemSpinner.SetSelection(id, true);
                                sdr.Close();
                            }
                        }
                        else CommonFunction.ShowMessage("数据库打开失败！", this, true);
                    }
                    catch (Exception ex)
                    {
                        CommonFunction.ShowMessage("数据库创建失败！", this, true);
                    }
                    finally
                    {
                        if (conn.State != System.Data.ConnectionState.Closed)
                        {
                            conn.Clone();
                        }
                        conn.Dispose();
                    }
                    #endregion
                }

                // 按钮事件
                Button bLogin = FindViewById<Button>(Resource.Id.bLogin);
                bLogin.Click += bLogin_Click;

                TextView txt_Exit = FindViewById<TextView>(Resource.Id.textView4);
                txt_Exit.Click += textExit;

                TextView txt_Init = FindViewById<TextView>(Resource.Id.textView5);
                txt_Init.Click += textInit;

                #region
                try
                {
                    IntentFilter ndef = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                    m_nfcAdapter = NfcAdapter.GetDefaultAdapter(this);

                    m_nfcAdapter.SetNdefPushMessage(CreateNdefMessageCallback(), this, this);
                }
                catch (NullReferenceException nullex)
                {
                    CommonFunction.ShowMessage(nullex.Message, this, true);
                }
                #endregion
            }
            catch (Exception ex)
            {
                CommonFunction.ShowMessage(ex.ToString(), this, true);
            }
        }
        #endregion

        #region 退出
        public void textExit(object sender, EventArgs e)
        {
            //Finish();
            Process.KillProcess(Android.OS.Process.MyPid());
        }
        #endregion

        #region 重置
        public void textInit(object sender, EventArgs e)
        {
            InitSqlDB(db);
        }
        #endregion

        #region ndefmessage消息
        public NdefMessage CreateNdefMessageCallback()
        {
            NdefMessage msg = new NdefMessage(new NdefRecord[] { NdefRecord.CreateApplicationRecord("icReader.FTJL") });
            return msg;
        }
        #endregion

        #region 系统类型选择
        /// <summary>
        /// 系统类型选择
        /// </summary>
        private void TargetSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            if (IsExistsSqlDB)
            {
                string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                string db = Path.Combine(documents, DataBaseName);
                var conn = new SqliteConnection("Data Source=" + db);
                var strSql = "select UserName from UserTB where SystemType = '" + CurSpinner.SelectedItem.ToString() + "' ";
                var cmd = new SqliteCommand(strSql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                try
                {
                    List<String> list = new List<String>();

                    conn.Open();
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        SqliteDataReader sdr = cmd.ExecuteReader();
                        while (sdr.Read())
                        {
                            list.Add(Convert.ToString(sdr["UserName"]));
                        }
                        Spinner UserNameSpinner = FindViewById<Spinner>(Resource.Id.mUserName);
                        ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, list);
                        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                        UserNameSpinner.Adapter = adapter;
                    }
                    else CommonFunction.ShowMessage("数据库打开失败！", this, true);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取用户名失败！", this, true);
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

        #region 登录系统
        /// <summary>
        /// 登录系统
        /// </summary>
        private void bLogin_Click(object sender, EventArgs e)
        {
            if (IsExistsSqlDB)
            {
                EditText userpassword = FindViewById<EditText>(Resource.Id.mUserPassword);
                Spinner systemtype = FindViewById<Spinner>(Resource.Id.mSystemType);
                Spinner username = FindViewById<Spinner>(Resource.Id.mUserName);

                string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
                string db = Path.Combine(documents, DataBaseName);
                var conn = new SqliteConnection("Data Source=" + db);
                var strSql = "select * from UserTB where SystemType = '" + systemtype.SelectedItem.ToString()
                                                                        + "' and UserName = '" + username.SelectedItem.ToString()
                                                                        + "' and UserPassword = '" + userpassword.Text + "'  ";
                try
                {
                    conn.Open();
                    SqliteCommand cmd = new SqliteCommand(strSql, conn);

                    #region 查询取样表信息
                    //string ssqqql = "select * from SampInfo";
                    //SqliteCommand cmdd = new SqliteCommand(ssqqql, conn);
                    //SqliteDataReader sdr = cmdd.ExecuteReader();
                    //cmdd.CommandType = System.Data.CommandType.Text;

                    //string strr = "";
                    //string sss = "";
                    //while (sdr.Read())
                    //{
                    //    strr = sdr["isSamp"].ToString();
                    //    sss += strr;
                    //}
                    //string ddd = sss;
                    #endregion

                    using (SqliteDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            dr.Close();
                            cmd.CommandText = "update LoginTB set SystemType = '" + systemtype.SelectedItem.ToString() + "' ,UserName = '" + username.SelectedItem.ToString() + "' ";
                            int c = cmd.ExecuteNonQuery();

                            LoginSystemType = systemtype.SelectedItem.ToString(); //2017-6-1 添加系统类型

                            if (systemtype.SelectedItem.ToString() == "管理系统")
                            {
                                StartActivity(typeof(SystemManager));
                            }
                            else if (systemtype.SelectedItem.ToString() == "汽车取样")
                            {
                                Intent intent = new Intent(this, typeof(CommonSampClass));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("LoginSystemType", systemtype.SelectedItem.ToString());
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "火车取样")
                            {
                                Intent intent = new Intent(this, typeof(TrainClass));
                                intent.PutExtra("LoginSystemType", systemtype.SelectedItem.ToString());
                                intent.SetFlags(ActivityFlags.ClearTop);
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "合金取样")
                            {
                                Intent intent = new Intent(this, typeof(RandomClass));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                intent.PutExtra("LoginSystemType", systemtype.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "合金取样")
                            {
                                Intent intent = new Intent(this, typeof(RandomClass));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "内盘计量")
                            {
                                Intent intent = new Intent(this, typeof(InsideDish));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "豫港取样")
                            {
                                Intent intent = new Intent(this, typeof(YugaSamp));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                             }
                            else if (systemtype.SelectedItem.ToString() == "取样机取样")
                            {
                                Intent intent = new Intent(this, typeof(SampMaction));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "卸货系统")
                            {
                                Intent intent = new Intent(this, typeof(Unload));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "质检系统")
                            {
                                Intent intent = new Intent(this, typeof(SampCm));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "白灰取样")
                            {
                                Intent intent = new Intent(this, typeof(BaiHuiActivity));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("LoginSystemType", systemtype.SelectedItem.ToString());
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                            else if (systemtype.SelectedItem.ToString() == "石灰石/白云石粉取样")
                            {
                                Intent intent = new Intent(this, typeof(LimeStoneActivity));
                                intent.SetFlags(ActivityFlags.ClearTop);
                                intent.PutExtra("LoginSystemType", systemtype.SelectedItem.ToString());
                                intent.PutExtra("username", username.SelectedItem.ToString());
                                StartActivity(intent);
                            }
                        }
                        else
                        {
                            CommonFunction.ShowMessage("请核对密码！", this, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("登录失败！", this, true);
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
      
        #region 重置系统
        /// <summary>
        /// 重置系统
        /// </summary>
        private void InitSqlDB(string dbname)
        {
            #region 创建数据库
            bool exists = File.Exists(dbname);
            if (!exists) File.Delete(dbname);

            SqliteConnection.CreateFile(dbname);
            var conn = new SqliteConnection("Data Source=" + dbname);
            string CommandText = "CREATE TABLE UserTB(SystemType varchar(20),UserName varchar(20),UserPassword varchar(20))";
            var cmd = new SqliteCommand(CommandText, conn);
            cmd.CommandType = System.Data.CommandType.Text;

            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    int c = cmd.ExecuteNonQuery();
                    if (c == 0)
                    {
                        cmd.CommandText = "CREATE TABLE LoginTB(SystemType varchar(20),UserName varchar(20) )";
                        c = cmd.ExecuteNonQuery();
                        if (c == 0)
                        {
                            cmd.CommandText = "INSERT into LoginTB(SystemType, UserName) values('汽车取样', '苗文娟' )";
                            cmd.ExecuteNonQuery();
                        }
                        if (c == 0)
                        {
                            cmd.CommandText = "CREATE TABLE SampMode(id int,modeName varchar(20))";
                            cmd.ExecuteNonQuery();
                        }
                        if (c == 0)
                        {
                            //同步远程内盘计量数据
                            cmd.CommandText = "CREATE TABLE CMOtherInfo(I_ClassID int,C_CarryNo varchar(30),C_SedFactoryID varchar(50),C_SedFactoryDes varchar(50),C_MaterielID varchar(50),C_MaterielDes varchar(50),C_RecFactoryID varchar(50),C_RecFactoryDes varchar(50),C_JZType varchar(50))";
                            cmd.ExecuteNonQuery();
    }

                        //cmd.CommandText = "delete from UserTB";
                        //cmd.ExecuteNonQuery();

                        cmd.CommandText = "delete from CMOtherInfo";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('管理系统', 'admin', '111111')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('卸货系统', '苗文娟', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('销售装货', '张三', '1')";
                        cmd.ExecuteNonQuery(); 
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('石灰石/白云石粉取样', '张三', '1')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','张晨光','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','田林朋','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','段延科','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','路志敏','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','杨龙','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','张超','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','李建','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','刘涛','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','李瑞杰','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','付峰','1')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','燕昭阳','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','郭鹏','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','李继峰','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','史二超','1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','王华东','1')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('质检系统','黄坤','1')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('汽车取样', '苗文娟', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('火车取样', '史二超', '1')";
                        cmd.ExecuteNonQuery();                

                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('合金取样', '李继峰', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('豫港取样', '田林朋', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('内盘计量', '王超', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('取样机取样', '张超', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('合金取样', '史二超', '1')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into UserTB(SystemType, UserName, UserPassword) values('白灰取样', '史二超', '1')";
                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "INSERT into SampMode(id, modeName) values (1, '正常')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into SampMode(id, modeName) values (2, '堆取')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into SampMode(id, modeName) values (3, '抽取')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into SampMode(id, modeName) values (4, '极端')";
                        cmd.ExecuteNonQuery();
                        cmd.CommandText = "INSERT into SampMode(id, modeName) values (5, '延用')";
                        cmd.ExecuteNonQuery();
                        CommonFunction.ShowMessage("系统数据库已经重置！", this, true);
                    }

                }
                else CommonFunction.ShowMessage("数据库打开失败！", this, true);
            }
            catch (Exception ex)
            {
                CommonFunction.ShowMessage("数据库创建失败！", this, true);
            }
            finally
            {
                if (conn.State != System.Data.ConnectionState.Closed)
                {
                    conn.Clone();
                }
                conn.Dispose();
            }
            #endregion
        }
        #endregion

        #region 操作提示对话框
        /// <summary>
        /// 操作提示对话框
        /// </summary>
        private void OperateDialogBox(object sender, EventArgs e)
        {
            var button = (Button)sender;
            //对话框
            var callDialog = new AlertDialog.Builder(this);

            //对话框内容
            callDialog.SetMessage("确定要进行" + button.Text + "吗?");

            //确定按钮
            callDialog.SetNeutralButton("确定", delegate { if (button.Text == "重置系统") InitSqlDB(db); });

            //取消按钮
            callDialog.SetNegativeButton("取消", delegate { });

            //显示对话框
            callDialog.Show();
        }
        #endregion
    }
}

