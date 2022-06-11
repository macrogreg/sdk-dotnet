using System;
using Temporal.Util;

namespace Temporal.Sdk.WorkflowClient.SimpleScenarios
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            (new SimpleClientScenarios()).Run();
        }
    }
}