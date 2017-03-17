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

namespace SiliconStudio.Xenko.Rendering.Compositing
{
    [DataContract]public partial class MSAAResolverParams : ShaderMixinParameters
    {
        public static readonly PermutationParameterKey<int> MSAASamples = ParameterKeys.NewPermutation<int>();
        public static readonly PermutationParameterKey<int> ResolveFilterType = ParameterKeys.NewPermutation<int>();
        public static readonly PermutationParameterKey<float> ResolveFilterDiameter = ParameterKeys.NewPermutation<float>();
    };
    internal static partial class ShaderMixins
    {
        internal partial class MSAAResolverEffect  : IShaderMixinBuilder
        {
            public void Generate(ShaderMixinSource mixin, ShaderMixinContext context)
            {
                context.Mixin(mixin, "MSAAResolverShader", context.GetParam(MSAAResolverParams.MSAASamples), context.GetParam(MSAAResolverParams.ResolveFilterType), context.GetParam(MSAAResolverParams.ResolveFilterDiameter));
            }

            [ModuleInitializer]
            internal static void __Initialize__()

            {
                ShaderMixinManager.Register("MSAAResolverEffect", new MSAAResolverEffect());
            }
        }
    }
}
