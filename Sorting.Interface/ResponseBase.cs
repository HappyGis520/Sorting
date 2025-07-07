using GalaSoft.MvvmLight.Messaging;

namespace Sorting.Interface
{
    public class ResponseBase<T>
    {
        public int Code ;
        public string Message;
        public T Data;
        public bool Success
        {
            get
            {
                return Code == 0;
            }
        }
    }
    public static class ResponseUnity
    {
        public static void EnsureStatusCode<T>(this ResponseBase<T> result, SupplyStationModel model)
        {
#if TestLogin
#else

            if (result?.Code == 401)
            {
                Messenger.Default.Send(model, "Unauthorized");
            }
#endif
        }
    }
}
