﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using static Friflo.Fliox.Engine.ECS.StructUtils;

// Hard rule: this file/section MUST NOT use GameEntity instances

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertConstructorToMemberInitializers
namespace Friflo.Fliox.Engine.ECS;

public sealed partial class EntityStore
{
    private Archetype GetArchetypeWith<T>(Archetype current)
        where T : struct
    {
        var hash = GetHashWith(typeof(T), current);
        if (TryGetArchetype(hash, out var archetype)) {
            return archetype;
        }
        var newHeap = StructHeap<T>.Create(Static.DefaultCapacity, typeStore);
        archetype   = Archetype.CreateFromArchetype(GetArchetypeConfig(), current, newHeap);
        AddArchetype(archetype);
        return archetype;
    }
    
    private Archetype GetArchetypeWith(Archetype current, StructFactory factory)
    {
        var hash = factory.hash ^ current.typeHash;
        if (TryGetArchetype(hash, out var archetype)) {
            return archetype;
        }
        var newHeap = factory.CreateHeap(Static.DefaultCapacity);
        archetype   = Archetype.CreateFromArchetype(GetArchetypeConfig(), current, newHeap);
        AddArchetype(archetype);
        return archetype;
    }
    
    private Archetype GetArchetype(StructFactory factory)
    {
        // could perform lookup per lookup array
        if (TryGetArchetype(factory.hash, out var archetype)) {
            return archetype;
        }
        var newHeap = factory.CreateHeap(Static.DefaultCapacity);
        archetype   = Archetype.CreateFromArchetype(GetArchetypeConfig(), defaultArchetype, newHeap);
        AddArchetype(archetype);
        return archetype;
    }
    
    private Archetype GetArchetypeWithout(Archetype archetype, Type removeType)
    {
        var hash = GetHashWithout(removeType, archetype);
        if (TryGetArchetype(hash, out var result)) {
            return result;
        }
        var componentCount = archetype.Heaps.Length - 1;
        if (componentCount == 0) {
            return null;
        }
        var heaps = new StructHeap[componentCount];
        int n = 0;
        foreach (var heap in archetype.Heaps) {
            if (heap.type == removeType)
                continue;
            heaps[n++] = heap;
        }
        if (n != componentCount) {
            throw new InvalidOperationException("unexpected length");
        }
        result = Archetype.CreateWithHeaps(GetArchetypeConfig(), heaps);
        AddArchetype(result);
        return result;
    }
    
    private bool TryGetArchetype (long hash, out Archetype result) {
        foreach (var arch in archetypeInfos) {
            if (arch.hash != hash) {
                continue;
            }
            result = arch.type;
            return true;
        }
        result = null;
        return false;
    }
    
    private void AddArchetype (Archetype archetype) {
        if (archetype == null) {
            return;
        }
        if (archetypesCount == archetypes.Length) {
            var newLen = 2 * archetypes.Length;
            Utils.Resize(ref archetypes,     newLen);
            Utils.Resize(ref archetypeInfos, newLen);
        }
        if (archetype.archIndex != archetypesCount) {
            throw new InvalidOperationException("invalid archIndex");
        }
        archetypes    [archetypesCount] = archetype;
        archetypeInfos[archetypesCount] = new ArchetypeInfo(archetype.typeHash, archetype);
        archetypesCount++;
    }
    
    // ------------------------------------ hash utils ------------------------------------
    private static long GetHashWith(Type newType, Archetype archetype) {
        return newType.Handle() ^ archetype.typeHash;
    }
    
    private static long GetHashWithout(Type removeType, Archetype archetype)
    {
        return removeType.Handle() ^ archetype.typeHash;
    }
    
    private static long GetHash(ReadOnlySpan<long> hashes) {
        long hash = 0;
        foreach (var h in hashes) {
            hash ^= h;
        }
        return hash;
    }
    
    internal static long GetHash(StructHeap[] heaps, StructHeap newComp) {
        long hash = default;
        if (newComp != null) {
            hash = newComp.hash;
        }
        foreach (var heap in heaps) {
            hash ^= heap.hash;
        }
        return hash;
    }
    
    // ------------------------------------ entity / component management ------------------------------------
    internal bool AddComponent<T>(
            int                 id,
        ref Archetype           archetype,
        ref int                 compIndex,
        in  T                   component,
            ComponentUpdater    updater)
        where T : struct
    {
        var arch = archetype;
        if (arch != defaultArchetype) {
            var compHeap = arch.FindComponentHeap<T>();
            if (compHeap != null) {
                // --- change component value 
                var heap = (StructHeap<T>)compHeap;
                heap.chunks[compIndex / ChunkSize].components[compIndex % ChunkSize] = component;
                return false;
            }
            // --- change entity archetype
            var newArchetype    = GetArchetypeWith<T>(arch);
            compIndex           = arch.MoveEntityTo(id, compIndex, newArchetype, updater);
            archetype           = arch = newArchetype;
        } else {
            // --- add entity to archetype
            arch                = GetArchetype<T>();
            compIndex           = arch.AddEntity(id);
            archetype           = arch;
        }
        // --- set component value 
        var structHeap = (StructHeap<T>)arch.HeapMap[StructHeap<T>.ComponentIndex];
        structHeap.chunks[compIndex / ChunkSize].components[compIndex % ChunkSize] = component;
        return true;
    }
    
    internal bool RemoveComponent<T>(
            int                 id,
        ref Archetype           archetype,
        ref int                 compIndex,
            ComponentUpdater    updater)
        where T : struct
    {
        var arch = archetype;
        if (arch == null) {
            return false;
        }
        var heap = arch.FindComponentHeap<T>();
        if (heap == null) {
            return false;
        }
        var newArchetype = GetArchetypeWithout(arch, typeof(T));
        if (newArchetype == null) {
            int removePos = compIndex; 
            // --- update entity
            compIndex   = 0;
            archetype   = null;
            arch.MoveLastComponentsTo(removePos, updater);
            return true;
        }
        // --- change entity archetype
        compIndex   = arch.MoveEntityTo(id, compIndex, newArchetype, updater);
        archetype   = newArchetype;
        return true;
    }
    
    public void RegisterStructComponent<T>() where T : struct  {
        var heapIndex   = StructHeap<T>.ComponentIndex;
        var key         = StructHeap<T>.ComponentKey;
        var factory     = new StructFactory<T>(heapIndex, key, typeStore);
        factories[key]  = factory;
    }
    
    internal bool ReadComponent(
        ObjectReader        reader,
        JsonValue           json,
        int                 id,
        ref Archetype       archetype,
        ref int             compIndex,
        StructFactory       factory,
        ComponentUpdater    updater)
    {
        var arch = archetype;
        if (arch != defaultArchetype) {
            var compHeap = arch.FindComponentHeap(factory.heapIndex);
            if (compHeap != null) {
                // --- change component value 
                compHeap.Read(reader, compIndex, json);
                return false;
            }
            // --- change entity archetype
            var newArchetype    = archetype.store.GetArchetypeWith(archetype, factory);
            compIndex           = arch.MoveEntityTo(id, compIndex, newArchetype, updater);
            archetype           = arch = newArchetype;
        } else {
            // --- add entity to archetype
            arch                = archetype.store.GetArchetype(factory);
            compIndex           = arch.AddEntity(id);
            archetype           = arch;
        }
        // --- set component value 
        var structHeap = arch.HeapMap[factory.heapIndex];
        structHeap.Read(reader, compIndex, json);
        return true;
    }
}

internal abstract class StructFactory
{
    internal readonly   int     heapIndex;
    internal readonly   string  keyName;
    internal readonly   long    hash;
        
    internal abstract StructHeap CreateHeap(int capacity);
    
    internal StructFactory(int heapIndex, string keyName, long hash) {
        this.heapIndex  = heapIndex;
        this.keyName    = keyName;
        this.hash       = hash;
    }
}

internal class StructFactory<T> : StructFactory 
    where T : struct
{
    private readonly    TypeMapper<T>   typeMapper;
    
    internal StructFactory(int heapIndex, string keyName, TypeStore typeStore)
        : base(heapIndex, keyName, typeof(T).Handle())
    {
        typeMapper = typeStore.GetTypeMapper<T>();
    }
    
    internal override StructHeap CreateHeap(int capacity) {
        return new StructHeap<T>(heapIndex, keyName, capacity, typeMapper);   
    }
}
