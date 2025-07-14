using Jlib;
using Linearstar.Windows.RawInput;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace Sorting.Proxy
{
    /// <summary>
    /// 扫码枪设备监控类
    /// </summary>
    public class BarCodeScannerMonitor
    {
        private  const int WM_INPUT = 0x00FF;
        private Window _WinHandle;
        private HardwareMonitor HardwareMonitor = new HardwareMonitor();
        /// <summary>
        /// 新插入的设备列表
        /// </summary>
        public ConcurrentDictionary<string, HardWareDevice> AppendDevices { get; private set; } = new ConcurrentDictionary<string, HardWareDevice>();
        /// <summary>
        /// 忽略的设备
        /// </summary>
        private ConcurrentDictionary<string, BarCodeScannerDevice> IgnoreDevices = new ConcurrentDictionary<string, BarCodeScannerDevice>();
        /// <summary>
        /// 已注册扫码枪设备
        /// </summary>
        private ConcurrentDictionary<string, BarCodeScannerDevice> RegistBarcodeScanner = new ConcurrentDictionary<string, BarCodeScannerDevice>();
        /// <summary>
        /// 接收到条码事件
        /// </summary>
        public Action<BarCodeScannerDevice, string> ReceivedBarcdoe { get; set; } = null;
        public BarCodeScannerMonitor(Window handle)
        {
            _WinHandle = handle; ;
            HardwareMonitor.DeviceChangedEvent += DeviceChangedEvent;
        }

        public void Start()
        {
            HardwareMonitor.StartMonitoring();
        }
        public void Stop()
        {
            HardwareMonitor.StopMonitoring();
        }
        /// <summary>
        /// 注册设备
        /// </summary>
        /// <param name="sourceDevice"></param>
        public void IgnoreDevice(RawInputDevice sourceDevice)
        {
        }
        public void ReightDevice(RawInputDevice sourceDevice)
        {
        }
        public void ReceivedSourceInitialized(EventArgs e)
        {
            var windowInteropHelper = new WindowInteropHelper(_WinHandle);
            var hwnd = windowInteropHelper.Handle;

            // Get the devices that can be handled with Raw Input.
            var devices = RawInputDevice.GetDevices();

            // register the keyboard device and you can register device which you need like mouse
            RawInputDevice.RegisterDevice(HidUsageAndPage.Keyboard,
                RawInputDeviceFlags.ExInputSink , hwnd);

            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(Hook);
        }
        /// <summary>
        /// 设备变更事件处理
        /// </summary>
        /// <param name="IsInsert"></param>
        /// <param name="pid"></param>
        /// <param name="vid"></param>
        /// <param name="id"></param>
        private void DeviceChangedEvent(bool IsInsert, string pid, string vid, string id)
        {
            Console.WriteLine($"设备 {(IsInsert ? "插入" : "拔出")} - VID: {vid}, PID: {pid}, ID: {id}");
            var vidPid = $"{vid}-{pid}-{id}";
            if (IsInsert)
            {
                AppendDevices.TryAdd(vidPid, new HardWareDevice(vid, pid, id));
            }
            else
            {
                RegistBarcodeScanner.TryRemove(vidPid, out _);
                AppendDevices.TryRemove(vidPid, out _);
                IgnoreDevices.TryRemove(vidPid, out _);
            }
        }
        private IntPtr Hook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // You can read inputs by processing the WM_INPUT message.
            if (msg == WM_INPUT)
            {
                var data = RawInputData.FromHandle(lparam);

                //如果不是键盘输入则忽略
                if (data == null || !(data is RawInputKeyboardData))
                {
                    return IntPtr.Zero;
                }

                //获取设备信息，非有效设备则忽略
                var sourceDeviceHandle = data.Header.DeviceHandle;
                var sourceDevice = data.Device;

                handled = false;
                if (sourceDevice == null || !sourceDevice.IsValidateDevice(out var deviceObj) || deviceObj == null)
                {
                  
                    return IntPtr.Zero;
                }

                //非注册扫码枪设备则忽略
                if (!RegistBarcodeScanner.TryGetValue(deviceObj.DeviceID, out BarCodeScannerDevice scannerDevice))
                {
                    Console.WriteLine($"未注册扫码枪设备: {deviceObj.DeviceID}");
                    return IntPtr.Zero;
                }


                //switch (data)
                //{
                //    case RawInputMouseData mouse:
                //        Debug.WriteLine(mouse.Mouse);
                //        break;
                //    case RawInputKeyboardData keyboard:
                //        Debug.WriteLine(keyboard.Keyboard.ToString());
                //        break;
                //    case RawInputHidData hid:
                //        Debug.WriteLine(hid.Hid);
                //        break;
                //}

                //if (data is RawInputKeyboardData)
                //{
                    AppendDevices.TryAdd(deviceObj.DeviceID, deviceObj);
                    return IntPtr.Zero;
                //}

                // The data will be an instance of either RawInputMouseData, RawInputKeyboardData, or RawInputHidData.
                // They contain the raw input data in their properties.
                //switch (data)
                //{
                //    case RawInputMouseData mouse:
                //        Debug.WriteLine(mouse.Mouse);
                //        break;
                //    case RawInputKeyboardData keyboard:
                //        Debug.WriteLine(keyboard.Keyboard.ToString());
                //        break;
                //    case RawInputHidData hid:
                //        Debug.WriteLine(hid.Hid);
                //        break;
                //}
            }
            return IntPtr.Zero;
        }
        /// <summary>
        /// 处理键盘输入
        /// </summary>
        /// <param name="keyboardData"></param>
        private void ProcessKeybordInput(RawInputKeyboardData keyboardData)
        {
            if (keyboardData == null || keyboardData.Device == null) return;
            var keycode = keyboardData.Keyboard.VirutalKey;
            var isUp = keyboardData.Keyboard.Flags == Linearstar.Windows.RawInput.Native.RawKeyboardFlags.Up;

            //回车键表示条码结束
            if (!isUp)
                return;
            // 转换按键为字符78
            char c = (char)keycode;
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_') // 常见条码字符
            {
                RegistBarcodeScanner[keyboardData.Device.DevicePath].AppenChar(c);
                if (RegistBarcodeScanner[keyboardData.Device.DevicePath].ReceiveSuccess(out string barcode))
                {
                    Console.WriteLine(keyboardData.Device.Handle.ToString() + $"{keyboardData.Header.DeviceHandle:X8} = " + keyboardData.Header.DeviceHandle.ToString() + "=" + barcode);
                }
            }
        }

    }
}
