namespace Sorting.Interface
{ 
    public class WorkTaskModel
    {
        /// <summary>
        /// 工作站ID
        /// </summary>
        public string StationId = "";
        /// <summary>
        /// Agv派送任务号
        /// </summary>
        public long AgvDistributeID = 0;
        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskID="";              //任务ID
        /// <summary>
        /// 当前小车ID
        /// </summary>
        public string AGVID = "";          //当前小车ID
        /// <summary>
        /// 扫码枪扫码内容
        /// </summary>
        public string Barcode="";             //扫码枪扫码内容
        /// <summary>
        /// 当前SKU
        /// </summary>
        public string SKU = "";            //当前SKU
        /// <summary>
        /// SKU名字
        /// </summary>
        public string SKUName = "";        //SKU名字
        /// <summary>
        /// AGV可承载该商品数量
        /// </summary>
        public int AgvLoadingQty = -1;     //AGV可承载该商品数量
        /// <summary>
        /// 重量，单位：毫克
        /// </summary>
        public int Weight = 0;             //重量，单位：毫克
        /// <summary>
        /// 波次ID
        /// </summary>
        public string WaveID = "";         //波次ID
        /// <summary>
        /// 订单ID
        /// </summary>
        public string OrderID="";          //订单ID
        /// <summary>
        /// 分拣口ID
        /// </summary>
        public string ChuteId = "";        //分拣口ID
        /// <summary>
        /// 当前任务数量
        /// </summary>
        public int Quantity = 1;           //当前任务数量
        /// <summary>
        /// 最大任务数量
        /// </summary>
        public int MaxQuantity = 1;       //最大任务数量
        /// <summary>
        /// 调度发车任务ID
        /// </summary>
        public string DepartureTaskID = "";   //调度发车任务ID
        /// <summary>
        /// 请求发车ID
        /// </summary>
        public string DepartureRequsetID = "";//请求发车ID
        /// <summary>
        /// 请求驱离空车发车ID
        /// </summary>
        public string DriveOutRobotReqID = "";//请求驱离空车发车ID

        /// <summary>
        /// 是否有效性检查
        /// </summary>
        /// <returns></returns>
        public bool Isvalidity()
        {
            if (string.IsNullOrEmpty(TaskID) || string.IsNullOrEmpty(AGVID) || string.IsNullOrEmpty(Barcode) || string.IsNullOrEmpty(SKU)|| string.IsNullOrEmpty(ChuteId) ||string.IsNullOrEmpty(OrderID))
            {
                return false;
            }
            return true;
        }

        public WorkTaskModel DeepCopy()
        {
            return new WorkTaskModel()
            {
                StationId = StationId,
                AgvDistributeID = AgvDistributeID,
                AGVID = AGVID,
                Barcode = Barcode,
                SKU = SKU,
                SKUName = SKUName,
                AgvLoadingQty =AgvLoadingQty,
                Weight = Weight,
                WaveID = WaveID,
                OrderID = OrderID,
                TaskID = TaskID,
                ChuteId = ChuteId,
                Quantity = Quantity,
                MaxQuantity = MaxQuantity,
                DepartureTaskID = DepartureTaskID

            };
        }
        public void InitionUUID(string supplyTaskId)
        {//车辆到达时生成一次

            DepartureRequsetID = supplyTaskId;
            DriveOutRobotReqID = supplyTaskId;
        }
        public void ClearUUID()
        {//车辆到达时生成一次

            DepartureRequsetID = "";
            DriveOutRobotReqID = "";
        }

    }
}
