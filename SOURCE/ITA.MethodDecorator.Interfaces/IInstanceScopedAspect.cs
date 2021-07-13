namespace MethodDecorator.Fody.Interfaces
{
    public interface IInstanceScopedAspect
    {
        object CreateInstance(object instance);
        void RuntimeInitializeInstance();
    }
}
