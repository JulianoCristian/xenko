﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;

namespace SiliconStudio.Xenko.Input.Mapping
{
    /// <summary>
    /// Represents a game controller axis reading
    /// </summary>
    [DataContract]
    public class GameControllerAxisGesture : InvertibleInputGesture, IAxisGesture, IInputEventListener<GameControllerAxisEvent>
    {
        /// <summary>
        /// The index of the axis to use
        /// </summary>
        public int AxisIndex = 0;
        
        private float currentState;

        public GameControllerAxisGesture()
        {
        }

        public GameControllerAxisGesture(int axisIndex)
        {
            AxisIndex = axisIndex;
        }

        [DataMemberIgnore]
        public float Axis => GetScaledOutput(currentState);

        [DataMemberIgnore]
        public bool IsRelative { get; } = true;

        public void ProcessEvent(GameControllerAxisEvent inputEvent)
        {
            if (inputEvent.GameController.Index == ActionMapping.ControllerIndex)
            {
                if (inputEvent.Index == AxisIndex)
                    currentState = inputEvent.Value;
            }
        }

        public override string ToString()
        {
            return $"{nameof(AxisIndex)}: {AxisIndex}, {nameof(Axis)}: {Axis}, {nameof(Inverted)}: {Inverted}, {nameof(IsRelative)}: {IsRelative}";
        }

        protected bool Equals(GameControllerAxisGesture other)
        {
            return AxisIndex == other.AxisIndex;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GameControllerAxisGesture)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return AxisIndex;
            }
        }
    }
}