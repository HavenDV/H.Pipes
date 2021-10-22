using System;
using System.Diagnostics;

namespace H.Pipes.AccessControl.Utilities
{
    /// <summary>
    /// Utilities related with <see cref="Process"/> class
    /// </summary>
    public static class ProcessUtilities
    {
        /// <summary>
        /// Gets the cleared name (if it is the name of the exe file)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetClearedApplicationName(string name)
        {
            name = name ?? throw new ArgumentNullException(nameof(name));

            return name.Contains('.')
                ? name.Substring(0, name.IndexOf(".", StringComparison.Ordinal))
                : name;
        }

        /// <summary>
        /// Gets the number of processes
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetProcessesCount(string name)
        {
            var cleanName = GetClearedApplicationName(name);

            var processes = Process.GetProcessesByName(cleanName);
            foreach (var process in processes)
            {
                process.Dispose();
            }

            return processes.Length;
        }

        /// <summary>
        /// Checks that there are no more processes with this name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool IsFirstProcess(string name)
        {
            return GetProcessesCount(name) == 1;
        }
    }
}
