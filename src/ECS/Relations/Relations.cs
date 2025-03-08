﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.


using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static System.Diagnostics.DebuggerBrowsableState;
using Browse = System.Diagnostics.DebuggerBrowsableAttribute;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Contains the relations of a specific entity returned by <see cref="RelationExtensions.GetRelations{TRelation}"/>.
/// </summary>
[DebuggerTypeProxy(typeof(RelationsDebugView<>))]
public readonly struct Relations<TRelation>
    where TRelation : struct
{
    public   override   string          ToString()  => $"Relations<{typeof(TRelation).Name}>[{Length}]";
    /// <summary>
    /// Return the number of relations.<br/>
    /// Executes in O(1).
    /// </summary>
    public   readonly   int             Length;     //  4
    internal readonly   int             start;      //  4
    internal readonly   int[]           positions;  //  8
    internal readonly   TRelation[]     components; //  8
    internal readonly   int             position;   //  4
    
    internal Relations(TRelation[] components, int[] positions, int start, int length)
    {
        this.components = components;
        this.positions  = positions;
        this.start      = start;
        Length          = length;
    }
   
    internal Relations(TRelation[] components, int position) {
        this.components = components;
        this.position   = position;
        Length          = 1;
    }

    // ReSharper disable twice StaticMemberInGenericType
    private static readonly bool        isEntity;
    private static readonly MethodInfo  GetRelationKey = MakeGetRelationKey(out isEntity);
    
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2090", Justification = "Only for debugging")]
    private static MethodInfo MakeGetRelationKey(out bool isEntity)
    {
        var methodInfo  = typeof(TRelation).GetMethod("GetRelationKey")!;
        isEntity        = methodInfo.ReturnType == typeof(Entity);
        return methodInfo;
    }
    
    internal int GetPosition(int index) {
        return positions != null ? positions[index] : position;
    }
    
    /// <summary>
    /// Returns a string containing the relation keys.<br/>E.g <c>"{ 1, 3, 7 }"</c>
    /// </summary>
    public string Debug()
    {
        if (Length == 0) return "{ }";
        var sb          = new StringBuilder();
        sb.Append("{ ");
        foreach (var component in this) {
            if (sb.Length > 2) sb.Append(", ");
            var key = GetRelationKey.Invoke(component, null)!;
            if (isEntity) {
                sb.Append(((Entity)key).Id);
            } else {
                sb.Append(key);
            }
        }
        sb.Append(" }");
        return sb.ToString();
    }
    
    
    /// <summary>
    /// Return the relation at the given <paramref name="index"/>.<br/>
    /// Executes in O(1).
    /// </summary>
    public ref TRelation this[int index] {
        get {
            if (index >= 0 && index < Length) {
                return ref components[positions != null ? positions[index] : position];
            }
            throw IndexOutOfRangeException(index);
        }
    }
    
    private IndexOutOfRangeException IndexOutOfRangeException(int index) {
        return new IndexOutOfRangeException($"index: {index} Length: {Length}");
    }

    // --- new
    public RelationsEnumerator<TRelation>            GetEnumerator() => new RelationsEnumerator<TRelation>(this);
}


public struct RelationsEnumerator<TRelation>
    where TRelation : struct
{
    private  readonly   int[]           positions;
    private  readonly   int             position;
    private  readonly   TRelation[]     components;
    private  readonly   int             start;
    private  readonly   int             last;
    private             int             index;
    
    
    internal RelationsEnumerator(in Relations<TRelation> relations) {
        positions   = relations.positions;
        position    = relations.position;
        components  = relations.components;
        start       = relations.start - 1;
        last        = start + relations.Length;
        index       = start;
    }
    
    // --- IEnumerator<>
    public readonly ref TRelation Current   => ref components[positions != null ? positions[index] : position];
    
    // --- IEnumerator
    public bool MoveNext() {
        if (index < last) {
            index++;
            return true;
        }
        return false;
    }

    public void Reset() {
        index = start;
    }
    
    // --- IDisposable
    public void Dispose() { }
}

internal class RelationsDebugView<TRelation> 
    where TRelation : struct
{
    [Browse(RootHidden)]
    public              TRelation[]        Relations => GetRelations();
    
    [Browse(Never)]
    private readonly    Relations<TRelation> relations;
        
    internal RelationsDebugView(Relations<TRelation> relations)
    {
        this.relations = relations;
    }
    
    private TRelation[] GetRelations()
    {
        var array = new TRelation[relations.Length];
        int n = 0; 
        foreach (var relation in relations) {
            array[n++] = relation;
        }
        return array;
    }
}