using System;
using System.Linq;
using Fody;
using MethodDecorator.Fody.Interfaces.Aspects;
using Mono.Cecil;

public class ReferenceFinder
{
    ModuleDefinition moduleDefinition;

    public ReferenceFinder(BaseModuleWeaver weaver)
    {
        this.moduleDefinition = weaver.ModuleDefinition;

        MethodBaseTypeRef = this.moduleDefinition.ImportReference(weaver.FindType("System.Reflection.MethodBase"));
        ExceptionTypeRef = this.moduleDefinition.ImportReference(weaver.FindType("System.Exception"));
        ObjectTypeRef = this.moduleDefinition.ImportReference(weaver.TypeSystem.ObjectDefinition);
        SystemTypeRef = weaver.FindType("System.Type");
        MemberInfoRef = weaver.FindType("System.Reflection.MemberInfo");
        ActivatorTypeRef = weaver.FindType("System.Activator");
        AttributeTypeRef = weaver.FindType("System.Attribute");
        VoidTypeRef = weaver.TypeSystem.VoidReference;
        BoolTypeRef = weaver.TypeSystem.BooleanReference;
        MethodExecutionArgsTypeRef = moduleDefinition.ImportReference(typeof(MethodExecutionArgs));
    }

    public TypeReference ObjectTypeRef { get; }
    public TypeReference MethodBaseTypeRef { get; }
    public TypeReference ExceptionTypeRef { get; }
    public TypeDefinition SystemTypeRef { get; }
    public TypeDefinition MemberInfoRef { get; }
    public TypeDefinition ActivatorTypeRef { get; }
    public TypeDefinition AttributeTypeRef { get; }
    public TypeReference VoidTypeRef { get; }
    public TypeReference BoolTypeRef { get; }
    public TypeReference MethodExecutionArgsTypeRef { get; }

    public MethodReference GetMethodReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
    {
        var typeDefinition = typeReference.Resolve();

        MethodDefinition methodDefinition;
        do
        {
            methodDefinition = typeDefinition.Methods.FirstOrDefault(predicate);
            typeDefinition = typeDefinition.BaseType?.Resolve();
        } while (methodDefinition == null && typeDefinition != null);

        return moduleDefinition.ImportReference(methodDefinition);
    }

    public MethodReference GetOptionalMethodReference(TypeReference typeReference, Func<MethodDefinition, bool> predicate)
    {
        var typeDefinition = typeReference.Resolve();

        MethodDefinition methodDefinition;
        do
        {
            methodDefinition = typeDefinition.Methods.FirstOrDefault(predicate);
            typeDefinition = typeDefinition.BaseType?.Resolve();
        } while (methodDefinition == null && typeDefinition != null);

        return null != methodDefinition ? moduleDefinition.ImportReference(methodDefinition) : null;
    }
}