﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class GenericOpenCloseAnim : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ComponentState = State.Closed;
        if (AnimWaypoints != null)
        {
            _phases = EndPoints(false).Concat(AnimWaypoints).Concat(EndPoints(true)).ToArray();
        }
        else
        {
            _phases = EndPoints(false).Concat(EndPoints(true)).ToArray();
        }
    }

    public void Open()
    {
        Open(null);
    }

    public void Open(Action onFinish)
    {
        if (ComponentState != State.Closed)
        {
            return;
        }

        StartCoroutine(AnimateOpen(onFinish));
    }

    public void Close(Action onFinish)
    {
        if (ComponentState != State.Open)
        {
            return;
        }

        StartCoroutine(AnimateClose(onFinish));
    }

    public void Close()
    {
        Close(null);
    }

    private IEnumerator AnimateOpen(Action onFinish)
    {
        int phase = 1;
        float timeStarted = Time.time;
        AnimState prev = _phases[phase - 1];
        AnimState curr = _phases[phase];
        ComponentState = State.Opening;
        while (true)
        {
            float phaseProgress = (Time.time - timeStarted) / curr.Duration;
            if (Mathf.Approximately(curr.Duration, 0) || phaseProgress >= 1)
            {
                for (int i = 0; i < AnimComponents.Length; ++i)
                {
                    AnimComponents[i].localPosition = curr.Positions[i];
                    AnimComponents[i].localRotation = Quaternion.Euler(curr.Rotations[i]);
                }
                ++phase;
                if (phase >= _phases.Length)
                {
                    break;
                }
                else
                {
                    prev = curr;
                    curr = _phases[phase];
                    timeStarted = Time.time;
                }
            }
            else
            {
                for (int i = 0; i < AnimComponents.Length; ++i)
                {
                    AnimComponents[i].localPosition = Vector3.Lerp(prev.Positions[i], curr.Positions[i], phaseProgress);
                    AnimComponents[i].localRotation = Quaternion.Slerp(Quaternion.Euler(prev.Rotations[i]), Quaternion.Euler(curr.Rotations[i]), phaseProgress);
                }
            }
            yield return _endOfFrameWait;
        }
        ComponentState = State.Open;
        onFinish?.Invoke();
        yield return null;
    }

    private IEnumerator AnimateClose(Action onFinish)
    {
        int phase = _phases.Length - 2;
        float timeStarted = Time.time;
        AnimState prev = _phases[phase + 1];
        AnimState curr = _phases[phase];
        ComponentState = State.Closing;
        while (true)
        {
            float phaseProgress = (Time.time - timeStarted) / prev.Duration;
            if (Mathf.Approximately(prev.Duration, 0) || phaseProgress >= 1)
            {
                for (int i = 0; i < AnimComponents.Length; ++i)
                {
                    AnimComponents[i].localPosition = curr.Positions[i];
                    AnimComponents[i].localRotation = Quaternion.Euler(curr.Rotations[i]);
                }
                --phase;
                if (phase < 0)
                {
                    break;
                }
                else
                {
                    prev = curr;
                    curr = _phases[phase];
                    timeStarted = Time.time;
                }
            }
            else
            {
                for (int i = 0; i < AnimComponents.Length; ++i)
                {
                    AnimComponents[i].localPosition = Vector3.Lerp(prev.Positions[i], curr.Positions[i], phaseProgress);
                    AnimComponents[i].localRotation = Quaternion.Slerp(Quaternion.Euler(prev.Rotations[i]), Quaternion.Euler(curr.Rotations[i]), phaseProgress);
                }
            }
            yield return _endOfFrameWait;
        }
        ComponentState = State.Closed;
        onFinish?.Invoke();
        yield return null;
    }

    public enum State { Closed, Closing, Open, Opening };

    public State ComponentState { get; private set; }

    [System.Serializable]
    public struct AnimState
    {
        public Vector3[] Positions;
        public Vector3[] Rotations;
        public float Duration;
    }

    private IEnumerable<AnimState> EndPoints(bool open)
    {
        if (open)
            yield return OpenState;
        else
            yield return ClosedState;
    }

    public Transform[] AnimComponents;
    public AnimState ClosedState;
    public AnimState OpenState;
    public AnimState[] AnimWaypoints;
    private AnimState[] _phases;

    private static readonly WaitForEndOfFrame _endOfFrameWait = new WaitForEndOfFrame();
}
