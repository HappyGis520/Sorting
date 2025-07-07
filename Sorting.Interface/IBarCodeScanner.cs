/*******************************************************************
 * * 功   能：  扫码枪接口
 * * 作   者：  Jason
 * * 编程语言： C# 
 * *******************************************************************/
using System;
using System.ComponentModel;
namespace Sorting.Interface
{
    /// <summary>
    /// 扫码枪接口
    /// </summary>
    public interface IBarCodeScanner: INotifyPropertyChanged
    {
        /// <summary>
        /// 连接相机
        /// </summary>
        /// <returns></returns>
        bool Connect();
        /// <summary>
        /// 断开相机
        /// </summary>
        /// <returns></returns>
        bool DisConnect();
        /// <summary>
        /// BarCode改变事件
        /// </summary>
        Action<string> BarCodeChangedEvent { get; set; }
        /// <summary>
        /// 相机连接状态改变事件
        /// </summary>
        Action<bool> ConnectedChangedEvent { get; set; }
    }
}
