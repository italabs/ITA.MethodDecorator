using System;

namespace MethodDecorator.Fody.Interfaces.Aspects
{
    public class MethodDecoratorBase : Attribute, IAspectMatchingRule
    {
        public string AttributeTargetTypes { get; set; }
        public bool AttributeExclude { get; set; }
        public int AttributePriority { get; set; }
        public int AspectPriority {get; set;}
    }
}
