using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodDecorator.Fody.Interfaces.Aspects;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace MethodDecorator.Fody {
    public class MethodDecorator {
        private readonly ReferenceFinder referenceFinder;

        public MethodDecorator(ModuleDefinition moduleDefinition, ReferenceFinder referenceFinder) {
            this.referenceFinder = referenceFinder;
        }

        public void Decorate(
            TypeDefinition type,
            MethodDefinition method,
            CustomAttribute attribute,
            bool explicitMatch,
            FieldDefinition attributeFieldDefinition,
            FieldDefinition methodFieldDefinition)
        {
            method.Body.InitLocals = true;

            var exceptionTypeRef = this.referenceFinder.ExceptionTypeRef;
            var parameterTypeRef = this.referenceFinder.ObjectTypeRef;
            var parametersArrayTypeRef = new ArrayType(parameterTypeRef);
            var boolTypeReference = referenceFinder.BoolTypeRef;
            var methodExecutionArgsTypeRef = referenceFinder.MethodExecutionArgsTypeRef;

            var initMethodRef1 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType, 
                md => md.Name == "Init" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(MethodBase).FullName);
            var initMethodRef2 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "Init" && md.Parameters.Count == 2 && md.Parameters[0].ParameterType.FullName == typeof(object).FullName );
            var initMethodRef3 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType, 
                md => md.Name == "Init" && md.Parameters.Count == 3);
            var initMethodRef4 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "Init" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName);
            var initMethodRef = initMethodRef4 ?? initMethodRef3 ?? initMethodRef2 ?? initMethodRef1;

            //var needBypassRef0 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
            //    md => md.Name == "NeedBypass" && md.Parameters.Count == 0);

            var onEntryMethodRef0 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType, 
                md => md.Name == "OnEntry" && md.Parameters.Count == 0 );
            var onEntryMethodRef1 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "OnEntry" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName);
            var onEntryMethodRef = onEntryMethodRef1 ?? onEntryMethodRef0;

            // OnExit()
            var onExitMethodRef0 = this.referenceFinder.GetOptionalMethodReference( attribute.AttributeType, 
                md => md.Name == "OnExit" && md.Parameters.Count == 0);
            // OnExit(object)
            var onExitMethodRef1 = this.referenceFinder.GetOptionalMethodReference( attribute.AttributeType, 
                md => md.Name == "OnExit" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(object).FullName);
            // OnExit(MethodExecutionArgs)
            var onExitMethodRef2 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "OnExit" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName);
            // OnExit(MethodExecutionArgs, object)
            var onExitMethodRef3 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "OnExit" && md.Parameters.Count == 2 && md.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName);

            var onExitWithoutRetVal = onExitMethodRef2 ?? onExitMethodRef0;
            var onExitWithRetVal = onExitMethodRef3 ?? onExitMethodRef1;

            var alterRetvalRef1 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "AlterRetval" && md.Parameters.Count == 1);

            var onExceptionMethodRef1 = this.referenceFinder.GetOptionalMethodReference( attribute.AttributeType, 
                md => md.Name == "OnException" && md.Parameters.Count == 1 && md.Parameters[0].ParameterType.FullName == typeof(Exception).FullName);
            var onExceptionMethodRef2 = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType,
                md => md.Name == "OnException" && md.Parameters.Count == 2 && md.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName);
            var onExceptionMethodRef = onExceptionMethodRef2 ?? onExceptionMethodRef1;

            var taskContinuationMethodRef = this.referenceFinder.GetOptionalMethodReference(attribute.AttributeType, md => md.Name == "OnTaskContinuation");

            var needBypass = (initMethodRef != null &&  boolTypeReference.FullName == initMethodRef.ReturnType.FullName);

            var useContextVariable = initMethodRef4 != null ||
                                     onEntryMethodRef1 != null ||
                                     onExitMethodRef2 != null ||
                                     onExitMethodRef3 != null ||
                                     onExceptionMethodRef2 != null;

            //var attributeVariableDefinition = AddVariableDefinition(method, "__fody$attribute", attribute.AttributeType);
            //var methodVariableDefinition = AddVariableDefinition(method, "__fody$method", methodBaseTypeRef);

            VariableDefinition exceptionVariableDefinition = null;
            VariableDefinition parametersVariableDefinition = null;
            VariableDefinition retvalVariableDefinition = null;
            VariableDefinition contextVariableDefinition = null;

            if (initMethodRef3 != null || useContextVariable)
            {
                parametersVariableDefinition = AddVariableDefinition(method, "__fody$parameters", parametersArrayTypeRef);
            }

            if (onExceptionMethodRef != null)
            {
                exceptionVariableDefinition = AddVariableDefinition(method, "__fody$exception", exceptionTypeRef);
            }

            bool needCatchReturn = needBypass ||
                (null != (onExitWithRetVal ?? onExitWithoutRetVal ?? onExceptionMethodRef ?? taskContinuationMethodRef ?? alterRetvalRef1));

            if (method.ReturnType.FullName != "System.Void" && needCatchReturn)
            {
                var returnType = method.ReturnType;

                if (returnType.IsOptionalModifier)
                    returnType = returnType.GetElementType();

                retvalVariableDefinition = AddVariableDefinition(method, "__fody$retval", returnType);
            }

            if (useContextVariable)
            {
                contextVariableDefinition = AddVariableDefinition(method, "__fody$context", methodExecutionArgsTypeRef);
            }

            MethodBodyRocks.SimplifyMacros(method.Body);

            var processor = method.Body.GetILProcessor();
            var methodBodyFirstInstruction = method.Body.Instructions.First();

            if (method.IsConstructor) {

                var callBase = method.Body.Instructions.FirstOrDefault(
                    i =>    (i.OpCode == OpCodes.Call) 
                            && (i.Operand is MethodReference) 
                            && ((MethodReference)i.Operand).Resolve().IsConstructor);

                methodBodyFirstInstruction = (callBase == null ? null : callBase.Next) ?? methodBodyFirstInstruction;
            }

            // If every code path ends up throwing an exception compiler uses rethrow instruction
            // at the end of the method and doesn't generate ret instruction.
            // In this case HandlerEnd instruction won't be specified for all root exception handlers.
            // This will mess up our generated exception handlers, so we need to generate ret instruction and
            // manually specify HandlerEnd instruction for such handlers.
            if (method.Body.HasExceptionHandlers)
            {
                Instruction lastInstruction = null;

                foreach (var exceptionHandler in method.Body.ExceptionHandlers.Where(h => h.HandlerEnd == null))
                {
                    if (lastInstruction == null)
                    {
                        lastInstruction = Instruction.Create(OpCodes.Ret);
                        processor.InsertAfter(method.Body.Instructions.Last(), lastInstruction);
                    }

                    exceptionHandler.HandlerEnd = lastInstruction;
                }
            }

            IEnumerable<Instruction> callInitInstructions = null,
                                     createParametersArrayInstructions = null,
                                     callOnEntryInstructions = null,
                                     saveRetvalInstructions = null,
                                     callOnExitInstructions = null,
                                     initContextVariableInstructions = null;

            if (parametersVariableDefinition != null)
            {
                createParametersArrayInstructions = CreateParametersArrayInstructions(
                    processor,
                    method,
                    parameterTypeRef,
                    parametersVariableDefinition);
            }

            if (contextVariableDefinition != null)
            {
                initContextVariableInstructions = CreateInitContextVariableInstructions(
                    processor,
                    type,
                    method,
                    methodFieldDefinition,
                    parametersVariableDefinition,
                    contextVariableDefinition);
            }

            VariableDefinition bypassVariableDefinition = null;

            if (initMethodRef != null)
            { 
                var instructions = GetCallInitInstructions(
                    processor,
                    type,
                    method,
                    attributeFieldDefinition,
                    methodFieldDefinition,
                    parametersVariableDefinition,
                    contextVariableDefinition,
                    initMethodRef);

                if (needBypass)
                {
                    bypassVariableDefinition = AddVariableDefinition(method, "__fody$needBypass", boolTypeReference);
                    instructions.Add(processor.Create(OpCodes.Stloc, bypassVariableDefinition));
                }

                callInitInstructions = instructions;
            }

            if (onEntryMethodRef != null)
            {
                callOnEntryInstructions = GetCallOnEntryInstructions(processor, attributeFieldDefinition, onEntryMethodRef, contextVariableDefinition);
            }

            if (retvalVariableDefinition != null)
            {
                saveRetvalInstructions = GetSaveRetvalInstructions(processor, retvalVariableDefinition);
            }

            if (onExitWithRetVal != null || onExitWithoutRetVal != null)
            {
                MethodReference onExitMethodRef;

                if (retvalVariableDefinition != null)
                {
                    // prioritize the call to OnExit(object)
                    onExitMethodRef = onExitWithRetVal ?? onExitWithoutRetVal;
                }
                else
                {
                    // prioritize the call to OnExit()
                    onExitMethodRef = onExitWithoutRetVal ?? onExitWithRetVal;
                }

                callOnExitInstructions = GetCallOnExitInstructions(processor, attributeFieldDefinition, onExitMethodRef, retvalVariableDefinition, contextVariableDefinition);
            }

            IEnumerable<Instruction> methodBodyReturnInstructions = null,
                                     tryCatchLeaveInstructions = null,
                                     catchHandlerInstructions = null,
                                     finallyHandlerInstructions = null,
                                     bypassInstructions = null;

            if (needCatchReturn)
            {
                methodBodyReturnInstructions = GetMethodBodyReturnInstructions(processor, attributeFieldDefinition, retvalVariableDefinition, alterRetvalRef1);

                if (callOnExitInstructions != null && onExceptionMethodRef == null)
                {
                    methodBodyReturnInstructions = callOnExitInstructions.Concat(methodBodyReturnInstructions);
                }

                if (needBypass)
                {
                    bypassInstructions = GetBypassInstructions(processor, bypassVariableDefinition, methodBodyReturnInstructions.First());
                }

                if (onExceptionMethodRef != null)
                {
                    tryCatchLeaveInstructions = GetTryCatchLeaveInstructions(processor, methodBodyReturnInstructions.First());
                    
                    catchHandlerInstructions = GetCatchHandlerInstructions(processor,
                        attributeFieldDefinition, exceptionVariableDefinition, onExceptionMethodRef, contextVariableDefinition, methodBodyReturnInstructions.First());

                    // generate empty finally block
                    finallyHandlerInstructions = new[] {
                        processor.Create(OpCodes.Nop),
                        processor.Create(OpCodes.Endfinally)
                    };

                    if (callOnExitInstructions != null)
                    {
                        // insert OnExit call to the finally block
                        finallyHandlerInstructions = callOnExitInstructions.Concat(finallyHandlerInstructions);
                    }
                }

                ReplaceRetInstructions(processor, (saveRetvalInstructions == null ? null : saveRetvalInstructions.FirstOrDefault()) ?? (tryCatchLeaveInstructions == null ? null : tryCatchLeaveInstructions.LastOrDefault()) ?? methodBodyReturnInstructions.First());
            }

            if (createParametersArrayInstructions!=null)
                processor.InsertBefore(methodBodyFirstInstruction, createParametersArrayInstructions);

            if (initContextVariableInstructions != null)
                processor.InsertBefore(methodBodyFirstInstruction, initContextVariableInstructions);

            if (callInitInstructions!=null) 
                processor.InsertBefore(methodBodyFirstInstruction, callInitInstructions);

            if(bypassInstructions != null)
                processor.InsertBefore(methodBodyFirstInstruction, bypassInstructions);

            if (callOnEntryInstructions != null)
                processor.InsertBefore(methodBodyFirstInstruction, callOnEntryInstructions);

            if (methodBodyReturnInstructions != null)
            {
                processor.InsertAfter(method.Body.Instructions.Last(), methodBodyReturnInstructions);

                if(saveRetvalInstructions!=null)
                    processor.InsertBefore(methodBodyReturnInstructions.First(), saveRetvalInstructions);

                if (taskContinuationMethodRef!=null)
                {
                    var taskContinuationInstructions = GetTaskContinuationInstructions(
                        processor,
                        retvalVariableDefinition,
                        attributeFieldDefinition,
                        taskContinuationMethodRef);

                    processor.InsertBefore(methodBodyReturnInstructions.First(), taskContinuationInstructions);
                }

                if (onExceptionMethodRef != null)
                {
                    processor.InsertBefore(methodBodyReturnInstructions.First(), tryCatchLeaveInstructions);
                    processor.InsertBefore(methodBodyReturnInstructions.First(), catchHandlerInstructions);
                    processor.InsertBefore(methodBodyReturnInstructions.First(), finallyHandlerInstructions);

                    method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                    {
                        CatchType = exceptionTypeRef,
                        TryStart = methodBodyFirstInstruction,
                        TryEnd = tryCatchLeaveInstructions.Last().Next,
                        HandlerStart = catchHandlerInstructions.First(),
                        HandlerEnd = catchHandlerInstructions.Last().Next
                    });

                    method.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                    {
                        TryStart = methodBodyFirstInstruction,
                        TryEnd = catchHandlerInstructions.Last().Next,
                        HandlerStart = finallyHandlerInstructions.First(),
                        HandlerEnd = finallyHandlerInstructions.Last().Next
                    });
                }
            }
            else
            {
                if (callOnExitInstructions != null)
                {
                    processor.InsertBefore(method.Body.Instructions.Last(), callOnExitInstructions);
                }
            }

            MethodBodyRocks.OptimizeMacros(method.Body);
        }

        private static VariableDefinition AddVariableDefinition(MethodDefinition method, string variableName, TypeReference variableType) {
            var variableDefinition = new VariableDefinition(variableType);
            method.Body.Variables.Add(variableDefinition);
            return variableDefinition;
        }

        private static IEnumerable<Instruction> CreateParametersArrayInstructions(ILProcessor processor, MethodDefinition method, TypeReference objectTypeReference /*object*/, VariableDefinition arrayVariable /*parameters*/) {
            var createArray = new List<Instruction> {
                processor.Create(OpCodes.Ldc_I4, method.Parameters.Count),  //method.Parameters.Count
                processor.Create(OpCodes.Newarr, objectTypeReference),      // new object[method.Parameters.Count]
                processor.Create(OpCodes.Stloc, arrayVariable)              // var objArray = new object[method.Parameters.Count]
            };

            foreach (var p in method.Parameters.Where(p => !p.IsOut))
                createArray.AddRange(IlHelper.ProcessParam(p, arrayVariable));

            return createArray;
        }

        private IEnumerable<Instruction> CreateInitContextVariableInstructions(
            ILProcessor processor,
            TypeDefinition typeDefinition,
            MethodDefinition methodDefinition,
            FieldDefinition methodFieldDefinition,
            VariableDefinition parametersVariableDefinition,
            VariableDefinition contextVariableDefinition)
        {
            var methodExecutionArgsTypeRef = referenceFinder.MethodExecutionArgsTypeRef;

            var methodExecutionArgsCtor = methodExecutionArgsTypeRef.Resolve()
                .GetConstructors()
                .First(m => m.Parameters.Count == 3 &&
                            m.Parameters[0].ParameterType.FullName == typeof(MethodBase).FullName &&
                            m.Parameters[1].ParameterType.FullName == typeof(object).FullName &&
                            m.Parameters[2].ParameterType.FullName == typeof(object[]).FullName);

            var methodExecutionArgsCtorRef = typeDefinition.Module.ImportReference(methodExecutionArgsCtor);

            var list = new List<Instruction>();

            list.Add(processor.Create(OpCodes.Ldsfld, methodFieldDefinition));

            if (methodDefinition.IsConstructor || methodDefinition.IsStatic)
            {
                list.Add(processor.Create(OpCodes.Ldnull));
            }
            else
            {
                list.Add(processor.Create(OpCodes.Ldarg_0));
                if (typeDefinition.IsValueType)
                {
                    list.Add(processor.Create(OpCodes.Box, typeDefinition));
                }
            }

            list.Add(processor.Create(OpCodes.Ldloc, parametersVariableDefinition));
            list.Add(processor.Create(OpCodes.Newobj, methodExecutionArgsCtorRef));
            list.Add(processor.Create(OpCodes.Stloc, contextVariableDefinition));

            return list;
        }

        private IEnumerable<Instruction> GetAttributeInstanceInstructions(
            ILProcessor processor,
            ICustomAttribute attribute,
            MethodDefinition method,
            FieldDefinition attributeFieldDefinition,
            FieldDefinition methodFieldDefinition,
            bool explicitMatch) {

            var getMethodFromHandleRef = this.referenceFinder.GetMethodReference(referenceFinder.MethodBaseTypeRef,
                md => md.Name == "GetMethodFromHandle" && md.Parameters.Count == 2);

            var getTypeof = this.referenceFinder.GetMethodReference(referenceFinder.SystemTypeRef,
                md => md.Name == "GetTypeFromHandle");

            var ctor = this.referenceFinder.GetMethodReference(referenceFinder.ActivatorTypeRef,
                md => md.Name == "CreateInstance" && md.Parameters.Count == 1);

            var getCustomAttrs = this.referenceFinder.GetMethodReference(referenceFinder.AttributeTypeRef, 
                md => md.Name == "GetCustomAttributes"  && 
                md.Parameters.Count == 2 && 
                md.Parameters[0].ParameterType.FullName == typeof(MemberInfo).FullName &&
                md.Parameters[1].ParameterType.FullName == typeof(Type).FullName);

            /* 
                    // Code size       23 (0x17)
                      .maxstack  1
                      .locals init ([0] class SimpleTest.IntersectMethodsMarkedByAttribute i)
                      IL_0000:  nop
                      IL_0001:  ldtoken    SimpleTest.IntersectMethodsMarkedByAttribute
                      IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
                      IL_000b:  call       object [mscorlib]System.Activator::CreateInstance(class [mscorlib]System.Type)
                      IL_0010:  castclass  SimpleTest.IntersectMethodsMarkedByAttribute
                      IL_0015:  stloc.0
                      IL_0016:  ret
            */

            var oInstructions = new List<Instruction>
                {
                    processor.Create(OpCodes.Nop),

                    processor.Create(OpCodes.Ldtoken, method),
                    processor.Create(OpCodes.Ldtoken, method.DeclaringType),
                    processor.Create(OpCodes.Call, getMethodFromHandleRef),      // Push method onto the stack, GetMethodFromHandle, result on stack
                    processor.Create(OpCodes.Stsfld, methodFieldDefinition),     // Store method in __fody$method

                    processor.Create(OpCodes.Nop),
                };

            if (explicitMatch &&
                method.CustomAttributes.Any(m => m.AttributeType.Equals(attribute.AttributeType)))
            {
                oInstructions.AddRange(new Instruction[]
                    {
                        processor.Create(OpCodes.Ldsfld, methodFieldDefinition),
                        processor.Create(OpCodes.Ldtoken, attribute.AttributeType),
                        processor.Create(OpCodes.Call,getTypeof),
                        processor.Create(OpCodes.Call,getCustomAttrs),

                        processor.Create(OpCodes.Dup),
                        processor.Create(OpCodes.Ldlen),
                        processor.Create(OpCodes.Ldc_I4_1),
                        processor.Create(OpCodes.Sub),

                        processor.Create(OpCodes.Ldelem_Ref),

                        processor.Create(OpCodes.Castclass, attribute.AttributeType),
                        processor.Create(OpCodes.Stsfld, attributeFieldDefinition),
                    });
            }
            else if (explicitMatch &&
                     method.DeclaringType.CustomAttributes.Any(m => m.AttributeType.Equals(attribute.AttributeType)))
            {
                oInstructions.AddRange(new Instruction[]
                    {
                        processor.Create(OpCodes.Ldtoken, method.DeclaringType),
                        processor.Create(OpCodes.Call,getTypeof),
                        processor.Create(OpCodes.Ldtoken, attribute.AttributeType),
                        processor.Create(OpCodes.Call,getTypeof),
                        processor.Create(OpCodes.Call,getCustomAttrs),

                        processor.Create(OpCodes.Dup),
                        processor.Create(OpCodes.Ldlen),
                        processor.Create(OpCodes.Ldc_I4_1),
                        processor.Create(OpCodes.Sub),

                        processor.Create(OpCodes.Ldelem_Ref),

                        processor.Create(OpCodes.Castclass, attribute.AttributeType),
                        processor.Create(OpCodes.Stsfld, attributeFieldDefinition),
                    });
            }
            else
            {
                oInstructions.AddRange(new Instruction[]
                    {
                        processor.Create(OpCodes.Ldtoken, attribute.AttributeType),
                        processor.Create(OpCodes.Call,getTypeof),
                        processor.Create(OpCodes.Call,ctor),
                        processor.Create(OpCodes.Castclass, attribute.AttributeType),
                        processor.Create(OpCodes.Stsfld, attributeFieldDefinition),
                    });
            }

            return oInstructions;
                   
        }

        private List<Instruction> GetCallInitInstructions(
            ILProcessor processor,
            TypeDefinition typeDefinition,
            MethodDefinition memberDefinition,
            FieldDefinition attributeFieldDefinition,
            FieldDefinition methodFieldDefinition,
            VariableDefinition parametersVariableDefinition,
            VariableDefinition contextVariableDefinition,
            MethodReference initMethodRef) {
            // Call __fody$attribute.Init(this, methodBase, args)

            var list = new List<Instruction>();

            // start with the attribute reference
            list.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));

            if (initMethodRef.Parameters.Count == 1 &&
                initMethodRef.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName)
            {
                // Call __fody$attribute.Init(context)

                list.Add(processor.Create(OpCodes.Ldloc, contextVariableDefinition));
            }
            else
            {
                // Call __fody$attribute.Init(this, methodBase, args)

                if (initMethodRef.Parameters.Count > 1)
                {
                    // then push the instance reference onto the stack
                    if (memberDefinition.IsConstructor || memberDefinition.IsStatic)
                    {
                        list.Add(processor.Create(OpCodes.Ldnull));
                    }
                    else
                    {
                        list.Add(processor.Create(OpCodes.Ldarg_0));
                        if (typeDefinition.IsValueType)
                        {
                            list.Add(processor.Create(OpCodes.Box, typeDefinition));
                        }
                    }
                }

                list.Add(processor.Create(OpCodes.Ldsfld, methodFieldDefinition));

                if (initMethodRef.Parameters.Count > 2)
                {
                    list.Add(processor.Create(OpCodes.Ldloc, parametersVariableDefinition));
                }
            }

            list.Add(processor.Create(OpCodes.Callvirt, initMethodRef));

            return list;
        }

        private static IEnumerable<Instruction> GetBypassInstructions(ILProcessor processor, VariableDefinition needBypassVar, Instruction exit)
        {
            return new List<Instruction>
                   {
                       processor.Create(OpCodes.Ldloc, needBypassVar),
                       processor.Create(OpCodes.Brfalse, exit),
                   };
        }

        private static IEnumerable<Instruction> GetCallOnEntryInstructions(
            ILProcessor processor,
            FieldDefinition attributeFieldDefinition,
            MethodReference onEntryMethodRef,
            VariableDefinition contextVariableDefinition) {
            // Call __fody$attribute.OnEntry()

            var instructions = new List<Instruction>();

            instructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));

            if (onEntryMethodRef.Parameters.Count == 1 &&
                onEntryMethodRef.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName)
            {
                instructions.Add(processor.Create(OpCodes.Ldloc, contextVariableDefinition));
            }

            instructions.Add(processor.Create(OpCodes.Callvirt, onEntryMethodRef));
            
            return instructions;
        }

        private static IList<Instruction> GetSaveRetvalInstructions(ILProcessor processor, VariableDefinition retvalVariableDefinition)
        {
            var oInstructions = new List<Instruction>();
            if (retvalVariableDefinition != null && processor.Body.Instructions.Any(i => i.OpCode == OpCodes.Ret))
            {
                if( !retvalVariableDefinition.VariableType.IsValueType &&
                    !retvalVariableDefinition.VariableType.IsGenericParameter)
                {
                    oInstructions.Add(processor.Create(OpCodes.Castclass, retvalVariableDefinition.VariableType));
                }
                oInstructions.Add(processor.Create(OpCodes.Stloc, retvalVariableDefinition));
            }
            return oInstructions;
    }

        private static IList<Instruction> GetCallOnExitInstructions(ILProcessor processor, FieldDefinition attributeFieldDefinition, MethodReference onExitMethodRef) {
            // Call __fody$attribute.OnExit()

            var instructions = new List<Instruction>();

            instructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));
            instructions.Add(processor.Create(OpCodes.Callvirt, onExitMethodRef));

            return instructions;
        }

        private static IList<Instruction> GetCallOnExitInstructions(ILProcessor processor, 
            FieldDefinition attributeFieldDefinition,
            MethodReference onExitMethodRef,
            VariableDefinition retvalVariableDefinition,
            VariableDefinition contextVariableDefinition)
        {
            var oInstructions = new List<Instruction>();

            oInstructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));

            if (onExitMethodRef.Parameters.Count > 0)
            {
                if (onExitMethodRef.Parameters[0].ParameterType.FullName == typeof(MethodExecutionArgs).FullName)
                {
                    oInstructions.Add(processor.Create(OpCodes.Ldloc, contextVariableDefinition));
                }

                if (onExitMethodRef.Parameters[0].ParameterType.FullName == typeof(object).FullName ||
                    onExitMethodRef.Parameters.Count > 1)
                {
                    if (retvalVariableDefinition != null)
                    {
                        oInstructions.Add(processor.Create(OpCodes.Ldloc, retvalVariableDefinition));

                        if (retvalVariableDefinition.VariableType.IsValueType ||
                            retvalVariableDefinition.VariableType.IsGenericParameter)
                        {
                            oInstructions.Add(processor.Create(OpCodes.Box, retvalVariableDefinition.VariableType));
                        }
                    }
                    else
                    {
                        oInstructions.Add(processor.Create(OpCodes.Ldnull));
                    }
                }
            }

            oInstructions.Add(processor.Create(OpCodes.Callvirt, onExitMethodRef));
            return oInstructions;
        }

        private static IList<Instruction> GetMethodBodyReturnInstructions(
            ILProcessor processor,
            FieldDefinition attributeFieldDefinition,
            VariableDefinition retvalVariableDefinition,
            MethodReference alterRetvalMethodRef)
        {
            var instructions = new List<Instruction>();
            if (retvalVariableDefinition != null)
            {
                if(alterRetvalMethodRef!=null)
                {
                    instructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));
                    instructions.Add(processor.Create(OpCodes.Ldloc, retvalVariableDefinition));

                    if (retvalVariableDefinition.VariableType.IsValueType ||
                        retvalVariableDefinition.VariableType.IsGenericParameter)
                    {
                        instructions.Add(processor.Create(OpCodes.Box, retvalVariableDefinition.VariableType));
                    }
                    instructions.Add(processor.Create(OpCodes.Callvirt,alterRetvalMethodRef));
                    instructions.Add(processor.Create(OpCodes.Unbox_Any, retvalVariableDefinition.VariableType));
                }
                else
                {
                    instructions.Add(processor.Create(OpCodes.Ldloc, retvalVariableDefinition));
                }
            }
            instructions.Add(processor.Create(OpCodes.Ret));
            return instructions;
        }

        private static IList<Instruction> GetTryCatchLeaveInstructions(ILProcessor processor, Instruction methodBodyReturnInstruction) {
            return new[] { processor.Create(OpCodes.Leave_S, methodBodyReturnInstruction) };
        }

        private static List<Instruction> GetCatchHandlerInstructions(
            ILProcessor processor,
            FieldDefinition attributeFieldDefinition,
            VariableDefinition exceptionVariableDefinition,
            MethodReference onExceptionMethodRef,
            VariableDefinition contextVariableDefinition,
            Instruction methodBodyReturnInstruction) {
            // Store the exception in __fody$exception
            // Call __fody$attribute.OnExcetion("{methodName}", __fody$exception)
            // rethrow
            
            var instructions = new List<Instruction>();

            instructions.Add(processor.Create(OpCodes.Stloc, exceptionVariableDefinition));
            instructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));

            if (onExceptionMethodRef.Parameters.Count > 1)
            {
                instructions.Add(processor.Create(OpCodes.Ldloc, contextVariableDefinition));
            }

            instructions.AddRange(
                new []
                {
                    processor.Create(OpCodes.Ldloc, exceptionVariableDefinition),
                    processor.Create(OpCodes.Callvirt, onExceptionMethodRef),
                    processor.Create(OpCodes.Rethrow),
                    processor.Create(OpCodes.Leave_S, methodBodyReturnInstruction)
                });

            return instructions;
        }

        private static void ReplaceRetInstructions(ILProcessor processor, Instruction methodEpilogueFirstInstruction) {
            // We cannot call ret inside a try/catch block. Replace all ret instructions with
            // an unconditional branch to the start of the OnExit epilogue
            var retInstructions = (from i in processor.Body.Instructions
                                   where i.OpCode == OpCodes.Ret
                                   select i).ToList();

            foreach (var instruction in retInstructions) {
                instruction.OpCode = OpCodes.Br;
                instruction.Operand = methodEpilogueFirstInstruction;
            }
        }

        private static IEnumerable<Instruction> GetTaskContinuationInstructions(ILProcessor processor, VariableDefinition retvalVariableDefinition, FieldDefinition attributeFieldDefinition, MethodReference taskContinuationMethodReference) {
            if (retvalVariableDefinition != null) {
                var tr = retvalVariableDefinition.VariableType;

                if (tr.FullName.Contains("System.Threading.Tasks.Task"))
                {
                    var instructions = new List<Instruction>();

                    instructions.AddRange(GetLoadFieldInstructions(processor, attributeFieldDefinition));
                    instructions.AddRange(
                        new[]
                        {
                            processor.Create(OpCodes.Ldloc, retvalVariableDefinition),
                            processor.Create(OpCodes.Callvirt, taskContinuationMethodReference),
                        });

                    return instructions;
                };
            }
            return new Instruction[0];
        }

        private static Instruction[] GetLoadFieldInstructions(ILProcessor processor, FieldDefinition attributeFieldDefinition)
        {
            return attributeFieldDefinition.IsStatic
                ? new[] { processor.Create(OpCodes.Ldsfld, attributeFieldDefinition) }
                : new[] { processor.Create(OpCodes.Ldarg_0), processor.Create(OpCodes.Ldfld, attributeFieldDefinition) };
        }
    }
}


