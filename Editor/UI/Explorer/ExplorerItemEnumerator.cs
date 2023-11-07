﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;

namespace Friflo.Fliox.Editor.UI.Explorer;

internal class ExplorerItemEnumerator : IEnumerator<ExplorerItem>
{
    private             ChildEnumerator entityEnumerator;
    private readonly    ExplorerTree    tree;
    
    internal ExplorerItemEnumerator(ExplorerItem item) {
        entityEnumerator    = item.entity.ChildNodes.GetEnumerator();
        tree                = item.tree;
    }
    
    public  ExplorerItem Current    => tree.items[entityEnumerator.Current.Id];
    object  IEnumerator.Current     => Current;
    
    // --- IEnumerator
    public bool MoveNext()  => entityEnumerator.MoveNext();

    public void Reset()     => entityEnumerator.Reset();

    public void Dispose() { }
}