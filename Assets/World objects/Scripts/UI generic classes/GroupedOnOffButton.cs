using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupedOnOffButton : OnOffButton
{
    protected override void Awake()
    {
        base.Awake();
        if (Group != null)
        {
            Group.Register(this);
        }
    }

    public override bool Value
    {
        get
        {
            return base.Value;
        }
        set
        {
            bool oldValue = base.Value;
            if (null != Group && !oldValue && value)
            {
                // Turning on
                if (Group.NumButtonsOn < Group.MaxOn)
                {
                    base.Value = value;
                }
                else
                {
                    GroupedOnOffButton other = Group.FirstOn;
                    if (null != other)
                    {
                        base.Value = value;
                        other.Value = false;
                    }
                }
            }
            else if (null != Group && oldValue && !value)
            {
                // Turning off
                if (Group.NumButtonsOn > Group.MinOn)
                {
                    base.Value = value;
                }
            }
            else
            {
                // No change
                base.Value = value;
            }
        }
    }

    [SerializeField]
    public MultiToggleGroup Group
    {
        get
        {
            return _group;
        }
        set
        {
            MultiToggleGroup oldGroup = _group;
            if (oldGroup != null)
            {
                oldGroup.UnRegister(this);
            }
            if (value != null)
            {
                value.Register(this);
            }
            _group = value;
        }
    }

    private MultiToggleGroup _group;
}
