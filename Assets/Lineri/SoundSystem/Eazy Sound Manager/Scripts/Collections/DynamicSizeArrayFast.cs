using System;
using System.Collections;
using System.Collections.Generic;

namespace Lineri.SoundSystem
{
    public abstract class DynamicSizeArrayFast<T> : ICollection, IEnumerable<T>
    {
        protected int _count = 0;
        public int Count => _count;
        protected int _arrayLastIndex = 0;
        public bool IsSynchronized => _array.IsSynchronized;
        public object SyncRoot => _array.SyncRoot;

        protected int _capacity = 8;

        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                RecreateArray();
                _count = value;
                _arrayLastIndex = value - 1;
            }
        }

        protected T[] _array;
        protected int _lastIndex = -1;
        
        public T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }
        
        #region Constructors
        public DynamicSizeArrayFast()
        {
            _array = new T[_capacity];
            _count = _capacity;
            _arrayLastIndex = _capacity - 1;
        }

        public DynamicSizeArrayFast(int capacity)
        {
            _capacity = capacity;
            _array = new T[capacity];
            _count = capacity;
            _arrayLastIndex = capacity - 1;
        }

        public DynamicSizeArrayFast(T[] array)
        {
            _array = (T[])array.Clone();
            _capacity = array.Length;
            _count = _capacity;
            _arrayLastIndex = _capacity - 1;
        }

        #endregion

        public Audio[] ToArray()
        {
            return (Audio[])_array.Clone();
        }

        public void CopyTo(Array array, int index)
        {
            _array.CopyTo(array, index);
        }
        
        #region Enumerators
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            int length = Count;
            for (int i = 0; i < length; i++)
            {
                if (_array[i] != null) yield return _array[i];
            }
        }
        #endregion
        
        public void Clear()
        {
            for (int i = Count - 1; i >= 0; i--)
            {
                _array[i] = default(T);
            }
        }
        
        protected virtual void RecreateArray()
        {
            T[] result = new T[_capacity];

            for (int i = Count - 1; i >= 0; i--)
            {
                result[i] = _array[i];
            }

            _array = result;
        }
    }
}