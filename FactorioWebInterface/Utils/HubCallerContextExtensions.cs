using Microsoft.AspNetCore.SignalR;

namespace FactorioWebInterface.Utils
{
    public static class HubCallerContextExtensions
    {
        public static T GetDataOrDefault<T>(this HubCallerContext context)
        {
            if (context.Items.TryGetValue(typeof(T), out object data))
            {
                return (T)data;
            }
            else
            {
                return default;
            }
        }

        public static bool TryGetData<T>(this HubCallerContext context, out T data)
        {
            if (context.Items.TryGetValue(typeof(T), out object obj))
            {
                data = (T)obj;
                return true;
            }
            else
            {
                data = default;
                return false;
            }
        }

        public static void SetData<T>(this HubCallerContext context, T value)
        {
            context.Items[typeof(T)] = value;
        }
    }
}
