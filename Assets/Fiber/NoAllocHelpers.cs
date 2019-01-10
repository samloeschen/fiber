using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Fiber
{
    public static class NoAllocHelpers
    {
        private static readonly Dictionary<Type, Delegate> ExtractArrayFromListTDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> ResizeListDelegates = new Dictionary<Type, Delegate>();

        public static T[] ExtractArrayFromListT<T>(List<T> list)
        {
            if (!ExtractArrayFromListTDelegates.TryGetValue(typeof(T), out var obj))
            {
                var ass = Assembly.GetAssembly(typeof(Mesh)); // any class in UnityEngine
                var type = ass.GetType("UnityEngine.NoAllocHelpers");
                var methodInfo = type.GetMethod("ExtractArrayFromListT", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(T));

                obj = ExtractArrayFromListTDelegates[typeof(T)] = Delegate.CreateDelegate(typeof(Func<List<T>, T[]>), methodInfo);
            }

            var func = (Func<List<T>, T[]>)obj;
            return func.Invoke(list);
        }

        public static void ResizeList<T>(List<T> list, int size)
        {
            if (!ResizeListDelegates.TryGetValue(typeof(T), out var obj))
            {
                var ass = Assembly.GetAssembly(typeof(Mesh)); // any class in UnityEngine
                var type = ass.GetType("UnityEngine.NoAllocHelpers");
                var methodInfo = type.GetMethod("ResizeList", BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(typeof(T));
                obj = ResizeListDelegates[typeof(T)] =
                    Delegate.CreateDelegate(typeof(Action<List<T>, int>), methodInfo);
            }

            var action = (Action<List<T>, int>)obj;
            action.Invoke(list, size);
        }
    }
}