using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITargetableEntity
{
    Vector3 EntityLocation { get; }
    bool Targetable { get; }
}
