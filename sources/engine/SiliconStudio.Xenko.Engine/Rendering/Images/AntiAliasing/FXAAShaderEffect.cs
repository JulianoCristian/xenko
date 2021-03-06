﻿// <auto-generated>
// Do not edit this file yourself!
//
// This code was generated by Xenko Shader Mixin Code Generator.
// To generate it yourself, please install SiliconStudio.Xenko.VisualStudio.Package .vsix
// and re-save the associated .xkfx.
// </auto-generated>

using System;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Shaders;
using SiliconStudio.Core.Mathematics;
using Buffer = SiliconStudio.Xenko.Graphics.Buffer;

namespace SiliconStudio.Xenko.Rendering.Images
{
    internal static partial class ShaderMixins
    {
        internal partial class FXAAShaderEffect  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSource mixin, ShaderMixinContext context)
            {
                mixin.AddMacro("FXAA_GREEN_AS_LUMA", context.GetParam(FXAAEffect.GreenAsLumaKey));
                mixin.AddMacro("FXAA_QUALITY__PRESET", context.GetParam(FXAAEffect.QualityKey));
                context.Mixin(mixin, "FXAAShader");
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("FXAAShaderEffect", new FXAAShaderEffect());
            }
        }
    }
}
