﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStoreBase
{
    /// <summary>
    /// Return the entity matching the given <paramref name="allTags"/>.<br/>
    /// <br/>
    /// Use Tags.Get&lt;>() to get filter tags.
    /// </summary>
    /// <exception cref="InvalidOperationException"> in case none or more than 1 matching entities found.</exception>
    [Obsolete("experimental")]
    public Entity FindEntityWithTags (in Tags allTags)
    {
        var query = internBase.findQuery ??= new ArchetypeQuery(this);
        query.Set(default, allTags);
        return FindSingleEntity(query);
    }
    
    /// <summary>
    /// Return the entity matching the given <paramref name="allTags"/>
    /// and the given <paramref name="requiredComponents"/>.<br/>
    /// <br/>
    /// Use Tags.Get&lt;>() to get filter tags.<br/>
    /// Use ComponentTypes.Get&lt;>() to get component types.<br/>
    /// </summary>
    /// <exception cref="InvalidOperationException"> in case none or more than 1 matching entities found.</exception>
    [Obsolete("experimental")]
    public Entity FindEntity (in Tags allTags, in ComponentTypes requiredComponents)
    {
        var query = internBase.findQuery ??= new ArchetypeQuery(this);
        query.Set(requiredComponents, allTags);
        return FindSingleEntity(query);
    }
    
    /// <summary>
    /// Return the entity matching the given <paramref name="allTags"/>.<br/>
    /// <br/>
    /// Use Tags.Get&lt;>() to get filter tags.
    /// </summary>
    /// <exception cref="InvalidOperationException"> in case none or more than 1 matching entities found.</exception>
    [Obsolete("experimental")]
    public Entity[] FindEntities (in Tags allTags, in ComponentTypes requiredComponents)
    {
        var query = internBase.findQuery ??= new ArchetypeQuery(this);
        query.Set(requiredComponents, allTags);
        return GetFindEntities(query);
    }
    
    /// <summary>
    /// Return the entity matching the given <paramref name="allTags"/>.<br/>
    /// <br/>
    /// Use Tags.Get&lt;>() to get filter tags.
    /// </summary>
    /// <exception cref="InvalidOperationException"> in case none or more than 1 matching entities found.</exception>
    [Obsolete("experimental")]
    public Entity[] FindEntitiesWithTags(in Tags allTags)
    {
        var query = internBase.findQuery ??= new ArchetypeQuery(this);
        query.Set(default, allTags);
        return GetFindEntities(query);
    }

    private Entity FindSingleEntity (ArchetypeQuery findQuery)
    {
        // --- check if query contains exact one entity 
        var queryEntityCount = findQuery.EntityCount;
        switch (queryEntityCount) {
            case 0:
                throw new InvalidOperationException("found no matching entity");
            case > 1:
                throw new InvalidOperationException($"found multiple matching entities. found: {queryEntityCount}");
        }
        
        // --- get the one entity
        var archetypes      = findQuery.GetArchetypes();
        Archetype archetype = null;
        for (int archIndex = 0; archIndex < archetypes.length; archIndex++) {
            archetype = archetypes.array[archIndex];
            if (archetype.entityCount == 0) {
                continue;
            }
            break;
        }
        var entityId = archetype!.entityIds[0];
        return new Entity((EntityStore)this, entityId);
    }
    
    private Entity[] GetFindEntities (ArchetypeQuery findQuery)
    {
        var entities        = new Entity[findQuery.EntityCount];
        var queryEntities   = new QueryEntities(findQuery);
        int n = 0;
        var store = (EntityStore)this;
        foreach (var chunkEntities in queryEntities) {
            foreach (var id in chunkEntities.Ids) {
                entities[n++] = new Entity(store, id);
            }
        }
        return entities;
    }
}
