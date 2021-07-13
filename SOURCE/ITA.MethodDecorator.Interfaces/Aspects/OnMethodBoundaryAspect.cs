using System;
using System.Reflection;

namespace MethodDecorator.Fody.Interfaces.Aspects
{
    public class AspectInfo
    {
    }

    public class OnMethodBoundaryAspect: MethodDecoratorBase
    {
        protected OnMethodBoundaryAspect() { }

        public virtual void CompileTimeInitialize(MethodBase inMethod, AspectInfo inAspectInfo) { }

        public void Init(MethodExecutionArgs context)
        {
            if (context.Instance != null)
            {
                var instanceScoped = this as IInstanceScopedAspect;
                if (instanceScoped != null)
                {
                    instanceScoped.RuntimeInitializeInstance();
                }
            }
        }

        public virtual void RuntimeInitialize(MethodBase method)
        {
            CompileTimeInitialize(method, new AspectInfo());
        }

        public virtual void OnEntry(MethodExecutionArgs context)
        {
        }

        public virtual void OnExit(MethodExecutionArgs context, object returnValue)
        {
        }

        public virtual void OnException(MethodExecutionArgs context, Exception exception)
        {
        }
    }
}
