using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using EMG.Lambda.LocalRunner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LambdaExec
{
    class Program
    {
        static async Task<int> Main(
            string defaultsJson = "aws-lambda-tools-defaults.json", 
            bool debugMode = false, 
            int? port = null)
        {
            if (string.IsNullOrWhiteSpace(defaultsJson))
            {
                Console.WriteLine("Path for json definition is missing!");
                return -1;
            }

            if (!File.Exists(defaultsJson))
            {
                Console.WriteLine($"Could not find {defaultsJson}");
                return -2;
            }

            if (port != null && (port < 0 || port > 65535))
            {
                Console.WriteLine($"Invalid port {port}, it should be a number between 0 and 65535");
                return -3;
            }

            LambdaManifest manifest;
            using (var manifestStream = File.OpenRead(defaultsJson))
            {
                var result = -99;
                (result, manifest) = await ReadManifestAsync(manifestStream);
                if (result != 0)
                {
                    return result;
                }
            }

            var assemblyRoot = Path.GetDirectoryName(defaultsJson);

            if (!TryFindAssembly(assemblyRoot, debugMode, manifest, out var reflectionInfo))
            {
                return -7;
            }

            IRunner runner;
            try
            {
                runner = BuildRunner(reflectionInfo.lambdaType, reflectionInfo.method, port);
            }
            catch
            {
                return -8;
            }

            await runner.RunAsync();
            return 0;
        }

        private static IRunner BuildRunner(Type lambdaType, MethodInfo method, int? port)
        {
            var returnType = method.ReturnType;
            var paramTypes = method.GetParameters();
            var inputType = GetInputType(paramTypes);
            var isAsync = false;

            if (returnType == typeof(void) || returnType == typeof(Task))
            {
                Console.WriteLine("Target lambda must return a value");
                throw new InvalidOperationException();
            }

            var builder = LambdaRunner.Create();
            
            if (port.HasValue)
            {
                builder = builder.UsePort(port.Value);
            }

            if (returnType.IsGenericType && 
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                returnType = returnType.GetGenericArguments()[0];
                isAsync = true;
            }

            object receivingBuilder = inputType == null
                ? builder.Receives<object>()
                : BuildReceivingBuilder(builder, inputType);

            var returningBuilder = receivingBuilder
                .GetType()
                .GetMethod("Returns")
                .MakeGenericMethod(returnType)
                .Invoke(receivingBuilder, null);

            var lambdaDelegate = BuildHandler(lambdaType, method, inputType);

            var usesFunctionMethodName = isAsync
                ? "UsesAsyncFunction"
                : "UsesFunction";
            var runnerBuilder = returningBuilder
                .GetType()
                .GetMethod(usesFunctionMethodName)
                .MakeGenericMethod(lambdaType)
                .Invoke(returningBuilder, new[] { lambdaDelegate.Compile() });

            var runner = runnerBuilder
                .GetType()
                .GetMethod("Build")
                .Invoke(runnerBuilder, null);

            return (IRunner) runner;
        }

        private static LambdaExpression BuildHandler(Type lambdaType, MethodInfo method, Type inputType)
        {
            var functionParamExpression = Expression.Parameter(lambdaType, "function");
            var inputParamExpression = inputType == null
                ? Expression.Parameter(typeof(object), "_")
                : Expression.Parameter(inputType, "input");
            var contextParamExpression = Expression.Parameter(typeof(ILambdaContext), "context");

            var lambdaParams = new[]
            {
                functionParamExpression,
                inputParamExpression,
                contextParamExpression
            };

            var args = method
                .GetParameters()
                .Select(p =>
                {
                    if (p.ParameterType == inputType)
                        return inputParamExpression;
                    if (p.ParameterType == typeof(ILambdaContext))
                        return contextParamExpression;
                    throw new InvalidOperationException($"Invalid parameter type! {p.ParameterType.Name}");
                });

            var handlerCallExpression = Expression.Call(functionParamExpression, method, args);
            var lambdaExpression = Expression.Lambda(handlerCallExpression, null, lambdaParams);
            return lambdaExpression;
        }

        private static object BuildReceivingBuilder(IRunnerBuilder builder, Type inputType)
        {
            return typeof(IRunnerBuilder)
                .GetMethod("Receives")
                .MakeGenericMethod(inputType)
                .Invoke(builder, null);
        }

        private static Type GetInputType(ParameterInfo[] paramTypes)
        {
            switch (paramTypes.Length)
            {
                case 0:
                case 1 when paramTypes[0].ParameterType == typeof(ILambdaContext):
                    return null;
                case 2 when paramTypes[0].ParameterType == typeof(ILambdaContext):
                    return paramTypes[1].ParameterType;
                case 2:
                    return paramTypes[0].ParameterType;
                default:
                    throw new ArgumentException("Invalid parameter types");
            }
        }

        private static bool TryFindAssembly(string assemblyRoot, bool debugMode, LambdaManifest manifest, out (Type lambdaType, MethodInfo method) reflectionInfo)
        {
            var config = debugMode
                ? "Debug"
                : manifest.Configuration;

            var expectedPath = Path.Combine(assemblyRoot, "bin", config, manifest.Framework, manifest.Entrypoint.AssemblyName + ".dll");
            if (File.Exists(expectedPath))
            {
                if (TryLoadAssembly(expectedPath, manifest.Entrypoint, out var info))
                {
                    reflectionInfo = info;
                    return true;
                }
                else
                {
                    Console.WriteLine($"Found an assembly, but could not find the expected handler at {expectedPath}\nLooking elsewhere...");
                }
            }
            else
            {
                Console.WriteLine($"Assembly not found at the expected path {expectedPath}\nLooking elsewhere...");
            }

            throw new NotImplementedException();
        }

        private static bool TryLoadAssembly(string path, EntrypointName entrypoint, out (Type lambdaType, MethodInfo method) reflectionInfo)
        {
            var assemblyName = Assembly.LoadFrom(path);

            var lambdaType = assemblyName.GetType(entrypoint.ClassName);

            if (lambdaType == null)
            {
                reflectionInfo = default;
                return false;
            }

            var methodInfo = lambdaType.GetMethod(entrypoint.MethodName);

            if (methodInfo == null)
            {
                reflectionInfo = default;
                return false;
            }

            reflectionInfo = (lambdaType, methodInfo);
            return true;
        }

        private static async Task<(int result, LambdaManifest manifest)> ReadManifestAsync(Stream manifestStream)
        {
            JToken json = null;

            using (var sr = new StreamReader(manifestStream))
            using (var jr = new JsonTextReader(sr))
            {
                json = await JObject.ReadFromAsync(jr);

                if (json == null || json.Type != JTokenType.Object)
                {
                    Console.WriteLine("Invalid JSON");
                    return (-4, default);
                }

                if (!TryGetValue<string>("function-handler", out var handlerValue) ||
                    !TryGetValue<string>("framework", out var framework) ||
                    !TryGetValue<string>("configuration", out var configuration))
                {
                    return (-5, default);
                }

                if (!TryGetValue<int>("function-memory-size", out var memorySize))
                {
                    Console.WriteLine("    Assuming default (256 MB)");
                    memorySize = 256;
                }

                if (!TryGetValue<int>("function-timeout", out var timeout))
                {
                    Console.WriteLine("    Assuming default (30 s)");
                    timeout = 30;
                }

                if (!EntrypointName.TryParse(handlerValue, out var entrypoint))
                {
                    Console.WriteLine($"Invalid entrypoint in manifest (key 'function-handler')\n    {handlerValue}");
                    return (-6, default);
                }

                var manifest = new LambdaManifest(configuration, framework, memorySize, timeout, entrypoint);
                return (0, manifest);
            }

            bool TryGetValue<T>(string key, out T result)
            {
                var token = json.SelectToken(key);
                if (token == null)
                {
                    Console.WriteLine($"Could not find key '{key}' in JSON");
                    result = default;
                    return false;
                }

                var value = token.Value<T>();
                result = value;
                return true;
            }
        }
    }
}
