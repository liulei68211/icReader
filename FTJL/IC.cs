using System;
using Android.Content;
using System.Text.RegularExpressions;
using Android.Nfc;
    

namespace FTJL
{
    class IC
    {
        #region 自定义变量

        /// <summary>
        /// 十六进制字符
        /// </summary>
        static char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        /// <summary>
        /// 密码A
        /// </summary>
        public string ma = "ffffffffffff";
        /// <summary>
        /// 密码B
        /// </summary>
        public string mb = "ffffffffffff";
        /// <summary>
        /// 控制位
        /// </summary>
        public string kzw = string.Empty;
        /// <summary>
        /// 卡序列号
        /// </summary>
        public string icserial = string.Empty;
        /// <summary>
        /// 是否读到卡
        /// </summary>
        public bool isReadIcOK;
        /// <summary>
        /// 扇区数组
        /// </summary>
        public int[] iSec;
        /// <summary>
        /// 块数组
        /// </summary>
        public int[] iBlack = new int[64];

        //定义变量
        public string[,] IcDatas = new string[5, 3]; //存储卡内数据   456 8910 121314 161718 202122
        public byte[] sectors = { 1, 2, 3, 4, 5 };                //将要读取的扇区编号


        //IcDatas[0, 0] - 1扇区 4块 从第一位开始：8位卡号 + 24位车号（解密后为正常车牌照）
        //IcDatas[0, 1] - 1扇区 5块 从第一位开始：5个标志位（/业务类型/计量类型/计重方式/退货标识/理重实重标识 + 1个待用标志 + 26位计量单号（读取）
        //IcDatas[0, 2] - 1扇区 6块 从第一位开始：12位卸货时间 + 6位扣吨数 + 12位装货时间（卸货时不操作改数据，取样时写入）

        //IcDatas[1, 0] - 2扇区 8块 从第一位开始：26位采购订单号（解密后为13位）
        //IcDatas[1, 1] - 2扇区 9块 从第一位开始：6位毛重 + 10位毛重时间 / 6位皮重 + 10位皮重时间 + 16位计量员（卸货时写入操作员名字）
        //IcDatas[1, 2] - 2扇区 10块

        // IcDatas[2, 0] - 3扇区 12块 从第一位开始：（卸货：客商名称，占用3个数据块）/（取样：2位取样代码，12位取样时间，18位取样人员名字,占用1个数据块）（读取）
        //IcDatas[2, 1] - 3扇区 13块   第一位 取样卡/计量卡 标识位 1为取样卡
        // IcDatas[2, 2] - 3扇区 14块

        //  IcDatas[3, 0] - 4扇区 16块 从第一位开始：存货名称，占用3个数据块（不读取）
        //IcDatas[3, 1] - 4扇区 17块
        // IcDatas[3, 2] - 4扇区 18块

        //  IcDatas[4, 0] - 5扇区 20块 从第一位开始：规格、型号，占用3个数据块（格式：规格!型号）（不读取）
        //IcDatas[4, 1] - 5扇区 21块
        // IcDatas[4, 2] - 5扇区 22块

        #endregion

        #region IC卡初始化
        public IC()
        {
            
            mb = StrToHex("797A77");//密码
            iSec = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
            for (int i = 0; i < 64; i++)
            {
                iBlack[i] = i;
            }
            isReadIcOK = false;
        }
        #endregion

        #region byte转16进制
        public byte GetHexBitsValue(byte ch)
        {
            try
            {
                byte sz = 0;
                if (ch <= '9' && ch >= '0')
                    sz = (byte)(ch - 0x30);
                if (ch <= 'F' && ch >= 'A')
                    sz = (byte)(ch - 0x37);
                if (ch <= 'f' && ch >= 'a')
                    sz = (byte)(ch - 0x57);
                return sz;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
        #endregion

        #region 转16进制
        public string ToHexString(byte[] bytes)
        {
            try
            {
                char[] chars = new char[bytes.Length * 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    int b = bytes[i];
                    chars[i * 2] = hexDigits[b >> 4];
                    chars[i * 2 + 1] = hexDigits[b & 0xF];
                }
                return new string(chars);
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region
        /// 从汉字转换到16进制
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string GetHexFromChs(string s)
        {
            if ((s.Length % 2) != 0)
            {
                s += " ";//空格
            }

            System.Text.Encoding chs = System.Text.Encoding.GetEncoding("gb2312");

            byte[] bytes = chs.GetBytes(s);

            string str = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                str += string.Format("{0:X}", bytes[i]);
            }

            return str;
        }
        #endregion

        #region 解密
        public  string HexToStr(string Msg)
        {
            try
            {
                byte[] buff = new byte[Msg.Length / 2];
                string Message = "";
                for (int i = 0; i < buff.Length; i++)
                {
                    buff[i] = byte.Parse(Msg.Substring(i * 2, 2),
                       System.Globalization.NumberStyles.HexNumber);
                }

                System.Text.Encoding chs = System.Text.Encoding.GetEncoding("gb2312");
                Message = chs.GetString(buff);
                return Message;
            }
            catch (Exception ex)
            {
                return "";
            }

        }
        #endregion

        #region 加密
        public string StrToHex(string Msg)
        {
            try
            {
                byte[] bytes = System.Text.Encoding.Default.GetBytes(Msg);
                string str = "";
                for (int i = 0; i < bytes.Length; i++)
                {
                    str += string.Format("{0:X}", bytes[i]);
                }
                return str;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 16进制转10进制
        public byte[] ToDigitsBytes(string theHex)
        {
            try
            {
                byte[] bytes = new byte[theHex.Length / 2 + (((theHex.Length % 2) > 0) ? 1 : 0)];
                for (int i = 0; i < bytes.Length; i++)
                {
                    char lowbits = theHex[i * 2];
                    char highbits;
                    if ((i * 2 + 1) < theHex.Length)
                        highbits = theHex[i * 2 + 1];
                    else
                        highbits = '0';
                    int a = (int)GetHexBitsValue((byte)lowbits);
                    int b = (int)GetHexBitsValue((byte)highbits);
                    bytes[i] = (byte)((a << 4) + b);
                }
                return bytes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region 去掉/0字符
        public string delZeroChar(string s)
        {
            try
            {
                string ss = @s;
                char[] c = ss.ToCharArray();
                int i;
                for (i = 0; i < c.Length; i++)
                {
                    if (c[i] == '\0')
                    {
                        ss = s.Substring(0, i);
                        return ss;
                    }
                }
                return ss;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 去除非法字符
        public string clearChar(string strTmp)
        {
            int l = strTmp.Length;
            while ((!isValidString(strTmp)) && (strTmp != ""))
            {
                strTmp = strTmp.Remove(l - 1);
                l--;
            }
            return strTmp;
        }
        #endregion

        #region 判断是否存在非法字符（数字、字母、汉字除外）
        private bool isValidString(string tmp)
        {
            return Regex.IsMatch(tmp, @"^[A-Za-z0-9\u4e00-\u9fa5]+$");
        }
        #endregion

        #region 加密卡号并存储
        /// <summary>
        /// 加密卡号并存储
        /// </summary>
        public string GetEncryptSequenceNumber(string tmp)
        {
            try
            {
                string card = tmp.Substring(0, 8);
                string car = StrToHex(tmp.Substring(8));
                int len = card.Length + car.Length;
                byte[] c = ToDigitsBytes(card + car);
                int xy = 0;
                for (int i = 0; i < c.Length; i++)
                {
                    xy += c[i];
                }
                string str = card + car + xy.ToString().Substring(0, 2);
                string data = str.PadRight(30, 'f') + len.ToString();
                return data;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 解密卡号
        /// <summary>
        /// 解密卡号
        /// </summary>
        public string GetDecryptSequenceNumber(string tmp)
       {
            try
            {
                string l = tmp.Substring(30);//从第30位开始截取
                //string cardtmp = tmp.Substring(0, 8);
                string cardtmp = tmp.Substring(0, 12);
               string cartmp = HexToStr(tmp.Substring(12, Convert.ToInt32(l) - 12));
               // string cartmp = HexToStr(tmp.Substring(8, Convert.ToInt32(l) - 8));
                return cardtmp + cartmp;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        public string GetDecryptSequenceNumberBZ(string tmp)
        {
            try
            {
                string l = tmp.Substring(30);
               
                string cartmp = HexToStr(tmp.Substring(0, Convert.ToInt32(l)));
                //string cartmp = HexToStr(tmp.Substring(8, Convert.ToInt32(l) - 8));
                return cartmp;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public string GetDecryptSequenceNumberBZ1(string tmp)
        {
            try
            {
                string l = tmp.Substring(31);

                string cartmp = HexToStr(tmp.Substring(0, Convert.ToInt32(l)));
                //string cartmp = HexToStr(tmp.Substring(8, Convert.ToInt32(l) - 8));
                return cartmp;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        #endregion

        #region 寻卡
        /// <summary>
        /// 寻卡
        /// </summary>
        public string FindIC(Intent intent)
        {
            try
            {
                byte[] myNFCID = intent.GetByteArrayExtra(NfcAdapter.ExtraId);
                string serial = ToHexString(myNFCID);
                return serial;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion

        #region 根据扇区信息获取车辆信息
        /// <summary>
        /// 根据扇区信息获取车辆信息
        /// </summary>
        public CarInfo GetCarInfo(string[,] icdatas)
        {
            CarInfo car = new CarInfo();
            try
            {
                #region 解析扇区数据

                //--------------内盘-------
               // string ssd = IcDatas[1, 2].Substring(0,4);
                //--------------------取样卡/计量卡标识 3区13块 [2,1]
                car.isSampIc = IcDatas[2,1].Substring(0,1);
                if (car.isSampIc !="1")
                {
                    #region 新的计量系统
                    car.CardID = GetDecryptSequenceNumber(IcDatas[0, 0]).Substring(0, 12);//计量单号
                    car.CarPlate = GetDecryptSequenceNumber(IcDatas[0, 0]).Substring(12);//车牌号
                    #endregion
                    //--------------------------读取IC卡号和车牌号并显示。      1扇区4块：8位卡号+24位车号
                    //car.CardID = GetDecryptSequenceNumber(IcDatas[0, 0]).Substring(0, 8);
                    //car.CarPlate = GetDecryptSequenceNumber(IcDatas[0, 0]).Substring(8);//旧的读取方法 
                    //  car.CarPlate = GetDecryptSequenceNumber(IcDatas[0, 0]).Substring(12);//新的读取方法

                    //读取业务类型 计量方式 计量类型 
                    //业务类型 位于1扇区5块第0位;  计量类型 位于1扇区5块第1位;计重方式 位于1扇区5块第2位;退货标识 位于1扇区5块第3位;
                    //0业务类型 1计量类型 2计重方式 3 退货标识


                    //string ywlx = IcDatas[1, 2].Substring(0, 1);
                    // string ddd = IcDatas[1, 2].Substring(1,3);
                    string ywlx = IcDatas[0, 1].Substring(0, 1);
                    //if (ywlx == "0") car.BusinessType = "采购进厂";
                    //else if (ywlx == "1") car.BusinessType = "采购退货";
                    //else if (ywlx == "2") car.BusinessType = "销售出厂";
                    //else if (ywlx == "3") car.BusinessType = "销售退货";
                    //else if (ywlx == "4") car.BusinessType = "其他";


                    //新系统
                    if (ywlx == "1") car.BusinessType = "采购进厂";
                    else if (ywlx == "2") car.BusinessType = "采购退货";
                    else if (ywlx == "5") car.BusinessType = "销售出厂";
                    else if (ywlx == "6") car.BusinessType = "销售退货";
                    else if (ywlx == "7") car.BusinessType = "其他";


                    //--------------------------计量类型	0：毛重 1：皮重
                    car.MeasurementType = IcDatas[0, 1].Substring(1, 1) == "0" ? "毛重" : "皮重";

                    //业务类型  0：采购进厂1：采购退货2：销售出厂3：销售退货 4：其他

                    //--------------------------计重方式	
                    //业务类型为采购进厂或销售出厂时0：一车一货1：一车多货
                    //业务类型为采购退货时：0：卸后退(全车退)。1：半车退
                    //业务类型为销售退货时：0：一车一货1：新安退库
                    //业务类型为其他时：0：普通 1：定期皮
                    string jzfs = IcDatas[0, 1].Substring(2, 1);        //2017-05-28 修改Substring(1, 2)为Substring(2, 1) ：原因：读取的卡都是一车多货
                    if (car.BusinessType == "采购进厂" || car.BusinessType == "销售出厂")  
                    {
                        car.MeasureMode = (jzfs == "0") ? "一车一货" : "一车多货";
                    }
                    else if (car.BusinessType == "采购退货")
                    {
                        car.MeasureMode = (jzfs == "0") ? "卸后退(全车退)" : "半车退";
                    }
                    else if (car.BusinessType == "销售退货")
                    {
                        car.MeasureMode = (jzfs == "0") ? "一车一货" : "新安退库";
                    }
                    else if (car.BusinessType == "其他")
                    {
                        car.MeasureMode = (jzfs == "0") ? "普通" : "定期皮";
                    }

                    string sdsss = IcDatas[0, 1].Substring(6);
                    car.MeasureID = HexToStr(IcDatas[0, 1].Substring(6)).Trim().Replace("\0", "");   //计量单号

                    //car.MeasureID = HexToStr(IcDatas[0, 1].Substring(8));   //计量单号（旧的读取方法 一区5块）
                   //  car.MeasureID = HexToStr(IcDatas[0, 0].Substring(0,12));   //计量单号（新读取方法 一区4块）
                                                                       

                    //2扇区8块 13位采购订单号 目前来看 好像这个订单号基本上不读取了 2016-6-2 不再读取
                    //Car.OrderID = ic.HexToStr(IcDatas[1, 0].Substring(0, 26));

                    //IcDatas[2,0] - 3扇区 12块 从第一位开始：（卸货：客商名称，占用3个数据块）/（取样：2位取样代码，12位取样时间，18位取样人员名字,占用1个数据块）
                    //IcDatas[2,1] - 3扇区 13块
                    //IcDatas[2,2] - 3扇区 14块
                    //--------------------------    3扇区12、13、14块：		客商名称或者取样标识 统一更改为取样标识
                    if (MainActivity.LoginSystemType == "卸货系统")
                    {
                        //string ksmc = HexToStr(IcDatas[2, 0] + IcDatas[2, 1] + IcDatas[2, 2]);
                        //car.SendUnit = ksmc.Trim();
                        car.QualityType = IcDatas[2, 0].Substring(0, 2);
                    }
                    else if (MainActivity.LoginSystemType == "质检系统") //3扇区12块 前两个字符为取样类型
                    {

                        // car.QualityType = IcDatas[2, 0].Substring(0, 2);
                        car.QualityType = IcDatas[0,1].Substring(4,1); //新系统
                        //string ddd  = IcDatas[0,1].Substring(3,1); //新系统
                        //string dddd  = IcDatas[0,1].Substring(0,1); //新系统
                        //string ddddd = IcDatas[0,1].Substring(1,1); //新系统
                        //string dddddd = IcDatas[0,1].Substring(4,1); //新系统
                        //string ddddddd = IcDatas[0,1].Substring(5, 1); //新系统
                    }
                    else if (MainActivity.LoginSystemType == "汽车取样") //3扇区12块 前两个字符为取样类型
                    {
                        car.QualityType = IcDatas[2, 0].Substring(0, 2);
                    }
                    else if (MainActivity.LoginSystemType == "白灰取样") //3扇区12块 前两个字符为取样类型
                    {
                        car.QualityType = IcDatas[2, 0].Substring(0, 2);
                    }
                    else if (MainActivity.LoginSystemType == "取样") //3扇区12块 前两个字符为取样类型
                    {
                        car.QualityType = IcDatas[2, 0].Substring(0, 2);
                    }


                    //--------------------------    4扇区：		存货名称 2016-6-2 不再读取
                    //string chmc = HexToStr(IcDatas[3, 0] + IcDatas[3, 1] + IcDatas[3, 2]);
                    //car.MeterielID = chmc.Trim();

                    //--------------------------    5扇区：		规格+"!"+型号  （此数据可能为空）2016-6-2 不再读取
                    //string ggxh = HexToStr(IcDatas[4, 0] + IcDatas[4, 1] + IcDatas[4, 2]);
                    //int index = ggxh.IndexOf("!", 0);
                    //if (index > 0)
                    //{
                    //    car.Specification = ggxh.Substring(0, index - 1);
                    //    car.Model = ggxh.Substring(index + 1);
                    //}

                    //--------------------------    2扇区9块：	6位毛重+10位毛重时间/6位皮重+10位皮重时间+16位计量员 （此数据可能为空）
                    string zljly = IcDatas[1, 1];
                    try
                    {
                        car.Gross = Convert.ToInt32(zljly.Substring(0, 6));
                    }
                    catch (Exception ex)
                    {
                        car.Gross = 0;
                    }
                    try
                    {
                        car.GrossDateTime = DateTime.ParseExact(zljly.Substring(6, 10), "MMddHHmmss", System.Globalization.CultureInfo.CurrentCulture).ToString();
                    }
                    catch (Exception ex)
                    {
                        car.GrossDateTime = null;
                    }
                    //try
                    //{
                    //    car.Person = HexToStr(zljly.Substring(16, 16)).Trim();    //2017-6-23 屏蔽，原因：这个参数用不到
                    //}
                    //catch (Exception ex)
                    //{
                    //    car.Person = null;
                    //}
                }
                #endregion
                return car;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
    }
}