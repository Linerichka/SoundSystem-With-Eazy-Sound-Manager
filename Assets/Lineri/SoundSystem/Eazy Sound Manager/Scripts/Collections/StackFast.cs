namespace Lineri.SoundSystem
{
    public class StackFast<T> : DynamicSizeArrayFast<T>
    {
        public new int Count => _lastIndex + 1;

        #region Constructors

        public StackFast() : base()
        {
        }

        public StackFast(int capacity) : base(capacity)
        {
        }

        public StackFast(T[] array) : base(array)
        {
        }

        #endregion


        public void Enqueue(T value)
        {
            if (_lastIndex == _arrayLastIndex)
            {
                Capacity *= 2;
            }

            _lastIndex++;
            _array[_lastIndex] = value;
        }

        public bool TryDequeue(out T value)
        {
            if (_lastIndex < 0)
            {
                value = default(T);
                return false;
            }

            value = _array[_lastIndex];
            _lastIndex--;

            return true;
        }
    }
}