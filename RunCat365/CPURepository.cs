// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat365.Properties;
using System.Runtime.InteropServices;

namespace RunCat365
{
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            return $"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_User}: {cpuInfo.User:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Kernel}: {cpuInfo.Kernel:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Available}: {cpuInfo.Idle:f1}%", true)
            };
            return resultLines;
        }
    }

    internal partial class CPURepository
    {
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;
        private long previousIdleTime;
        private long previousKernelTime;
        private long previousUserTime;

        internal bool IsAvailable { get; }

        internal CPURepository()
        {
            IsAvailable = GetSystemTimes(out previousIdleTime, out previousKernelTime, out previousUserTime);
        }

        internal void Update()
        {
            if (!IsAvailable) return;
            if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime)) return;

            var idleDelta = idleTime - previousIdleTime;
            var kernelDelta = kernelTime - previousKernelTime;
            var userDelta = userTime - previousUserTime;
            previousIdleTime = idleTime;
            previousKernelTime = kernelTime;
            previousUserTime = userTime;

            var elapsed = kernelDelta + userDelta;
            if (elapsed <= 0) return;

            var total = Math.Clamp(100f * (elapsed - idleDelta) / elapsed, 0, 100);
            var user = Math.Clamp(100f * userDelta / elapsed, 0, 100);
            var kernel = Math.Clamp(100f * (kernelDelta - idleDelta) / elapsed, 0, 100);
            var idle = Math.Max(0, 100 - user - kernel);

            var cpuInfo = new CPUInfo
            {
                Total = total,
                User = user,
                Kernel = kernel,
                Idle = idle,
            };

            cpuInfoList.Add(cpuInfo);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            return new CPUInfo
            {
                Total = cpuInfoList.Average(x => x.Total),
                User = cpuInfoList.Average(x => x.User),
                Kernel = cpuInfoList.Average(x => x.Kernel),
                Idle = cpuInfoList.Average(x => x.Idle)
            };
        }

        internal void Close()
        {
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetSystemTimes(out long idleTime, out long kernelTime, out long userTime);
    }
}
