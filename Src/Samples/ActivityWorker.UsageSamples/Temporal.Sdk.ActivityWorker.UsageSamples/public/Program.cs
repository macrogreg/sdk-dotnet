using System;
using Temporal.Util;

namespace Temporal.Sdk.ActivityWorker.UsageSamples
{
    public class Program
    {
        public static void Main(string[] _)
        {
            Console.WriteLine($"RuntimeEnvironmentInfo: \n{RuntimeEnvironmentInfo.SingletonInstance}");

            Console.WriteLine($"\n{typeof(Program).FullName} has finished.\n");
        }
    }
}