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
            for (int i = _array.Length - 1; i >= 0; i--)
            {
                if (_array[i] == value) return i;
            }

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
            int result = 0;
            for (int i = _array.Length - 1; i >= 0; i--)
            {
                if (_array[i] != null) result++;
            }

            return result;
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