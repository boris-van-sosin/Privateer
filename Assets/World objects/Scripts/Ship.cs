﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;

public class Ship : MonoBehaviour
{
    void Awake()
    {
        HullHitPoints = MaxHullHitPoints;
        Energy = 0;
        Heat = 0;
        MaxHeat = 100;
        ComputeLength();
        InitComponentSlots();
        InitCrew();
        InitDamageEffects();
    }

    // Use this for initialization
    void Start ()
    {
    }

    public void Activate()
    {
        InitElectromagneticClamps();
        InitComponents();
        InitArmour();
        InitShield();
        FindTurrets();
        _manualTurrets = new HashSet<ITurret>(_turrets);
        StartCoroutine(ContinuousComponents());
        ShipDisabled = false;
        ShipImmobilized = false;
        ShipSurrendered = false;
        InBoarding = false;
        LastInCombat = Time.time;
    }

    private void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);
        _minEnergyPerShot = -1;
        foreach (TurretHardpoint hp in hardpoints)
        {
            TurretBase turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                if (_minEnergyPerShot < 0)
                {
                    _minEnergyPerShot = turret.EnergyToFire;
                }
                else
                {
                    _minEnergyPerShot = System.Math.Min(_minEnergyPerShot, turret.EnergyToFire);
                }
                turrets.Add(turret);
                _componentSlots[hp.LocationOnShip].Add(Tuple<ComponentSlotType, IShipComponent>.Create(turret.TurretType, turret));
            }
        }
        _turrets = turrets.ToArray();
    }

    private void InitComponentSlots()
    {
        _componentsSlotTypes.Add(ShipSection.Center, CenterComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Fore, ForeComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Aft, AftComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Left, LeftComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Right, RightComponentSlots);

        _componentSlots.Add(ShipSection.Center, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Fore, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Aft, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Left, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Right, new List<Tuple<ComponentSlotType, IShipComponent>>());
        _componentSlots.Add(ShipSection.Hidden, new List<Tuple<ComponentSlotType, IShipComponent>>());
    }

    private void InitCrew()
    {
        _crew = new List<ShipCharacter>(MaxCrew);
        _specialCharacters = new List<SpecialCharacter>(MaxSpecialCharacters);
    }

    private void InitDamageEffects()
    {
        Transform t = transform.Find("Damage smoke effect");
        if (t != null)
        {
            _engineDamageSmoke = t.GetComponent<ParticleSystem>();
        }
    }

    private void InitComponents()
    {
        _electromagneticClamps = ElectromagneticClamps.DefaultComponent(this);
        _electromagneticClamps.OnToggle += ElectromagneticClampsToggled;
        _componentSlots[ShipSection.Hidden].Add(Tuple<ComponentSlotType, IShipComponent>.Create(ComponentSlotType.Hidden, _electromagneticClamps));

        _energyCapacityComps = AllComponents.Where(x => x is IEnergyCapacityComponent).Select(y => y as IEnergyCapacityComponent).ToArray();
        _updateComponents = AllComponents.Where(x => x is IPeriodicActionComponent).Select(y => y as IPeriodicActionComponent).ToArray();
        _shieldComponents = AllComponents.Where(x => x is IShieldComponent).Select(y => y as IShieldComponent).ToArray();
        _combatDetachments = AllComponents.Where(x => x is CombatDetachment).Select(y => y as CombatDetachment).ToArray();
        _totalMaxShield = 0;
        foreach (IShieldComponent shield in _shieldComponents)
        {
            _totalMaxShield += shield.MaxShieldPoints;
        }
    }

    private void InitArmour()
    {
        _maxArmour.Add(ShipSection.Fore, DefaultArmorFront);
        _minArmour.Add(ShipSection.Fore, MinArmorFront);
        _currArmour.Add(ShipSection.Fore, DefaultArmorFront);

        _maxArmour.Add(ShipSection.Aft, DefaultArmorAft);
        _minArmour.Add(ShipSection.Aft, MinArmorAft);
        _currArmour.Add(ShipSection.Aft, DefaultArmorAft);

        _maxArmour.Add(ShipSection.Left, DefaultArmorLeft);
        _minArmour.Add(ShipSection.Left, MinArmorLeft);
        _currArmour.Add(ShipSection.Left, DefaultArmorLeft);

        _maxArmour.Add(ShipSection.Right, DefaultArmorRight);
        _minArmour.Add(ShipSection.Right, MinArmorRight);
        _currArmour.Add(ShipSection.Right, DefaultArmorRight);
        foreach (ShipSection section in _componentSlots.Keys)
        {
            foreach (Tuple<ComponentSlotType, IShipComponent> comp in _componentSlots[section])
            {
                if (!_maxArmour.ContainsKey(section))
                {
                    _maxArmour.Add(section, 0);
                }
                if (!_currArmour.ContainsKey(section))
                {
                    _currArmour.Add(section, _maxArmour[section]);
                }
                ExtraArmour a = comp.Item2 as ExtraArmour;
                if (a != null)
                {
                    _maxArmour[section] += a.ArmourAmount;
                    _currArmour[section] += a.ArmourAmount;
                }
            }
        }
    }

    private void ComputeLength()
    {
        Mesh m = GetComponent<MeshFilter>().mesh;
        ShipUnscaledLength = m.bounds.size.y;
        ShipUnscaledWidth = m.bounds.size.x;
        ShipLength = ShipUnscaledLength * transform.lossyScale.y;
        ShipWidth = ShipUnscaledWidth * transform.lossyScale.x;
    }

    private void InitShield()
    {
        Transform t = transform.Find("Shield");
        if (t != null)
        {
            _shieldCapsule = t.gameObject;
            if (ShipTotalShields > 0 && _shieldCapsule)
            {
                _shieldCapsule.SetActive(true);
            }
        }
    }

    private void InitElectromagneticClamps()
    {
        Transform t = transform.Find("MagneticField");
        if (t != null)
        {
            _electromagneticClampsEffect = t.GetComponent<ParticleSystem>();
        }
    }

    public IEnumerable<TurretHardpoint> WeaponHardpoints
    {
        get
        {
            TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
            foreach (TurretHardpoint hp in hardpoints)
            {
                yield return hp;
            }
        }
    }

    public bool PlaceTurret(TurretHardpoint hp, TurretBase t)
    {
        if (hp == null || t == null || !hp.AllowedWeaponTypes.Contains(t.TurretType))
        {
            return false;
        }
        TurretBase existingTurret = hp.GetComponentInChildren<TurretBase>();
        if (existingTurret != null)
        {
            return false;
        }
        Quaternion q = Quaternion.LookRotation(-hp.transform.up);
        t.transform.rotation = q;
        t.transform.parent = hp.transform;
        t.transform.localScale = Vector3.one;
        t.transform.localPosition = Vector3.zero;
        if (_minEnergyPerShot < 0)
        {
            _minEnergyPerShot = t.EnergyToFire;
        }
        else
        {
            _minEnergyPerShot = System.Math.Min(_minEnergyPerShot, t.EnergyToFire);
        }
        if (_turrets != null)
        {
            ITurret[] newTurretArr = new ITurret[_turrets.Length + 1];
            _turrets.CopyTo(newTurretArr, 0);
            newTurretArr[newTurretArr.Length - 1] = t;
            _turrets = newTurretArr;
        }
        else
        {
            _turrets = new ITurret[]
            {
                t
            };
        }
        _componentSlots[hp.LocationOnShip].Add(Tuple<ComponentSlotType, IShipComponent>.Create(t.TurretType, t));
        return true;
    }

    public bool PlaceComponent(ShipSection sec, IShipComponent comp)
    {
        if (comp.ComponentType != ComponentSlotType.ShipSystem && comp.ComponentType != ComponentSlotType.BoardingForce && comp.ComponentType != ComponentSlotType.Engine)
        {
            return false;
        }
        if (ShipSize < comp.MinShipSize || ShipSize > comp.MaxShipSize)
        {
            return false;
        }
        int availableSlots = 0;
        foreach (ComponentSlotType s in _componentsSlotTypes[sec])
        {
            if (s == comp.ComponentType)
            {
                ++availableSlots;
            }
        }
        foreach (Tuple<ComponentSlotType, IShipComponent> c  in _componentSlots[sec])
        {
            if (c.Item1 == comp.ComponentType)
            {
                --availableSlots;
            }
        }
        if (availableSlots > 0)
        {
            _componentSlots[sec].Add(Tuple<ComponentSlotType, IShipComponent>.Create(comp.ComponentType, comp));
            if (comp.ComponentType == ComponentSlotType.Engine)
            {
                _engine = comp as ShipEngine;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_inCollision)
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
            _prevPos = transform.position;
            _prevRot = transform.rotation;
            transform.position += Time.deltaTime * (ActualVelocity = directionMult * _speed * transform.up);
            //if (Follow) Debug.Log(string.Format("Velocity vector: {0}", ActualVelocity));
            if (_autoHeading && !ShipImmobilized && !ShipDisabled)
            {
                RotateToHeading();
            }
        }
	}

    protected void RevertRotation()
    {
        transform.rotation = _prevRot;
    }

    private IEnumerator MoveBackAfterCollision(Vector3 collisionVec, float massFactor)
    {
        yield return new WaitForEndOfFrame();
        while (_inCollision)
        {
            transform.position -= Time.deltaTime * 0.1f * MaxSpeed * collisionVec;
            yield return new WaitForEndOfFrame();
        }
        ActualVelocity = Vector3.zero;
        yield return null;
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
            MaxEnergy = newMaxEnergy;
            foreach (IPeriodicActionComponent comp in _updateComponents)
            {
                comp.PeriodicAction();
            }
            if (_shieldCapsule)
            {
                _shieldCapsule.SetActive(ShipTotalShields > 0);
            }

            if (HullHitPoints <= 0)
            {
                yield break;
            }
            yield return new WaitForSeconds(0.25f);
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
        if (_engine != null)
        {
            _engine.ComponentActive = true;
            if (!_engine.ThrustWorks || !_engine.ComponentIsWorking)
            {
                return;
            }
        }
        float thrustCoefficient = 1.0f;
        if (_engine.Status == ComponentStatus.LightlyDamaged)
        {
            thrustCoefficient = 0.9f;
        }
        else if (_engine.Status == ComponentStatus.HeavilyDamaged)
        {
            thrustCoefficient = 0.75f;
        }
        float newSpeed = _speed + Thrust * thrustCoefficient * Time.deltaTime;
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
        if (_engine != null)
        {
            _engine.ComponentActive = true;
            if (!_engine.ThrustWorks || !_engine.ComponentIsWorking)
            {
                return;
            }
        }
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

    public void ApplyBraking()
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
        if (_inCollision)
        {
            return;
        }
        if (_engine != null)
        {
            _engine.ComponentActive = true;
            if (!_engine.ThrustWorks || !_engine.ComponentIsWorking)
            {
                return;
            }
        }
        float turnFactor = 1.0f;
        if (left)
        {
            turnFactor = -1.0f;
        }
        float turnCoefficient = 1.0f;
        if (_engine.Status == ComponentStatus.LightlyDamaged)
        {
            turnCoefficient = 0.9f;
        }
        else if (_engine.Status == ComponentStatus.HeavilyDamaged)
        {
            turnCoefficient = 0.75f;
        }
        Quaternion deltaRot = Quaternion.AngleAxis(turnFactor * TurnRate * turnCoefficient * Time.deltaTime, transform.forward);
        transform.rotation = deltaRot * transform.rotation;
        ApplyBraking(0.5f, 0.5f);
    }

    public void SetRequiredHeading(Vector3 targetPoint)
    {
        if (ShipImmobilized || ShipDisabled)
        {
            return;
        }
        _autoHeadingVector = targetPoint - transform.position;
        _autoHeadingVector.y = 0;
        _autoHeadingVector.Normalize();
        _autoHeading = true;
    }

    private void RotateToHeading()
    {
        if (Mathf.Abs(Vector3.Cross(transform.up, _autoHeadingVector).y) < 0.1f)
        {
            _autoHeading = false;
            return;
        }

        ApplyTurning(Vector3.Cross(transform.up, _autoHeadingVector).y < 0);
    }

    public bool MovingForward { get { return MovementDirection == ShipDirection.Forward; } }

    public void FireManual(Vector3 target)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ITurret t in _manualTurrets.Where(x => x.GetTurretBehavior() == TurretBase.TurretMode.Manual))
        {
            t.Fire(target);
            sb.AppendFormat("Turret {0}:{1}, ", t, t.CurrLocalAngle);
        }
        //Debug.Log(sb.ToString());
    }

    public Vector3 ActualVelocity { get; private set; }

    public bool TryChangeEnergy(int delta)
    {
        int newEnergy = Energy + delta;
        if (0 <= newEnergy)
        {
            Energy = System.Math.Min(newEnergy, MaxEnergy);
            return true;
        }
        return false;
    }

    public bool TryChangeHeat(int delta)
    {
        int newHeat = Heat + delta;
        if (newHeat <= MaxHeat)
        {
            Heat = System.Math.Max(newHeat, 0);
            return true;
        }
        return false;
    }

    public bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat)
    {
        int newEnergy = Energy + deltaEnergy;
        int newHeat = Heat + deltaHeat;
        if (newEnergy >= 0 && newHeat <= MaxHeat)
        {
            Energy = System.Math.Min(newEnergy, MaxEnergy);
            Heat = System.Math.Max(newHeat, 0);
            return true;
        }
        return false;
    }

    public void TakeHit(Warhead w, Vector3 location)
    {
        ShipSection sec = GetHitSection(location);
        Debug.Log(string.Format("Ship {0} hit in {1}", name, sec));
        LastInCombat = Time.time;
        // if shields are present, take shield damage
        if (!Combat.DamageShields(w.ShieldDamage, _shieldComponents))
        {
            if (_shieldCapsule)
            {
                _shieldCapsule.SetActive(ShipTotalShields > 0);
            }
            return;
        }
        if (_shieldCapsule)
        {
            _shieldCapsule.SetActive(ShipTotalShields > 0);
        }

        // armour penetration
        int armourAtLocation = _currArmour[sec];
        if (Combat.ArmourPenetration(armourAtLocation, w.ArmourPenetration))
        {
            // a random component at the section is damaged.
            List<IShipActiveComponent> damageableComps = new List<IShipActiveComponent>(_componentSlots[sec].Count);
            foreach (Tuple<ComponentSlotType, IShipComponent> c in _componentSlots[sec])
            {
                IShipActiveComponent c2 = c.Item2 as IShipActiveComponent;
                if (c2 != null && c2.Status != ComponentStatus.Destroyed)
                {
                    damageableComps.Add(c2);
                }
            }
            foreach (Tuple<ComponentSlotType, IShipComponent> c in _componentSlots[ShipSection.Center])
            {
                IShipActiveComponent c2 = c.Item2 as IShipActiveComponent;
                if (c2 != null && c2.Status != ComponentStatus.Destroyed)
                {
                    damageableComps.Add(c2);
                }
            }
            if (damageableComps.Count > 0)
            {
                IShipActiveComponent comp = ObjectFactory.GetRandom(damageableComps);
                comp.ComponentHitPoints -= w.SystemDamage;
            }
            HullHitPoints = System.Math.Max(0, HullHitPoints - w.HullDamage);
        }
        _currArmour[sec] = System.Math.Max(_minArmour[sec], _currArmour[sec] - w.ArmourDamage);
        CheckCriticalDamage();
    }

    private ShipSection GetHitSection(Vector3 hitLocation)
    {
        Vector3 localHitLocation = transform.InverseTransformPoint(hitLocation);
        if (localHitLocation.y > ShipUnscaledLength / 6)
        {
            return ShipSection.Fore;
        }
        else if (localHitLocation.y < -ShipUnscaledLength / 6)
        {
            return ShipSection.Aft;
        }
        else if (localHitLocation.x > 0)
        {
            return ShipSection.Left;
        }
        else
        {
            return ShipSection.Right;
        }
    }

    public int ShipTotalShields
    {
        get
        {
            int totalShields = 0;
            foreach (IShieldComponent shield in _shieldComponents)
            {
                totalShields += shield.CurrShieldPoints;
            }
            return totalShields;
        }
    }

    public int ShipTotalMaxShields
    {
        get
        {
            return _totalMaxShield;
        }
    }

    public IEnumerable<ITurret> Turrets
    {
        get
        {
            return _turrets;
        }
    }

    private void CheckCriticalDamage()
    {
        bool critical = false;
        if (HullHitPoints == 0)
        {
            foreach (IShipActiveComponent comp in AllComponents.Where(c => c is IShipActiveComponent))
            {
                comp.ComponentHitPoints = 0;
            }
            ParticleSystem explosion = ObjectFactory.CreateWeaponEffect(ObjectFactory.WeaponEffect.BigExplosion, transform.position);
            switch (ShipSize)
            {
                case ObjectFactory.ShipSize.Sloop:
                    explosion.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    break;
                case ObjectFactory.ShipSize.Frigate:
                    explosion.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    break;
                case ObjectFactory.ShipSize.Destroyer:
                    explosion.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                    break;
                case ObjectFactory.ShipSize.Cruiser:
                    explosion.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
                    break;
                case ObjectFactory.ShipSize.CapitalShip:
                    explosion.transform.localScale = new Vector3(2.0f, 2.0f, 2.0f);
                    break;
                default:
                    break;
            }
            Destroy(explosion.gameObject, 5.0f);
            critical = true;
        }
        else
        {
            // no power
            bool noPower = true;
            foreach (IPeriodicActionComponent comp in _updateComponents)
            {
                PowerPlant p = comp as PowerPlant;
                if (p != null && p.ComponentIsWorking)
                {
                    noPower = false;
                    break;
                }
            }
            critical = noPower && Energy < _minEnergyPerShot;

            if (!critical)
            {
                if (_turrets.All(x => !x.ComponentIsWorking))
                {
                    critical = true;
                }
            }
        }

        if (critical)
        {
            if (HullHitPoints > 0)
            {
                Debug.Log(string.Format("Ship {0} is in critical!", this));
            }
            else
            {
                Debug.Log(string.Format("Ship {0} destroyed!", this));
            }
            ShipDisabled = true;
        }
        if (!_engine.ComponentIsWorking)
        {
            ShipImmobilized = true;
            _engineDamageSmoke.Play();
        }
        else
        {
            ShipImmobilized = false;
            _engineDamageSmoke.Stop();
        }
    }

    public bool ArmourAtFull
    {
        get
        {
            foreach (ShipSection sec in _currArmour.Keys)
            {
                if (_currArmour[sec] != _maxArmour[sec])
                {
                    return false;
                }
            }
            return true;
        }
    }

    public bool ComponentsAtFull
    {
        get
        {
            foreach (IShipActiveComponent c in AllComponents.Where(x => x is IShipActiveComponent).Select(y => y as IShipActiveComponent).Where(z => z.Status != ComponentStatus.Destroyed))
            {
                if (c.ComponentMaxHitPoints != c.ComponentHitPoints)
                {
                    return false;
                }
            }
            return true;
        }
    }

    public void RepairHull(int maxRepairPoints)
    {
        HullHitPoints = System.Math.Min(HullHitPoints + maxRepairPoints, MaxHullHitPoints);
    }

    public void RepairArmour(int maxRepairPoints)
    {
        int repairPointsLeft = maxRepairPoints;
        while (repairPointsLeft > 0)
        {
            ShipSection minArmourSec = ShipSection.Fore;
            int minArmour = -1;
            bool needsRepair = false;
            foreach (ShipSection sec in _currArmour.Keys)
            {
                if (_currArmour[sec] != _maxArmour[sec])
                {
                    needsRepair = true;
                    if (minArmour < 0 || _currArmour[sec] < minArmour)
                    {
                        minArmourSec = sec;
                        minArmour = _currArmour[sec];
                    }
                }
            }
            if (!needsRepair)
            {
                return;
            }
            if (_maxArmour[minArmourSec] - _currArmour[minArmourSec] <= repairPointsLeft)
            {
                repairPointsLeft -= _maxArmour[minArmourSec] - _currArmour[minArmourSec];
                _currArmour[minArmourSec] = _maxArmour[minArmourSec];
            }
            else
            {
                _currArmour[minArmourSec] += repairPointsLeft;
                repairPointsLeft = 0;
            }
        }
    }

    public void NotifyInComabt()
    {
        LastInCombat = Time.time;
    }

    public void ToggleElectromagneticClamps()
    {
        if (_electromagneticClamps != null)
        {
            _electromagneticClamps.ComponentActive = !_electromagneticClamps.ComponentActive;
            if (_electromagneticClamps.ComponentActive && _useShields)
            {
                // turn off shields if magnetic clamps are on
                ToggleShields();
            }
        }
    }
    public bool ElectromagneticClampsActive { get { return _electromagneticClamps != null &&_electromagneticClamps.ClampsWorking; } }
    private void ElectromagneticClampsToggled(bool active)
    {
        if (active)
        {
            _electromagneticClampsEffect.Play();
        }
        else
        {
            _electromagneticClampsEffect.Stop();
        }
    }
    public void ToggleShields()
    {
        if (_shieldComponents != null)
        {
            _useShields = !_useShields;
            if (_useShields && _electromagneticClamps != null && _electromagneticClamps.ComponentActive)
            {
                ToggleElectromagneticClamps();
            }
            foreach (IShieldComponent shield in _shieldComponents)
            {
                shield.ComponentActive = _useShields;
            }
        }
    }

    public void ResolveBoardingAction(Ship otherShip, bool captured)
    {
        if (captured)
        {
            ShipSurrendered = true;
            foreach (ITurret t in Turrets)
            {
                t.SetTurretBehavior(TurretBase.TurretMode.Off);
            }
        }
        InBoarding = false;
        ResolveCollision(otherShip, Mass + otherShip.Mass);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogWarning(string.Format("Trigger enter: {0}, {1}", this, other.gameObject));
        Ship otherShip = other.GetComponent<Ship>();
        if (otherShip != null)
        {
            RevertRotation();
            otherShip.RevertRotation();
            _inCollision = true;
            float massSum = Mass + otherShip.Mass;
            NotifyInComabt();

            if (ElectromagneticClampsActive &&
                (!InBoarding && !otherShip.InBoarding) &&
                (!ShipSurrendered && !otherShip.ShipSurrendered) &&
                ((otherShip.ShipImmobilized || otherShip.ShipDisabled) && otherShip.HullHitPoints > 0))
            {
                InBoarding = true;
                otherShip.InBoarding = true;
                StartCoroutine(Combat.BoardingCombat(this, otherShip));
            }
            else if (otherShip.ElectromagneticClampsActive)
            {
                //?
            }
            else
            {
                ResolveCollision(otherShip, massSum);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Debug.LogWarning(string.Format("Trigger exit: {0}, {1}", this, other.gameObject));
        Ship otherShip = other.GetComponent<Ship>();
        if (otherShip != null)
        {
            _inCollision = false;
        }
    }

    void OnCollisionEnter()
    {
        Debug.LogWarning(string.Format("Collision: {0}", this));
    }

    private void OnCollisionExit()
    {
        Debug.LogWarning(string.Format("Collision exit: {0},", this));
    }

    private void ResolveCollision(Ship otherShip, float massSum)
    {
        Vector3 collisionVec = (otherShip.transform.position - transform.position).normalized;
        StartCoroutine(MoveBackAfterCollision(collisionVec, Mass / massSum));
    }

    public void AddCrew(ShipCharacter c)
    {
        switch (c.Role)
        {
            case ShipCharacter.CharacterProfession.Crew:
                if (_crew.Count < MaxCrew)
                {
                    _crew.Add(c);
                }
                break;
            case ShipCharacter.CharacterProfession.Captain:
                break;
            case ShipCharacter.CharacterProfession.Combat:
                foreach (CombatDetachment cd in _combatDetachments)
                {
                    if (cd.Forces.Count < cd.CrewCapacity)
                    {
                        cd.Forces.Add(c);
                    }
                }
                break;
            default:
                break;
        }
    }

    public void AddCrew(IEnumerable<ShipCharacter> crew)
    {
        foreach (ShipCharacter c in crew)
        {
            AddCrew(c);
        }
    }

    private enum ShipDirection { Stopped, Forward, Reverse };
    public enum ShipSection { Fore, Aft, Left, Right, Center, Hidden };

    public string ProductionKey;
    public ObjectFactory.ShipSize ShipSize;

    public float MaxSpeed;
    public float Mass;
    public float Thrust;
    public float Braking;
    public float TurnRate;
    private float _speed;
    private bool _autoHeading = false;
    private Vector3 _autoHeadingVector;
    private ShipDirection MovementDirection = ShipDirection.Stopped;

    private ITurret[] _turrets;
    private IEnumerable<ITurret> _manualTurrets;
    private int _minEnergyPerShot;

    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; }
    public int Heat { get; private set; }
    public int MaxHeat { get; private set; }

    public ComponentSlotType[] CenterComponentSlots;
    public ComponentSlotType[] ForeComponentSlots;
    public ComponentSlotType[] AftComponentSlots;
    public ComponentSlotType[] LeftComponentSlots;
    public ComponentSlotType[] RightComponentSlots;
    private Dictionary<ShipSection, ComponentSlotType[]> _componentsSlotTypes = new Dictionary<ShipSection, ComponentSlotType[]>();
    private Dictionary<ShipSection, List<Tuple<ComponentSlotType, IShipComponent>>> _componentSlots = new Dictionary<ShipSection, List<Tuple<ComponentSlotType, IShipComponent>>>();

    public IEnumerable<IShipComponent> AllComponents
    {
        get
        {
            foreach (List<Tuple<ComponentSlotType, IShipComponent>> l in _componentSlots.Values)
            {
                foreach (Tuple<ComponentSlotType, IShipComponent> comp in l)
                {
                    yield return comp.Item2;
                }
            }
        }
    }

    public IEnumerable<ShipCharacter> AllCrew
    {
        get
        {
            if (Captain != null)
            {
                yield return Captain;
            }
            foreach (ShipCharacter c in SpecialCharacters)
            {
                yield return c;
            }
            foreach (ShipCharacter c in Crew)
            {
                yield return c;
            }
            foreach (CombatDetachment combatGroup in _combatDetachments)
            {
                foreach (ShipCharacter c in combatGroup.Forces)
                {
                    yield return c;
                }
            }
        }
    }

    private IEnergyCapacityComponent[] _energyCapacityComps;
    private IPeriodicActionComponent[] _updateComponents;
    private IShieldComponent[] _shieldComponents;
    private CombatDetachment[] _combatDetachments;
    private bool _useShields = true;
    private ShipEngine _engine;
    public float ShipLength { get; private set; }
    public float ShipWidth { get; private set; }
    public float ShipUnscaledLength { get; private set; }
    public float ShipUnscaledWidth { get; private set; }

    private ElectromagneticClamps _electromagneticClamps;
    private ParticleSystem _electromagneticClampsEffect;

    public int MaxHullHitPoints;
    public int HullHitPoints { get; set; }
    private int _totalMaxShield;
    public int DefaultArmorFront;
    public int DefaultArmorAft;
    public int DefaultArmorLeft;
    public int DefaultArmorRight;
    public int MinArmorFront;
    public int MinArmorAft;
    public int MinArmorLeft;
    public int MinArmorRight;
    private Dictionary<ShipSection, int> _maxArmour = new Dictionary<ShipSection, int>();
    private Dictionary<ShipSection, int> _minArmour = new Dictionary<ShipSection, int>();
    private Dictionary<ShipSection, int> _currArmour = new Dictionary<ShipSection, int>();
    public bool ShipDisabled { get; private set; }
    public bool ShipImmobilized { get; private set; }
    public bool ShipSurrendered { get; private set; }
    public bool InBoarding { get; private set; }

    public int OperationalCrew;
    public int MaxCrew;
    public int MaxSpecialCharacters;
    public IEnumerable<ShipCharacter> Crew { get { return _crew; } }
    private List<ShipCharacter> _crew;
    public IEnumerable<SpecialCharacter> SpecialCharacters { get { return _specialCharacters; } }
    private List<SpecialCharacter> _specialCharacters;
    public ShipCharacter Captain { get; set; }

    private Vector3 _prevPos;
    private Quaternion _prevRot;
    private bool _inCollision = false;

    public float LastInCombat { get; private set; }

    private GameObject _shieldCapsule;
    public GameObject ShieldCapsule { get { return _shieldCapsule; } }

    private ParticleSystem _engineDamageSmoke;

    public Faction Owner;
}
