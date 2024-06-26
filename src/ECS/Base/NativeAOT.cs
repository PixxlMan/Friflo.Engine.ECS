﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable UseRawString
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

// ReSharper disable once InconsistentNaming
public sealed class NativeAOT
{
    private             EntitySchema        entitySchema;
    private             bool                engineTypesRegistered;
        
    private readonly    List<Type>          types       = new();
    private readonly    HashSet<Type>       typeSet     = new();
    private readonly    TypeStore           typeStore   = new TypeStore();
    private readonly    SchemaTypes         schemaTypes = new();
    
    private static      NativeAOT           Instance;
    
    [ExcludeFromCodeCoverage]
    internal static EntitySchema GetSchema()
    {
        var schema = Instance?.entitySchema;
        if (schema != null) {
            return schema;
        }
        return CreateDefaultSchema();
    }
    
    [ExcludeFromCodeCoverage]
    private static EntitySchema CreateDefaultSchema()
    {
        var schema = Instance?.entitySchema;
        if (schema != null) {
            return schema;
        }
        var msg =
@"EntitySchema not created.
NativeAOT requires schema creation on startup:
1. Create NativeAOT instance:   var aot = new NativeAOT();
2. Register types with:         aot.Register...(); 
3. Finish with:                 aot.CreateSchema();";
        Console.Error.WriteLine(msg);
        var aot = new NativeAOT();
        Console.WriteLine("Using default EntitySchema");
        return aot.CreateSchemaInternal();
/*  Return default schema instead of throwing an exception.
    By doing this subsequent access to components, tags & script result in meaningful stack traces.
    
    Throwing an exception is not helpful.
    E.g. the exception is thrown from within a constructor - like EntityStore(). In this case the exception log looks like:
        
A type initializer threw an exception. To determine which type, inspect the InnerException's StackTrace property.
   Stack Trace:
   at System.Runtime.CompilerServices.ClassConstructorRunner.EnsureClassConstructorRun(StaticClassConstructionContext*) + 0x247
   at System.Runtime.CompilerServices.ClassConstructorRunner.CheckStaticClassConstructionReturnGCStaticBase(StaticClassConstructionContext*, Object) + 0x1c
   at Friflo.Engine.ECS.EntityStoreBase.GetArchetypeConfig(EntityStoreBase) + 0x39
   at Friflo.Engine.ECS.EntityStoreBase..ctor() + 0xe1
   at Friflo.Engine.ECS.EntityStore..ctor(PidType) + 0x43
   at Friflo.Engine.ECS.EntityStore..ctor() + 0x1a
*/
    }
    
    private EntitySchema CreateSchemaInternal()
    {
        InitSchema();
        
        var assemblyMap = new Dictionary<Assembly, int>();
        var assemblies  = new List<Assembly>();
        foreach (var type in types) {
            var assembly = type.Assembly;
            if (!assemblyMap.TryGetValue(assembly, out int assemblyIndex)) {
                assemblyIndex = assemblies.Count;
                assemblyMap.Add(assembly, assemblyIndex);
                assemblies.Add(assembly);
            }
            schemaTypes.AddSchemaType(type, assemblyIndex);
        }
        var dependants  = schemaTypes.CreateSchemaTypes(typeStore, assemblies);
        entitySchema    = new EntitySchema(dependants, schemaTypes);
        Instance        = this;
        return entitySchema;
    }
    
    public EntitySchema CreateSchema()
    {
        Console.WriteLine("NativeAOT.CreateSchema()");
        return CreateSchemaInternal();
    }
    
    private void InitSchema()
    {
        if (Instance?.entitySchema != null) {
            throw new InvalidOperationException("EntitySchema already created");
        }
        if (engineTypesRegistered) {
            return;
        }
        engineTypesRegistered = true;

        RegisterComponent<EntityName>();
        RegisterComponent<Position>();
        RegisterComponent<Rotation>();
        RegisterComponent<Scale3>();
        RegisterComponent<Transform>();
        RegisterComponent<TreeNode>();
        RegisterComponent<UniqueEntity>();
        RegisterComponent<Unresolved>();

        RegisterTag<Disabled>();
    }
    
    public void RegisterComponent<T>() where T : struct, IComponent 
    {
        InitSchema();
        if (typeSet.Add(typeof(T))) {
            types.Add(typeof(T));
            SchemaUtils.CreateComponentType<T>(typeStore, 0, null); // dummy call to prevent trimming required type info
        }
    }
    
    public void RegisterTag<T>()  where T : struct, ITag 
    {
        InitSchema();
        if (typeSet.Add(typeof(T))) {
            types.Add(typeof(T));
            SchemaUtils.CreateTagType<T>(0);                        // dummy call to prevent trimming required type info
        }
    }
    
    public void RegisterScript<T>()  where T : Script, new()
    {
        InitSchema();
        if (typeSet.Add(typeof(T))) {
            types.Add(typeof(T));
            SchemaUtils.CreateScriptType<T>(typeStore, 0);          // dummy call to prevent trimming required type info
        }
    }
}