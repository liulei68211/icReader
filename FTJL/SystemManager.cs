using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Widget;
using System.IO;
using Mono.Data.Sqlite;

namespace FTJL
{
    [Activity(Label = "丰田肥业手持终端系统管理")]
    public class SystemManager : Activity
    {
      
        string _systemtype;
        string _username;
        string _userpassword;
        List<String> list = new List<String>();//用户名称

        #region Activity创建
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            //通常每个Activity对应1个Layout，在onCreate时指定layout（否则引用的还是main的layout）,然后调用startActivity：
            SetContentView(Resource.Layout.SystemManager);

            //系统类型数据填充
            Spinner TargetSpinner = FindViewById<Spinner>(Resource.Id.sSystemList);
            ArrayAdapter adapter1 = ArrayAdapter.CreateFromResource(this, Resource.Array.SystemType, Android.Resource.Layout.SimpleSpinnerItem);
            adapter1.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            TargetSpinner.Adapter = adapter1;
            TargetSpinner.Prompt = "请选择";
            TargetSpinner.ItemSelected += selectSystemType_ItemSelected;

            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            bool exists = File.Exists(db);
            if (exists)
            {
                var conn = new SqliteConnection("Data Source=" + db);
                var strSql = "select * from UserTB ";
                var cmd = new SqliteCommand(strSql, conn);
                cmd.CommandType = System.Data.CommandType.Text;
                try
                {
                    conn.Open();
                    if (conn.State == System.Data.ConnectionState.Open)
                    {
                        SqliteDataReader sdr = cmd.ExecuteReader();
                        while (sdr.Read())
                        {
                            list.Add(Convert.ToString(sdr["UserName"]));
                        }

                        Spinner UserNameList = FindViewById<Spinner>(Resource.Id.sUserList);
                        ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, list);
                        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                        UserNameList.Adapter = adapter;

                        //Spinner UserNameList1 = FindViewById<Spinner>(Resource.Id.s1UserName);
                        //UserNameList1.Adapter = adapter;
                    }
                    else CommonFunction.ShowMessage("数据库打开失败！", this, true);
                }
                catch (Exception ex)
                {
                    CommonFunction.ShowMessage("获取用户名失败！", this, true);
                    Finish();
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
            //系统类型选中的名称
            Spinner selectSystemName = FindViewById<Spinner>(Resource.Id.sSystemList);
            selectSystemName.ItemSelected += selectSystemType_ItemSelected;

            //用户下拉框选中的名称
            Spinner selectUserName = FindViewById<Spinner>(Resource.Id.sUserList);
            selectUserName.ItemSelected += selectUserName_ItemSelected;

            Button adduser = FindViewById<Button>(Resource.Id.bAddUser);
            adduser.Click += adduser_Click;

            Button modifyuser = FindViewById<Button>(Resource.Id.bModifyUser);
            modifyuser.Click += modifyuser_Click;

            Button deluser = FindViewById<Button>(Resource.Id.bDelUser);
            deluser.Click += deluser_Click;

            Button exit = FindViewById<Button>(Resource.Id.bExit);
            exit.Click += exit_Click;
        }
        #endregion

        #region 系统类型选择
        private void selectSystemType_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            var conn = new SqliteConnection("Data Source=" + db);
            var strSql = "select * from UserTB where SystemType = '" + CurSpinner.SelectedItem.ToString() + "' ";
            var cmd = new SqliteCommand(strSql, conn);
            cmd.CommandType = System.Data.CommandType.Text;

            list.Clear();
            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    SqliteDataReader sdr = cmd.ExecuteReader();
                    while (sdr.Read())
                    {
                        list.Add(Convert.ToString(sdr["UserName"]));
                    }                  
                    Spinner UserNameList = FindViewById<Spinner>(Resource.Id.sUserList);
                    ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, list);
                    adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                     UserNameList.Adapter = adapter;                  
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
        #endregion

        #region 用户名称选择
        /// <summary>
        /// 用户名称选择
        /// </summary>
        private void selectUserName_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            var CurSpinner = (Spinner)sender;
            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            var conn = new SqliteConnection("Data Source=" + db);
            var strSql = "select * from UserTB where UserName = '" + CurSpinner.SelectedItem.ToString() + "' ";
            var cmd = new SqliteCommand(strSql, conn);
            cmd.CommandType = System.Data.CommandType.Text;
            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    SqliteDataReader sdr = cmd.ExecuteReader();
                    if (sdr.Read())
                    {
                        // EditText systemtype = FindViewById<EditText>(Resource.Id.sSystemType);
                        //systemtype.Text = Convert.ToString(sdr["SystemType"]);
                       Spinner systemType = FindViewById<Spinner>(Resource.Id.sSystemList);

                        EditText username = FindViewById<EditText>(Resource.Id.sUserName);
                        username.Text = Convert.ToString(sdr["UserName"]);

                        EditText userpassword = FindViewById<EditText>(Resource.Id.sUserPassword);
                        userpassword.Text = Convert.ToString(sdr["UserPassword"]);

                       // _systemtype = systemtype.Text;
                        _username = username.Text;
                        _userpassword = userpassword.Text;
                    }
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
        #endregion

        #region 查询数据库返回真假值
        private bool Bool_QueryInfo(SqliteConnection SqlConn, string SqlStr)
        {
            bool result = false;
            try
            {
                if (SqlConn.State != System.Data.ConnectionState.Open)
                {
                    SqlConn.Open();
                }
                if (SqlConn.State == System.Data.ConnectionState.Open)
                {
                    SqliteCommand cmd = new SqliteCommand(SqlStr, SqlConn);
                    SqliteDataReader reader = cmd.ExecuteReader();
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

        #region 添加用户
        /// <summary>
        /// 添加用户
        /// </summary>
        private void adduser_Click(object sender, EventArgs e)
        {
            //系统类别框对应的数据
            Spinner systemType = FindViewById<Spinner>(Resource.Id.sSystemList);

            EditText username = FindViewById<EditText>(Resource.Id.sUserName);

            EditText userpassword = FindViewById<EditText>(Resource.Id.sUserPassword);            

            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            var conn = new SqliteConnection("Data Source=" + db);
            var strSql = "insert into UserTB (SystemType,UserName,UserPassword) values('" + systemType.SelectedItem.ToString() + "','" + username.Text + "','" + userpassword.Text + "') ";
            var cmd = new SqliteCommand(strSql, conn);
            cmd.CommandType = System.Data.CommandType.Text;
            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    string query = "select * from UserTB where SystemType = '" + systemType.SelectedItem.ToString() + "' and UserName = '" + username.Text + "' ";
                    if (Bool_QueryInfo(conn, query))
                    {
                        CommonFunction.ShowMessage("系统已经存在该用户，禁止重复添加！", this, true);
                    }
                    else
                    {
                        if (cmd.ExecuteNonQuery() > 0)
                        {
                            list.Add(username.Text);
                            Spinner UserNameList = FindViewById<Spinner>(Resource.Id.sUserList);
                            ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, list);
                            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                            UserNameList.Adapter = adapter;

                            CommonFunction.ShowMessage("OK！", this, true);
                        }
                    }
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
        #endregion

        #region 删除用户
        /// <summary>
        /// 删除用户
        /// </summary>
        private void deluser_Click(object sender, EventArgs e)
        {
            // EditText systemtype = FindViewById<EditText>(Resource.Id.sSystemType);
            //系统类别框对应的数据
            Spinner systemType = FindViewById<Spinner>(Resource.Id.sSystemList);

            EditText username = FindViewById<EditText>(Resource.Id.sUserName);

            EditText userpassword = FindViewById<EditText>(Resource.Id.sUserPassword);

            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            var conn = new SqliteConnection("Data Source=" + db);
            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    if (systemType.SelectedItem.ToString() == "管理系统")
                    {
                        string count = "0";
                        var strSql1 = "select count(UserName) from UserTB where SystemType = '" + systemType.SelectedItem.ToString() + "' ";
                        var cmd1 = new SqliteCommand(strSql1, conn);
                        cmd1.CommandType = System.Data.CommandType.Text;
                        SqliteDataReader sdr = cmd1.ExecuteReader();
                        try
                        {
                            if (sdr.Read()) count = sdr[0].ToString();
                        }
                        catch (Exception ex)
                        {
                            CommonFunction.ShowMessage("获取管理系统人员信息异常！", this, true);
                            return;
                        }
                        finally
                        {
                            sdr.Close();
                        }
                        if (Convert.ToInt32(count) < 2)
                        {
                            CommonFunction.ShowMessage("管理员人数不能小于1，禁止删除", this, true);
                            return;
                        }
                    }

                    var strSql2 = "delete from UserTB where SystemType = '" + systemType.SelectedItem.ToString() + "' and  UserName = '" + _username + "' ";
                    var cmd2 = new SqliteCommand(strSql2, conn);

                    cmd2.CommandType = System.Data.CommandType.Text;
                    if (cmd2.ExecuteNonQuery() > 0)
                    {
                        list.Remove(username.Text);
                        Spinner UserNameList = FindViewById<Spinner>(Resource.Id.sUserList);
                        ArrayAdapter<string> adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerDropDownItem, list);
                        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
                        UserNameList.Adapter = adapter;

                        CommonFunction.ShowMessage("OK！", this, true);
                    }
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
        #endregion

        #region 修改用户
        /// <summary>
        /// 修改用户
        /// </summary>
        private void modifyuser_Click(object sender, EventArgs e)
        {
            //系统类别框对应的数据
            Spinner systemType = FindViewById<Spinner>(Resource.Id.sSystemList);

            EditText username = FindViewById<EditText>(Resource.Id.sUserName);

            EditText userpassword = FindViewById<EditText>(Resource.Id.sUserPassword);

            string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            string db = Path.Combine(documents, MainActivity.DataBaseName);
            var conn = new SqliteConnection("Data Source=" + db);
            var strSql = "update UserTB set UserPassword = '" + userpassword.Text + "', UserName ='"+ username.Text+ "' where SystemType = '" + systemType.SelectedItem.ToString() + "' and UserName = '" + _username + "' ";
            var cmd = new SqliteCommand(strSql, conn);
            cmd.CommandType = System.Data.CommandType.Text;
            try
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    if (cmd.ExecuteNonQuery() > 0)
                    {                       
                        CommonFunction.ShowMessage("OK！", this, true);
                    }
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
        #endregion

        #region 退出系统
        /// <summary>
        /// 退出系统
        /// </summary>
        private void exit_Click(object sender, EventArgs e)
        {
            Finish();
        }
        #endregion
    }
}