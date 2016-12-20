// Copyright (c) 2014-2016 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering.Lights;
using SiliconStudio.Xenko.Shaders;

namespace SiliconStudio.Xenko.Rendering.Shadows
{
    /// <summary>
    /// Renders a shadow map from a directional light.
    /// </summary>
    public class LightSpotShadowMapRenderer : LightShadowMapRendererBase
    {
        /// <summary>
        /// The various UP vectors to try.
        /// </summary>
        private static readonly Vector3[] VectorUps = { Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX };

        private PoolListStruct<LightSpotShadowMapShaderData> shaderDataPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpotShadowMapRenderer"/> class.
        /// </summary>
        public LightSpotShadowMapRenderer(ShadowMapRenderer parent) : base(parent)
        {
            shaderDataPool = new PoolListStruct<LightSpotShadowMapShaderData>(8, CreateLightSpotShadowMapShaderData);
        }
        
        public override void Reset()
        {
            shaderDataPool.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(LightShadowType shadowType)
        {
            return new LightSpotShadowMapGroupShaderData(shadowType);
        }
        
        public override bool CanRenderLight(IDirectLight light)
        {
            return light is LightSpot;
        }

        public override void Collect(RenderContext context, LightShadowMapTexture lightShadowMap)
        {
            // TODO: Min and Max distance can be auto-computed from readback from Z buffer
            var shadow = (LightStandardShadowMap)lightShadowMap.Shadow;

            // Computes the cascade splits
            var lightComponent = lightShadowMap.LightComponent;
            var spotLight = (LightSpot)lightComponent.Type;
            var position = lightComponent.Position;
            var direction = lightComponent.Direction;
            var target = position + spotLight.Range * direction;
            var orthoSize = spotLight.LightRadiusAtTarget;

            // Fake value
            // It will be setup by next loop
            Vector3 side = Vector3.UnitX;
            Vector3 upDirection = Vector3.UnitX;

            // Select best Up vector
            // TODO: User preference?
            foreach (var vectorUp in VectorUps)
            {
                if (Vector3.Dot(direction, vectorUp) < (1.0 - 0.0001))
                {
                    side = Vector3.Normalize(Vector3.Cross(vectorUp, direction));
                    upDirection = Vector3.Normalize(Vector3.Cross(direction, side));
                    break;
                }
            }

            // Get new shader data from pool
            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = shadow.BiasParameters.DepthBias;
            shaderData.OffsetScale = shadow.BiasParameters.NormalOffsetScale;

            // Update the shadow camera
            var viewMatrix = Matrix.LookAtLH(position, target, upDirection); // View;;
            // TODO: Calculation of near and far is hardcoded/approximated. We should find a better way to calculate it.
            var projectionMatrix = Matrix.PerspectiveFovLH(spotLight.AngleOuterInRadians, 1.0f, 0.01f, spotLight.Range * 2.0f); // Perspective Projection for spotlights
            Matrix viewProjectionMatrix;
            Matrix.Multiply(ref viewMatrix, ref projectionMatrix, out viewProjectionMatrix);

            var shadowMapRectangle = lightShadowMap.GetRectangle(0);

            var cascadeTextureCoords = new Vector4((float)shadowMapRectangle.Left / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Top / lightShadowMap.Atlas.Height,
                (float)shadowMapRectangle.Right / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Bottom / lightShadowMap.Atlas.Height);

            //// Add border (avoid using edges due to bilinear filtering and blur)
            //var borderSizeU = VsmBlurSize / lightShadowMap.Atlas.Width;
            //var borderSizeV = VsmBlurSize / lightShadowMap.Atlas.Height;
            //cascadeTextureCoords.X += borderSizeU;
            //cascadeTextureCoords.Y += borderSizeV;
            //cascadeTextureCoords.Z -= borderSizeU;
            //cascadeTextureCoords.W -= borderSizeV;

            float leftX = (float)lightShadowMap.Size / lightShadowMap.Atlas.Width * 0.5f;
            float leftY = (float)lightShadowMap.Size / lightShadowMap.Atlas.Height * 0.5f;
            float centerX = 0.5f * (cascadeTextureCoords.X + cascadeTextureCoords.Z);
            float centerY = 0.5f * (cascadeTextureCoords.Y + cascadeTextureCoords.W);

            // Compute receiver view proj matrix
            Matrix adjustmentMatrix = Matrix.Scaling(leftX, -leftY, 1.0f) * Matrix.Translation(centerX, centerY, 0.0f);
            // Calculate View Proj matrix from World space to Cascade space
            Matrix.Multiply(ref viewProjectionMatrix, ref adjustmentMatrix, out shaderData.WorldToShadowCascadeUV);

            shaderData.ViewMatrix = viewMatrix;
            shaderData.ProjectionMatrix = projectionMatrix;
        }

        public override void CreateRenderViews(LightShadowMapTexture lightShadowMap, VisibilityGroup visibilityGroup)
        {  
            // Allocate shadow render view
            var shadowRenderView = ShadowMapRenderer.ShadowRenderViews.Add();
            shadowRenderView.RenderView = ShadowMapRenderer.CurrentView;
            shadowRenderView.ShadowMapTexture = lightShadowMap;
            shadowRenderView.Rectangle = lightShadowMap.GetRectangle(0);

            // Compute view parameters
            var shaderData = (LightSpotShadowMapShaderData)lightShadowMap.ShaderData;
            shadowRenderView.View = shaderData.ViewMatrix;
            shadowRenderView.Projection = shaderData.ProjectionMatrix;

            Matrix.Multiply(ref shadowRenderView.View, ref shadowRenderView.Projection, out shadowRenderView.ViewProjection);

            // Add the render view for the current frame
            ShadowMapRenderer.RenderSystem.Views.Add(shadowRenderView);

            // Collect objects in shadow views
            visibilityGroup.Collect(shadowRenderView);
        }

        private class LightSpotShadowMapShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            public float DepthBias;

            public float OffsetScale;

            public Matrix WorldToShadowCascadeUV;

            public Matrix ViewMatrix;

            public Matrix ProjectionMatrix;
        }

        private class LightSpotShadowMapGroupShaderData : LightShadowMapShaderGroupDataBase
        {
            private const string ShaderName = "ShadowMapReceiverSpot";

            private Matrix[] worldToShadowCascadeUV;

            private float[] depthBiases;

            private float[] offsetScales;

            private Texture shadowMapTexture;

            private Vector2 shadowMapTextureSize;

            private Vector2 shadowMapTextureTexelSize;

            private ObjectParameterKey<Texture> shadowMapTextureKey;

            private ValueParameterKey<Matrix> worldToShadowCascadeUVsKey;

            private ValueParameterKey<float> depthBiasesKey;

            private ValueParameterKey<float> offsetScalesKey;
            
            private ValueParameterKey<Vector2> shadowMapTextureSizeKey;

            private ValueParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            /// <summary>
            /// Initializes a new instance of the <see cref="LightSpotShadowMapGroupShaderData" /> class.
            /// </summary>
            /// <param name="shadowType">Type of the shadow.</param>
            /// <param name="lightCountMax">The light count maximum.</param>
            public LightSpotShadowMapGroupShaderData(LightShadowType shadowType) : base(shadowType)
            {
            }
            
            public override ShaderClassSource CreateShaderSource(int lightCurrentCount)
            {
                return new ShaderClassSource(ShaderName, lightCurrentCount, (ShadowType & LightShadowType.Debug) != 0);
            }

            public override void UpdateLayout(string compositionKey)
            {
                shadowMapTextureKey = ShadowMapKeys.Texture.ComposeWith(compositionKey);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionKey);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionKey);
                worldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.WorldToShadowCascadeUV.ComposeWith(compositionKey);
                depthBiasesKey = ShadowMapReceiverBaseKeys.DepthBiases.ComposeWith(compositionKey);
                offsetScalesKey = ShadowMapReceiverBaseKeys.OffsetScales.ComposeWith(compositionKey);
            }

            public override void UpdateLightCount(int lightLastCount, int lightCurrentCount)
            {
                base.UpdateLightCount(lightLastCount, lightCurrentCount);

                Array.Resize(ref worldToShadowCascadeUV, lightCurrentCount);
                Array.Resize(ref depthBiases, lightCurrentCount);
                Array.Resize(ref offsetScales, lightCurrentCount);
            }

            public override void ApplyDrawParameters(RenderDrawContext context, ParameterCollection parameters, FastListStruct<LightDynamicEntry> currentLights, ref BoundingBoxExt boundingBox)
            {
                var boundingBox2 = (BoundingBox)boundingBox;
                bool shadowMapCreated = false;
                int lightIndex = 0;

                for (int i = 0; i < currentLights.Count; ++i)
                {
                    var lightEntry = currentLights[i];

                    var light = lightEntry.Light;

                    if (light.BoundingBox.Intersects(ref boundingBox2))
                    {
                        var singleLightData = (LightSpotShadowMapShaderData)lightEntry.ShadowMapTexture.ShaderData;
                        worldToShadowCascadeUV[lightIndex] = singleLightData.WorldToShadowCascadeUV;

                        depthBiases[lightIndex] = singleLightData.DepthBias;
                        offsetScales[lightIndex] = singleLightData.OffsetScale;
                        lightIndex++;

                        if (!shadowMapCreated)
                        {
                            shadowMapTexture = singleLightData.Texture;
                            if (shadowMapTexture != null)
                            {
                                shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                                shadowMapTextureTexelSize = 1.0f / shadowMapTextureSize;
                            }
                            shadowMapCreated = true;
                        }
                    }
                }

                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);
                parameters.Set(worldToShadowCascadeUVsKey, worldToShadowCascadeUV);
                parameters.Set(depthBiasesKey, depthBiases);
                parameters.Set(offsetScalesKey, offsetScales);
            }
        }

        private static LightSpotShadowMapShaderData CreateLightSpotShadowMapShaderData()
        {
            return new LightSpotShadowMapShaderData();
        }
    }
}