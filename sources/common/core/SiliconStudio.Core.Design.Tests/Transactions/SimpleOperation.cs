// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using SiliconStudio.Core.Transactions;

namespace SiliconStudio.Core.Design.Tests.Transactions
{
    internal class SimpleOperation : Operation
    {
        public Guid Guid { get; } = Guid.NewGuid();

        public bool IsDone { get; private set; } = true;

        public int RollbackCount { get; private set; }

        public int RollforwardCount { get; private set; }

        protected override void Rollback()
        {
            IsDone = false;
            ++RollbackCount;
        }

        protected override void Rollforward()
        {
            IsDone = true;
            ++RollforwardCount;
        }
    }
}
