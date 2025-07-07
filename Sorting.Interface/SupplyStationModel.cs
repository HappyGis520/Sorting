namespace Sorting.Interface
{
    /// <summary>
    /// 分拣任务模型
    /// </summary>
    public class SupplyStationModel
    {
        public string OffLineReqID = "";                    //离线请求ID
        public string OnLineReqID = "";                     //返岗请求ID
        public bool IsLeft = false;                         //是否是左工作站
        public string ClientID = "001";                     //客户端编号
        public string WcsServerUrl = "";                    //WCS服务地址
        public string AccessToken = "";                     //登录凭证       
        public string LoginUserName = "";                   //登录用户名
        public string PlatformID = "";                      //平台ID
        public string StationID = "";                       //站点ID
        public string StationName = "";                     //站点名称
        public bool RCSConneted = false;                    //RCS连接状态
        public string Status = "";                          //状态信息

        public WorkTaskModel CurrentWorkTask =new WorkTaskModel();         //当前任务
        public WorkTaskModel PreWorkTask = new WorkTaskModel();            //上一个任务

    }
}
