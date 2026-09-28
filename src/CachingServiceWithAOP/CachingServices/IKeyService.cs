using System.Linq;
using System.Text;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    /// <summary>Makes the cache key of an intercepted call.</summary>
    public interface IKeyService
    {
        /// <summary>A key that is equal for calls that may share a cached result.</summary>
        /// <param name="invocation">The intercepted call.</param>
        /// <returns>The key.</returns>
        string GenerateUniqueKeyForCall(IInvocation invocation);
    }

    /// <summary>
    /// The 1.x key: the target's <c>ToString()</c>, the method name, its generic arguments, its parameter types and each
    /// argument as JSON in the format of .NET Framework's JavaScriptSerializer, byte for byte as 1.0.1 made it. An argument
    /// that cannot be written (a reference cycle, nesting deeper than 100, a dictionary with non-string keys, more than
    /// 2 097 152 characters) throws as it did in 1.0.1; the interceptor then runs the call without caching it.
    /// </summary>
    public class DefaultCacheKeyService : IKeyService
    {
        /// <inheritdoc />
        public string GenerateUniqueKeyForCall(IInvocation invocation)
        {
            var builder = new StringBuilder();

            ProcessClassAndMethod(invocation, builder);

            ProcessGenericArguments(invocation, builder);

            ProcessArguments(invocation, builder);

            return builder.ToString();
        }

        private static void ProcessArguments(IInvocation invocation, StringBuilder builder)
        {
            ProcessArgumentTypes(invocation, builder);

            ProcessArgumentValues(invocation, builder);
        }

        private static void ProcessArgumentValues(IInvocation invocation, StringBuilder builder)
        {
            var parameterCount = invocation.Method.GetParameters().Length;

            if (parameterCount == 0)
            {
                return;
            }

            builder.Append("values:");

            for (var i = 0; i < parameterCount; i++)
            {
                var value = invocation.GetArgumentValue(i);

                var jsonValue = ScriptJson.Serialize(value);

                builder.Append(jsonValue + "|");
            }

            builder.Append(';');
        }

        private static void ProcessArgumentTypes(IInvocation invocation, StringBuilder builder)
        {
            var parameters = invocation.Method.GetParameters();

            if (parameters.Length == 0)
            {
                return;
            }

            var argumentTypes = parameters.Select(x => x.ParameterType.ToString() + ",");

            builder.Append("Argument Types:");

            foreach (var argumentTypeName in argumentTypes)
            {
                builder.Append(argumentTypeName);
            }

            builder.Append(';');
        }

        private static void ProcessClassAndMethod(IInvocation invocation, StringBuilder builder)
        {
            var className = invocation.InvocationTarget + ";";

            builder.Append(className);

            var methodName = invocation.Method.Name + ";";

            builder.Append(methodName);
        }

        private static void ProcessGenericArguments(IInvocation invocation, StringBuilder builder)
        {
            var genericArguments = invocation.GenericArguments;

            if (genericArguments == null || genericArguments.Length == 0)
            {
                return;
            }

            builder.Append("Generic Arguments:");

            foreach (var arguments in genericArguments)
            {
                builder.Append(arguments + ",");
            }

            builder.Append(';');
        }
    }
}
