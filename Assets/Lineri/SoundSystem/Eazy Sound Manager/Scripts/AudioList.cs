using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lineri.SoundSystem
{
    public class ListAudio : ICollection, IEnumerable<Audio>
    {
        public int Count => _array.Length;
        public bool IsSynchronized => _array.IsSynchronized;
        public object SyncRoot => _array.SyncRoot;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;

        private int _capacity = 64;
        /// <summary>
        /// Do not specify less than 8. Workability is not guaranteed if the number is less than 8.
        /// </summary>
        public int Capacity
        {
            get => _capacity;
            set
            {
                _capacity = value;
                RecreateArray();
            }
        }

        private Audio[] _array;
        private int _lastIndex = 0;

        #region Constructors
        public ListAudio() 
        { 
            _array = new Audio[_capacity];
        }

        public ListAudio(int capacity)
        {
            _array = new Audio[capacity];
        }

        public ListAudio(Audio[] array)
        {
            _array = (Audio[])array.Clone();
            Capacity = array.Length < Capacity ? Capacity : array.Length;
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

        public IEnumerator<Audio> GetEnumerator()
        {
            int length = _array.Length;
            for (int i = 0; i < length; i++)
            {
                if (_array[i] != null) yield return _array[i];
            }
        }
        #endregion

        public Audio this[int index]
        {
            get
            {
                return _array[index];
            }
            set
            {
                _array[index] = value;
            }
        }

        public void Remove(int index)
        {
            _array[index] = null;
        }

        public void Clear()
        {
            for (int i = _array.Length - 1; i >= 0; i--)
            {
                _array[i] = null;
            }
        }

        public bool Contains(int index)
        {
            if (index < 0 || index >= _array.Length) return false;
            return _array[index] != null;
        }

        #region IndexOf
        public int IndexOf(Audio value)
        {
            for (int i = _array.Length - 1; i >= 7; i -= 8)
            {
                if (_array[i] == value) return i;
                else if (_array[i - 1] == value) return i - 1;
                else if (_array[i - 2] == value) return i - 2;
                else if (_array[i - 3] == value) return i - 3;
                else if (_array[i - 4] == value) return i - 4;
                else if (_array[i - 5] == value) return i - 5;
                else if (_array[i - 6] == value) return i - 6;
                else if (_array[i - 7] == value) return i - 7;
            }

            if (_array[0] == value) return 0;
            else if (_array[1] == value) return 1;
            else if (_array[2] == value) return 2;
            else if (_array[3] == value) return 3;
            else if (_array[4] == value) return 4;
            else if (_array[5] == value) return 5;
            else if (_array[6] == value) return 6;
            else if (_array[7] == value) return 7;

            return -1;
        }

        public int IndexOf(Predicate<Audio> predicate)
        {
            for (int i = _array.Length - 1; i >= 0; i--)
            {
                if (predicate(_array[i])) return i;
            }

            return -1;
        }

        public int IndexOf(AudioClip value)
        {
            for (int i = _array.Length - 1; i >= 0; i--)
            {
                if (_array[i]?.Clip == value) return i;
            }

            return -1;
        }
        #endregion

        public int Add(Audio audio)
        {
            int index = GetFreeIndex();
            _array[index] = audio;
            return index;
        }

        public int GetFreeIndex(bool reset = true)
        {
            int length = _array.Length;

            if (_lastIndex >= length - 1)
            {
                if (reset)
                {
                    ResetIndex();
                }
                else
                {
                    Capacity *= 2;
                }
            }

            if (_array[_lastIndex] == null)
            {
                return _lastIndex;
            }
            else
            {
                _lastIndex++;

                if (_array[_lastIndex] == null)
                {
                    return _lastIndex;
                }
            }

            for (int i = _lastIndex; i < length; i++)
            {
                if (_array[i] != null)
                {
                    _lastIndex++;
                }
                else
                {
                    return i;
                }
            }

            return GetFreeIndex(false);
        }

        public int GetCountNoNull()
        {
            int r0 = 0;
            int r1 = 0;
            int r2 = 0;
            int r3 = 0;
            int r4 = 0;
            int r5 = 0;
            int r6 = 0;
            int r7 = 0;

            int i = _array.Length - 1;
            for (; i >= 7; i -= 8)
            {
                if (_array[i] != null) r0++;
                if (_array[i - 1] != null) r1++;
                if (_array[i - 2] != null) r2++;
                if (_array[i - 3] != null) r3++;
                if (_array[i - 4] != null) r4++;
                if (_array[i - 5] != null) r5++;
                if (_array[i - 6] != null) r6++;
                if (_array[i - 7] != null) r7++;
            }

            if (i > 0 && _array[0] != null) r0++;
            if (i > 1 && _array[1] != null) r1++;
            if (i > 2 && _array[2] != null) r2++;
            if (i > 3 && _array[3] != null) r3++;
            if (i > 4 && _array[4] != null) r4++;
            if (i > 5 && _array[5] != null) r5++;
            if (i > 6 && _array[6] != null) r6++;
            if (i > 7 && _array[7] != null) r7++;

            return r0+r1+r2+r3+r4+r5+r6+r7;
        }

        private void ResetIndex()
        {
            _lastIndex = 0;
        }

        private void RecreateArray()
        {
            Audio[] result = new Audio[_capacity];

            for (int i = _array.Length - 1; i >= 0; i--)
            {
                result[i] = _array[i];
            }

            _array = result;
        }
    }
}