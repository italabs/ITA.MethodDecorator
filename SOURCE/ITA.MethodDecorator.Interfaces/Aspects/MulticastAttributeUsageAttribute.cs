using System;

namespace MethodDecorator.Fody.Interfaces.Aspects
{
    [Flags]
    public enum MulticastTargets
    {
        Default = 0,
        Class = 1,
        Struct = 2,
        Enum = 4,
        Delegate = 8,
        Interface = 16,
        AnyType = 31,
        Field = 32,
        Method = 64,
        InstanceConstructor = 128,
        StaticConstructor = 256,
        Constructor = 384,
        Property = 512,
        Event = 1024,
        AnyMember = 2016,
        Assembly = 2048,
        Parameter = 4096,
        ReturnValue = 8192,
        All = 16383
    }

    public class MulticastAttributeUsageAttribute: Attribute
    {
        public MulticastAttributeUsageAttribute(MulticastTargets validOn) { ValidOn = validOn; }
        public MulticastTargets ValidOn { get; private set; }
        public bool AllowMultiple { get; set; }
    }
}
