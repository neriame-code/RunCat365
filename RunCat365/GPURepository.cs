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

namespace RunCat365
{
    struct GPUInfo
    {
        internal float Average { get; set; }
        internal float Maximum { get; set; }
    }

    internal static class GPUInfoExtension
    {
        internal static string GetDescription(this GPUInfo gpuInfo)
        {
            return $"{Strings.SystemInfo_GPU}: {gpuInfo.Maximum:f1}%";
        }

        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_GPU}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Average}: {gpuInfo.Average:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Maximum}: {gpuInfo.Maximum:f1}%", true)
            };
            return resultLines;
        }
    }

    internal class GPURepository
    {
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;
        private const string COUNTER_PATH = @"\GPU Engine(*engtype_3D)\Utilization Percentage";

        private readonly PDHWildcardCounter? counter;
        private readonly List<GPUInfo> gpuInfoList = [];

        internal bool IsAvailable => counter is not null;

        internal GPURepository()
        {
            counter = PDHWildcardCounter.TryCreate(COUNTER_PATH);
        }

        internal void Update()
        {
            if (counter is null) return;

            var values = counter.ReadValues();
            if (values.Count == 0) return;

            var gpuInfo = new GPUInfo
            {
                Average = Math.Min(100, values.Average()),
                Maximum = Math.Min(100, values.Max())
            };

            gpuInfoList.Add(gpuInfo);
            if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
            {
                gpuInfoList.RemoveAt(0);
            }
        }

        internal GPUInfo? Get()
        {
            if (counter is null || gpuInfoList.Count == 0) return null;

            return new GPUInfo
            {
                Average = gpuInfoList.Average(x => x.Average),
                Maximum = gpuInfoList.Max(x => x.Maximum)
            };
        }

        internal void Close()
        {
            counter?.Dispose();
        }
    }
}
