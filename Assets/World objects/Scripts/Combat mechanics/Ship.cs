using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Ship : ShipBase
{
    protected override void Awake()
    {
        base.Awake();
        Energy = 0;
        Heat = 0;
        //MaxHeat = 100;
        InitDamageEffects();
        _speedOnTurningCoefficient = 0.5f;
    }

    public override void PostAwake()
    {
        base.PostAwake();
        InitComponentSlots();
        InitCrew();
    }

    public override void Activate()
    {
        InitElectromagneticClamps();
        InitComponents();
        InitArmour();
        InitEngines();
        StartCoroutine(ContinuousComponents());
        ShipSurrendered = false;
        InBoarding = false;
        LastInCombat = Time.time;
        base.Activate();
        _crewNumBuff = DynamicBuff.Default();
        SetCrewNumBuff();
        _manualTurrets = new HashSet<ITurret>(_turrets);
        ApplyHitPointBuffs();
        _shipAI = GetComponent<ShipAIController>();
        SetMinEergyToAct();
        SetMaxEnergyAndHeat();
    }

    protected override void FindTurrets()
    {
        TurretHardpoint[] hardpoints = GetComponentsInChildren<TurretHardpoint>();
        List<ITurret> turrets = new List<ITurret>(hardpoints.Length);
        _minEnergyForActive = -1;
        foreach (List<Tuple<string, IShipComponent>> l in _turretSlotsOccupied.Values)
        {
            l.Clear();
        }
        foreach (TurretHardpoint hp in hardpoints)
        {
            TurretBase turret = hp.GetComponentInChildren<TurretBase>();
            if (turret != null)
            {
                turrets.Add(turret);
                _turretSlotsOccupied[hp.LocationOnShip].Add(new Tuple<string, IShipComponent>(turret.TurretType, turret));
            }
        }
        _turrets = turrets.ToArray();
    }

    private void SetMinEergyToAct()
    {
        _minEnergyForActive = -1;
        foreach (TurretBase t in _turrets.OfType<TurretBase>().Where(a => a.ComponentIsWorking))
        {
            if (_minEnergyForActive < 0)
            {
                _minEnergyForActive = t.EnergyToFire;
            }
            else
            {
                _minEnergyForActive = System.Math.Min(_minEnergyForActive, t.EnergyToFire);
            }
        }
        foreach (DamageControlNode comp in _updateComponents.OfType<DamageControlNode>().Where(c => c.ComponentIsWorking))
        {
            if (_minEnergyForActive < 0)
            {
                _minEnergyForActive = comp.EnergyPerTick;
            }
            else
            {
                _minEnergyForActive = System.Math.Min(_minEnergyForActive, comp.EnergyPerTick);
            }
        }
    }

    private void SetMaxEnergyAndHeat()
    {
        MaxEnergy = 0;
        foreach (var c in _energyCapacityComps)
        {
            MaxEnergy += c.EnergyCapacity;
        }
        MaxHeat = 0;
        foreach (var c in _heatCapacityComps)
        {
            MaxHeat += c.HeatCapacity;
        }
    }

    private void InitComponentSlots()
    {
        _componentsSlotTypes.Add(ShipSection.Center, CenterComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Fore, ForeComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Aft, AftComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Left, LeftComponentSlots);
        _componentsSlotTypes.Add(ShipSection.Right, RightComponentSlots);

        foreach (ShipSection sec in _componentsSlotTypes.Keys)
        {
            _componentSlotsOccupied.Add(sec, new Tuple<string, IShipComponent>[_componentsSlotTypes[sec].Length]);
        }
        
        _componentSlotsOccupied.Add(ShipSection.Hidden, new Tuple<string, IShipComponent>[1]);

        _turretSlotsOccupied.Add(ShipSection.Fore, new List<Tuple<string, IShipComponent>>());
        _turretSlotsOccupied.Add(ShipSection.Aft, new List<Tuple<string, IShipComponent>>());
        _turretSlotsOccupied.Add(ShipSection.Left, new List<Tuple<string, IShipComponent>>());
        _turretSlotsOccupied.Add(ShipSection.Right, new List<Tuple<string, IShipComponent>>());
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
        _componentSlotsOccupied[ShipSection.Hidden][0] = new Tuple<string, IShipComponent>("Hidden", _electromagneticClamps);

        _energyCapacityComps = AllComponents.Where(x => x is IEnergyCapacityComponent).Select(y => y as IEnergyCapacityComponent).ToArray();
        _heatCapacityComps = AllComponents.Where(x => x is IHeatCapacityComponent).Select(y => y as IHeatCapacityComponent).ToArray();
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
        _maxMitigationArmour = DefaultMitigationArmour.ToDict();
        _currMitigationArmour = DefaultMitigationArmour.ToDict();
        _armour = Armour.ToDict();
        _reducedArmour = ReducedArmour.ToDict();
        foreach (ShipSection section in _componentSlotsOccupied.Keys)
        {
            if (!_maxMitigationArmour.ContainsKey(section))
            {
                _maxMitigationArmour.Add(section, 0);
            }
            if (!_currMitigationArmour.ContainsKey(section))
            {
                _currMitigationArmour.Add(section, _maxMitigationArmour[section]);
            }
            if (!_armour.ContainsKey(section))
            {
                _armour.Add(section, 0);
            }
            if (!_reducedArmour.ContainsKey(section))
            {
                _reducedArmour.Add(section, 0);
            }
            foreach (IShipComponent comp in AllComponentsInSection(section, false))
            {
                ExtraArmour a = comp as ExtraArmour;
                if (a != null)
                {
                    _armour[section] += a.ArmourAmount;
                    _maxMitigationArmour[section] += a.MitigationArmourAmount;
                    _currMitigationArmour[section] += a.MitigationArmourAmount;
                }
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

    public override bool PlaceTurret(TurretHardpoint hp, TurretBase t)
    {
        bool baseSuccedded = base.PlaceTurret(hp, t);
        if (!baseSuccedded)
        {
            return false;
        }

        if (_minEnergyForActive < 0)
        {
            _minEnergyForActive = t.EnergyToFire;
        }
        else
        {
            _minEnergyForActive = System.Math.Min(_minEnergyForActive, t.EnergyToFire);
        }
        _turretSlotsOccupied[hp.LocationOnShip].Add(new Tuple<string, IShipComponent>(t.AllowedSlotTypes.First(), t));
        return true;
    }

    public bool PlaceComponent(ShipSection sec, IShipComponent comp)
    {
        if (sec == ShipSection.Hidden)
        {
            return false;
        }
        // Only ship systems and engines are placed using this function
        if (comp.AllowedSlotTypes.All(x => x != "ShipSystem" &&
                                      x != "ShipSystemCenter" &&
                                      x != "Engine" &&
                                      x != "BoardingForce"))
        {
            return false;
        }
        if (ShipSize < comp.MinShipSize || ShipSize > comp.MaxShipSize)
        {
            return false;
        }

        int availableSlotIdx = -1;
        for (int i = 0; i < _componentsSlotTypes[sec].Length; ++i)
        {
            if (_componentSlotsOccupied[sec][i] == null && comp.AllowedSlotTypes.Contains(_componentsSlotTypes[sec][i]))
            {
                availableSlotIdx = i;
                break;
            }
        }
        if (availableSlotIdx >= 0)
        {
            string occupiedSlot = _componentsSlotTypes[sec].Intersect(comp.AllowedSlotTypes).First();
            _componentSlotsOccupied[sec][availableSlotIdx] = new Tuple<string, IShipComponent>(occupiedSlot, comp);
            if (comp is ShipEngine)
            {
                _engine = comp as ShipEngine;
                _engine.OnToggle += SetEngineParticleSystems;
            }
            return true;
        }
        else
        {
            return false;
        }

        /*
        int availableSlots = 0;
        foreach (ComponentSlotType s in _componentsSlotTypes[sec])
        {
            if (comp.AllowedSlotTypes.Contains(s))
            {
                ++availableSlots;
            }
        }
        foreach (Tuple<ComponentSlotType, IShipComponent> c  in _componentSlotsOccupied[sec])
        {
            if (comp.AllowedSlotTypes.Contains(c.Item1))
            {
                --availableSlots;
            }
        }
        if (availableSlots > 0)
        {
            ComponentSlotType occupiedSlot = _componentsSlotTypes[sec].Intersect(comp.AllowedSlotTypes).First();
            _componentSlotsOccupied[sec].Add(Tuple<ComponentSlotType, IShipComponent>.Create(occupiedSlot, comp));
            if (comp.AllowedSlotTypes.Contains(ComponentSlotType.Engine))
            {
                _engine = comp as ShipEngine;
                _engine.OnToggle += SetEngineParticleSystems;
            }
            return true;
        }
        else
        {
            return false;
        }*/
    }

    private void InitEngines()
    {
        Transform t = transform.Find("Engine exhaust on");
        if (t != null)
        {
            List<ParticleSystem> tmpPS = new List<ParticleSystem>(t.childCount);
            for (int i = 0; i < t.childCount; ++i)
            {
                ParticleSystem p = t.GetChild(i).GetComponent<ParticleSystem>();
                if (p != null && p.gameObject.activeInHierarchy)
                {
                    p.Stop();
                    tmpPS.Add(p);
                }
            }
            _engineExhaustsOn = tmpPS.ToArray();
        }
        else
        {
            _engineExhaustsOn = new ParticleSystem[0];
        }

        t = transform.Find("Engine exhaust idle");
        if (t != null)
        {
            List<ParticleSystem> tmpPS = new List<ParticleSystem>(t.childCount);
            for (int i = 0; i < t.childCount; ++i)
            {
                ParticleSystem p = t.GetChild(i).GetComponent<ParticleSystem>();
                if (p != null && p.gameObject.activeInHierarchy)
                {
                    p.Play();
                    tmpPS.Add(p);
                }
            }
            _engineExhaustsIdle = tmpPS.ToArray();
        }
        else
        {
            _engineExhaustsIdle = new ParticleSystem[0];
        }

        t = transform.Find("Engine exhaust brake");
        if (t != null)
        {
            List<ParticleSystem> tmpPS = new List<ParticleSystem>(t.childCount);
            for (int i = 0; i < t.childCount; ++i)
            {
                ParticleSystem p = t.GetChild(i).GetComponent<ParticleSystem>();
                if (p != null && p.gameObject.activeInHierarchy)
                {
                    p.Stop();
                    tmpPS.Add(p);
                }
            }
            _engineExhaustsBrake = tmpPS.ToArray();
        }
        else
        {
            _engineExhaustsBrake = new ParticleSystem[0];
        }
    }

    private void SetEngineParticleSystems(bool On)
    {
        if (On && (_acceleratingForward || _brakingForward))
        {
            foreach (ParticleSystem p in _engineExhaustsOn)
            {
                p.Play();
            }
            foreach (ParticleSystem p in _engineExhaustsIdle.Concat(_engineExhaustsBrake))
            {
                p.Stop();
            }
        }
        else if (On && (_acceleratingBackwards || _brakingBackwards))
        {
            foreach (ParticleSystem p in _engineExhaustsBrake.Concat(_engineExhaustsIdle))
            {
                p.Play();
            }
            foreach (ParticleSystem p in _engineExhaustsOn)
            {
                p.Stop();
            }
        }
        else
        {
            foreach (ParticleSystem p in _engineExhaustsIdle)
            {
                p.Play();
            }
            foreach (ParticleSystem p in _engineExhaustsOn.Concat(_engineExhaustsBrake))
            {
                p.Stop();
            }
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (!InBoarding)
        {
            base.Update();
        }
        if (!ShipDisabled && Vector3.Angle(transform.up, Vector3.up) > Mathf.Epsilon)
        {
            Quaternion flattenedRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, flattenedRotation, TurnRate * Time.deltaTime);
        }
	}

    protected void RevertRotation()
    {
        transform.rotation = _prevRot;
    }

    private IEnumerator ContinuousComponents()
    {
        //int newMaxEnergy = 0;
        //foreach (IEnergyCapacityComponent comp in _energyCapacityComps)
        //{
        //    newMaxEnergy += comp.EnergyCapacity;
        //}
        //MaxEnergy = newMaxEnergy;
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.25f));
        while (true)
        {
            foreach (IPeriodicActionComponent comp in _updateComponents)
            {
                comp.PeriodicAction();
            }
            if (_shieldCapsule)
            {
                _shieldCapsule.SetActive(ShipTotalShields > 0);
            }

            UpdateAndApplyBuffs();

            if (HullHitPoints <= 0)
            {
                yield break;
            }
            yield return _componentPulseDelay;
        }
    }

    private void UpdateAndApplyBuffs()
    {
        CombinedBuff = DynamicBuff.Combine(AllBuffs);
        foreach (ITurret t in Turrets)
        {
            t.ApplyBuff(CombinedBuff);
        }
    }

    private void ApplyHitPointBuffs()
    {
        if (InherentBuff != null)
        {
            MaxHullHitPoints += InherentBuff.HitPointData.Hull;
            foreach (IShipActiveComponent comp in AllComponents.OfType<IShipActiveComponent>())
            {
                comp.ApplyHitPointBuff(InherentBuff.HitPointData);
            }
        }
    }

    public override void MoveForward()
    {
        base.MoveForward();

        _acceleratingForward = MovingForward;
        _brakingForward = !_acceleratingForward;
        _acceleratingBackwards = _brakingBackwards = false;
    }

    public override void MoveBackward()
    {
        base.MoveBackward();

        _brakingBackwards = MovingForward;
        _acceleratingBackwards = !_brakingBackwards;
        _acceleratingForward = _brakingForward = false;
    }

    public override void ApplyBraking()
    {
        base.ApplyBraking();

        _brakingBackwards = MovingForward;
        _brakingForward = MovingBackwards;
        _acceleratingForward = _acceleratingBackwards = false;
    }

    protected override void ApplyThrust()
    {
        if (_engine != null)
        {
            _engine.ComponentActive = true;
        }
        _thrustCoefficient = GlobalOtherConstants.ShipEngineUndamagedThrustCoeff;
        if (_engine.Status == ComponentStatus.LightlyDamaged)
        {
            _thrustCoefficient = GlobalOtherConstants.ShipEngineLightDamageThrustCoeff;
        }
        else if (_engine.Status == ComponentStatus.HeavilyDamaged)
        {
            _thrustCoefficient = GlobalOtherConstants.ShipEngineHeavyDamageThrustCoeff;
        }
        base.ApplyThrust();
    }

    protected override void ApplyBrakingInner()
    {
        if (_engine != null)
        {
            _engine.SetBraking();
        }
        base.ApplyBrakingInner();
    }


    protected override bool CanDoAcceleration()
    {
        return _engine.RequestEngine();
    }

    public override void ApplyTurning(bool left)
    {
        if (_engine != null)
        {
            if (!_engine.RequestEngine())
            {
                return;
            }
        }
        _turnCoefficient = GlobalOtherConstants.ShipEngineUndamagedTurnCoeff;
        if (_engine.Status == ComponentStatus.LightlyDamaged)
        {
            _turnCoefficient = GlobalOtherConstants.ShipEngineLightDamageTurnCoeff;
        }
        else if (_engine.Status == ComponentStatus.HeavilyDamaged)
        {
            _turnCoefficient = GlobalOtherConstants.ShipEngineHeavyDamageTurnCoeff;
        }
        ApplyBrakingInner();
        ApplyBrakingCoefficients(GlobalOtherConstants.ShipTurnBrakeFactor, GlobalOtherConstants.ShipTurnTargetSpeedFactor);
        base.ApplyTurning(left);
    }

    protected override bool CanDoTurning()
    {
        return _engine.RequestEngine();
    }

    public void SetRequiredHeading(Vector3 targetPoint)
    {
        if (!ShipControllable)
        {
            return;
        }
        _autoHeadingVector = targetPoint - transform.position;
        _autoHeadingVector.y = 0;
        _autoHeadingVector.Normalize();
        _autoHeading = true;
    }

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

    public override bool TryChangeEnergyAndHeat(int deltaEnergy, int deltaHeat)
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

    public override void TakeHit(Warhead w, Vector3 location)
    {
        ShipSection sec = GetHitSection(location);
        //Debug.Log(string.Format("Ship {0} hit in {1}", name, sec));
        LastInCombat = Time.time;
        bool tookCasualties = false;

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
        int armourAtLocation = GetArmourAtSection(sec);
        bool recheckMinEnergyToAct = false;
        for (int i = 0; i < w.HitMultiplicity; ++i)
        {
            if (Combat.ArmourPenetration(armourAtLocation, w.ArmourPenetration))
            {
                float mitigationFactor = 1f;
                if (_currMitigationArmour[sec] > 0)
                {
                    _currMitigationArmour[sec] = System.Math.Max(0, _currMitigationArmour[sec] - w.ArmourDamage);
                    mitigationFactor = 1f - ArmourMitigation;
                }
                // a random component at the section is damaged.
                List<IShipActiveComponent> damageableComps = new List<IShipActiveComponent>(_componentSlotsOccupied[sec].Length + _turretSlotsOccupied[sec].Count);
                foreach (IShipComponent c in AllComponentsInSection(sec, true))
                {
                    IShipActiveComponent c2 = c as IShipActiveComponent;
                    if (c2 != null && c2.Status != ComponentStatus.Destroyed)
                    {
                        damageableComps.Add(c2);
                    }
                }
                foreach (IShipComponent c in AllComponentsInSection(ShipSection.Center, true))
                {
                    IShipActiveComponent c2 = c as IShipActiveComponent;
                    if (c2 != null && c2.Status != ComponentStatus.Destroyed)
                    {
                        damageableComps.Add(c2);
                    }
                }
                if (damageableComps.Count > 0)
                {
                    IShipActiveComponent comp = ObjectFactory.GetRandom(damageableComps);
                    comp.ComponentHitPoints -= Mathf.CeilToInt(w.SystemDamage * mitigationFactor);
                    if (!comp.ComponentIsWorking && comp is TurretBase)
                    {
                        recheckMinEnergyToAct = true;
                    }
                }
                // Crew casualties:
                LinkedList<ShipCharacter> casualtiesQueue = new LinkedList<ShipCharacter>(AllCrew.Where(x => x.Status == ShipCharacter.CharacterStaus.Active).OrderBy(x => x.CombatPriority));
                if (casualtiesQueue.Count > 0)
                {
                    foreach (ShipCharacter character in casualtiesQueue.Take(Combat.CrewHit))
                    {
                        if (UnityEngine.Random.value < mitigationFactor * w.AntiPersonnel)
                        {
                            tookCasualties = true;
                            character.Status = ShipCharacter.CharacterStaus.Incapacitated;
                        }
                    }
                }
                HullHitPoints = System.Math.Max(0, HullHitPoints - Mathf.CeilToInt(w.HullDamage * mitigationFactor));
            }
        }
        if (tookCasualties)
        {
            SetCrewNumBuff();
        }
        if (recheckMinEnergyToAct)
        {
            SetMinEergyToAct();
        }
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

    private int GetArmourAtSection(ShipSection sec)
    {
        if (_currMitigationArmour[sec] > 0)
            return _armour[sec];
        else
            return _reducedArmour[sec];
    }

    public override int ShipTotalShields
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

    private void CheckCriticalDamage()
    {
        bool critical = false;
        bool noPower = true;
        if (HullHitPoints == 0)
        {
            foreach (IShipActiveComponent comp in AllComponents.Where(c => c is IShipActiveComponent))
            {
                comp.ComponentHitPoints = 0;
            }
            if (!_explosionPlayed)
            {
                ParticleSystem explosion = ObjectFactory.CreateWeaponEffect(ObjectFactory.WeaponEffect.BigExplosion, transform.position);
                explosion.Play();
                switch (ShipSize)
                {
                    case ObjectFactory.ShipSize.Sloop:
                        explosion.transform.localScale = GlobalDistances.ShipExplosionSizeSloop;
                        break;
                    case ObjectFactory.ShipSize.Frigate:
                        explosion.transform.localScale = GlobalDistances.ShipExplosionSizeSFrigate;
                        break;
                    case ObjectFactory.ShipSize.Destroyer:
                        explosion.transform.localScale = GlobalDistances.ShipExplosionSizeDestroyer;
                        break;
                    case ObjectFactory.ShipSize.Cruiser:
                        explosion.transform.localScale = GlobalDistances.ShipExplosionSizeCruiser;
                        break;
                    case ObjectFactory.ShipSize.CapitalShip:
                        explosion.transform.localScale = GlobalDistances.ShipExplosionSizeCapitalShip;
                        break;
                    default:
                        break;
                }
                Destroy(explosion.gameObject, 5.0f);
                _explosionPlayed = true;
            }
            critical = true;
        }
        else
        {
            // no power
            //foreach (IPeriodicActionComponent comp in _updateComponents)
            //{
            //    PowerPlant p = comp as PowerPlant;
            //    if (p != null && p.ComponentIsWorking)
            //    {
            //        noPower = false;
            //        break;
            //    }
            //}
            noPower = !_updateComponents.OfType<PowerPlant>().Any(p => p.ComponentIsWorking);
            critical = noPower && Energy < _minEnergyForActive;

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
                Debug.Log(string.Format("Ship {0} {1} is in critical!", this, DisplayName.ShortName));
            }
            else
            {
                Debug.Log(string.Format("Ship {0} {1} destroyed!", this, DisplayName.ShortName));
            }
            ShipDisabled = true;
        }
        if (!_engine.ComponentIsWorking || (noPower && Energy < _engine.EnergyPerTick))
        {
            ShipImmobilized = true;
            _speed = Mathf.Min(_speed, MaxSpeed / 2f);
            _engineDamageSmoke.Play();
            foreach (ParticleSystem p in _engineExhaustsOn.Union(_engineExhaustsIdle))
            {
                p.gameObject.SetActive(false);
            }
        }
        else
        {
            ShipImmobilized = false;
            _engineDamageSmoke.Stop();
            foreach (ParticleSystem p in _engineExhaustsOn.Union(_engineExhaustsIdle))
            {
                p.gameObject.SetActive(true);
            }
        }
    }

    public bool ArmourAtFull
    {
        get
        {
            foreach (ShipSection sec in _currMitigationArmour.Keys)
            {
                if (_currMitigationArmour[sec] != _maxMitigationArmour[sec])
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
            foreach (ShipSection sec in _currMitigationArmour.Keys)
            {
                if (_currMitigationArmour[sec] != _maxMitigationArmour[sec])
                {
                    needsRepair = true;
                    if (minArmour < 0 || _currMitigationArmour[sec] < minArmour)
                    {
                        minArmourSec = sec;
                        minArmour = _currMitigationArmour[sec];
                    }
                }
            }
            if (!needsRepair)
            {
                return;
            }
            if (_maxMitigationArmour[minArmourSec] - _currMitigationArmour[minArmourSec] <= repairPointsLeft)
            {
                repairPointsLeft -= _maxMitigationArmour[minArmourSec] - _currMitigationArmour[minArmourSec];
                _currMitigationArmour[minArmourSec] = _maxMitigationArmour[minArmourSec];
            }
            else
            {
                _currMitigationArmour[minArmourSec] += repairPointsLeft;
                repairPointsLeft = 0;
            }
        }
    }

    public override void NotifyInComabt()
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
            SetCircleStatus(ShipCircleStatus.Surrendered);
        }
        InBoarding = false;
    }

    private void ResetSpeed()
    {
        _speed = 0f;
        _rigidBody.velocity = Vector3.zero;
    }

    private void HandleShipCollision(Collider other)
    {
        Ship otherShip = other.GetComponent<Ship>();
        if (otherShip != null)
        {
            // Stop immediately
            //_rigidBody.AddForce(-_rigidBody.velocity, ForceMode.VelocityChange);
            _rigidBody.velocity = Vector3.zero;
            //RevertRotation();
            //otherShip.RevertRotation();
            //_inCollision = true;
            float massSum = Mass + otherShip.Mass;
            NotifyInComabt();

            if (ElectromagneticClampsActive &&
                (!InBoarding && !otherShip.InBoarding) &&
                (!ShipSurrendered && !otherShip.ShipSurrendered) &&
                ((otherShip.ShipImmobilized || otherShip.ShipDisabled) && otherShip.HullHitPoints > 0))
            {
                ResetSpeed();
                InBoarding = true;
                otherShip.InBoarding = true;
                StartCoroutine(Combat.BoardingCombat(this, otherShip));
            }
            else if (otherShip.ElectromagneticClampsActive)
            {
                //?
                ResetSpeed();
            }
            else
            {
                //ResolveCollision(otherShip, massSum);
            }
        }
    }

    private void HandleShipCollisionExit()
    {
        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        transform.rotation = Quaternion.LookRotation(forward, up);
        _rigidBody.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision c)
    {
        Debug.LogWarning(string.Format("Collision {0}", this));
        HandleShipCollision(c.collider);
    }

    private void OnCollisionExit(Collision c)
    {
        Debug.LogWarning(string.Format("Collision exit: {0},", this));
        _rigidBody.angularVelocity = Vector3.zero;
        HandleShipCollisionExit();
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
                foreach (CombatDetachment cd in AllComponents.Where(x => x is CombatDetachment).Select(y => y as CombatDetachment))
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

    public override bool Targetable { get { return ShipActiveInCombat; } }
    public override TargetableEntityInfo TargetableBy => TargetableEntityInfo.AntiShip | TargetableEntityInfo.Torpedo;
    public override ObjectFactory.TacMapEntityType TargetableEntityType
    {
        get
        {
            switch (ShipSize)
            {
                case ObjectFactory.ShipSize.Sloop:
                    return ObjectFactory.TacMapEntityType.Sloop;
                case ObjectFactory.ShipSize.Frigate:
                    return ObjectFactory.TacMapEntityType.Frigate;
                case ObjectFactory.ShipSize.Destroyer:
                    return ObjectFactory.TacMapEntityType.Destroyer;
                case ObjectFactory.ShipSize.Cruiser:
                    return ObjectFactory.TacMapEntityType.Cruiser;
                case ObjectFactory.ShipSize.CapitalShip:
                    return ObjectFactory.TacMapEntityType.CapitalShip;
                default:
                    throw new Exception("ShipSize not found. This should never happen.");
            }
        }
    }

    // Fields
    public enum ShipSection { Fore, Aft, Left, Right, Center, Hidden };

    public ObjectFactory.ShipSize ShipSize;

    private int _minEnergyForActive;

    public int Energy { get; private set; }
    public int MaxEnergy { get; private set; }
    public int Heat { get; private set; }
    public int MaxHeat { get; private set; }

    public string[] CenterComponentSlots;
    public string[] ForeComponentSlots;
    public string[] AftComponentSlots;
    public string[] LeftComponentSlots;
    public string[] RightComponentSlots;
    private Dictionary<ShipSection, string[]> _componentsSlotTypes = new Dictionary<ShipSection, string[]>();
    private Dictionary<ShipSection, Tuple<string, IShipComponent>[]> _componentSlotsOccupied = new Dictionary<ShipSection, Tuple<string, IShipComponent>[]>();
    private Dictionary<ShipSection, List<Tuple<string, IShipComponent>>> _turretSlotsOccupied = new Dictionary<ShipSection, List<Tuple<string, IShipComponent>>>();

    private IEnumerable<IShipComponent> AllComponentsInSection(ShipSection sec, bool includeTurrets)
    {
        if (_componentSlotsOccupied.ContainsKey(sec))
        {
            foreach (Tuple<string, IShipComponent> comp in _componentSlotsOccupied[sec])
            {
                if (comp != null)
                {
                    yield return comp.Item2;
                }
            }
        }
        if (includeTurrets && _turretSlotsOccupied.ContainsKey(sec))
        {
            foreach (Tuple<string, IShipComponent> comp in _turretSlotsOccupied[sec])
            {
                if (comp != null)
                {
                    yield return comp.Item2;
                }
            }
        }
    }

    public IEnumerable<IShipComponent> AllComponents
    {
        get
        {
            foreach (IEnumerable<Tuple<string, IShipComponent>> l in _componentSlotsOccupied.Values)
            {
                foreach (Tuple<string, IShipComponent> comp in l)
                {
                    if (comp != null)
                    {
                        yield return comp.Item2;
                    }
                }
            }
            foreach (IEnumerable<Tuple<string, IShipComponent>> l in _turretSlotsOccupied.Values)
            {
                foreach (Tuple<string, IShipComponent> comp in l)
                {
                    if (comp != null)
                    {
                        yield return comp.Item2;
                    }
                }
            }
        }
    }

    public IEnumerable<ShipCharacter> ShipCrew
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
        }
    }

    public IEnumerable<ShipCharacter> CombatCrew
    {
        get
        {
            foreach (CombatDetachment combatGroup in _combatDetachments)
            {
                foreach (ShipCharacter c in combatGroup.Forces)
                {
                    yield return c;
                }
            }
        }
    }

    public IEnumerable<ShipCharacter> AllCrew
    {
        get
        {
            return ShipCrew.Union(CombatCrew);
        }
    }

    public override bool ShipControllable { get { return base.ShipControllable && (!ShipSurrendered); } }
    public override bool ShipActiveInCombat { get { return base.ShipActiveInCombat && (!ShipSurrendered); } }

    private void SetCrewNumBuff()
    {
        int numCrew = 0;
        int totalLevels = 0;
        int avgLevel = 0;
        foreach (ShipCharacter currCrew in ShipCrew.Where(c => c.Status == ShipCharacter.CharacterStaus.Active))
        {
            totalLevels += (int)currCrew.Level;
            ++numCrew;
        }
        if (numCrew >= OperationalCrew)
        {
            avgLevel = Mathf.RoundToInt(((float)totalLevels) / numCrew);
        }
        else
        {
            foreach (ShipCharacter currCrew in CombatCrew.Where(c => c.Status == ShipCharacter.CharacterStaus.Active))
            {
                ++numCrew;
                if (numCrew >= OperationalCrew)
                {
                    break;
                }
            }
            avgLevel = Mathf.RoundToInt(((float)totalLevels) / numCrew);
        }
        _crewNumBuff = StandardBuffs.UndercrewedDebuff(numCrew, OperationalCrew, SkeletonCrew);
        _crewExperienceBuff = StandardBuffs.CrewExperienceBuff(avgLevel, numCrew < OperationalCrew);
    }

    private IEnumerable<DynamicBuff> AllBuffs
    {
        get
        {
            if (InherentBuff != null)
            {
                yield return InherentBuff.DynamicData;
            }
            yield return _crewNumBuff;
            yield return _crewExperienceBuff;
            foreach (IShipComponent comp in AllComponents)
            {
                yield return comp.ComponentBuff;
            }
        }
    }

    // In formation behavior
    public override void AddToFormation(FormationBase f)
    {
    }

    public override void RemoveFromFormation()
    {
    }

    public override bool InPositionInFormation()
    {
        return true;
    }

    private IEnergyCapacityComponent[] _energyCapacityComps;
    private IHeatCapacityComponent[] _heatCapacityComps;
    private IPeriodicActionComponent[] _updateComponents;
    private IShieldComponent[] _shieldComponents;
    private CombatDetachment[] _combatDetachments;
    private bool _useShields = true;
    private ShipEngine _engine;

    private ElectromagneticClamps _electromagneticClamps;
    private ParticleSystem _electromagneticClampsEffect;

    public float Mass;

    public ShipHullFourSidesValues Armour;
    public ShipHullFourSidesValues ReducedArmour;
    public ShipHullFourSidesValues DefaultMitigationArmour;
    public float ArmourMitigation;
    private IDictionary<ShipSection, int> _armour;
    private IDictionary<ShipSection, int> _reducedArmour;
    private IDictionary<ShipSection, int> _maxMitigationArmour;
    private IDictionary<ShipSection, int> _currMitigationArmour;
    public bool ShipSurrendered { get; private set; }
    public bool InBoarding { get; private set; }

    public int SkeletonCrew;
    public int OperationalCrew;
    public int MaxCrew;
    public int MaxSpecialCharacters;
    public IEnumerable<ShipCharacter> Crew { get { return _crew; } }
    private List<ShipCharacter> _crew;
    public IEnumerable<SpecialCharacter> SpecialCharacters { get { return _specialCharacters; } }
    private List<SpecialCharacter> _specialCharacters;
    public ShipCharacter Captain { get; set; }
    public ShipDisplayName DisplayName { get; set; }

    public float LastInCombat { get; private set; }

    private ParticleSystem _engineDamageSmoke;

    private ParticleSystem[] _engineExhaustsOn, _engineExhaustsIdle, _engineExhaustsBrake;
    private bool _acceleratingForward = false;
    private bool _acceleratingBackwards = false;
    private bool _brakingForward = false;
    private bool _brakingBackwards = false;

    // Buff/debuff mechanic
    public StaticBuff InherentBuff { get; set; }
    private DynamicBuff _crewNumBuff;
    private DynamicBuff _crewExperienceBuff;

    private bool _explosionPlayed = false;

    private static readonly WaitForSeconds _componentPulseDelay = new WaitForSeconds(0.25f);
}

[Serializable]
public struct ShipHullFourSidesValues
{
    public int Fore;
    public int Aft;
    public int Left;
    public int Right;

    public int ForeValue { get { return Fore; } set { Fore = value; } }
    public int AftValue { get { return Aft; } set { Aft = value; } }
    public int LeftValue { get { return Left; } set { Left = value; } }
    public int RightValue { get { return Right; } set { Right = value; } }

    public IDictionary<Ship.ShipSection, int> ToDict()
    {
        IDictionary<Ship.ShipSection, int> res = new Dictionary<Ship.ShipSection, int>();
        res[Ship.ShipSection.Fore] = Fore;
        res[Ship.ShipSection.Aft] = Aft;
        res[Ship.ShipSection.Left] = Left;
        res[Ship.ShipSection.Right] = Right;
        return res;
    }
}
