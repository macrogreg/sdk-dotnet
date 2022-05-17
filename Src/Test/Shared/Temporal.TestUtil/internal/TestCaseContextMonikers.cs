using System;
using System.Runtime.CompilerServices;
using Temporal.Util;

namespace Temporal.TestUtil
{
    internal sealed class TestCaseContextMonikers
    {
        private readonly DateTimeOffset _startTime;
        private readonly string _startTimeString;

        public TestCaseContextMonikers()
            : this(DateTimeOffset.Now)
        {
        }

        public TestCaseContextMonikers(DateTimeOffset startTime)
        {
            _startTime = startTime;
            _startTimeString = Format.AsReadablePreciseLocal(startTime);
        }

        public DateTimeOffset StartTime
        {
            get { return _startTime; }
        }

        public string IdBase(object testClass, [CallerMemberName] string testMethodName = null)
        {
            string testClassName = testClass?.GetType().Name ?? "Test";
            testMethodName ??= "Fact";

            return $"{testClassName}.{testMethodName}[{_startTimeString}]";
        }

        public string ForWorkflowId(object testClass, [CallerMemberName] string testMethodName = null)
        {
            string idBase = IdBase(testClass, testMethodName);
            return $"WfId:{idBase}";
        }

        public string ForTaskQueue(object testClass, [CallerMemberName] string testMethodName = null)
        {
            string idBase = IdBase(testClass, testMethodName);
            return $"TaskQ:{idBase}";
        }
    }
}
