﻿using Friflo.Engine.ECS;

namespace Tests.ECS.Relations {

[ComponentKey("multi-attack")]
public struct AttackRelation : ILinkRelation
{
    public          int     speed;
    public          Entity  target;
    public          Entity  GetRelationKey()    => target;

    public override string  ToString()          => target.Id.ToString();
}

internal struct IntRelation : IRelation<int>
{
    public          int     value;
    public          int     GetRelationKey()    => value;

    public override string  ToString()          => value.ToString();
}

internal struct StringRelation : IRelation<string>
{
    public          string  value;
    public          string  GetRelationKey()    => value;

    public override string  ToString()          => value;
}

internal enum InventoryItemType {
    Axe     = 1,
    Gun     = 2,
    Sword   = 3,
    Shield  = 4,
}

/// <summary> <see cref="IRelation{TKey}"/> using an enum as relation key. </summary>
[ComponentKey("item")]
internal struct InventoryItem : IRelation<InventoryItemType>
{
    public          InventoryItemType   type;
    public          int                 amount;
    public          InventoryItemType   GetRelationKey()    => type;

    public override string              ToString()          => type.ToString();
}

/// Check generic relation
[GenericInstanceType("relation-string", typeof(string))] 
internal struct GenericRelation<T> : IRelation<int>
{
    public          int     key;
    public          T       value;
    public          int     GetRelationKey()    => key;
    
    public override string  ToString()          => key.ToString();
}

}