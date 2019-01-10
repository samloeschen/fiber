using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Fiber
{ 
    public static class ManagedListExtensions
    {
        public static void AddRange<T>(this List<T> list, NativeArray<T> array)
            where T : struct
        {
            AddRange(list, array, array.Length);
        }

        public static unsafe void AddRange<T>(this List<T> list, NativeArray<T> array, int length)
            where T : struct
        {
            list.AddRange(array.GetUnsafeReadOnlyPtr(), length);
        }

        public static unsafe void AddRange<T>(this List<T> list, NativeList<T> nativeList)
            where T : struct
        {
            list.AddRange(nativeList.GetUnsafePtr(), nativeList.Length);
        }

        public static unsafe void AddRange<T>(this List<T> list, NativeSlice<T> nativeSlice)
            where T : struct
        {
            list.AddRange(nativeSlice.GetUnsafeReadOnlyPtr(), nativeSlice.Length);
        }

        public static unsafe void AddRange<T>(this List<T> list, DynamicBuffer<T> dynamicBuffer)
            where T : struct
        {
            list.AddRange(dynamicBuffer.GetUnsafePtr(), dynamicBuffer.Length);
        }

        public static unsafe void AddRange<T>(this List<T> list, void* arrayBuffer, int length)
            where T : struct {
            var index = list.Count;
            var newLength = index + length;

            // Resize our list if we require
            if (list.Capacity < newLength)
            {
                list.Capacity = newLength;
            }

            var items = NoAllocHelpers.ExtractArrayFromListT(list);
            var size = UnsafeUtility.SizeOf<T>();

            // Get the pointer to the end of the list
            var bufferStart = (IntPtr)UnsafeUtility.AddressOf(ref items[0]);
            var buffer = (byte*)(bufferStart + (size * index));

            UnsafeUtility.MemCpy(buffer, arrayBuffer, length * (long)size);

            NoAllocHelpers.ResizeList(list, newLength);
        }


        public static void Resize<T>(this List<T> list, int sz, T c = default)
        {
            int cur = list.Count;

            if (sz < cur)
            {
                list.RemoveRange(sz, cur - sz);
            }
            else if (sz > cur)
            {
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }

        public static void EnsureLength<T>(this List<T> list, int sz, T c = default)
        {
            int cur = list.Count;

            if (sz > cur)
            {
                list.AddRange(Enumerable.Repeat(c, sz - cur));
            }
        }
    }
}