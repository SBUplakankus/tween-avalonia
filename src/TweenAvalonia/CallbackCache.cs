using System;
using System.Collections.Generic;
using System.Reflection;

namespace TweenAvalonia;

/// <summary>
/// Caches the untyped callback wrappers used by target-based tween callbacks
/// (<c>OnComplete(target, static t => ...)</c>, <c>OnUpdate</c>,
/// <c>Custom(target, ...)</c>). A wrapper is created once per unique callback
/// method + target and reused, so with static lambdas (no captures) target-based
/// callbacks allocate nothing after their first use at each call site. Closure
/// lambdas miss the cache every time and fall back to allocating — write
/// callbacks as static lambdas to stay allocation-free.
/// </summary>
internal static class CallbackCache
{
    private readonly record struct Key(MethodInfo Method, object? Target);

    private static readonly Dictionary<Key, Delegate> Cache = new();

    private static class CompleteWrapper<TTarget> where TTarget : class
    {
        public static Action<object?> Create(Action<TTarget> callback) => target => callback((TTarget)target!);
    }

    private static class UpdateWrapper<TTarget> where TTarget : class
    {
        public static Action<object?, double> Create(Action<TTarget, double> callback) =>
            (target, value) => callback((TTarget)target!, value);
    }

    private static class ValueWrapper<TTarget, TValue> where TTarget : class
    {
        public static Action<object?, TValue> Create(Action<TTarget, TValue> callback) =>
            (target, value) => callback((TTarget)target!, value);
    }

    internal static Action<object?> WrapComplete<TTarget>(TTarget target, Action<TTarget> callback) where TTarget : class
    {
        var key = new Key(callback.Method, callback.Target);
        if (Cache.TryGetValue(key, out Delegate? cached))
        {
            return (Action<object?>)cached;
        }

        Action<object?> wrapper = CompleteWrapper<TTarget>.Create(callback);
        Cache[key] = wrapper;
        return wrapper;
    }

    internal static Action<object?, double> WrapUpdate<TTarget>(TTarget target, Action<TTarget, double> callback)
        where TTarget : class
    {
        var key = new Key(callback.Method, callback.Target);
        if (Cache.TryGetValue(key, out Delegate? cached))
        {
            return (Action<object?, double>)cached;
        }

        Action<object?, double> wrapper = UpdateWrapper<TTarget>.Create(callback);
        Cache[key] = wrapper;
        return wrapper;
    }

    internal static Action<object?, TValue> WrapValue<TTarget, TValue>(TTarget target, Action<TTarget, TValue> callback)
        where TTarget : class
    {
        var key = new Key(callback.Method, callback.Target);
        if (Cache.TryGetValue(key, out Delegate? cached))
        {
            return (Action<object?, TValue>)cached;
        }

        Action<object?, TValue> wrapper = ValueWrapper<TTarget, TValue>.Create(callback);
        Cache[key] = wrapper;
        return wrapper;
    }
}
