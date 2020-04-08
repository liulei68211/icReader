using System;
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
   public class CarInfo
    {
        /// <summary>
        /// 车辆卡号
        /// </summary>
        public string CardID;
        /// <summary>
        /// 皮计量员
        /// </summary>
        public string TarePerson;
        /// <summary>
        /// 毛计量员
        /// </summary>
        public string GrossPerson;
        /// <summary>
        /// 计皮磅房
        /// </summary>
        public string TSite;
        /// <summary>
        /// 计毛磅房
        /// </summary>
        public string GSite;
        /// <summary>
        /// 扣吨
        /// </summary>
        public int Subtract = 0;
        /// <summary>
        /// 收货单位
        /// </summary>
        public string ReceiveUnit;
        /// <summary>
        /// 发货单位
        /// </summary>
        public string SendUnit;
        /// <summary>
        /// 业务类型 - 采购进厂 销售出厂 厂间调拨
        /// </summary>
        public string BusinessType;
        /// <summary>
        /// 计重方式 - 一车一皮 定期皮 一车多货 退货 
        /// </summary>
        public string MeasureMode;
        /// <summary>
        /// 计量类型 - 毛重计量 皮重计量
        /// </summary>
        public string MeasurementType;
        /// <summary>
        /// 车号
        /// </summary>
        public string CarPlate;
        /// <summary>
        /// 毛重
        /// </summary>
        public int Gross;
        /// <summary>
        /// 皮重
        /// </summary>
        public int Tare;
        /// <summary>
        /// 净重
        /// </summary>
        public int Sullte;
        /// <summary>
        /// 毛重时间
        /// </summary>
        public string GrossDateTime;
        /// <summary>
        /// 皮重时间
        /// </summary>
        public string TareDateTime;
        /// <summary>
        /// 净重时间
        /// </summary>
        public string SullteDateTime;
        /// <summary>
        /// 计量单号
        /// </summary>
        public string MeasureID;
        /// <summary>
        /// 运送物料
        /// </summary>
        public string MeterielID;
        /// <summary>
        /// 规格
        /// </summary>
        public string Specification;
        /// <summary>
        /// 型号
        /// </summary>
        public string Model;
        /// <summary>
        /// 操作员
        /// </summary>
        public string Person;
        /// <summary>
        /// 取样类型
        /// </summary>
        public string QualityType;   //2017-6-1 添加

        public string isSampIc;//取样卡与计量卡标识符
    }
}