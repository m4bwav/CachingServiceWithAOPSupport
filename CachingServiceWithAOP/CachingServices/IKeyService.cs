using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using Castle.DynamicProxy;

namespace CachingServiceWithAOP.CachingServices
{
    public interface IKeyService
    {
        string GenerateUniqueKeyForCall(IInvocation invocation);
    }

    public class DefaultCacheKeyService : IKeyService
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

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
            var parameterCount = invocation.Method.GetParameters().Count();

            if (parameterCount == 0)
                return;

            builder.Append("values:");

            for (var iii = 0; iii < parameterCount; iii++)
            {
                var value = invocation.GetArgumentValue(iii);

                var jsonValue = Serializer.Serialize(value);

                builder.Append(jsonValue + "|");
            }

            builder.Append(";");
        }

        private static void ProcessArgumentTypes(IInvocation invocation, StringBuilder builder)
        {
            var parameters = invocation.Method.GetParameters();

            if (!parameters.Any())
                return;

            var argumentTypes = parameters.Select(x => x.ParameterType.ToString() + ",");

            builder.Append("Argument Types:");

            foreach (var argumentTypeName in argumentTypes)
                builder.Append(argumentTypeName);

            builder.Append(";");
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

            if (genericArguments == null || !genericArguments.Any())
                return;

            builder.Append("Generic Arguments:");

            foreach (var arguments in genericArguments)
                builder.Append(arguments + ",");


            builder.Append(";");
        }
    }
}