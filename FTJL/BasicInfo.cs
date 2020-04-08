using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace FTJL
{
    class BasicInfo
    {
        #region 变量
        /// <summary>
        /// 货物名称 和 id 
        /// </summary>
        public List<string> ncGoodsNameList = new List<string>();
        /// <summary>
        /// 取样方式 和 id
        /// </summary>
        public int sampModeId;
        public List<string> sampModeNameList = new List<string>();
        /// <summary>
        /// 取样地点 和 Code
        /// </summary>
        public List<string> sampAdressList = new List<string>(); 
        public int adressID;

        /// <summary>
        /// 货物单位
        /// </summary>
        public List<string> sampUtileList = new List<string>();

        /// <summary>
        /// 返回的自增值
        /// </summary>
        public int pk;
        /// <summary>
        /// 理论来料数
        /// </summary>
        public int iTheoryNum;
        /// <summary>
        /// 实际来料数
        /// </summary>
        public int iJoinSampleNum;

        Sql sql = new Sql();
        
        SqlDataReader sqlrd;
        public DataTable dt;

        string strErr = string.Empty;
        string update_seqId, insert_samph, insert_sampb;
        string tsqlList;

        int intResult;//sql语句执行结果
       private string tmnw;
        #endregion

        #region 查询单位
        public List<string> selectUtil()
        {
            sql.getcon2();
            string sql_util = "select util_name from QQB_Utile order by id";
            dt = sql.GetTable(sql_util);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                sampUtileList.Add(dt.Rows[i]["util_name"].ToString().Trim());
            }
            sql.closecon();
            return sampUtileList;
        }
        #endregion

        #region 查询取样地点
        public List<string> selectAdress()
        {
            sql.getcon2();
            string sql_adress;
            sql_adress = "select pk,cPlaceName from QSB_Place ";
            dt = sql.GetTable(sql_adress);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                sampAdressList.Add(dt.Rows[i]["cPlaceName"].ToString().Trim());
            }
            sql.closecon();
            return sampAdressList;
            
        }
        #endregion

        #region 查询货物信息
        public List<string> selectGoods()
        {
            sql.getcon2();
            string sql_goods;
            sql_goods = "select cInvCode,cInvName from QSB_Goods  ";
            dt = sql.GetTable(sql_goods);
            for (int i=0;i<dt.Rows.Count;i++)
            {
                ncGoodsNameList.Add(dt.Rows[i]["cInvName"].ToString().Trim());
            }
           sql.closecon();
           return ncGoodsNameList;
        }
        #endregion

        #region 在货物表中查询 理论取样车（来样数量）理论参与车辆数据
        public void selectGoodsNum(string goodsName,out int num1,out int num2 )
        {
            string sql_goodsNum;
            sql_goodsNum = " select iTheorySampleNum, iTheoryJoinSampleNum from QSB_Goods " +
                           " where cInvName = '"+ goodsName + "'";
            sql.getcon2();
            sql.getsqlcom(sql_goodsNum);

            sqlrd = sql.getrd(sql_goodsNum);
            if (sqlrd.Read())
            {
                iTheoryNum = Convert.ToInt32(sqlrd["iTheorySampleNum"].ToString());
                iJoinSampleNum = Convert.ToInt32(sqlrd["iTheoryJoinSampleNum"].ToString());
            }
            else
            {
                iTheoryNum = 0;
                iJoinSampleNum = 0;
            }
            num1 = iTheoryNum;
            num2 = iJoinSampleNum;
            sqlrd.Close();
            sql.closecon();
        }
        #endregion

        #region 查询取样方式
        public List<string> selectSampMode()
        {
            sql.getcon2();
            string sql_mode = "select pk,cBusiName from QQB_BusiType ";
            dt = sql.GetTable(sql_mode);

            for (int i=0;i<dt.Rows.Count;i++)
            {
                sampModeNameList.Add(dt.Rows[i]["cBusiName"].ToString());
            }
            sql.closecon();
            return sampModeNameList;
        }
        #endregion

        #region 测试
        public void test()
        {
            string insert_samph = "insert into tt " +
                                "([text]) " +
                                " values " +
                                "('dd')SELECT @@IDENTITY AS returnName";
            sql.getcon2();
            dt = sql.GetTable(insert_samph);

            int pk = Convert.ToInt32(dt.Rows[0]["returnName"].ToString());


          
        }
        #endregion

        #region  用事务向取样主表 子表中插入数据  
        public void InsertSampInfo(int placepk,string seqId,
                                   double sampCount,string sampUnit, 
                                   int adressCode, int sampModeId, string cNCGoodsCode, int theoryNum, 
                                   int joinSampleNum, string dStart, string username, int isSampFinish,int isSign,
                                   List<string> measureId, List<string> orderId, List<string> sampCarNum, List<string> supplieCode,
                                   List<string> supplieName, List<string> goodsCode, List<string> goodsName,string sampTime,
                                   List<int> carBags, List<string> sampCarBags,List<string> getBag,List<int> isSamp,string SampYshi,out string strError)     
        {
            SqlConnection SqlConn = sql.getcon2();
            //if (SqlConn.State == ConnectionState.Closed)
            //    SqlConn.Open();
            //else
            //{
            //    SqlConn.Close();
            //    SqlConn.Open();
            //}
            SqlCommand SqlCmd = SqlConn.CreateCommand();
            SqlTransaction SqlTran = SqlConn.BeginTransaction();

            try
            {
                SqlCmd.Connection = sql.mycon;
                SqlCmd.Transaction = SqlTran;

                //更新seqId
                update_seqId = @"update [JGCM].[dbo].[QSS_SeqID] set [cSeqID] = '"+ seqId + "' where [iPlace_pk]= '"+ placepk + "'";
                string cSampleCode = seqId;

                sql.getcon2();
                #region
                // sql.getsqlcom(update_seqId);
                //向主表 和 子表中插入相应的信息
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
                #endregion
                insert_samph = @"insert into QQQ_Sampe_h " +
                                "(cSampleCode,nSampleQuantity,cMeasUnit,iPlace_pk,iBusiType_pk,cInVCode,iTheorySampleNum,iTheoryJoinSampleNum,dStart,cNewUser,bFinish,isSign) " +
                                " values " +
                                "('" + cSampleCode + "','" + sampCount + "','" + sampUnit + "','" + adressCode + "','" + sampModeId + "'," +
                                "'" + cNCGoodsCode + "','" + theoryNum + "','" + joinSampleNum + "','" + sampTime + "','" + username + "','"+isSampFinish+"','"+isSign+"')SELECT @@IDENTITY AS returnName";
                //sql.getsqlcom(insert_samph);
                dt = sql.GetTable(insert_samph);

                pk = Convert.ToInt32( dt.Rows[0]["returnName"].ToString());
                #region
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
                if (measureId.Count == 1)
                {
                    insert_sampb = @"insert into QQQ_Sample_b " +
                              "(pk_h,cMeasure_ID,cCarCode,cOrderID,CSupplierCode,CSupplierName,cInvCode,cInvName,dReadIC,iCarBagNum,iSampleBagNum,getbag,bSample,cSampYshi) " +
                              " values " +
                              "('" + pk + "','" + measureId[0] + "','" + sampCarNum[0] + "','" + orderId[0] + "','" + supplieCode[0] + "','" + supplieName[0] + "','" + goodsCode[0] + "','" + goodsName[0] + "','" + sampTime + "','" + carBags[0] + "','" + sampCarBags[0] + "','"+getBag[0]+"','"+isSamp[0]+"','"+SampYshi+"')";
                    tsqlList = update_seqId + ";" + insert_sampb;

                    SqlCmd.CommandText = tsqlList;
                    intResult = SqlCmd.ExecuteNonQuery();
                    if (intResult !=2)
                    {
                        strErr = "写入数据库失败";// +strUText;
                        SqlTran.Rollback();
                    }
                    else
                    {
                        SqlTran.Commit();
                    }                  
                }
                else
                {
                    insert_sampb = "";
                    string str;
                    for (int i=0;i<measureId.Count;i++)
                    {
                          str = @"insert into QQQ_Sample_b(pk_h,cMeasure_ID,cCarCode,cOrderID,CSupplierCode,CSupplierName,cInvCode,cInvName,dReadIC,iCarBagNum,iSampleBagNum,getbag,bSample,cSampYshi)values " +
                        "('" + pk + "','" + measureId[i] + "','" + sampCarNum[i] + "','" + orderId[i] + "','" + supplieCode[i] + "','" + supplieName[i] + "'," + "'" + goodsCode[i] + "','" + goodsName[i] + "','" + sampTime + "','" + carBags[i] + "','" + sampCarBags[i] + "','"+getBag[i]+"','" + isSamp[i] + "','"+ SampYshi + "')";
                        insert_sampb += str + ";";
                    }
                  
                    //拼接多个语句
                    tsqlList = update_seqId + ";" + insert_sampb;

                    SqlCmd.CommandText = tsqlList;
                    intResult = SqlCmd.ExecuteNonQuery();
                    if (intResult != (1 + measureId.Count))
                    {
                        strErr = "写入数据库失败";// +strUText;
                        SqlTran.Rollback();
                    }
                    else
                    {
                        SqlTran.Commit();
                    }
                }
               // tsqlList = update_seqId + ";"+ insert_samph +";" +insert_sampb;
                //tsqlList = update_seqId + ";" +insert_sampb;
               // SqlCmd.CommandText = tsqlList;
            }
            catch (Exception expt)
            {
                SqlTran.Rollback();
                strErr = expt.Message;
                //SaveLog.SaveSysLog(strSerPath, "JGILS", strLocIP, "SaveSqlLog", "返回计量单号发生异常,执行语句:" + strUText + ",异常信息:" + expt.Message + ",时间:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            finally
            {
                sql.mycon.Close();
                //sql.closecon();
            }
            strError = strErr;
        }
        #endregion

        #region 根据车号查询最近的 相关货物信息
        public void selectCmInfo(string carNum,string sampId, out string cMeasureID,out string purchaseOrderID, out string factoryCode, out string factoryName, out string goodsCode, out string goodsName)
        {
            tmnw = System.DateTime.Now.ToString("yyyy-MM-dd");
            sql.getcon2();
            string sql_selectCm = " SELECT C_PurchaseOrderID 采购订单号,C_SendFactoryID 发货单位编码,C_SendFactoryDes 发货单位名称," +
                                  " C_MaterielID 存货编码,C_MaterielDes 存货名称 " +
                                  " FROM [JYILSDB].[dbo].[TM_MeasureInfo] where [C_CarryNo] = '"+ carNum + "' and [C_MeasureDocID]='"+ sampId + "' and C_GrossTime like '%"+tmnw+"%'";

            dt = sql.GetTable(sql_selectCm);
            if (dt.Rows.Count == 0)
            {
                //没有找到符合条件的采购订单号 根据车号查询最新的计量单号
                sql_selectCm = " SELECT TOP 1  C_MeasureDocID 计量单号,C_PurchaseOrderID 采购订单号,C_SendFactoryID 发货单位编码,C_SendFactoryDes 发货单位名称, C_MaterielID 存货编码,C_MaterielDes 存货名称  FROM [JYILSDB].[dbo].[TM_MeasureInfo] where [C_CarryNo] = '" + carNum + "'  and C_GrossTime like '%" + tmnw + "%' ORDER BY AutoID DESC";

                dt = sql.GetTable(sql_selectCm);
                cMeasureID = dt.Rows[0]["计量单号"].ToString();
                purchaseOrderID = dt.Rows[0]["采购订单号"].ToString();
                factoryCode = dt.Rows[0]["发货单位编码"].ToString();
                factoryName = dt.Rows[0]["发货单位名称"].ToString();
                goodsCode = dt.Rows[0]["存货编码"].ToString();
                goodsName = dt.Rows[0]["存货名称"].ToString();
                sql.closecon();
            }
            else
            {
                cMeasureID = sampId;
                purchaseOrderID = dt.Rows[0]["采购订单号"].ToString();
                factoryCode = dt.Rows[0]["发货单位编码"].ToString();
                factoryName = dt.Rows[0]["发货单位名称"].ToString();
                goodsCode = dt.Rows[0]["存货编码"].ToString();
                goodsName = dt.Rows[0]["存货名称"].ToString();
                sql.closecon();
            }      
        }
        #endregion

        #region  用事务向取样主表 子表中插入数据  2
        public void InsertSampInfo2(int placepk, string seqId,
                                   double sampCount, string sampUnit,
                                   int adressCode, int sampModeId, string cNCGoodsCode, int theoryNum,
                                   int joinSampleNum, DateTime dStart, string username, int isSampFinish,
                                   string measureId, string orderId, string sampCarNum, string supplieCode,
                                   string supplieName, string goodsCode, string goodsName, DateTime sampTime,
                                   int carBags, string sampCarBags,int isSamp,
                                   out string strError)
        {

            SqlConnection SqlConn = sql.getcon2();
            //if (SqlConn.State == ConnectionState.Closed)
            //    SqlConn.Open();
            //else
            //{
            //    SqlConn.Close();
            //    SqlConn.Open();
            //}
            SqlCommand SqlCmd = SqlConn.CreateCommand();
            SqlTransaction SqlTran = SqlConn.BeginTransaction();

            try
            {
                SqlCmd.Connection = SqlConn;
                SqlCmd.Transaction = SqlTran;

                //更新seqId
                update_seqId = @"update  [JGCM].[dbo].[QSS_SeqID] set [cSeqID] = '" + seqId + "' where [iPlace_pk]= '" + placepk + "'";
                string cSampleCode = seqId;

                sql.getcon2();
                //dt = sql.GetTable(update_seqId);
                //int pk = Convert.ToInt32(dt.Rows[0]["returnName"].ToString());

                // sql.getsqlcom(update_seqId);
                //向主表 和 子表中插入相应的信息
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
                insert_samph = @"insert into QQQ_Sampe_h " +
                                "(cSampleCode,nSampleQuantity,cMeasUnit,iPlace_pk,iBusiType_pk,cInVCode,iTheorySampleNum,iTheoryJoinSampleNum,dStart,cNewUser) " +
                                " values " +
                                "('" + cSampleCode + "','" + sampCount + "','" + sampUnit + "','" + adressCode + "','" + sampModeId + "'," +
                                "'" + cNCGoodsCode + "','" + theoryNum + "','" + joinSampleNum + "','" + sampTime + "','" + username + "')SELECT @@IDENTITY AS returnName";
                //sql.getsqlcom(insert_samph);
                dt = sql.GetTable(insert_samph);

                pk = Convert.ToInt32(dt.Rows[0]["returnName"].ToString());
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

                insert_sampb = @"insert into QQQ_Sample_b " +
                                "(pk_h,cMeasure_ID,cCarCode,cOrderID,CSupplierCode,CSupplierName,cInvCode,cInvName,dReadIC,iCarBagNum,iSampleBagNum) " +
                                " values " +
                                "('" + pk + "','" + measureId + "','" + sampCarNum + "','" + orderId + "','" + supplieCode + "','" + supplieName + "'," +
                                "'" + goodsCode + "','" + goodsName + "','" + sampTime + "','" + carBags + "','" + sampCarBags + "')";

                // tsqlList = update_seqId + ";"+ insert_samph +";" +insert_sampb;
                tsqlList = update_seqId + ";" + insert_sampb;

                SqlCmd.CommandText = tsqlList;
                int intResult = SqlCmd.ExecuteNonQuery();

                if (intResult != 2)
                {
                    strErr = "写入数据库失败";// +strUText;
                }
                SqlTran.Commit();
            }
            catch (Exception expt)
            {
                SqlTran.Rollback();
                strErr = expt.Message;
                //SaveLog.SaveSysLog(strSerPath, "JGILS", strLocIP, "SaveSqlLog", "返回计量单号发生异常,执行语句:" + strUText + ",异常信息:" + expt.Message + ",时间:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            finally
            {
                sql.mycon.Close();
                //sql.closecon();
            }
            strError = strErr;
        }
        #endregion
        //select dbo.Q_GetSeqID(2)如果返回null 表示：QSS_SeqID表中没有设备运行地址主键（iPlace_pk）！ 
    }
}