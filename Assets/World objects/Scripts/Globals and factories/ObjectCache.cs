
using System.Collections.Generic;

public class ObjectCache
{
    public SpecificCache<SelectedShipCard> ShipCards = new SpecificCache<SelectedShipCard>();

    public class SpecificCache<T>
    {
        public T Acquire()
        {
            return _cache.Dequeue();
        }

        public int Count => _cache.Count;

        public void Release(T t)
        {
            _cache.Enqueue(t);
        }

        private Queue<T> _cache = new Queue<T>();
    }
}

