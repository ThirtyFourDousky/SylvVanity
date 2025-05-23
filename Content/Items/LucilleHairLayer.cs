using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SylvVanity.Content.Items
{
    public class LucilleHairLayer : PlayerDrawLayer
    {
        /// <summary>
        /// A custom vertex type with a position using Vector2 instead of Vector4, as Terraria is only a 2D game.
        /// </summary>
        /// <remarks>This represents a vertex that will be rendered by the GPU.</remarks>
        public readonly struct VertexPosition2DColorTexture(Vector2 position, Color color, Vector2 textureCoordinates, float widthCorrectionFactor) : IVertexType
        {
            /// <summary>
            /// The position of the vertex.
            /// </summary>
            public readonly Vector2 Position = position;

            /// <summary>
            /// The color of the vertex.
            /// </summary>
            public readonly Color Color = color;

            /// <summary>
            /// The texture-coordinate of the vertex.
            /// </summary>
            /// /// <remarks>
            /// The Z component isn't actually related to 3D, it holds the width of the vertex at the given point, since arbitrary data cannot be saved on a per-vertex basis and needs to be contained within some pre-defined format.
            /// </remarks>
            public readonly Vector3 TextureCoordinates = new(textureCoordinates, widthCorrectionFactor);

            /// <summary>
            /// The vertex declaration. This declares the layout and size of the data in the vertex shader.
            /// </summary>
            public VertexDeclaration VertexDeclaration => VertexDeclaration2D;

            public static readonly VertexDeclaration VertexDeclaration2D = new(
            [
                new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
            ]);
        }

        /// <summary>
        /// The render target that holds ear data.
        /// </summary>
        public static InstancedRequestableTarget? EarTarget
        {
            get;
            private set;
        }

        /// <summary>
        /// The render target that holds ribbon primitive data.
        /// </summary>
        public static InstancedRequestableTarget? RibbonTarget
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            EarTarget = new();
            RibbonTarget = new();
            Main.ContentThatNeedsRenderTargets.Add(EarTarget);
            Main.ContentThatNeedsRenderTargets.Add(RibbonTarget);
        }

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.shadow == 0f || !drawInfo.drawPlayer.dead;

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player drawPlayer = drawInfo.drawPlayer;
            Item headItem = drawPlayer.armor[0];

            if (drawPlayer.armor[10].type > ItemID.None)
                headItem = drawPlayer.armor[10];

            if (headItem.type == ModContent.ItemType<LucillesEars>())
            {
                string equipSlotName = headItem.ModItem.Name;
                int equipSlot = EquipLoader.GetEquipSlot(Mod, equipSlotName, EquipType.Head);

                if (!drawPlayer.dead && equipSlot == drawPlayer.head)
                {
                    int dyeShader = drawPlayer.dye?[0].dye ?? 0;

                    // It is imperative to use drawInfo.Position and not drawInfo.Player.Position, or else the layer will break on the player select & map (in the case of a head layer).
                    Vector2 headDrawPosition = drawInfo.Position - Main.screenPosition;
                    headDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);

                    // Floor the draw position to remove jitter.
                    headDrawPosition = headDrawPosition.Floor();
                    headDrawPosition += drawPlayer.headPosition + drawInfo.headVect;

                    // Draw ears.
                    EarTarget?.Request(400, 400, drawPlayer.whoAmI, () =>
                    {
                        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
                        DrawEarsToTarget(drawPlayer);
                        Main.spriteBatch.End();
                    });
                    if ((EarTarget?.TryGetTarget(drawPlayer.whoAmI, out RenderTarget2D? target) ?? false) && target is not null)
                    {
                        Color lightColor = drawInfo.colorHair;
                        lightColor = new(Vector4.Clamp(lightColor.ToVector4() / drawPlayer.hairColor.ToVector4(), Vector4.Zero, Vector4.One));

                        DrawData earDrawData = new(target, headDrawPosition, null, lightColor, 0f, target.Size() * 0.5f, 1f, 0, 0)
                        {
                            shader = dyeShader
                        };
                        drawInfo.DrawDataCache.Add(earDrawData);
                    }
                }
            }
        }

        private static void DrawEarsToTarget(Player drawPlayer)
        {
            if (!drawPlayer.TryGetModPlayer(out SylvVanityPlayer vanityPlayer))
                return;

            // Rotate the ears based on the player's rotation.
            Vector2 targetSize = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 earDrawPosition = targetSize * 0.5f + new Vector2(drawPlayer.direction == 1 ? 1f : -1f, drawPlayer.gravDir == 1 ? -4f : 16f);

            Texture2D leftEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarLeft").Value;
            Texture2D rightEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarRight").Value;
            Texture2D rightEarRibbon = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarRight").Value;

            Vector2 forward = Vector2.UnitX;
            Vector2 up = Vector2.UnitY;

            Rectangle hairFrame = drawPlayer.bodyFrame;
            hairFrame.Y -= 336;

            if (hairFrame.Y == 56 || hairFrame.Y == 112 || hairFrame.Y == 168 || hairFrame.Y == 448 || hairFrame.Y == 504 || hairFrame.Y == 560)
                earDrawPosition -= up * 2f;

            Vector2 leftEarDrawPosition = earDrawPosition.Floor() + forward * (drawPlayer.direction == 1 ? -4f : 4f);
            Vector2 rightEarDrawPosition = leftEarDrawPosition + forward * drawPlayer.direction * 12f;

            // Calculate the base ear rotation.
            float earRotation = drawPlayer.direction * -0.16f;

            // Calculate the angular offset of ears based on twitch.
            float easedTwitchAnimationCompletion = 1f - MathF.Pow(1f - vanityPlayer.EarTwitchAnimationCompletion, 3f);
            float twitchAngleOffset = MathF.Sin(MathHelper.Pi * easedTwitchAnimationCompletion) * drawPlayer.direction * 0.26f;

            float leftEarRotation = (earRotation - twitchAngleOffset) * drawPlayer.gravDir;
            float rightEarRotation = (earRotation + twitchAngleOffset * 0.04f) * drawPlayer.gravDir;

            // Calculate the size of the ears.
            Vector2 earScale = Vector2.One * 0.5f;

            float squishA = MathF.Cos(drawPlayer.Center.X * 0.017f) * 0.5f + 0.5f;
            float squishB = MathF.Cos(drawPlayer.Center.X * 0.011f) * 0.5f + 0.5f;
            float movementBounce = Utils.GetLerpValue(7f, 21f, drawPlayer.velocity.Length(), true);
            float squish = squishA * squishB * movementBounce * 0.25f;
            earScale.X *= 1f - squish * 0.6f;
            earScale.Y *= 1f + squish * 1.3f;

            // Draw ears.
            SpriteEffects earDirection = (drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally) | (drawPlayer.gravDir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically);

            ApplyPixelation(leftEar);
            Main.spriteBatch.Draw(leftEar, leftEarDrawPosition, null, Color.White, leftEarRotation, new Vector2(leftEar.Width * 0.5f, drawPlayer.gravDir == 1 ? leftEar.Height : 0f), earScale, earDirection, 0);

            ApplyPixelation(rightEar);
            Main.spriteBatch.Draw(rightEar, rightEarDrawPosition, null, Color.White, rightEarRotation, new Vector2(rightEar.Width * 0.5f, drawPlayer.gravDir == 1 ? rightEar.Height : 0f), earScale, earDirection, 0);

            // Draw feelers.
            Vector2 feelerCenter = rightEarDrawPosition + (drawPlayer.gravDir == 1 ? -(up * 4f) : (up * 4f));
            Vector2 velocityOffset = -vanityPlayer.VelocityMovingAverage * new Vector2(0.022f, 0.011f);
            Vector2 feelerEndOffset = Vector2.UnitY * MathF.Abs(vanityPlayer.VelocityMovingAverage.X) * -0.6f;

            Vector2 leftFeelerDirection = -Vector2.UnitX * drawPlayer.direction + velocityOffset;
            Vector2 rightFeelerDirection = Vector2.UnitX * drawPlayer.direction * 0.7f + velocityOffset;
            DrawRibbonFeeler(feelerCenter, leftFeelerDirection, feelerEndOffset, Color.White, drawPlayer.whoAmI, drawPlayer.gravDir != 1);
            DrawRibbonFeeler(feelerCenter, rightFeelerDirection, feelerEndOffset, Color.White, drawPlayer.whoAmI + 1000, drawPlayer.gravDir != 1);

            // Draw the ribbon again to ensure that it layers over the feelers.
            ApplyPixelation(rightEarRibbon);
            Main.spriteBatch.Draw(rightEarRibbon, rightEarDrawPosition, null, Color.White, rightEarRotation, new Vector2(rightEarRibbon.Width * 0.5f, drawPlayer.gravDir == 1 ? rightEarRibbon.Height : 0), earScale, earDirection, 0);
        }

        private static void DrawRibbonFeeler(Vector2 center, Vector2 direction, Vector2 endOffset, Color lightColor, int identifier, bool UpsideDown)
        {
            if (RibbonTarget is null)
                return;

            Vector2 ribbonTargetArea = new(200f);
            RibbonTarget.Request((int)ribbonTargetArea.X, (int)ribbonTargetArea.Y, identifier, () =>
            {
                Asset<Effect>? feelerShaderAsset = ShaderSystem.FeelerRibbonShader;
                if (feelerShaderAsset is null)
                    return;

                Effect feelerShader = feelerShaderAsset.Value;
                feelerShader.Parameters["feelerColorStart"]?.SetValue(0.61f);
                feelerShader.Parameters["colorSpacingFactor"]?.SetValue(1.9f);
                feelerShader.Parameters["pixelationFactor"]?.SetValue(40f);
                feelerShader.Parameters["outlineColor"]?.SetValue(new Color(109, 102, 112).ToVector4());
                feelerShader.Parameters["uWorldViewProjection"].SetValue(Matrix.CreateOrthographicOffCenter(0f, ribbonTargetArea.X, ribbonTargetArea.Y, 0f, -1f, 1f));
                feelerShader.CurrentTechnique.Passes[0].Apply();

                // Construct draw positions.
                Vector2 targetCenter = ribbonTargetArea * 0.5f;
                Vector2[] feelerDrawPositions = new Vector2[16];
                for (int i = 0; i < feelerDrawPositions.Length; i++)
                {
                    float completionRatio = i / (float)(feelerDrawPositions.Length - 1f);
                    float verticalOffsetInterpolant = Utils.GetLerpValue(0.1f, 0.5f, completionRatio, true);
                    Vector2 verticalOffset = Vector2.UnitY * MathF.Sin(MathHelper.Pi * completionRatio * 2f - Main.GlobalTimeWrappedHourly * 3.2f + direction.X * 2f) * verticalOffsetInterpolant * 2.6f;

                    feelerDrawPositions[i] = targetCenter + direction * i * 1.9f + verticalOffset + endOffset * completionRatio;
                }

                // Create a vertex trail from the draw positions.
                VertexPosition2DColorTexture[] vertices = new VertexPosition2DColorTexture[(feelerDrawPositions.Length - 1) * 2];
                for (int i = 0; i < feelerDrawPositions.Length - 1; i++)
                {
                    float completionRatio = i / (float)(feelerDrawPositions.Length - 2f);
                    Vector2 basePosition = feelerDrawPositions[i];
                    Vector2 perpendicular = (feelerDrawPositions[i + 1] - feelerDrawPositions[i]).SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);

                    float width = FeelerWidthFunction();
                    Color color = FeelerColorFunction();

                    Vector2 left = basePosition - perpendicular * width;
                    Vector2 right = basePosition + perpendicular * width;

                    Vector2 leftTextureCoord = new(completionRatio, 0.5f - width * 0.5f);
                    Vector2 rightTextureCoord = new(completionRatio, 0.5f + width * 0.5f);

                    vertices[i * 2] = new(left, color, leftTextureCoord, width);
                    vertices[i * 2 + 1] = new(right, color, rightTextureCoord, width);
                }

                // Render the vertex trail.
                Main.instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
            });

            if (!RibbonTarget.TryGetTarget(identifier, out RenderTarget2D? target) || target is null || ShaderSystem.PixelationShader is null)
                return;

            Effect pixelationShader = ShaderSystem.PixelationShader.Value;
            pixelationShader.Parameters["pixelationFactor"]?.SetValue(Vector2.One * 0.8f / target.Size());
            pixelationShader.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(target, center, null, lightColor, 0f, target.Size() * 0.5f, 1f, 0, 0f);
        }

        private static float FeelerWidthFunction() => 2.85f;

        private static Color FeelerColorFunction() => new(232, 229, 245);

        private static void ApplyPixelation(Texture2D texture)
        {
            if (ShaderSystem.PixelationShader is null)
                return;

            Effect pixelationShader = ShaderSystem.PixelationShader.Value;
            pixelationShader.Parameters["pixelationFactor"]?.SetValue(Vector2.One * 2f / texture.Size());
            pixelationShader.CurrentTechnique.Passes[0].Apply();
        }
    }
}
