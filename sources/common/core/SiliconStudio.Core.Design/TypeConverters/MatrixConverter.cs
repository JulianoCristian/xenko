﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under MIT License. See LICENSE.md for details.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// -----------------------------------------------------------------------------
// Original code from SlimMath project. http://code.google.com/p/slimmath/
// Greetings to SlimDX Group. Original code published with the following license:
// -----------------------------------------------------------------------------
/*
* Copyright (c) 2007-2011 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Core.TypeConverters
{
    /// <summary>
    /// Defines a type converter for <see cref="Matrix"/>.
    /// </summary>
    public class MatrixConverter : BaseConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatrixConverter"/> class.
        /// </summary>
        public MatrixConverter()
        {
            Type type = typeof(Matrix);
            Properties = new PropertyDescriptorCollection(new PropertyDescriptor[] 
            { 
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M11))), 
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M12))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M13))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M14))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M21))), 
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M22))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M23))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M24))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M31))), 
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M32))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M33))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M34))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M41))), 
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M42))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M43))),
                new FieldPropertyDescriptor(type.GetField(nameof(Matrix.M44))),
            });
        }
        
        /// <inheritdoc/>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null) throw new ArgumentNullException(nameof(destinationType));

            if (value is Matrix)
            {
                var matrix = (Matrix)value;

                if (destinationType == typeof(string))
                    return matrix.ToString();

                if (destinationType == typeof(InstanceDescriptor))
                {
                    var constructor = typeof(Matrix).GetConstructor(MathUtil.Array(typeof(float), 16));
                    if (constructor != null)
                        return new InstanceDescriptor(constructor, matrix.ToArray());
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <inheritdoc/>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var str = value as string;
            if (string.IsNullOrEmpty(str))
                return null;

            str = str.Replace("[", "").Replace("]", "");
            return ConvertFromString<Matrix, float>(context, culture, str);
        }

        /// <inheritdoc/>
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null) throw new ArgumentNullException(nameof(propertyValues));
            var matrix = new Matrix
            {
                M11 = (float)propertyValues[nameof(Matrix.M11)],
                M12 = (float)propertyValues[nameof(Matrix.M12)],
                M13 = (float)propertyValues[nameof(Matrix.M13)],
                M14 = (float)propertyValues[nameof(Matrix.M14)],
                M21 = (float)propertyValues[nameof(Matrix.M21)],
                M22 = (float)propertyValues[nameof(Matrix.M22)],
                M23 = (float)propertyValues[nameof(Matrix.M23)],
                M24 = (float)propertyValues[nameof(Matrix.M24)],
                M31 = (float)propertyValues[nameof(Matrix.M31)],
                M32 = (float)propertyValues[nameof(Matrix.M32)],
                M33 = (float)propertyValues[nameof(Matrix.M33)],
                M34 = (float)propertyValues[nameof(Matrix.M34)],
                M41 = (float)propertyValues[nameof(Matrix.M41)],
                M42 = (float)propertyValues[nameof(Matrix.M42)],
                M43 = (float)propertyValues[nameof(Matrix.M43)],
                M44 = (float)propertyValues[nameof(Matrix.M44)]
            };
            return matrix;
        }
    }
}
