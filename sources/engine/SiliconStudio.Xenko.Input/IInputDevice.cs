// Copyright (c) 2016 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;

namespace SiliconStudio.Xenko.Input
{
    public interface IInputDevice
    {
        /// <summary>
        /// The name of the device
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The unique identifier of this device
        /// </summary>
        Guid Id { get; }
        
        /// <summary>
        /// The device priority. Larger means higher priority when selecting the first device of some type
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// The input source the device belongs to.
        /// </summary>
        IInputSource Source { get; }

        /// <summary>
        /// Updates the input device, filling the list <see cref="inputEvents"/> with input events that were generated by this device this frame
        /// </summary>
        /// <remarks>Input devices are always updated after their respective input source</remarks>
        /// <param name="inputEvents">A list that gets filled with input events that were generated since the last frame</param>
        void Update(List<InputEvent> inputEvents);
    }
}