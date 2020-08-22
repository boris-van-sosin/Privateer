
using StagPoint.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCache
{
    public SpecificCache<SelectedShipCard> ShipCards = new SpecificCache<SelectedShipCard>();
    private Dictionary<(string, string), SpecificCache<ParticleSystem>> _particleSystems = new Dictionary<(string, string), SpecificCache<ParticleSystem>>();
    private Dictionary<(string, string), BinaryMinHeap<ParticleSystem, float>> _particleSystemRecyclers = new Dictionary<(string, string), BinaryMinHeap<ParticleSystem, float>>();

    public SpecificCache<ParticleSystem> GetOrCreateParticleSystemCache(string assetBundleSource, string asset)
    {
        SpecificCache<ParticleSystem> currCache;
        if (!_particleSystems.TryGetValue((assetBundleSource, asset), out currCache))
        {
            _particleSystems.Add((assetBundleSource, asset), currCache = new SpecificCache<ParticleSystem>());
        }
        return currCache;
    }

    public void RecycleParticleSystem(string assetBundleSource, string asset, ParticleSystem ps, float time)
    {
        BinaryMinHeap<ParticleSystem, float> currRecycler;
        if (!_particleSystemRecyclers.TryGetValue((assetBundleSource, asset), out currRecycler))
        {
            _particleSystemRecyclers.Add((assetBundleSource, asset), currRecycler = new BinaryMinHeap<ParticleSystem, float>());
        }
        currRecycler.Add(ps, time);
    }

    public void AdvanceParticleSystemRecycler(string assetBundleSource, string asset, float time)
    {
        BinaryMinHeap<ParticleSystem, float> currRecycler;
        if (!_particleSystemRecyclers.TryGetValue((assetBundleSource, asset), out currRecycler))
        {
            return;
        }
        while (currRecycler.Count > 0)
        {
            (ParticleSystem, float) next = currRecycler.PeekWithCost();
            if (time < next.Item2)
            {
                break;
            }
            else
            {
                next.Item1.gameObject.SetActive(false);
                GetOrCreateParticleSystemCache(assetBundleSource, asset).Release(next.Item1);
                currRecycler.Remove();
            }
        }
    }

    public void AdvanceAllParticleSystemRecyclers(float time)
    {
        foreach (KeyValuePair<(string, string), BinaryMinHeap<ParticleSystem, float>> currRecyclerKV in _particleSystemRecyclers)
        {
            SpecificCache<ParticleSystem> currCache = GetOrCreateParticleSystemCache(currRecyclerKV.Key.Item1, currRecyclerKV.Key.Item2);
            while (currRecyclerKV.Value.Count > 0)
            {
                (ParticleSystem, float) next = currRecyclerKV.Value.PeekWithCost();
                if (time < next.Item2)
                {
                    break;
                }
                else
                {
                    next.Item1.gameObject.SetActive(false);
                    currCache.Release(next.Item1);
                    currRecyclerKV.Value.Remove();
                }
            }
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

        private Queue<T> _cache = new Queue<T>();
    }
}

