using System;
using System.Text;

namespace Sorting.Proxy
{
    public class HardWareDevice 
    {
        public string ProductId = "";
        public string VendorId = "";
        public string SerialNumber = "";
        public EnumDeviceType DeviceType = EnumDeviceType.Unknown;
        public string DeviceID
        {
            get
            {
                return $"{VendorId}-{ProductId}-{SerialNumber}";
            }
        }
        public HardWareDevice(string vid, string pid,string snumer)
        {
            ProductId = pid;
            VendorId = vid;
            SerialNumber = snumer;
        }
        public void SetDeviceType(EnumDeviceType deviceType)
        {
            DeviceType = deviceType;
        }

    }
    /// <summary>
    /// 条形码读取设备
    /// </summary>
    public class BarCodeScannerDevice: HardWareDevice
    {
        /// <summary>
        /// 
        /// </summary>
        private StringBuilder BarCodeBuild = new StringBuilder();
        /// <summary>
        /// 开始接收时间
        /// </summary>
        private DateTime StartReciedTime = DateTime.Now;

        /// <summary>
        /// 厂商名称
        /// </summary>
        public string ManuFacturerName { get; private set; }
        public bool IsLeftDevice { get; private set; }
        public BarCodeScannerDevice(string vendorId,string productId, string deviceID,bool isLeft):base(vendorId,productId, deviceID)
        {
            ManuFacturerName = "未知厂商";
            IsLeftDevice = isLeft;
        }
        public BarCodeScannerDevice(HardWareDevice device, bool isLeft) : base(device.VendorId, device.ProductId, device.SerialNumber)
        {
            ManuFacturerName = "未知厂商";
            IsLeftDevice = isLeft;
        }
        public void AppenChar(char c)
        {
            if (BarCodeBuild.Length == 0)
            {
                StartReciedTime = DateTime.Now;
            }
        }
        public bool ReceiveSuccess(out string barcode) 
        {
                barcode = "";
                //判断接收是否超过30毫秒
                if (DateTime.Now - StartReciedTime > TimeSpan.FromMilliseconds(30))
                {
                    barcode = BarCodeBuild.ToString();
                    BarCodeBuild.Clear();
                    return true;
                }
                return false;
        }


    }
}
