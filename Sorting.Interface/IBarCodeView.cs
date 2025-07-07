/*******************************************************************
 * * 功   能：  带视频的扫码设备
 * * 作   者：  Jason
 * * 编程语言： C# 
 * *******************************************************************/
using System;
using System.Windows.Media.Imaging;
namespace Sorting.Interface
{
    /// <summary>
    /// 带有相机的扫码枪接口
    /// </summary>
    public interface IBarCodeView:IBarCodeScanner
    {
        /// <summary>
        /// 相机图像改变事件
        /// </summary>
        Action<BitmapSource> CameraImageChangedEvent { get; set; }


    }
}
