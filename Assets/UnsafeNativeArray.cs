using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;

[NativeContainerSupportsMinMaxWriteRestriction]
[DebuggerDisplay("Length = {Length}")]
[NativeContainer]
[DebuggerTypeProxy(typeof(NativeArrayTDebugView<>))]
public unsafe struct UnsafeNativeArray<T> : IDisposable, IEnumerable<T> where T : struct
{
    [NativeDisableUnsafePtrRestriction] private void* _arrayPointer;
    internal int m_MinIndex;
    internal int m_MaxIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    internal AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
#endif
    
    public UnsafeNativeArray(T[] array)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!UnsafeUtility.IsBlittable<T>())
            throw new ArgumentException(
                $"{typeof(T) as object} used in {nameof(NativeArray<T>)} must be blittable");

        if (array == null)
            throw new ArgumentNullException(nameof(array));
#endif

        var length = array.Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be > 0");
#endif

        _arrayPointer = UnsafeUtility.AddressOf(ref array[0]);
        
        Length = length;

        m_MinIndex = 0;
        m_MaxIndex = length - 1;

        // Create a dispose sentinel to track memory leaks. This also creates the AtomicSafetyHandle
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 1, Allocator.Persistent);
#endif
    }

    public int Length { get; private set; }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private void CheckElementReadAccess(int index)
    {
        if (index < m_MinIndex || index > m_MaxIndex)
            FailOutOfRangeError(index);
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
    }

    private void CheckElementWriteAccess(int index)
    {
        if (index < m_MinIndex || index > m_MaxIndex)
            FailOutOfRangeError(index);
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
    }
#endif

    public T this[int index]
    {
        get
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckElementReadAccess(index);
#endif
            return UnsafeUtility.ReadArrayElement<T>(_arrayPointer, index);
        }
        [WriteAccessRequired]
        set
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckElementWriteAccess(index);
#endif
            UnsafeUtility.WriteArrayElement(_arrayPointer, index, value);
        }
    }

    public bool IsCreated => (IntPtr) _arrayPointer != IntPtr.Zero;

    [WriteAccessRequired]
    public void Dispose()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
        _arrayPointer = null;
        Length = 0;
    }

    [WriteAccessRequired]
    public void CopyFrom(T[] array)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
        if (Length != array.Length)
            throw new ArgumentException("array.Length does not match the length of this instance");
        for (var index = 0; index < Length; ++index)
            UnsafeUtility.WriteArrayElement(_arrayPointer, index,  array[index]);
    }

    public void CopyTo(T[] array)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
        if (Length != array.Length)
            throw new ArgumentException("array.Length does not match the length of this instance");

        for (var index = 0; index < Length; ++index)
            array[index] = UnsafeUtility.ReadArrayElement<T>(_arrayPointer, index);
    }

    private void FailOutOfRangeError(int index)
    {
        if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
            throw new IndexOutOfRangeException(
                $"Index {index} is out of restricted IJobParallelFor range [{m_MinIndex}...{m_MaxIndex}] in ReadWriteBuffer.\nReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
        throw new IndexOutOfRangeException($"Index {index} is out of range of '{Length}' Length.");
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    [ExcludeFromDocs]
    public struct Enumerator : IEnumerator<T>
    {
        private readonly UnsafeNativeArray<T> _mArray;
        private int _mIndex;

        public Enumerator(ref UnsafeNativeArray<T> array)
        {
            _mArray = array;
            _mIndex = -1;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            ++_mIndex;
            return _mIndex < _mArray.Length;
        }

        public void Reset()
        {
            _mIndex = -1;
        }

        public T Current => _mArray[_mIndex];

        object IEnumerator.Current => Current;
    }
}

// Visualizes the custom array in the C# debugger
internal sealed class NativeArrayTDebugView<T> where T : struct
{
    private UnsafeNativeArray<T> _array;

    public NativeArrayTDebugView(UnsafeNativeArray<T> array)
    {
        _array = array;
    }

    public T[] Items => _array.ToArray();
}
