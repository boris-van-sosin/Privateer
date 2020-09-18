using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiToggleGroup : MonoBehaviour
{
    public int MinOn;
    public int MaxOn;

    public bool Register(GroupedOnOffButton btn)
    {
        if (!_buttons.Contains(btn))
        {
            _buttons.Add(btn);
            Action<bool> callback = (v => ButtonChanged(btn, v));
            _callbacks.Add(callback);
            btn.onValueChanged += callback;
            if (btn.Value)
            {
                _onButtonHistory.AddLast(btn);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool UnRegister(GroupedOnOffButton btn)
    {
        int idx = _buttons.IndexOf(btn);
        if (idx >= 0)
        {
            btn.onValueChanged -= _callbacks[idx];
            _callbacks.RemoveAt(idx);
            _buttons.RemoveAt(idx);
            return true;
        }
        else
        {
            return false;
        }
    }

    public IEnumerable<GroupedOnOffButton> ButtonsOn => _buttons.Where(b => b.Value);
    public IEnumerable<GroupedOnOffButton> ButtonsOff => _buttons.Where(b => !b.Value);
    public IReadOnlyList<GroupedOnOffButton> ButtonsAll => _buttons;
    public GroupedOnOffButton FirstOn
    {
        get
        {
            LinkedListNode<GroupedOnOffButton> first = _onButtonHistory.First;
            if (first != null)
            {
                return first.Value;
            }
            else
            {
                return null;
            }
        }
    }

    private void ButtonChanged(GroupedOnOffButton btn, bool on)
    {
        if (on)
        {
            _onButtonHistory.AddLast(btn);
        }
        else
        {
            LinkedListNode<GroupedOnOffButton> queueItem = _onButtonHistory.Find(btn);
            if (queueItem != null)
            {
                _onButtonHistory.Remove(queueItem);
            }
        }
    }

    public int NumButtonsAll => _buttons.Count;
    public int NumButtonsOn => _buttons.Count(b => b.Value);
    public int NumButtonsOff => _buttons.Count(b => !b.Value);

    private List<GroupedOnOffButton> _buttons = new List<GroupedOnOffButton>();
    private List<Action<bool>> _callbacks = new List<Action<bool>>();
    private LinkedList<GroupedOnOffButton> _onButtonHistory = new LinkedList<GroupedOnOffButton>();
}
