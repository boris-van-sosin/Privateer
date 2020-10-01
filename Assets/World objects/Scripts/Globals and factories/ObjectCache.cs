
using StagPoint.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCache
{
    public ObjectCache()
    {
        ShipCards = new SpecificCache<SelectedShipCard>();
        ProjectileCache = new SpecificCache<Projectile>();
        HarpaxCache = new SpecificCache<HarpaxBehavior>();
        TorpedoCache = new SpecificCache<Torpedo>();
        HarpaxCableCache = new SpecificCache<CableBehavior>();
    }

    public SpecificCache<SelectedShipCard> ShipCards { get; private set; }
    private Dictionary<(string, string), CacheWithRecycler<ParticleSystem>> _particleSystemCache = new Dictionary<(string, string), CacheWithRecycler<ParticleSystem>>();

    public SpecificCache<Projectile> ProjectileCache { get; private set; }
    public SpecificCache<HarpaxBehavior> HarpaxCache { get; private set; }
    public SpecificCache<Torpedo> TorpedoCache { get; private set; }
    public SpecificCache<CableBehavior> HarpaxCableCache { get; private set; }

    public CacheWithRecycler<ParticleSystem> GetOrCreateParticleSystemCache(string assetBundleSource, string asset)
    {
        CacheWithRecycler<ParticleSystem> currCache;
        if (!_particleSystemCache.TryGetValue((assetBundleSource, asset), out currCache))
        {
            _particleSystemCache.Add((assetBundleSource, asset), currCache = new CacheWithRecycler<ParticleSystem>());
        }
        return currCache;
    }

    public void RecycleParticleSystem(string assetBundleSource, string asset, ParticleSystem ps, float time)
    {
        CacheWithRecycler<ParticleSystem> currCache;
        if (!_particleSystemCache.TryGetValue((assetBundleSource, asset), out currCache))
        {
            _particleSystemCache.Add((assetBundleSource, asset), currCache = new CacheWithRecycler<ParticleSystem>());
        }
        currCache.Recycle(ps, time);
    }

    public void AdvanceParticleSystemRecycler(string assetBundleSource, string asset, float time)
    {
        CacheWithRecycler<ParticleSystem> currCache;
        if (_particleSystemCache.TryGetValue((assetBundleSource, asset), out currCache))
        {
            currCache.AdvanceRecycler(time);
        }
    }

    public void AdvanceAllParticleSystemRecyclers(float time)
    {
        foreach (CacheWithRecycler<ParticleSystem> currRecycler in _particleSystemCache.Values)
        {
            currRecycler.AdvanceRecycler(time);
        }
    }

    public void ClearAll()
    {
        ShipCards.Clear();
        ProjectileCache.Clear();
        HarpaxCableCache.Clear();
        TorpedoCache.Clear();
        HarpaxCache.Clear();
        foreach (CacheWithRecycler<ParticleSystem> particleSysCache in _particleSystemCache.Values)
        {
            particleSysCache.Clear();
        } 
    }

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

        public void Clear()
        {
            _cache.Clear();
        }

        private Queue<T> _cache = new Queue<T>();
    }

    public class CacheWithRecycler<T> where T : Component
    {
        public CacheWithRecycler(bool deactivateOnRecycle)
        {
            _deactivateOnRecycle = deactivateOnRecycle;
        }

        public CacheWithRecycler() : this(true)
        {
        }

        public T Acquire()
        {
            return _cache.Acquire();
        }

        public void Recycle(T t, float time)
        {
            _recycler.Add(t, time);
        }

        public void Recycle(T t)
        {
            _cache.Release(t);
        }

        public void AdvanceRecycler(float time)
        {
            while (_recycler.Count > 0)
            {
                (T, float) next = _recycler.PeekWithCost();
                if (time < next.Item2)
                {
                    break;
                }
                else
                {
                    if (_deactivateOnRecycle)
                    {
                        next.Item1.gameObject.SetActive(false);
                    }
                    _cache.Release(next.Item1);
                    _recycler.Remove();
                }
            }
        }

        public void Clear()
        {
            _cache.Clear();
            _recycler.Clear();
        }

        public int Count => _cache.Count;

        private SpecificCache<T> _cache = new SpecificCache<T>();
        private BinaryMinHeap<T, float> _recycler = new BinaryMinHeap<T, float>();
        private bool _deactivateOnRecycle;
    }
}

