namespace Sorting.Interface
{
    public class RequetTaskParam
    {
        /// <summary>
        /// 工作站
        /// </summary>
        public string StationId;
        /// <summary>
        /// SKU
        /// </summary>
        public string Sku;
        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode;
        /// <summary>
        /// 建议发车数量
        /// </summary>
        public int Completed;
        /// <summary>
        /// 
        /// </summary>
        public string CloseType;
    }
    public class SupplyTask
    {
        public string TaskId;
        public string ChuteId;
        public string WaveId;
        public string OrderId;
        public int Count;
    }
}
