﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS.Index;

internal static class ComponentIndexUtils
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077", Justification = "TODO")] // TODO
    internal static AbstractComponentIndex CreateComponentIndex(EntityStore store, ComponentType componentType)
    {
        var flags   = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance;
        var args    = new object[] { store, componentType };
        var obj     = Activator.CreateInstance(componentType.IndexType, flags, null, args, null);
        var index   = (AbstractComponentIndex)obj!;
        return index;
    }
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "TODO")] // TODO
    internal static Type GetIndexType(Type componentType, out Type valueType)
    {
        var interfaces = componentType.GetInterfaces();
        foreach (var i in interfaces)
        {
            if (!i.IsGenericType) continue;
            var genericType = i.GetGenericTypeDefinition();
            if (genericType != typeof(IIndexedComponent<>)) {
                continue;
            }
            valueType = i.GenericTypeArguments[0];
            return MakeIndexType(valueType, componentType);
        }
        valueType = null;
        return null;
    }
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2055", Justification = "TODO")] // TODO
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2065", Justification = "TODO")] // TODO
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "TODO")] // TODO
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "TODO")] // TODO
    private static Type MakeIndexType(Type valueType, Type componentType)
    {
        if (valueType == typeof(Entity)) {
            return typeof(EntityIndex<>).MakeGenericType(new [] { componentType });
        }
        var indexType   = GetComponentIndex(componentType);
        var typeArgs    = new [] { componentType, valueType };
        if (indexType != null) {
            return indexType.                 MakeGenericType(typeArgs);
        }
        if (valueType.IsClass) {
            return typeof(ValueClassIndex<,>).MakeGenericType(typeArgs);
        }
        return typeof(ValueStructIndex<,>).   MakeGenericType(typeArgs);
    }
    
    private static Type GetComponentIndex(Type type)
    {
        foreach (var attr in type.CustomAttributes) {
            if (attr.AttributeType != typeof(ComponentIndexAttribute)) {
                continue;
            }
            var arg = attr.ConstructorArguments;
            return (Type) arg[0].Value;
        }
        return null;
    }
}

