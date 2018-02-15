using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public class Ship : MonoBehaviour
{
    void Awake()
    {
        if (Follow)
        {
            _userCamera = Camera.main;
            _cameraOffset = _userCamera.transform.position - transform.position;
            CameraOffsetFactor = 1.0f;
        }
    }

    // Use this for initialization
    void Start ()
    {
        FindTurrets();
        _manualTurrets = new HashSet<ITurret>(_turrets);
    }
	
    private void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);
        foreach (TurretHardpoint hp in hardpoints)
        {
            ITurret turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                turrets.Add(turret);
            }
        }
        _turrets = turrets.ToArray();
    }

    private void InitComponents()
    {
        _components = new IShipComponent[]
        {
            new PowerPlant() { PowerOutput = 10 },
            new CapacitorBank() { Capacity = 50 },
            new HeatExchange() { CoolingRate = 20 }
        };

        _energyUsingComps = _components.Where(x => x is IEnergyUsingComponent).Select(y => y as IEnergyUsingComponent).ToArray();
        _energyCapacityComps = _components.Where(x => x is IEnergyCapacityComponent).Select(y => y as IEnergyCapacityComponent).ToArray();
        _heatUsingComps = _components.Where(x => x is IHeatUsingComponent).Select(y => y as IHeatUsingComponent).ToArray();
    }

	// Update is called once per frame
	void Update()
    {
        float directionMult = 0.0f;
        if (MovementDirection == ShipDirection.Forward)
        {
            directionMult = 1.0f;
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            directionMult = -1.0f;
        }
        //transform.position += Time.deltaTime * (ActualVelocity = Mathf.Abs(Vector3.Dot(_velocity, transform.up.normalized)) * directionMult * transform.up.normalized);
        transform.position += Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);
        if (Follow) Debug.Log(string.Format("Velocity vector: {0}", ActualVelocity));
        if (_autoHeading)
        {
            RotateToHeading();
            //ApplyThrust();
        }
        if (_userCamera != null)
        {
            _userCamera.transform.position = transform.position + (_cameraOffset * CameraOffsetFactor);
        }
	}

    private IEnumerator ContinuousComponents()
    {
        while(true)
        {
            int newMaxEnergy = 0;
            foreach (IEnergyCapacityComponent comp in _energyCapacityComps)
            {
                newMaxEnergy += comp.EnergyCapacity;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    public void ManualTarget(Vector3 target)
    {
        foreach (ITurret t in _manualTurrets)
        {
            t.ManualTarget(target);
        }
    }

    public void MoveForeward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Forward;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyThrust();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyBraking();
        }
    }

    private void ApplyThrust()
    {
        float newSpeed = _speed + Thrust * Time.deltaTime;
        if (newSpeed > MaxSpeed)
        {
            _speed = MaxSpeed;
        }
        else
        {
            _speed = newSpeed;
        }
    }

    private void ApplyThrust(float factor, float targetSpeedFactor)
    {
        float targetSpeed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        if (targetSpeed < _speed)
        {
            return;
        }
        float newSpeed = _speed + Thrust * Mathf.Clamp01(factor) * Time.deltaTime;
        if (newSpeed > MaxSpeed * Mathf.Clamp01(targetSpeedFactor))
        {
            _speed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        }
        else
        {
            _speed = newSpeed;
        }
    }

    public void MoveBackward()
    {
        if (MovementDirection == ShipDirection.Stopped)
        {
            MovementDirection = ShipDirection.Reverse;
        }
        if (MovementDirection == ShipDirection.Forward)
        {
            ApplyBraking();
        }
        else if (MovementDirection == ShipDirection.Reverse)
        {
            ApplyThrust();
        }
    }

    private void ApplyBraking()
    {
        float newSpeed = _speed - Braking * Time.deltaTime;
        if (newSpeed < 0)
        {
            _speed = 0;
            MovementDirection = ShipDirection.Stopped;
        }
        else
        {
            _speed = newSpeed;
        }
    }

    private void ApplyBraking(float factor, float targetSpeedFactor)
    {
        float targetSpeed = MaxSpeed * Mathf.Clamp01(targetSpeedFactor);
        float newSpeed = _speed - Braking * Mathf.Clamp01(factor) * Time.deltaTime;
        if (targetSpeed > _speed)
        {
            return;
        }
        if (newSpeed < targetSpeed)
        {
            _speed = MaxSpeed * targetSpeed;
            if (_speed <= 0)
            {
                MovementDirection = ShipDirection.Stopped;
                _speed = 0;
            }
        }
        else
        {
            _speed = newSpeed;
        }
    }

    public void ApplyTurning(bool left)
    {
        float turnFactor = 1.0f;
        if (left)
        {
            turnFactor = -1.0f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
        ApplyBraking(0.5f, 0.5f);
    }

    public void SetRequiredHeading(Vector3 targetPoint)
    {
        Vector3 requiredHeadingVector = targetPoint - transform.position;
        requiredHeadingVector.y = 0;
        _autoHeadingRotation = Quaternion.LookRotation(transform.forward, requiredHeadingVector);
        _autoHeading = true;
    }

    private void RotateToHeading()
    {
        if (Quaternion.Angle(transform.rotation,_autoHeadingRotation) < 0.5f)
        {
            _autoHeading = false;
            return;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, _autoHeadingRotation, TurnRate * Time.deltaTime);
    }

    public bool MovingForward { get { return MovementDirection == ShipDirection.Forward; } }

    public void FireManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ITurret t in _manualTurrets)
        {
            t.Fire(target);
            sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
        }
        Debug.Log(sb.ToString());
    }

    public Vector3 ActualVelocity { get; private set; }

    public bool TryChangeEnergy(int delta)
    {
        int newEnergy = Energy + delta;
        if (0 <= newEnergy && newEnergy <= MaxEnergy)
        {
            Energy = newEnergy;
            return true;
        }
        return false;
    }

    public bool TryChangeHeat(int delta)
    {
        int newHeat = Heat + delta;
        if (0 <= newHeat && newHeat <= MaxHeat)
        {
            Heat = newHeat;
            return true;
        }
        return false;
    }

    private enum ShipDirection { Stopped, Forward, Reverse };

    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    private float _speed;
    private bool _autoHeading = false;
    private Quaternion _autoHeadingRotation;
    private ShipDirection MovementDirection = ShipDirection.Stopped;

    private ITurret[] _turrets;
    private IEnumerable<ITurret> _manualTurrets;

    private int Energy = 0;
    private int MaxEnergy;
    private int Heat = 0;
    private int MaxHeat = 100;

    private IShipComponent[] _components;
    private IEnergyUsingComponent[] _energyUsingComps;
    private IEnergyCapacityComponent[] _energyCapacityComps;
    private IHeatUsingComponent[] _heatUsingComps;

    private Camera _userCamera;
    private Vector3 _cameraOffset;
    public float CameraOffsetFactor { get; set; }

    public bool Follow; // tmp
}
