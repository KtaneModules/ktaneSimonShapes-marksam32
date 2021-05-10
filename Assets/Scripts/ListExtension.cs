using System;
using System.Collections.Generic;

namespace SimonShapesModule
{
    public static class ListExtension
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var element in source)
            {
                action(element);
            }
        }
    }
}