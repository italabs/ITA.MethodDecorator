using System;

namespace MethodDecorator.Fody.Interfaces.Aspects
{
    //
    // Сводка:
    //     List of standard roles.
    public static class StandardRoles
    {
        //
        // Сводка:
        //     Validation of field, property, or parameter value.
        public const string Validation = "Validation";
        //
        // Сводка:
        //     Tracing and logging.
        public const string Tracing = "Tracing";
        //
        // Сводка:
        //     Performance instrumentation (for instance performance counters).
        public const string PerformanceInstrumentation = "PerformanceInstrumentation";
        //
        // Сводка:
        //     Security enforcing (typically authorization).
        public const string Security = "Security";
        //
        // Сводка:
        //     Caching.
        public const string Caching = "Caching";
        //
        // Сводка:
        //     Transaction handling.
        public const string TransactionHandling = "Transaction";
        //
        // Сводка:
        //     Exception handling.
        public const string ExceptionHandling = "ExceptionHandling";
        //
        // Сводка:
        //     Data binding (for instance implementation of System.ComponentModel.INotifyPropertyChanged).
        public const string DataBinding = "DataBinding";
        //
        // Сводка:
        //     Object persistence (for instance Object-Relational Mapper).
        public const string Persistence = "Persistence";
        //
        // Сводка:
        //     Event broker (a system role used internally by PostSharp to realize the PostSharp.Aspects.IEventInterceptionAspect.OnInvokeHandler(PostSharp.Aspects.EventInterceptionArgs)
        //     handler).
        public const string EventBroker = "Event Broker";
        //
        // Сводка:
        //     Threading (locking).
        public const string Threading = "Threading";
    }

    public class ProvideAspectRoleAttribute : Attribute
    {
        //
        // Сводка:
        //     Initializes a new PostSharp.Aspects.Dependencies.ProvideAspectRoleAttribute.
        //
        // Параметры:
        //   role:
        //     Role.
        public ProvideAspectRoleAttribute(string role)
        {
            Role = role;
        }

        //
        // Сводка:
        //     Gets the role into which the aspect or advice to which this custom attribute
        //     is applied will be enrolled.
        public string Role { get; set; }
    }
}
