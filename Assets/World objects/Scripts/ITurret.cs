using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITurret
{
    void ManualTarget(Vector3 target);
    void Fire(Vector3 target);
    float CurrAngle { get; }
    float CurrLocalAngle { get; }
}
