using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace TiaGitAddIn.Entry
{
    internal static class MenuSelectionContextResolver
    {
        private static readonly string[] MethodNames =
        [
            "GetSelection",
            "GetSelectedObjects",
            "GetSelectedItems"
        ];

        private static readonly string[] PropertyNames =
        [
            "Selection",
            "SelectedObjects",
            "SelectedItems",
            "Items",
            "Context"
        ];

        public static object? Resolve(object provider)
        {
            if (provider == null)
            {
                return null;
            }

            foreach (string methodName in MethodNames)
            {
                object? value = TryInvoke(provider, methodName);
                object? normalized = Normalize(value);
                if (normalized != null)
                {
                    return normalized;
                }
            }

            foreach (string propertyName in PropertyNames)
            {
                object? value = TryRead(provider, propertyName);
                object? normalized = Normalize(value);
                if (normalized != null)
                {
                    return normalized;
                }
            }

            return provider;
        }

        private static object? TryInvoke(object source, string methodName)
        {
            try
            {
                MethodInfo? method = source
                    .GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(candidate =>
                        candidate.Name == methodName &&
                        candidate.GetParameters().Length == 0 &&
                        !candidate.IsGenericMethodDefinition)
                    .OrderBy(candidate => candidate.ReturnType == typeof(void))
                    .FirstOrDefault();

                return method?.Invoke(source, null);
            }
            catch (TargetInvocationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (AmbiguousMatchException)
            {
                return null;
            }
        }

        private static object? TryRead(object source, string propertyName)
        {
            try
            {
                PropertyInfo? property = source.GetType().GetProperty(propertyName);
                return property?.GetValue(source, null);
            }
            catch (TargetInvocationException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static object? Normalize(object? value)
        {
            if (value is string)
            {
                return value;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    return item;
                }

                return null;
            }

            return value;
        }
    }
}
