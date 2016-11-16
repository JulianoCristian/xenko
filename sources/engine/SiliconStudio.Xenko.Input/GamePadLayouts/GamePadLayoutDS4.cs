﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

namespace SiliconStudio.Xenko.Input
{
    /// <summary>
    /// A gamepad layout for a DualShock4 controller
    /// </summary>
    public class GamePadLayoutDS4 : GamePadLayout
    {
        private static Guid commonProductId = new Guid(0x05c4054c, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        
        public GamePadLayoutDS4()
        {
            AddButtonMapping(9, GamePadButton.Start);
            AddButtonMapping(8, GamePadButton.Back);
            AddButtonMapping(10, GamePadButton.LeftThumb);
            AddButtonMapping(11, GamePadButton.RightThumb);
            AddButtonMapping(4, GamePadButton.LeftShoulder);
            AddButtonMapping(5, GamePadButton.RightShoulder);
            AddButtonMapping(1, GamePadButton.A);
            AddButtonMapping(2, GamePadButton.B);
            AddButtonMapping(0, GamePadButton.X);
            AddButtonMapping(3, GamePadButton.Y);
            AddAxisMapping(3, GamePadAxis.LeftThumbX);
            AddAxisMapping(2, GamePadAxis.LeftThumbY, true);
            AddAxisMapping(1, GamePadAxis.RightThumbX);
            AddAxisMapping(0, GamePadAxis.RightThumbY, true);
            AddAxisMapping(5, GamePadAxis.LeftTrigger);
            AddAxisMapping(4, GamePadAxis.RightTrigger);
        }

        public override void InitializeDevice(IGamePadDevice device)
        {
            // Make triggers not bi-directional
            device.AxisInfos[4].IsBiDirectional = false;
            device.AxisInfos[5].IsBiDirectional = false;

            base.InitializeDevice(device);
        }

        public override bool MatchDevice(IInputSource source, string deviceName, Guid productId)
        {
            return CompareProductId(productId, commonProductId, 4);
        }
    }
}