using System.Reflection;

namespace MethodDecorator.Fody.Interfaces.Aspects
{
    public class MethodExecutionArgs
    {
        public MethodExecutionArgs(MethodBase method, object instance, object[] arguments)
        {
            Method = method;
            Instance = instance;
            Arguments = arguments;
        }

        public object Instance { get; set; }
        public MethodBase Method { get; set; }
        public object[] Arguments { get; set; }
        public object MethodExecutionTag { get; set; }
        public long StartTime { get; set; }
        public bool HasFailed { get; set; }
    }
}
