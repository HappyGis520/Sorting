using Linearstar.Windows.RawInput;

namespace Sorting.Proxy
{
    public static class RawInputDeviceExtensions
    {
        public static bool IsValidateDevice(this RawInputDevice sourceDevice,out HardWareDevice device)
        {
            device = null;
            if (sourceDevice == null || string.IsNullOrEmpty(sourceDevice?.SerialNumber))
                return false;
            //把整型转为十六进制字符串
            var vid = sourceDevice?.VendorId.ToString("X4") ?? "0000";
            var pid = sourceDevice?.ProductId.ToString("X4") ?? "0000";
            var id = sourceDevice?.SerialNumber;
            device = new HardWareDevice(vid, pid, id); 
            return true;

        }
    }
}
