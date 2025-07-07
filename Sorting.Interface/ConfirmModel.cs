namespace Sorting.Interface
{ 
    public enum EnumConfirmType
    {
        OffLineConfirm,
        OffLine,
        CancelOffline,
        ExitConfirm,
        Exit,
        CancelExit, 
    }
    public class ConfirmModel
    {
        public EnumConfirmType ConfirmType;
        public string Message;
        public bool IsConfirmed = false;
        public bool IsLeft = true; // 是否在左侧显示
        public ConfirmModel(EnumConfirmType confirmType, string message,bool isLeft =true)
        {
            ConfirmType = confirmType;
            Message = message;
            IsLeft = isLeft;
        }

    }
}
