namespace Global
{
    using System;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using static Global.EasyObject;

#if GLOBAL_SYS
    public
 #else
    internal
#endif
    class JsonApiServer
    {
        IntPtr Handle = IntPtr.Zero;
        IntPtr CallPtr = IntPtr.Zero;
        IntPtr LastErrorPtr = IntPtr.Zero;
        delegate IntPtr proto_Call(IntPtr name, IntPtr args);
        public JsonApiServer()
        {
        }
        static ThreadLocal<IntPtr> HandleCallPtr = new ThreadLocal<IntPtr>();
        public IntPtr HandleNativeCall(Type apiType, IntPtr nameAddr, IntPtr inputAddr)
        {
            //if (HandleCallPtr.Value != IntPtr.Zero)
            //{
            //    Sys.FreeHGlobal(HandleCallPtr.Value);
            //    HandleCallPtr.Value = IntPtr.Zero;
            //}
            var name = Sys.UTF8AddrToString(nameAddr);
            var input = Sys.UTF8AddrToString(inputAddr);
            EasyObject args = FromJson(input)!;
            MethodInfo? mi = apiType.GetMethod(name);
            dynamic? result = null;
            if (mi == null)
            {
                result = $"API not found: {name}";
            }
            else
            {
                try
                {
                    result = mi.Invoke(null, new object[] { args });
                    EasyObject okResult = EasyObject.EmptyArray;
                    okResult.Add(result);
                    result = okResult;
                }
                catch (TargetInvocationException ex)
                {
                    result = ex.InnerException!.ToString();
                }
            }
            string output = FromObject(result).ToJson();
            //HandleCallPtr.Value = Sys.StringToUTF8Addr(output);
            //return HandleCallPtr.Value;
            return Sys.ReassignThreadLocalStringPointer(HandleCallPtr, output);
        }
        public IntPtr HandleNativeCallBase64(Type apiType, IntPtr nameAddr, IntPtr inputAddr)
        {
            if (HandleCallPtr.Value != IntPtr.Zero)
            {
                Sys.FreeHGlobal(HandleCallPtr.Value);
                HandleCallPtr.Value = IntPtr.Zero;
            }
            var name = Sys.UTF8AddrToString(nameAddr);
            var input = Sys.UTF8AddrToString(inputAddr);
            EasyObject args = FromJson(input)!;
            MethodInfo? mi = apiType.GetMethod(name);
            dynamic? result = null;
            if (mi == null)
            {
                result = $"API not found: {name}";
            }
            else
            {
                try
                {
                    result = mi.Invoke(null, new object[] { args });
                    EasyObject okResult = EasyObject.EmptyArray;
                    okResult.Add(result);
                    result = okResult;
                }
                catch (TargetInvocationException ex)
                {
                    result = ex.InnerException!.ToString();
                }
            }
            string output = FromObject(result).ToJson();
            output = Convert.ToBase64String(Encoding.UTF8.GetBytes(output));
            HandleCallPtr.Value = Sys.StringToUTF8Addr(output);
            return HandleCallPtr.Value;
        }
        public string HandleDotNetCall(Type apiType, string name, string input)
        {
            EasyObject args = FromJson(input)!;
            MethodInfo? mi = apiType.GetMethod(name);
            dynamic? result = null;
            if (mi == null)
            {
                result = $"API not found: {name}";
            }
            else
            {
                try
                {
                    result = mi.Invoke(null, new object[] { args });
                    EasyObject okResult = EasyObject.EmptyArray;
                    okResult.Add(result);
                    result = okResult;
                }
                catch (TargetInvocationException ex)
                {
                    result = ex.InnerException!.ToString();
                }
            }
            string output = FromObject(result).ToJson();
            return output;
        }
    }

}
