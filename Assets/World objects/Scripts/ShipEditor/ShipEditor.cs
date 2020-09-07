﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class ShipEditor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ShipHullDefinition[] hulls = ObjectFactory.GetAllShipHulls().ToArray();
        float offset = 0.0f;
        for (int i = 0; i < hulls.Length; ++i)
        {
            RectTransform t = Instantiate(ButtonPrototype);

            t.SetParent(ShipHullsScrollViewContent, false);
            float height = t.rect.height;
            float pivotOffset = (1.0f - t.pivot.y) * height;
            t.anchoredPosition  = new Vector2(t.anchoredPosition.x, offset + pivotOffset);
            offset -= height;

            TextMeshProUGUI textElem = t.GetComponentInChildren<TextMeshProUGUI>();

            string hullKey = hulls[i].HullName;
            textElem.text = hullKey;

            Button buttonElem = t.GetComponent<Button>();
            buttonElem.onClick.AddListener(() => CreateShipDummy(hullKey));
        }
    }

    private void CreateShipDummy(string key)
    {
        _currShip?.gameObject.SetActive(false);
        Transform s;
        if (_shipsCache.TryGetValue(key, out s))
        {
            s.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogFormat("Createing hull {0}", key);
            s = ObjectFactory.CreateShipDummy(key);
            s.position = new Vector3(-2.5f, 0, 0);
            _shipsCache[key] = s;
        }
        _currShip = s;
    }

    public RectTransform ShipClassesScrollViewContent;
    public RectTransform ShipHullsScrollViewContent;
    public RectTransform ButtonPrototype;

    private Transform _currShip = null;
    private Dictionary<string, Transform> _shipsCache = new Dictionary<string, Transform>();
}
