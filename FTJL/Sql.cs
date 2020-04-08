using System.IO;
using System.Data;
using System.Data.SqlClient;
using Mono.Data.Sqlite;


namespace FTJL
{
    class Sql
    {
        public SqlConnection mycon;
        public SqlCommand mycom;
        public SqlDataReader mydr;
        public SqlDataAdapter myda;

        public SqliteConnection sqlliteCon;
        public SqliteCommand sqliteCom;
        public SqliteDataReader sqliteRd;

        public static string DataBaseName = "UserDB.db3";//数据库名称
        static string documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string db = Path.Combine(documents, DataBaseName);
        string constr ;

        public SqlConnection getcon()
        {
            constr = "server=221.13.152.202,1433;database=JGCM;uid=sa;pwd=HNJG123hnjg";
            mycon = new SqlConnection(constr);
            mycon.Open();
            return mycon;
        }

        public SqlConnection getcon2()
        {
            constr = "server=221.13.152.202,1433;database=JGCM;uid=sa;pwd=HNJG123hnjg";
            mycon = new SqlConnection(constr);
            mycon.Open();
            return mycon;
        }
        public void closecon()
        {
            if (mycon.State == ConnectionState.Open)
            {
                mycon.Dispose();
                mycon.Close();
            }
        }
        public  bool getsqlcom(string SQLstr)
        {
            bool result = false;
            mycom = new SqlCommand(SQLstr, mycon);
            if (mycom.ExecuteNonQuery() > 0)
            {
                result = true;
            }
            return result;
           
            // mycom.Dispose();
        }
        public  SqlDataReader getrd(string SQLstr)
        {
            mycom = new SqlCommand(SQLstr, mycon);
            mydr = mycom.ExecuteReader();
            return mydr;
        }

        #region 生成数据表
        public  DataTable GetTable(string sqlstr)//生成数据表
        {
            myda = new SqlDataAdapter(sqlstr, mycon);
            // DataSet myds = new DataSet();
            DataTable mydt = new DataTable();
            myda.Fill(mydt);
            return mydt;
        }
        #endregion

        #region sqlite数据库连接
        public SqliteConnection sqlitecon(string documents,string db)
        {
            sqlliteCon = new SqliteConnection("Data Source=" + db);
            sqlliteCon.Open();
            return sqlliteCon;
        }
        #endregion

        #region sqlite执行sql命令
        public void sqlitesqlcom(string SQLstr)
        {
            sqliteCom = new SqliteCommand(SQLstr, sqlliteCon);
            sqliteCom.ExecuteNonQuery();
            // mycom.Dispose();
        }
        #endregion

        #region sqlite读取数据
        public SqliteDataReader sqliteRdd(string SQLstr)
        {
            sqliteCom = new SqliteCommand(SQLstr, sqlliteCon);
            sqliteRd = sqliteCom.ExecuteReader();
            return sqliteRd;
        }
        #endregion
    }


}