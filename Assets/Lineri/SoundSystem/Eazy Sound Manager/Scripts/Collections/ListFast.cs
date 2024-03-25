namespace Lineri.SoundSystem
{
    public class ListFast<T> : DynamicSizeArrayFast<T>
    {
        public new int Count => _lastIndex + 1;

        #region Constructors
        public ListFast() : base()
        {
        }

        public ListFast(int capacity) : base(capacity)
        {
        }

        public ListFast(T[] array) : base(array)
        {
        }
        #endregion
        
        public bool Contains(int index)
        {
            if (index < 0 || index > _lastIndex) return false;
            return _array[index] != null;
        }
        
        public void Add(T value)
        {
            if (_lastIndex == _arrayLastIndex)
            {
                Capacity *= 2;
            }
            
            _lastIndex++;
            _array[_lastIndex] = value;
        }
        
        public void RemoveAt(int index)
        {
            _array[index] = _array[_lastIndex];
            _lastIndex--;
        }
    }
}