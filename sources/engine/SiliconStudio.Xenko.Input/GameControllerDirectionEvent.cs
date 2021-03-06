// Copyright (c) 2016-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

namespace SiliconStudio.Xenko.Input
{
    /// <summary>
    /// An event to describe a change in game controller direction state
    /// </summary>
    public class GameControllerDirectionEvent : InputEvent
    {
        /// <summary>
        /// The index of the direction controller
        /// </summary>
        public int Index;

        /// <summary>
        /// The new direction
        /// </summary>
        public Direction Direction;

        /// <summary>
        /// The gamepad that sent this event
        /// </summary>
        public IGameControllerDevice GameController => (IGameControllerDevice)Device;

        public override string ToString()
        {
            return $"{nameof(Index)}: {Index} ({GameController.DirectionInfos[Index].Name}), {nameof(Direction)}: {Direction} ({GetDirectionFriendlyName()}), {nameof(GameController)}: {GameController.Name}";
        }

        private string GetDirectionFriendlyName()
        {
            if (Direction.IsNeutral)
                return "Neutral";
            switch (Direction.GetTicks(8))
            {
                case 0:
                    return "Up";
                case 1:
                    return "RightUp";
                case 2:
                    return "Right";
                case 3:
                    return "RightDown";
                case 4:
                    return "Down";
                case 5:
                    return "LeftDown";
                case 6:
                    return "Left";
                case 7:
                    return "LeftUp";
                default:
                    return "Out of range";
            }
        }
    }
}