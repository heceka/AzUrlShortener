using System.Net;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Cloud5mins.ShortenerTools.Functions.Extensions
{
    internal static class FunctionContextExtensions
    {
        //public static void SetHttpResponseStatusCode(this FunctionContext context, HttpStatusCode statusCode)
        //{
        //    // Terrible reflection code since I haven't found a nicer way to do this...
        //    // For some reason the types are marked as internal
        //    // If there's code that will break in this sample,
        //    // it's probably here.
        //    var coreAssembly = Assembly.Load("Microsoft.Azure.Functions.Worker.Core");
        //    var featureInterfaceName = "Microsoft.Azure.Functions.Worker.Context.Features.IFunctionBindingsFeature";
        //    var featureInterfaceType = coreAssembly.GetType(featureInterfaceName);
        //    var bindingsFeature = context.Features.Single(
        //        f => f.Key.FullName == featureInterfaceType.FullName).Value;
        //    var invocationResultProp = featureInterfaceType.GetProperty("InvocationResult");

        //    var grpcAssembly = Assembly.Load("Microsoft.Azure.Functions.Worker.Grpc");
        //    var responseDataType = grpcAssembly.GetType("Microsoft.Azure.Functions.Worker.GrpcHttpResponseData");
        //    var responseData = Activator.CreateInstance(responseDataType, context, statusCode);

        //    invocationResultProp.SetMethod.Invoke(bindingsFeature, new object[] { responseData });
        //}

        internal static async Task SetHttpResponseStatusCodeAsync(this FunctionContext context, HttpStatusCode statusCode)
        {
            // To access the RequestData
            var requestData = await context.GetHttpRequestDataAsync();

            // To set the ResponseData
            if (requestData == null)
                return;

            var responseData = requestData.CreateResponse(statusCode);
            await responseData.WriteStringAsync("");
            context.GetInvocationResult().Value = responseData;
        }

        internal static MethodInfo GetTargetFunctionMethod(this FunctionContext context)
        {
            // More terrible reflection code..
            // Would be nice if this was available out of the box on FunctionContext

            // This contains the fully qualified name of the method
            // E.g. IsolatedFunctionAuth.TestFunctions.ScopesAndAppRoles
            var entryPoint = context.FunctionDefinition.EntryPoint;

            var assemblyPath = context.FunctionDefinition.PathToAssembly;
            var assembly = Assembly.LoadFrom(assemblyPath);
            var typeName = entryPoint.Substring(0, entryPoint.LastIndexOf('.'));
            var type = assembly.GetType(typeName);
            var methodName = entryPoint.Substring(entryPoint.LastIndexOf('.') + 1);
            var method = type.GetMethod(methodName);
            return method;
        }
    }
}
