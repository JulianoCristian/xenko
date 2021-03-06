// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections;
using System.Collections.Generic;

namespace SiliconStudio.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A cast expression.
    /// </summary>
    public partial class CastExpression : Expression
    {
        #region Public Properties

        /// <summary>
        ///   Gets or sets from.
        /// </summary>
        /// <value>
        ///   From.
        /// </value>
        public Expression From { get; set; }

        /// <summary>
        ///   Gets or sets the target.
        /// </summary>
        /// <value>
        ///   The target.
        /// </value>
        public TypeBase Target { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override IEnumerable<Node> Childrens()
        {
            ChildrenList.Clear();
            ChildrenList.Add(Target);
            ChildrenList.Add(From);
            return ChildrenList;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("({0}){1}", Target, From);
        }

        #endregion
    }
}
