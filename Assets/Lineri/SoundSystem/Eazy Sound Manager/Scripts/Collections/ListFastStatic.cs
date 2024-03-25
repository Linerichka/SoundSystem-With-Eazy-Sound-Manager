
namespace Lineri.SoundSystem
{
    public class ListFastStatic<T> : DynamicSizeArrayFast<T>
    {
        #region Constructors
        public ListFastStatic() : base()
        {
        }

        public ListFastStatic(int capacity) : base(capacity)
        {
        }

        public ListFastStatic(T[] array) : base(array)
        {
        }
        #endregion

        public void RemoveAt(int index)
        {
            _array[index] = default(T);
        }

        public bool Contains(int index)
        {
            if (index < 0 || index > _arrayLastIndex) return false;
            return _array[index] != null;
        }

        public int Add(T audio)
        {
            int index = GetFreeIndex();
            _array[index] = audio;
            return index;
        }

        public int GetFreeIndex(bool reset = true)
        {
            if (_lastIndex == _arrayLastIndex)
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

            _lastIndex++;

            if (_array[_lastIndex] == null)
            {
                return _lastIndex;
            }

            int length = Count;
            for (int i = _lastIndex; i < length; i++)
            {
                if (_array[i] == null)
                {
                    return i;
                }
                
                _lastIndex++;
            }

            _lastIndex--;
            
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

            int i = _arrayLastIndex;
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

            return r0 + r1 + r2 + r3 + r4 + r5 + r6 + r7;
        }

        private void ResetIndex()
        {
            _lastIndex = -1;
        }
    }
}