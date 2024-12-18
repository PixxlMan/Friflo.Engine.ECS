using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public readonly struct CopyContext
{
    public readonly Entity source;
    public readonly Entity target;
    
    internal CopyContext(Entity source, Entity target) {
        this.source = source;
        this.target = target;
    }
}

internal delegate void CopyValue<TValue>(in TValue value, ref TValue target, in CopyContext context);


internal static class CopyValueUtils
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090", Justification = "TODO")] // TODO
    internal static CopyValue<TComponent> CreateCopyValue<TComponent>()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.InvokeMethod;
        var method = typeof(TComponent).GetMethod("CopyValue", flags);
        if (method is null) {
            var schema = EntityStoreBase.Static.EntitySchema;
            var componentType = schema.ComponentTypeByType[typeof(TComponent)];
            if (componentType.IsBlittable) {
                return null;
            }
            return MissingCopyValue;
        }
        try {
            var genericDelegate = Delegate.CreateDelegate(typeof(CopyValue<TComponent>), method);
            return (CopyValue<TComponent>)genericDelegate;
        } catch (ArgumentException) {
            return IncompatibleSignature;
        }
    }
    
    private static void MissingCopyValue<TValue>(in TValue value, ref TValue target, in CopyContext context) {
        var name = typeof(TValue).Name;
        var msg = $"type: {typeof(TValue).Namespace}.{name} - expect: static void CopyValue(in {name} source, ref {name} target, in CopyContext context)";
        throw new MissingMethodException(msg);
    }
    
    private static void IncompatibleSignature<TValue>(in TValue value, ref TValue target, in CopyContext context) {
        var name = typeof(TValue).Name;
        var msg = $"Incompatible method signature: {typeof(TValue).Namespace}.{name}.CopyValue() - expect: static void CopyValue(in {name} source, ref {name} target, in CopyContext context)";
        throw new ArgumentException(msg);
    }
}

internal static class CopyValueUtils<TValue>
{
    internal static readonly    CopyValue<TValue>     CopyValue        = CopyValueUtils.CreateCopyValue<TValue>();
}
