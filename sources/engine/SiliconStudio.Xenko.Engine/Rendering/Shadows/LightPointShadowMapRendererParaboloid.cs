﻿// Copyright (c) 2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Linq;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Lights;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering.Shadows
{
    /// <summary>
    /// Renders omnidirectional shadow maps using paraboloid shadow maps
    /// </summary>
    public class LightPointShadowMapRendererParaboloid : LightShadowMapRendererBase
    {
        public readonly RenderStage ShadowMapRenderStageParaboloid;

        private PoolListStruct<ShadowMapRenderView> shadowRenderViews;
        private PoolListStruct<ShaderData> shaderDataPool;
        private PoolListStruct<ShadowMapTexture> shadowMapTextures;

        public LightPointShadowMapRendererParaboloid(ShadowMapRenderer parent) : base(parent)
        {
            ShadowMapRenderStageParaboloid = ShadowMapRenderer.RenderSystem.GetRenderStage("ShadowMapCasterParaboloid");

            shaderDataPool = new PoolListStruct<ShaderData>(4, () => new ShaderData());
            shadowRenderViews = new PoolListStruct<ShadowMapRenderView>(16, () => new ShadowMapRenderView { RenderStages = { ShadowMapRenderStageParaboloid } });
            shadowMapTextures = new PoolListStruct<ShadowMapTexture>(16, () => new ShadowMapTexture());
        }

        public override void Reset()
        {
            foreach(var view in shadowRenderViews)
                ShadowMapRenderer.RenderSystem.Views.Remove(view);

            shaderDataPool.Clear();
            shadowRenderViews.Clear();
            shadowMapTextures.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new ShaderGroupData(shadowType);
        }

        public override bool CanRenderLight(IDirectLight light)
        {
            var pl = light as LightPoint;
            if (pl != null)
            {
                var type = ((LightPointShadowMap)pl.Shadow).Type;
                return type == LightPointShadowMapType.DualParaboloid;
            }
            return false;
        }

        public override LightShadowMapTexture CreateTexture(LightComponent lightComponent, IDirectLight light, int shadowMapSize)
        {
            var lightShadowMap = shadowMapTextures.Add();
            lightShadowMap.Initialize(lightComponent, light, light.Shadow, shadowMapSize, this);
            
            lightShadowMap.CascadeCount = 2; // 2 faces
            return lightShadowMap;
        }

        public override void CreateRenderViews(LightShadowMapTexture lightShadowMap, VisibilityGroup visibilityGroup)
        {
            for (int i = 0; i < 2; i++)
            {
                // Allocate shadow render view
                var shadowRenderView = shadowRenderViews.Add();
                shadowRenderView.RenderView = ShadowMapRenderer.CurrentView;
                shadowRenderView.ShadowMapTexture = lightShadowMap;
                shadowRenderView.Rectangle = lightShadowMap.GetRectangle(i);
                shadowRenderView.NearClipPlane = 0.0f;
                shadowRenderView.FarClipPlane = GetShadowMapFarPlane(lightShadowMap);

                // Compute view parameters
                // Note: we only need view here since we are doing paraboloid projection in the vertex shader
                GetViewParameters(lightShadowMap, i, out shadowRenderView.View, true);

                Matrix virtualProjectionMatrix = shadowRenderView.View;
                virtualProjectionMatrix *= Matrix.Scaling(1.0f/shadowRenderView.FarClipPlane);

                shadowRenderView.ViewProjection = virtualProjectionMatrix;

                shadowRenderView.VisiblityIgnoreDepthPlanes = false;

                // Add the render view for the current frame
                ShadowMapRenderer.RenderSystem.Views.Add(shadowRenderView);

                // Collect objects in shadow views
                visibilityGroup.Collect(shadowRenderView);
            }
        }

        public override void ApplyViewParameters(RenderDrawContext context, ParameterCollection parameters, LightShadowMapTexture shadowMapTexture)
        {
            parameters.Set(ShadowMapCasterParaboloidProjectionKeys.DepthParameters, GetShadowMapDepthParameters(shadowMapTexture));
        }

        public override void Collect(RenderContext context, LightShadowMapTexture lightShadowMap)
        {
            var visibilityGroup = context.Tags.Get(SceneInstance.CurrentVisibilityGroup);
            CalculateViewDirection(lightShadowMap);


            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = lightShadowMap.Light.Shadow.BiasParameters.DepthBias;

            Vector2 atlasSize = new Vector2(lightShadowMap.Atlas.Width, lightShadowMap.Atlas.Height);

            Rectangle frontRectangle = lightShadowMap.GetRectangle(0);

            // Coordinates have 1 border pixel so that the shadow receivers don't accidentally sample outside of the texture area
            shaderData.FaceSize = new Vector2(frontRectangle.Width-2, frontRectangle.Height-2) / atlasSize;
            shaderData.Offset = new Vector2(frontRectangle.Left+1, frontRectangle.Top+1) / atlasSize;

            Rectangle backRectangle = lightShadowMap.GetRectangle(1);
            shaderData.BackfaceOffset = new Vector2(backRectangle.Left+1, backRectangle.Top+1) / atlasSize - shaderData.Offset;
            
            shaderData.DepthParameters = GetShadowMapDepthParameters(lightShadowMap);
            
            GetViewParameters(lightShadowMap, 0, out shaderData.View, false);
        }

        /// <summary>
        /// Calculates the direction of the split between the shadow maps
        /// </summary>
        private void CalculateViewDirection(LightShadowMapTexture shadowMapTexture)
        {
            var pointShadowMapTexture = shadowMapTexture as ShadowMapTexture;
            Matrix.Orthonormalize(ref shadowMapTexture.LightComponent.Entity.Transform.WorldMatrix, out pointShadowMapTexture.ForwardMatrix);
            pointShadowMapTexture.ForwardMatrix.Invert();
        }

        /// <returns>
        /// x = Near; y = 1/(Far-Near)
        /// </returns>
        private Vector2 GetShadowMapDepthParameters(LightShadowMapTexture shadowMapTexture)
        {
            var lightPoint = shadowMapTexture.Light as LightPoint;
            Vector2 clippingPlanes = GetLightClippingPlanes(lightPoint);
            return new Vector2(clippingPlanes.X, 1.0f / (clippingPlanes.Y - clippingPlanes.X));
        }

        private Vector2 GetLightClippingPlanes(LightPoint pointLight)
        {
            return new Vector2(0.0f, pointLight.Radius + 2.0f);
        }

        private float GetShadowMapFarPlane(LightShadowMapTexture shadowMapTexture)
        {
            return GetLightClippingPlanes(shadowMapTexture.Light as LightPoint).Y;
        }

        private void GetViewParameters(LightShadowMapTexture shadowMapTexture, int index, out Matrix view, bool forCasting)
        {
            var pointShadowMapTexture = shadowMapTexture as ShadowMapTexture;
            Matrix flippingMatrix = Matrix.Identity;

            // Flip Y for rendering shadow maps
            if (forCasting)
            {
                // Render upside down, so reading doesn't need any modification
                flippingMatrix.Up = -flippingMatrix.Up;
            }

            // Apply light position
            view = Matrix.Translation(-shadowMapTexture.LightComponent.Position);

            // Apply mapping plane rotatation
            view *= pointShadowMapTexture.ForwardMatrix;

            if (index == 0)
            {
                // Camera (Front)
                // no rotation
            }
            else
            {
                // Camera (Back)
                flippingMatrix.Forward = -flippingMatrix.Forward;
            }
            view *= flippingMatrix;
        }

        private class ShadowMapTexture : LightShadowMapTexture
        {
            public Matrix ForwardMatrix;
        }

        private class ShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            /// <summary>
            /// Normalized offset of the front face of the shadow map in normalized coordinates
            /// </summary>
            public Vector2 Offset;

            /// <summary>
            /// Offset from fromnt to back face in normalized texture coordinates in the atlas
            /// </summary>
            public Vector2 BackfaceOffset;
            
            /// <summary>
            /// Size of a single face of the shadow map
            /// </summary>
            public Vector2 FaceSize;

            /// <summary>
            /// Matrix that converts from world space to the front face space of the light's shadow map
            /// </summary>
            public Matrix View;

            /// <summary>
            /// Radius of the point light, used to determine the range of the depth buffer
            /// </summary>
            public Vector2 DepthParameters;
            
            public float DepthBias;
        }

        private class ShaderGroupData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverPointParaboloid";
            
            private Texture shadowMapTexture;
            private Vector2 shadowMapTextureSize;
            private Vector2 shadowMapTextureTexelSize;

            private Matrix[] viewMatrices;
            private Vector2[] offsets;
            private Vector2[] backfaceOffsets;
            private Vector2[] faceSize;
            private Vector2[] depthParameters;
            private float[] depthBiases;

            private ValueParameterKey<float> depthBiasesKey;
            private ValueParameterKey<Matrix> viewKey;
            private ValueParameterKey<Vector2> offsetsKey;
            private ValueParameterKey<Vector2> backfaceOffsetsKey;
            private ValueParameterKey<Vector2> faceSizeKey;
            private ValueParameterKey<Vector2> depthParametersKey;

            private ObjectParameterKey<Texture> shadowMapTextureKey;
            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;
            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            public ShaderGroupData(LightShadowType shadowType) : base(shadowType)
            {
            }

            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                return new ShaderClassSource(ShaderName, lightCurrentCount);
            }
            
            public override void UpdateLayout(string compositionName)
            {
                shadowMapTextureKey = ShadowMapKeys.Texture.ComposeWith(compositionName);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionName);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionName);
                offsetsKey = ShadowMapReceiverPointParaboloidKeys.FaceOffset.ComposeWith(compositionName);
                backfaceOffsetsKey = ShadowMapReceiverPointParaboloidKeys.BackfaceOffset.ComposeWith(compositionName);
                faceSizeKey = ShadowMapReceiverPointParaboloidKeys.FaceSize.ComposeWith(compositionName);
                depthParametersKey = ShadowMapReceiverPointParaboloidKeys.DepthParameters.ComposeWith(compositionName);
                viewKey = ShadowMapReceiverPointParaboloidKeys.View.ComposeWith(compositionName);
                depthBiasesKey = ShadowMapReceiverPointParaboloidKeys.DepthBiases.ComposeWith(compositionName);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref offsets, lightCurrentCount);
                Array.Resize(ref backfaceOffsets, lightCurrentCount);
                Array.Resize(ref faceSize, lightCurrentCount);
                Array.Resize(ref depthParameters, lightCurrentCount);
                Array.Resize(ref viewMatrices, lightCurrentCount);
                Array.Resize(ref depthBiases, lightCurrentCount);
            }

            public override void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBox2 = (BoundingBox)boundingBox;
                bool shadowMapCreated = false;
                int lightIndex = 0;

                for (int i = 0; i < currentLights.Count; ++i)
                {
                    var lightEntry = currentLights[i];
                    if (lightEntry.Light.BoundingBox.Intersects(ref boundingBox2))
                    {
                        var shaderData = (ShaderData)lightEntry.ShadowMapTexture.ShaderData;
                        offsets[lightIndex] = shaderData.Offset;
                        backfaceOffsets[lightIndex] = shaderData.BackfaceOffset;
                        faceSize[lightIndex] = shaderData.FaceSize;
                        depthParameters[lightIndex] = shaderData.DepthParameters;
                        depthBiases[lightIndex] = shaderData.DepthBias;
                        viewMatrices[lightIndex] = shaderData.View;
                        lightIndex++;

                        // TODO: should be setup just once at creation time
                        if (!shadowMapCreated)
                        {
                            shadowMapTexture = shaderData.Texture;
                            if (shadowMapTexture != null)
                            {
                                shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                                shadowMapTextureTexelSize = 1.0f/shadowMapTextureSize;
                            }
                            shadowMapCreated = true;
                        }
                    }
                }

                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);

                parameters.Set(viewKey, viewMatrices);
                parameters.Set(offsetsKey, offsets);
                parameters.Set(backfaceOffsetsKey, backfaceOffsets);
                parameters.Set(faceSizeKey, faceSize);
                parameters.Set(depthParametersKey, depthParameters);

                parameters.Set(depthBiasesKey, depthBiases);
            }
        }
    }
}