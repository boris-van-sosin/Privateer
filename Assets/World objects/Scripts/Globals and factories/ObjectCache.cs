
using System.Collections.Generic;
using UnityEngine;

public class ObjectCache
{
    public SpecificCache<SelectedShipCard> ShipCards = new SpecificCache<SelectedShipCard>();
    public Dictionary<(string, string), SpecificCache<ParticleSystem>> ParticleSystems = new Dictionary<(string, string), SpecificCache<ParticleSystem>>();

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

