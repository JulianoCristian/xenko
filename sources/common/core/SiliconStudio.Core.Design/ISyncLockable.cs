// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Threading;

namespace SiliconStudio.Core
{
    /// <summary>
    /// An interface representing a token of a <see cref="MicroThreadLock"/> that can be synchronously locked.
    /// </summary>
    public interface ISyncLockable
    {
        /// <summary>
        /// Takes the synchronous lock on the related <see cref="MicroThreadLock"/>. The lock will be bound to the calling thread, similarly to a <see cref="Monitor"/>,
        /// and will be released when the returned object is disposed. This must occur on the same thread.
        /// </summary>
        /// <returns>A disposable object that will release the lock when disposed.</returns>
        IDisposable Lock();
    }
}
