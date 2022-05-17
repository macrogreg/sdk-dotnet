using System;
using System.IO;

namespace Temporal.TestUtil
{
    internal static class TestEnvironment
    {
        public static bool IsWindows
        {
            get { return (Environment.OSVersion.Platform.ToString().IndexOf("Win") == 0); }
        }

        public static string GetBinaryRootDirPath()
        {
            const string BinaryRootDirName = "_build";

            string currentFolder = Environment.CurrentDirectory;
            int offset = currentFolder.IndexOf(BinaryRootDirName, StringComparison.Ordinal);
            if (offset != -1)
            {
                return currentFolder.Substring(0, offset + BinaryRootDirName.Length);
            }

            throw new Exception($"Cannot find Binary Root Dir \"{BinaryRootDirName}\" above the current working dir \"{currentFolder}\"/");
        }

        public static string GetEnvironmentRootDirPath()
        {
            string binaryRootDirPath = TestEnvironment.GetBinaryRootDirPath();
            return Path.GetDirectoryName(binaryRootDirPath);
        }
    }
}
