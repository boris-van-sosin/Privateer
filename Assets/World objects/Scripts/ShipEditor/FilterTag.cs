using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FilterTag : MonoBehaviour
{
    public ShipComponentFilteringTag FilteringTag;
}

public enum ShipComponentFilteringTag { All, CompatibleOnly, CompatibleFirst }
