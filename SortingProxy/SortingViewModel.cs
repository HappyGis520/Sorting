/*******************************************************************
 * * 功   能：  站点视图模型
 * * 作   者：  Jason
 * * 编程语言： C# 
 * *******************************************************************/
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using JLeap.Common;
using Jlib;
using Jlib.Controls;
using Sorting.Interface;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Sorting.Proxy
{
    /// <summary>
    /// 供包台业务模型
    /// </summary>
    public class SortingViewModel : WPFViewModelBase<IWesService, SupplyStationModel>
    {
        private readonly SynchronizationContext _UIContext = new SynchronizationContext();      //跨线程更新锁
        private readonly ReaderWriterLockSlim _CamerLockSlim = new ReaderWriterLockSlim();      //多线程锁
        private ConcurrentStack<string> _ReveivedBarCodeStack = new ConcurrentStack<string>();  //接收到的条码队列
        private IBarCodeView _BarcodeViewer;                                                    //顶扫
        private readonly int _RequiredConsistentCount = 3;                                      //连续一致的条码数量
        private object _FlowLock = new object();
        private object _MessageLock = new object();
        private EnumSupplyFlow _CurrentFlow = EnumSupplyFlow.Inition;
        /// <summary>
        /// 当前分拣流程状态
        /// </summary>
        private EnumSupplyFlow CurrentFlow
        {
            get
            {
                var rel = Monitor.TryEnter(_FlowLock);
                try
                {

                    return _CurrentFlow;
                }
                finally
                {
                    if (rel)
                    {
                        Monitor.Exit(_FlowLock);
                    }
                }

            }
            set
            {
                var rel = Monitor.TryEnter(_FlowLock);
                try
                {

                    _CurrentFlow = value;
                }
                finally
                {
                    if (rel)
                    {
                        Monitor.Exit(_FlowLock);
                    }
                }
            }
        }
        private IBarCodeView BarcodeViewer
        {
            get
            {
                return _BarcodeViewer;
            }
            set
            {
                _BarcodeViewer = value;
                RaisePropertyChanged(nameof(BarcodeViewer));
            }
        }
        private string CamerName
        {
            get
            {
                return Source.IsLeft ? "左相机" : "右相机";
            }
        }
        private IBarCodeScanner _BarcodeScanner;
        private IBarCodeScanner BarcodeScanner
        {
            get
            {
                return _BarcodeScanner;
            }
            set
            {
                _BarcodeScanner = value;
                RaisePropertyChanged(nameof(BarcodeScanner));
            }
        }
        private IWesService _WCSClient = null;
        private IWesService WCSClient
        {
            get
            {
                return _WCSClient;
            }
        }
        private bool _CancelCreateSupplyTaskTag = false;         //取消分拣任务的标志位
        private Thread _QuerySupplyTaskThread = null;           //查询分拣任务的线程
        private Thread _DepartAGVThread = null;                 //AGV发车线程
        private bool IsReLoad = false;  //是否为重新加载；
        //=============================================属性数据==============================================
        public string StationID
        {
            get
            {
                return Source.CurrentWorkTask.StationId.Substring(Source.CurrentWorkTask.StationId.Length-5,5);
            }
        }
        public string StationName
        {
            get
            {
                return Source.StationName;
            }
        }
        public string UserName
        {
            get
            {
                return Source.LoginUserName;
            }
        }
        public string AGVID
        {
            get
            {
                return Source.CurrentWorkTask.AGVID;
            }
            set
            {
                if (Source.CurrentWorkTask.AGVID != value)
                {
                    Source.CurrentWorkTask.AGVID = value;
                    RaisePropertyChanged(nameof(AGVID));
                }

            }
        }
        public string BarCode
        {
            get
            {
                return Source.CurrentWorkTask.Barcode;
            }
            set
            {
                if (Source.CurrentWorkTask.Barcode != value)
                {
                    Source.CurrentWorkTask.Barcode = value;
                    RaisePropertyChanged(nameof(BarCode));
                }
            }
        }
        private string _ReceivedBarCode = ""; //接收到的条码
        private readonly object _ReceidvedBarCodeLocktag = new object();        //多线程锁
        public string ReceivedBarCode
        {
            get
            {
                var result = Monitor.TryEnter(_ReceidvedBarCodeLocktag);
                try
                {
                    return _ReceivedBarCode;
                }
                finally
                {
                    if(result)
                        Monitor.Exit(_ReceidvedBarCodeLocktag);
                }
            }
            set
            {

                if (_ReceivedBarCode == value) return;
                var result = Monitor.TryEnter(_ReceidvedBarCodeLocktag);
                try
                {
                    _ReceivedBarCode = value;
                    RaisePropertyChanged(nameof(ReceivedBarCode));
                }
                finally
                {
                    if (result)
                        Monitor.Exit(_ReceidvedBarCodeLocktag);
                }
            }







        }
        private Visibility _ShowReceivedBarCode = Visibility.Collapsed; //是否显示接收到的条码
        public Visibility ShowReceivedBarCode
        {
            get
            {
                return _ShowReceivedBarCode;
            }
            set
            {
                if (_ShowReceivedBarCode != value)
                {
                    _ShowReceivedBarCode = value;
                    RaisePropertyChanged(nameof(ShowReceivedBarCode));
                }
            }
        }
        public string SKU
        {
            get
            {
                return Source.CurrentWorkTask.SKU;
            }
            set
            {
                Source.CurrentWorkTask.SKU = value;
                RaisePropertyChanged(nameof(SKU));
            }
        }
        public string SKUName
        {
            get
            {
                return Source.CurrentWorkTask.SKUName;
            }
            set
            {
                Source.CurrentWorkTask.SKUName = value;
                RaisePropertyChanged(nameof(SKUName));
            }
        }
        public string WaveID
        {
            get
            {
                return Source.CurrentWorkTask.WaveID;
            }
            set
            {
                Source.CurrentWorkTask.WaveID = value;
                RaisePropertyChanged(nameof(WaveID));
            }
        }
        public string OrderID
        {
            get
            {
                return Source.CurrentWorkTask.OrderID;
            }
            set
            {
                Source.CurrentWorkTask.OrderID = value;
                RaisePropertyChanged(nameof(OrderID));
            }
        }
        private Notice _Message = new Notice("",EnumNotificationType.None);
        public Notice Message
        {
            get
            {
                var rel = Monitor.TryEnter(_MessageLock);
                try
                {

                    return _Message;
                }
                finally
                {
                    if (rel)
                    {
                        Monitor.Exit(_MessageLock);
                    }
                }

            }
            set
            {
                var rel = Monitor.TryEnter(_MessageLock);
                try
                {

                    _Message = value;
                    RaisePropertyChanged(nameof(Message));
                }
                finally
                {
                    if (rel)
                    {
                        Monitor.Exit(_MessageLock);
                    }
                }
            }
        }
        /// <summary>
        /// 发车时数量
        /// </summary>
        public int Quantity
        {
            get
            {
                return Source.CurrentWorkTask.Quantity;
            }
            set
            {
                Source.CurrentWorkTask.Quantity = value;
                RaisePropertyChanged(nameof(Quantity));
            }
        }
        public int MaxQuantity
        {
            get
            {
                return Source.CurrentWorkTask.MaxQuantity;
            }
            set
            {
                Source.CurrentWorkTask.MaxQuantity = value;
                RaisePropertyChanged(nameof(MaxQuantity));
            }
        }
        public ISortingLogger SortingLogger
        {
            get;private set;
        }
        public SortingViewModel(IWesService wesService, SupplyStationModel source, IBarCodeView barcodeView ,ISortingLogger logger) : base(wesService, source)
        {
            source.OnLineReqID = ""; //初始化返岗请求ID
            source.OffLineReqID = ""; //初始化离岗请求ID
            SortingLogger = logger; 
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var jobFile = $"{dir}BarCode.job";
            source.CurrentWorkTask.AGVID = "";
            source.CurrentWorkTask.StationId = source.StationID; //设置站点ID
            _WCSClient = wesService;
            BarcodeViewer = barcodeView;// new DHMVPCameraService(jobFile, Source.IsLeft);
            BarcodeViewer.BarCodeChangedEvent += BarCodeChanged; ;
            BarcodeViewer.CameraImageChangedEvent += (image) =>
            {
                SortingLogger.Logger.Debug($"[{CamerName}]更新流");
                CameraImage = image;

            };
            BarcodeViewer.ConnectedChangedEvent += (connected) =>
            {
                IsConnected = connected;
                SortingLogger.Logger.Debug($"工作[{StationID}]相机连接状态：{connected}");
            };
            //创建查询分拣任务的线程
            _QuerySupplyTaskThread = new Thread(CreateSupplyTaskThreadWorkAsync);
            _QuerySupplyTaskThread.IsBackground = true;               //设置为后台线程
            _QuerySupplyTaskThread.Name = "QuerySupplyTaskThread";    //设置线程名称
            _QuerySupplyTaskThread.Priority = ThreadPriority.Normal;  //设置线程优先级
            //创建发车线程
            _DepartAGVThread = new Thread(DepartAGVThreaWork);        //发车线程
            _DepartAGVThread.IsBackground = true;                     //设置为后台线程
            _DepartAGVThread.Name = "DepartAGVThread";                //设置线程名称
            _DepartAGVThread.Priority = ThreadPriority.Normal;        //设置线程优先级
        }
        //=============================================顶扫相关数据 ==============================================
        private BitmapSource _CameraImage;
        public BitmapSource CameraImage
        {
            get
            {
                _CamerLockSlim.EnterReadLock();
                try
                {
                    return _CameraImage;
                }
                finally
                {
                    _CamerLockSlim.ExitReadLock();
                }
            }
            set
            {
                _CamerLockSlim.EnterWriteLock();
                try
                {
                    SortingLogger.Logger.Debug($"CameraImage: {CamerName}");
                    _CameraImage = value;
                    SortingLogger.Logger.Debug($"CameraImage: {CamerName}111");
                    _UIContext.Post(_ => RaisePropertyChanged(nameof(CameraImage)), null);
                    SortingLogger.Logger.Debug($"CameraImage: {CamerName}");
                }
                catch(Exception ex)
                {
                    SortingLogger.Logger.Error(CamerName + ex.Message);
                }
                finally
                {
                    _CamerLockSlim.ExitWriteLock();
                }
            }



        }
        public bool _IsConnected = false;
        public bool IsConnected
        {
            get
            {
                return true;
            }
            private set
            {
                if (_IsConnected != value)
                {
                    _IsConnected = value;
                    RaisePropertyChanged(nameof(IsConnected));
                }
            }
        }
        //============================================= 窗口加载事件 ==============================================
        private ICommand _FrmLoadCommand;
        public ICommand FrmLoadCommand
        {
            get
            {
                if (_FrmLoadCommand == null)
                    _FrmLoadCommand = new RelayCommand(ExcutFrmLoadCommand, CanExcutFrmLoadCommand, false);
                return _FrmLoadCommand;
            }

            set
            {
                _FrmLoadCommand = value;
            }
        }
        private bool CanExcutFrmLoadCommand()
        {
            //是否可以执行命令的逻辑判断代码
            return true;
        }
        private void ExcutFrmLoadCommand()
        {
            if (IsReLoad == false)
            {
                IsReLoad = true; //设置为重新加载状态
                _QuerySupplyTaskThread.Start();
                _DepartAGVThread.Start();       //启动发车线程
                Dispatcher.CurrentDispatcher.Invoke(new Action(() =>
                {
                    Task.Run(() =>
                    {
                        bool connectOk = false;
                        while (!connectOk)
                        {
                            connectOk = BarcodeViewer.Connect();
                            Task.Delay(2000).Wait(); //等待1秒后重试连接
                        }

                    });

                }));
            }

        }
        //============================================= 按钮按下命令 ==============================================

        #region 发车指令
        /// <summary>
        /// 发车请求ID
        /// </summary>
        private string _CurrentSendAgvID = "";
        private ICommand _SendAGVCommand;
        public ICommand SendAGVCommand
        {
            get
            {
                if (_SendAGVCommand == null)
                    _SendAGVCommand = new RelayCommand(ExcutSendAGVCommand, CanExcutSendAGVCommand, true);
                return _SendAGVCommand;
            }

            set
            {
                _SendAGVCommand = value;
            }
        }
        private bool CanExcutSendAGVCommand()
        {
            Source.CurrentWorkTask.AgvLoadingQty = 1; //设置AGV装载数量为1
          
            //是否可以执行命令的逻辑判断代码
            return   Source.CurrentWorkTask.Isvalidity() && CurrentFlow == EnumSupplyFlow.SendRobotFail;
        }
        private void ExcutSendAGVCommand()
        {
            Message = new Notice( "发车中", EnumNotificationType.Info);  
            CurrentFlow = EnumSupplyFlow.SendingRobot;
            Messenger.Default.Send<SupplyStationModel>(Source, "SendRobot");
//            SortingLogger.Logger.Debug($"工位[{StationID}]执行发车逻辑，任务信息:{Source.CurrentWorkTask.ToJson()}");
//            Task.Run(async () =>
//            {
//                int maxtryCount = 5;
//                int tryCount = 0;
//                BaseResult<RobotDepartureOutputDto> result = null;
//                if (String.IsNullOrEmpty(_CurrentSendAgvID))
//                    _CurrentSendAgvID = UUID.NewTimeUUID;
//                while (result == null && tryCount < maxtryCount)
//                {  
//                    result = await WCSClient.RobotDeparture(Source.CurrentWorkTask.ToDepartureParam(_CurrentSendAgvID));
//                    tryCount++;
//                    Task.Delay(1000).Wait(); //等待1秒后重试
//                }
//                result?.EnsureStatusCode(Source);
//                if (result != null && result.Success)
//                {
//                    Message = new Notice("发车成功", EnumNotificationType.Info);
//                    CurrentFlow = EnumSupplyFlow.SendRobotComplete;
//                    Source.CurrentWorkTask.DepartureTaskID = result.Data.TaskId;
//                    _CurrentSendAgvID = ""; //清空当前发车ID
//                    SortingLogger.Logger.Debug($"工位[{StationID}]执行发车[{Source.CurrentWorkTask.AGVID}]成功");
//                    Source.PreWorkTask = Source.CurrentWorkTask.DeepCopy();
//                    InitionSupplyTask();   //发车成功后，重新初始化分拣任务
//                }
//                else
//                {
//                    Message = new Notice("发车失败", EnumNotificationType.Error,true,true);
//#if RESCAN
//                    CurrentFlow = EnumSupplyFlow.QueryGoodsInfo; //如果发车失败，则返回到扫码状态
//#else
//                    Application.Current.Dispatcher.Invoke(() => {
//                        CurrentFlow = EnumSupplyFlow.SendRobotFail;
//                        ((RelayCommand)SendAGVCommand).RaiseCanExecuteChanged();
//                    });
//#endif

//                }
//            });
        }
        /// <summary>
        /// 更新发车状态
        /// </summary>
        /// <param name="success">成功，失败</param>
        /// <param name="taskID">WES返回的，发车成功任务号</param>
        public void UpdateSendResult(bool success, string taskID="")
        {
            if (success)
            {
                Message = new Notice("发车成功", EnumNotificationType.Info);
                CurrentFlow = EnumSupplyFlow.SendRobotComplete;
                Source.CurrentWorkTask.DepartureTaskID = taskID;
                _CurrentSendAgvID = ""; //清空当前发车ID
                SortingLogger.Logger.Debug($"工位[{StationID}]执行发车[{Source.CurrentWorkTask.AGVID}]成功");
                Source.PreWorkTask = Source.CurrentWorkTask.DeepCopy();
                InitionSupplyTask();   //发车成功后，重新初始化分拣任务
            }
            else
            {
                Message = new Notice("发车失败", EnumNotificationType.Error, true, true);
#if RESCAN
                CurrentFlow = EnumSupplyFlow.QueryGoodsInfo; //如果发车失败，则返回到扫码状态
#else
                    Application.Current.Dispatcher.Invoke(() => {
                        CurrentFlow = EnumSupplyFlow.SendRobotFail;
                        ((RelayCommand)SendAGVCommand).RaiseCanExecuteChanged();
                    });
#endif
            }
        }
        #endregion

        #region 显示退出登录界面
        private ICommand _SwitchToLogoutViewCommand;
        public ICommand SwitchToLogoutViewCommand
        {
            get
            {
                if (_SwitchToLogoutViewCommand == null)
                    _SwitchToLogoutViewCommand = new RelayCommand(ExcutSwitchToLogoutViewCommand, CanExcutSwitchToLogoutViewCommand, false);
                return _SwitchToLogoutViewCommand;
            }

            set
            {
                _SwitchToLogoutViewCommand = value;
            }
        }
        private bool CanExcutSwitchToLogoutViewCommand()
        {
            //是否可以执行命令的逻辑判断代码
            return true;
        }
        private void ExcutSwitchToLogoutViewCommand()
        {
            Messenger.Default.Send<ConfirmModel>(new ConfirmModel(EnumConfirmType.ExitConfirm, "确认退出登录？", Source.IsLeft)); //发送离岗确认消息
        }
        #endregion

        #region 显示接收到的Barcode(测试用)
        private ICommand _ShowReceivedBarCodeCommand;
        public ICommand ShowReceivedBarCodeCommand
        {
            get
            {
                if (_ShowReceivedBarCodeCommand == null)
                    _ShowReceivedBarCodeCommand = new RelayCommand(ExcutShowReceivedBarCodeCommand, CanExcutShowReceivedBarCodeCommand, false);
                return _ShowReceivedBarCodeCommand;
            }
            set
            {
                _ShowReceivedBarCodeCommand = value;
            }
        }
        private bool CanExcutShowReceivedBarCodeCommand()
        {
            //是否可以执行命令的逻辑判断代码
            return true;
        }
        private void ExcutShowReceivedBarCodeCommand( )
        {  
            if(ShowReceivedBarCode == Visibility.Visible)
            {
                ShowReceivedBarCode = Visibility.Collapsed;
            }
            else
            {
                ShowReceivedBarCode = Visibility.Visible; //显示当前条码
            }
        }
        #endregion

        #region 缺件发车
        private ICommand _SendAGVIDLCommand;
        public ICommand SendAGVIDLCommand
        {
            get
            {
                if (_SendAGVIDLCommand == null)
                    _SendAGVIDLCommand = new RelayCommand(ExcutSendAGVIDLCommand, CanExcutSendAGVIDLCommand, true);
                return _SendAGVIDLCommand;
            }

            set
            {
                _SendAGVIDLCommand = value;
            }
        }
        private bool CanExcutSendAGVIDLCommand( )
        {
            //是否可以执行命令的逻辑判断代码
            return true;
        }
        private void ExcutSendAGVIDLCommand( )
        {
            //命令代码
#if TestSound
            Message = new Notice("发车失败", EnumNotificationType.Error,true);
#endif
        }
        #endregion

        #region 驱离空车
        /// <summary>
        /// 当前驱离空车请求ID
        /// </summary>
        private string CurrentDriveOutRobotReqID  //当前驱离空车请求ID
        {
            get
            {
                return  Source.CurrentWorkTask.DriveOutRobotReqID ;
            }
            set
            {
                Source.CurrentWorkTask.DriveOutRobotReqID = value ;
            }
        }
        private ICommand _ReleaseAGVCommand;
        public ICommand ReleaseAGVCommand
        {
            get
            {
                if (_ReleaseAGVCommand == null)
                    _ReleaseAGVCommand = new RelayCommand(ExcutReleaseAGVCommand, CanExcutReleaseAGVCommand, false);
                return _ReleaseAGVCommand;
            }

            set
            {
                _ReleaseAGVCommand = value;
            }
        }
        private bool CanExcutReleaseAGVCommand( )
        {
            //是否可以执行命令的逻辑判断代码
            return CurrentFlow == EnumSupplyFlow.AgvArrived || CurrentFlow== EnumSupplyFlow.QueryGoodsInfo;
        }
        private void ExcutReleaseAGVCommand( )
        {
#if TestSound
            Message = new Notice("请扫码", EnumNotificationType.Info,false,true, "Sounds/AGVArrived.mp3");
#endif
            CurrentFlow = EnumSupplyFlow.DriveOutRobot;
            Messenger.Default.Send<SupplyStationModel>(Source, "DriveOutRobot");
        }
        public void UpdateDriveOutRobotStatus(bool success)
        {
            if (success)
            {
                Message = new Notice("赶空车成功", EnumNotificationType.Info);
                SortingLogger.Logger.Debug($"工位[{StationID}]赶空车[{Source.CurrentWorkTask.AGVID}]成功");
                InitionSupplyTask(); //赶空车成功后，重新初始化分拣任务
            }
            else
            {
                Message = new Notice("赶空车失败", EnumNotificationType.Error, true, true);
                Application.Current.Dispatcher.Invoke(() => {
                    CurrentFlow = EnumSupplyFlow.QueryGoodsInfo;
                    ((RelayCommand)ReleaseAGVCommand).RaiseCanExecuteChanged();
                });
            }
        }
        #endregion

        #region 取消前车
        private ICommand _CancelPreAGVCommand;
        public ICommand CancelPreAGVCommand
        {
            get
            {
                if (_CancelPreAGVCommand == null)
                    _CancelPreAGVCommand = new RelayCommand(ExcutCancelPreAGVCommand, CanExcutCancelPreAGVCommand, false);
                return _CancelPreAGVCommand;
            }

            set
            {
                _CancelPreAGVCommand = value;
            }
        }
        private bool CanExcutCancelPreAGVCommand( )
        {
            //是否可以执行命令的逻辑判断代码
            return true;
        }
        private void ExcutCancelPreAGVCommand( )
        {
#if TestSound
            Message = new Notice("请扫码", EnumNotificationType.Info, false, true, "Sounds/ScannedBarcdoe.mp3");
#endif
        }
        #endregion

        #region 离岗命令
        private string _currnetOfflineReqId = "";
        private ICommand _OffLineCommand;
        public ICommand OffLineCommand
        {
            get
            {
                if (_OffLineCommand == null)
                    _OffLineCommand = new RelayCommand(ExcutOffLineCommand, CanExcutOffLineCommand, true);
                return _OffLineCommand;
            }

            set
            {
                _OffLineCommand = value;
            }
        }
        private bool CanExcutOffLineCommand()
        {
#if TestSound
            return true; //测试声音时，直接返回true
#endif
            return true;
        }
        private void ExcutOffLineCommand()
        {
#if TestSound
            Message = new Notice("发车成功", EnumNotificationType.Info,false,true, "Sounds/SendOK.wav");
            return;
#endif
            //CurrentFlow = EnumSupplyFlow.OffLine; 
            Messenger.Default.Send<ConfirmModel>(new ConfirmModel(EnumConfirmType.OffLineConfirm, "确认离岗？",Source.IsLeft)); //发送离岗确认消息
        }
        #endregion

        //============================================= 业务逻辑 ==================================================
        /// <summary>
        /// 初始化分拣任务
        /// </summary>
        /// <param name="agvId"></param>
        private void InitionSupplyTask(bool isOffline=false)
        {
            AGVID = "";
            //初始化分拣任务
            CurrentFlow = isOffline?EnumSupplyFlow.OffLine: EnumSupplyFlow.Inition;
            //清理商品信息
            BarCode = "";
            SKU = "";
            SKUName = "";
            Source.CurrentWorkTask.AgvLoadingQty = -1;
            Source.CurrentWorkTask.Weight = 0;
            Source.CurrentWorkTask.DepartureTaskID = "";
            Source.CurrentWorkTask.ClearUUID();
            //清除分拣任务信息
            WaveID = "";
            OrderID = "";
            
            Message = new Notice("等待车辆到达", EnumNotificationType.Info);
        }
        /// <summary>
        /// 更新AGV信息
        /// </summary>
        /// <param name="AgvId"></param>
        public void AGVArrived(long sseId, string AgvId,string supplyTaskId)
        {
            SortingLogger.Logger.Debug($"工作站[{StationID}],收到AGV[{AgvId}]到达，工作站状态[{CurrentFlow.GetCaption()}]");
            if (!String.IsNullOrEmpty(AGVID) && !AGVID.Equals(AgvId))//如果当前AGVID不为空，则需要判断是否更新AGVID
            {
                SortingLogger.Logger.Debug($"工位[{StationID}]收到AGV[{AgvId}]到达通知，当前AGVID[{AGVID}]不为空，则需要判断是否更新AGVID");
                var cursseId = Source.CurrentWorkTask.AgvDistributeID;
                var newsseId = sseId;

                if (newsseId > cursseId)
                {
                    SortingLogger.Logger.Error($"工位[{StationID}] 更新Agv，当前AGVID:{AGVID}，新AGVID:{AgvId}");
                    AGVID = AgvId;                                  //更新AGVID
                    Source.CurrentWorkTask.AgvDistributeID = sseId; //更新AGVID的时间戳
                    return;
                }
                else
                {
                    SortingLogger.Logger.Error($"工位[{StationID}]到达AGVID[{AgvId}]通知 ，时间戳[{sseId}]，当前AGVID[{AGVID}]，时间戳[{Source.CurrentWorkTask.AgvDistributeID}]，丢弃");
                    return;
                }
            }
            BarCode = string.Empty; //清空条码
            AGVID = AgvId; //设置AGVID
            CurrentFlow = EnumSupplyFlow.AgvArrived;   //更新工作状态为：AGV到达
            Source.CurrentWorkTask.AgvDistributeID = sseId;
            Source.CurrentWorkTask.InitionUUID(supplyTaskId);
            Message = new Notice("请扫码", EnumNotificationType.Info);
            SortingLogger.Logger.Debug($"工位[{StationID}]收到AGV[{AgvId}]到达通知，开始新分拣任务");
            //发送创建分拣任务信号
            CurrentFlow = EnumSupplyFlow.QueryGoodsInfo;
            Application.Current.Dispatcher.Invoke(() => {
                ((RelayCommand)ReleaseAGVCommand).RaiseCanExecuteChanged();
            });

        }
        private void BarCodeChanged(string barcode)
        {
            if (ShowReceivedBarCode == Visibility.Visible)
            {
                ReceivedBarCode = barcode; //更新接收到的条码
            }
            if (CurrentFlow == EnumSupplyFlow.QueryGoodsInfo)//
            {
                _ReveivedBarCodeStack.Push(barcode); //添加新的条码到队列尾部
            }
        }
        private void CreateSupplyTaskThreadWorkAsync()
        {
            while (!_CancelCreateSupplyTaskTag)
            {
                if (CurrentFlow != EnumSupplyFlow.QueryGoodsInfo)
                {
                    Thread.Sleep(200); //如果不是创建分拣任务状态，则等待1秒
                    continue;
                }
                Message = new Notice("请扫码", EnumNotificationType.Info,true,false);
                //提取有效条码
                if (!TryValidateBarcode(out var validBarcode)) //提取条码，失败则继续等待
                {
                    Thread.Sleep(50);
                    continue;
                }

                BarCode = validBarcode; //更新条码
                CurrentFlow = EnumSupplyFlow.CreateSupplyTask; //更新工作流状态为：创建分拣任务，停止条码读取
                _ReveivedBarCodeStack.Clear(); //清空条码队列  //清空条码队列，防止重复读取
                //查询商品信息
                Message = new Notice("正在查询商品信息...", EnumNotificationType.Info);
                var goodsInfo = TryQueryGoodsInfoByBarCode(validBarcode);
                if (goodsInfo == null || goodsInfo.Result == null || goodsInfo.Result.Items.Count == 0)
                {
                    Thread.Sleep(1000);
                    SortingLogger.Logger.Error($"工位[{StationID}]查询商品信息失败，条码[{BarCode}]，SKU[{SKU}]，AGVID[{AGVID}]返回到扫码状态");
                    CurrentFlow = EnumSupplyFlow.QueryGoodsInfo;   //如果查询商品信息失败，则返回到扫码状态
                    continue;
                }
                //更新商品信息,更新当前任务信息
                UpdateGoodsInfo(goodsInfo.Result.Items.First());
                Message = new Notice("正在获取任务...", EnumNotificationType.Info);
                var taskInfo = TryRequestSupplyTask(Source.CurrentWorkTask);
                if (taskInfo == null || taskInfo.Result == null)
                {
                    Thread.Sleep(1000);
                    SortingLogger.Logger.Error($"工位[{StationID}]请求任务失败，条码[{BarCode}]，SKU[{SKU}]，AGVID[{AGVID}]返回到扫码状态");
                    CurrentFlow = EnumSupplyFlow.QueryGoodsInfo;    //如果请求任务失败，则返回到扫码状态
                    Message = new Notice("获取任务失败", EnumNotificationType.Error);
                    continue;
                }
                UpdateSupplyTaskInfo(taskInfo.Result);
                CurrentFlow = EnumSupplyFlow.CreateSupplyTaskComplete;
                Message = new Notice("请发车", EnumNotificationType.Info);
                //发送发车信号
                CurrentFlow = EnumSupplyFlow.AutoSendRobot;
            }
        }
        /// <summary>
        /// 获取条码
        /// </summary>
        /// <param name="validBarcode">成功验证的条码</param>
        /// <param name="timeoutMs">单次读取超时时间（毫秒）</param>
        /// <returns>是否验证成功</returns>
        public bool TryValidateBarcode(out string validBarcode, int timeoutMs = 10)
        {
            validBarcode = null;
            string lastBarcode = "";
            int consistentCount = 0;
            SortingLogger.Logger.Debug($"工位[{StationID}]提取条码开始，当前状态：{CurrentFlow}");
            while (consistentCount < _RequiredConsistentCount && !_CancelCreateSupplyTaskTag)
            {
                
                if (!_ReveivedBarCodeStack.TryPop(out var currentBarcode) || string.IsNullOrEmpty(currentBarcode))
                {
                    Thread.Sleep(timeoutMs); // 等待一段时间再尝试读取
                    continue;
                }
               
                if (!string.IsNullOrEmpty(lastBarcode))
                {//初始化lastBarcode
                    lastBarcode = currentBarcode;
                    consistentCount++;
                    continue;
                }
                if (currentBarcode == lastBarcode)
                {
                    consistentCount++;
                }
                else
                {
                    // 遇到不一致的条码，重置计数器
                    lastBarcode = currentBarcode;
                    consistentCount = 1;
                }
            }
            if (_CancelCreateSupplyTaskTag)
            {
                validBarcode = null;
                return false; // 取消创建分拣业务，提交有效条码失败

            }
            else if (consistentCount >= _RequiredConsistentCount)
            {
                SortingLogger.Logger.Debug($"工位[{StationID}]提取条码成功,BarCode[{lastBarcode}]");
                validBarcode = lastBarcode;
                return true;
            }
            else
            {
                validBarcode = null;
                return false; // 取消创建分拣业务，提交有效条码失败
            }
        }
        /// <summary>
        /// 查询商品信息
        /// </summary>
        /// <param name="barcode"></param>
        /// <returns></returns>
        private async Task<GoodsList> TryQueryGoodsInfoByBarCode(string barcode)
        {
            int maxQuery = 3;
            int queryCount = 0;
            string currentQueryBarcode = barcode;
            ResponseBase<GoodsList> result = null;
            while (result == null && queryCount < maxQuery && !_CancelCreateSupplyTaskTag)
            {//查询商品信息，直到查询到商品信息，或者查询次数超过最大次数
                queryCount++;                    //增加查询次数
                result = await WCSClient.QueryGoodsInfo(new QueryGoodsParam() { Barcode = currentQueryBarcode });
                if (result == null)
                    Thread.Sleep(1000);
            }
            result.EnsureStatusCode(Source);
            if (result != null)//请求成功
            {
                if (result.Success)//表示执行完成
                {
                    //查询到商品信息，更新商品信息
                    SortingLogger.Logger.Debug($"查询到条码[{currentQueryBarcode}]的商品信息，当前状态：{CurrentFlow}");
                    Debug.Assert(result.Data.Items.Count == 1, "商品数量只能是1");
                    if (result.Data.Items.Count > 1)
                    {//商品数量大于1，要显示选择商品界面
                        //提示用户选择商品
                        SortingLogger.Logger.Debug($"查询到条码[{BarCode}]的商品信息，商品数量大于1，提示用户选择商品");
                        //用户选择商品界面
                        //调试，校验是否正确
                        //更新当前商品信息
                        return result.Data;
                    }
                    else if (result.Data.Items.Count == 1)
                    {
                        return result.Data;

                    }
                    else
                    {//没有查询到商品
                        //未查询到商品信息，提示换货
                        SortingLogger.Logger.Debug($"未查询到条码[{BarCode}]的商品信息，原因[{result.Message}]，提示换商品");
                        Message = new Notice("没有查询商品信息，请换货", EnumNotificationType.Error,true,true);
                        Task.Delay(1000).Wait();//等2秒，方便读消息
                        return null;
                    }
                }
                else
                {
                    //服务端执行失败，提示失败原因
                    Message =new Notice( result.Message,EnumNotificationType.Error);
                    Task.Delay(1000).Wait();//等2秒，方便读消息
                    return null;
                }
            }
            else
            {
                //请求失败，记录日志，重试
                SortingLogger.Logger.Error($"发送查询商品信息请求[{queryCount}]次失败");
                if (queryCount >= maxQuery)
                {
                    Message = new Notice("查询商品信息失败，请联系管理员", EnumNotificationType.Error,true,true);
                    Task.Delay(2000).Wait(); //等2秒，方便读消息
                }
                Task.Delay(1000).Wait();
                return null;
            }

        }
        private void UpdateGoodsInfo(Goods item)
        {
            SKU = item.Sku;
            SKUName = item.SkuName;
            Source.CurrentWorkTask.AgvLoadingQty = item.AgvLoadingQty;
            Source.CurrentWorkTask.Weight = item.Weight;
        }
        /// <summary>
        /// 获取任务
        /// </summary>
        /// <param name="taskModel"></param>
        /// <returns></returns>
        private async Task<SupplyTask> TryRequestSupplyTask(WorkTaskModel taskModel)
        {
            int maxQuery = 3;
            int queryCount = 0;
            ResponseBase<SupplyTask> result = null;
            while (!_CancelCreateSupplyTaskTag && result == null && queryCount < maxQuery)//如果没有取消查询
            {
                var param = taskModel.ToRequestTaskParam2();
                SortingLogger.Logger.Debug($"工位[{taskModel.StationId}]起起获取任务请求，参数[{param.ToJson()}]");
                result = await WCSClient.RequetSupplyTask(param);
                if (result == null)
                    Thread.Sleep(1000);
            }
            result?.EnsureStatusCode(Source);
            if (result != null)//请求成功
            {
                if (result.Success)//表示执行完成
                {
                    if (result.Data != null)
                    {
                        //更新任务信息
                        SortingLogger.Logger.Debug($"查询到条码[{taskModel.SKU}]的任务信息，当前状态：{CurrentFlow}");
                        return result.Data;
                    }
                    else
                    {//没有查询任务
                        SortingLogger.Logger.Debug($"未请求到商品[{taskModel.SKU}]任务信息，原因[{result.Message}]，提示换商品");
                        Message = new Notice(result.Message, EnumNotificationType.Error);
                        return null;
                    }
                }
                else
                {
                    //服务端执行失败，提示失败原因
                    SortingLogger.Logger.Debug($"未请求到商品[{SKU}]任务信息，原因[{result.Message}]，提示换商品");
                    Message =new Notice( $"该商品没有任务，请更换商品", EnumNotificationType.Error,true,true);
                    return null;
                }
            }
            else
            {
                //请求失败，记录日志，重试
                SortingLogger.Logger.Error($"发送查询商品信息请求[{queryCount}]次失败,重新查询...");
                Message = new Notice( $"请求任务失败，请联系管理员",EnumNotificationType.Error,true,true);
                Task.Delay(1000).Wait();
                return null;
            }
        }
        private void UpdateSupplyTaskInfo(SupplyTask data)
        {
            WaveID = data.WaveId;
            OrderID = data.OrderId;
            Source.CurrentWorkTask.TaskID = data.TaskId;
            Source.CurrentWorkTask.MaxQuantity = data.Count;
            Source.CurrentWorkTask.ChuteId = data.ChuteId;
        }
        private void DepartAGVThreaWork()
        {
            while (true)
            {
                if (!(CurrentFlow == EnumSupplyFlow.AutoSendRobot && Source.CurrentWorkTask.Isvalidity()))
                {
                    Thread.Sleep(500);
                    continue;
                }
                SortingLogger.Logger.Debug($"工位[{StationID}]开始发车线程，当前状态：{CurrentFlow}");
                ExcutSendAGVCommand(); //执行发车命令
            }
        }
        //============================================= 销毁资源  ==================================================
        public override void Dispose()
        {
            base.Dispose();
            _CancelCreateSupplyTaskTag = true; //设置取消查询商品信息标志位
            BarcodeViewer?.DisConnect();  //断开相机连接
            BarcodeViewer = null;
        }
    }

    public class TestWjj
    {
        public TestWjj() { }
    }
}
