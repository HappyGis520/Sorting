using Jlib;

namespace Sorting.Interface
{
    /// <summary>
    /// 分拣流程
    /// </summary>
    public enum EnumSupplyFlow
    {
        [Caption("初始化")]
        Inition =0 ,                    //初始化  
        [Caption("AGV到达")]
        AgvArrived = 1,                 //AGV到达  
        [Caption("查询商品中")]
        QueryGoodsInfo = 2,             //查询商品中  
        [Caption("申请分拣任务中")]
        CreateSupplyTask = 3,           //创建分拣任务  
        [Caption("请求任务完成")]
        CreateSupplyTaskComplete = 4,   //请求任务完成 
        [Caption("发车中")]
        AutoSendRobot = 5,              //自动发车中    
        [Caption("发车中")]
        SendingRobot = 6,               //发车中    
        [Caption("发车失败")]
        SendRobotFail = 7,              //发车失败    
        [Caption("发车完成")]
        SendRobotComplete = 8,          //发车完成  
        [Caption("离岗确认中")]
        OffLine = 9,                    //离岗
        [Caption("驱离机器人")]
        DriveOutRobot = 10,             //驱离机器人

    }
}
