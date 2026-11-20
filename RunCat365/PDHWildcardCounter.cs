using System.Runtime.InteropServices;

namespace RunCat365
{
    internal sealed partial class PDHWildcardCounter : IDisposable
    {
        private const uint ERROR_SUCCESS = 0;
        private const uint PDH_MORE_DATA = 0x800007D2;
        private const uint PDH_CSTATUS_VALID_DATA = 0x00000000;
        private const uint PDH_CSTATUS_NEW_DATA = 0x00000001;
        private const uint PDH_FMT_DOUBLE = 0x00000200;
        private const uint PDH_FMT_NOCAP100 = 0x00008000;
        private const int INITIAL_BUFFER_SIZE = 16 * 1024;

        private readonly Lock queryLock = new();
        private nint queryHandle;
        private readonly nint counterHandle;
        private byte[] buffer = new byte[INITIAL_BUFFER_SIZE];

        private PDHWildcardCounter(nint queryHandle, nint counterHandle)
        {
            this.queryHandle = queryHandle;
            this.counterHandle = counterHandle;
        }

        internal static PDHWildcardCounter? TryCreate(string counterPath)
        {
            if (PdhOpenQueryW(null, 0, out var query) != ERROR_SUCCESS) return null;

            if (PdhAddEnglishCounterW(query, counterPath, 0, out var counter) != ERROR_SUCCESS
                || PdhCollectQueryData(query) != ERROR_SUCCESS
                || CountInstances(counter) == 0)
            {
                _ = PdhCloseQuery(query);
                return null;
            }

            return new PDHWildcardCounter(query, counter);
        }

        private static unsafe uint CountInstances(nint counter)
        {
            uint bufferSize = 0;
            _ = PdhGetRawCounterArrayW(counter, ref bufferSize, out var itemCount, null);
            return itemCount;
        }

        internal unsafe List<float> ReadValues()
        {
            lock (queryLock)
            {
                if (queryHandle == 0) return [];
                if (PdhCollectQueryData(queryHandle) != ERROR_SUCCESS) return [];

                var bufferSize = (uint)buffer.Length;
                uint itemCount;
                uint status;
                fixed (byte* bufferPointer = buffer)
                {
                    status = PdhGetFormattedCounterArrayW(
                        counterHandle, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref bufferSize, out itemCount, bufferPointer
                    );
                }
                if (status == PDH_MORE_DATA)
                {
                    buffer = new byte[bufferSize];
                    fixed (byte* bufferPointer = buffer)
                    {
                        status = PdhGetFormattedCounterArrayW(
                            counterHandle, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref bufferSize, out itemCount, bufferPointer
                        );
                    }
                }
                if (status != ERROR_SUCCESS) return [];

                var values = new List<float>((int)itemCount);
                fixed (byte* bufferPointer = buffer)
                {
                    var items = (FormattedCounterValueItem*)bufferPointer;
                    for (var i = 0; i < itemCount; i++)
                    {
                        var value = items[i].Value;
                        if (value.CStatus is not (PDH_CSTATUS_VALID_DATA or PDH_CSTATUS_NEW_DATA)) continue;
                        values.Add((float)value.DoubleValue);
                    }
                }
                return values;
            }
        }

        public void Dispose()
        {
            lock (queryLock)
            {
                if (queryHandle == 0) return;
                _ = PdhCloseQuery(queryHandle);
                queryHandle = 0;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FormattedCounterValue
        {
            [FieldOffset(0)] public uint CStatus;
            [FieldOffset(8)] public double DoubleValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FormattedCounterValueItem
        {
            [FieldOffset(0)] public nint Name;
            [FieldOffset(8)] public FormattedCounterValue Value;
        }

        [LibraryImport("pdh.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint PdhOpenQueryW(string? dataSource, nint userData, out nint query);

        [LibraryImport("pdh.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint PdhAddEnglishCounterW(nint query, string fullCounterPath, nint userData, out nint counter);

        [LibraryImport("pdh.dll")]
        private static partial uint PdhCollectQueryData(nint query);

        [LibraryImport("pdh.dll")]
        private static unsafe partial uint PdhGetFormattedCounterArrayW(
            nint counter, uint format, ref uint bufferSize, out uint itemCount, byte* itemBuffer
        );

        [LibraryImport("pdh.dll")]
        private static unsafe partial uint PdhGetRawCounterArrayW(
            nint counter, ref uint bufferSize, out uint itemCount, byte* itemBuffer
        );

        [LibraryImport("pdh.dll")]
        private static partial uint PdhCloseQuery(nint query);
    }
}
