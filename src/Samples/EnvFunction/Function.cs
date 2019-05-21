using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EnvFunction
{
    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public IDictionary<string, string> FunctionHandler(string input, ILambdaContext context)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                var envVars = Environment.GetEnvironmentVariables();
                var vars = new Dictionary<string, string>();
                foreach (var key in envVars.Keys)
                {
                    var value = (string) envVars[key];
                    var skey = (string) key;
                    vars[skey] = value;
                }

                return vars;
            }

            var envVar = Environment.GetEnvironmentVariable(input) ?? "[null]";
            return new Dictionary<string, string>
            {
                [input] = envVar
            };
        }
    }
}
