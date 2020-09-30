using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneStack
{
    public static void CallScene(string scene, ISceneParams args)
    {
        _stack.Push(args);
        SceneManager.LoadScene(scene);
    }

    public static void ReturnFromScene<T>() where T : ISceneParams
    {
        ISceneParams topOfStack = _stack.Peek();
        if (topOfStack is T t)
        {
            _stack.Pop();
            SceneManager.LoadScene(topOfStack.CalledFromScene);
        }
        else
        {
            Debug.LogWarningFormat("Attempted to return from scene {0} to scene {1} with incorrect parameter type. Expected: {2} Got: {3}",
                                   SceneManager.GetActiveScene().name, topOfStack.CalledFromScene, typeof(T), topOfStack.GetType());
        }
    }

    public static T GetSceneParams<T>() where T : ISceneParams
    {
        if (_stack.Count == 0)
        {
            Debug.LogWarningFormat("Attempted to get parameters from empty stack in scene {0}", SceneManager.GetActiveScene().name);
            return default;
        }

        ISceneParams topOfStack = _stack.Peek();
        if (topOfStack is T t)
        {
            return t;
        }
        else
        {
            Debug.LogWarningFormat("Incorrect parameter type called for scene {0}. Expected: {1} Got: {2}",
                       SceneManager.GetActiveScene().name, typeof(T), topOfStack.GetType());
            return default;
        }
    }

    private static Stack<ISceneParams> _stack = new Stack<ISceneParams>();
}

public interface ISceneParams
{
    string CalledFromScene { get; set; }
}
