using UnityEngine;

static class ComponentExtensions
{
    public static T[] GetComponentsInChildrenOneLevel<T>(this Component t) 
    {
        return t.GetComponentsInChildrenOneLevel<T>(true);
    }

    public static T[] GetComponentsInChildrenOneLevel<T>(this Component t, bool includeInactive)
    {
        int numComps = 0;
        for (int i = 0; i < t.transform.childCount; ++i)
        {
            T comp;
            if (t.transform.GetChild(i).TryGetComponent<T>(out comp) && (includeInactive || t.transform.GetChild(i).gameObject.activeInHierarchy))
            {
                ++numComps;
            }
        }

        T[] res = new T[numComps];
        int j = 0;
        for (int i = 0; i < t.transform.childCount; ++i)
        {
            T comp;
            if (t.transform.GetChild(i).TryGetComponent<T>(out comp) && (includeInactive || t.transform.GetChild(i).gameObject.activeInHierarchy))
            {
                res[j] = comp;
                ++j;
            }
        }

        return res;
    }

    public static T GetComponentInChildrenOneLevel<T>(this Component t) where T: class
    {
        return t.GetComponentInChildrenOneLevel<T>(true);
    }

    public static T GetComponentInChildrenOneLevel<T>(this Component t, bool includeInactive) where T : class
    {
        for (int i = 0; i < t.transform.childCount; ++i)
        {
            T comp;
            if (t.transform.GetChild(i).TryGetComponent<T>(out comp) && (includeInactive || t.transform.GetChild(i).gameObject.activeInHierarchy))
            {
                return comp;
            }
        }
        return null;
    }
}
