using System;
using Temporal.Util;

namespace Temporal.Sdk.WorkflowClient.UsageSamples
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            (new Part1_SimpleClientUsage()).Run();
            (new Part2_AdvancedClientUsage()).Run();
            (new Part3_AddressIndividualRuns()).Run();

            Console.WriteLine($"\n{typeof(Program).FullName} has finished.\n");
        }
    }
}