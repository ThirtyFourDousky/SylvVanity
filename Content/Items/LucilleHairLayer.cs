using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

                    // Draw hair.
                    Texture2D hair = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucilleHair").Value;
                    Rectangle hairFrame = drawPlayer.bodyFrame;
                    hairFrame.Y -= 336;
                    if (hairFrame.Y < 0)
                        hairFrame.Y = 0;

                    DrawData hairDrawData = new(hair, headDrawPosition, hairFrame, drawInfo.colorHair, drawPlayer.headRotation, drawInfo.headVect, 1f, drawInfo.playerEffect, 0)
                    {
                        shader = dyeShader
                    };

                    drawInfo.DrawDataCache.Add(hairDrawData);

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
            Vector2 earDrawPosition = targetSize * 0.5f + new Vector2(drawPlayer.direction == 1 ? 1f : -11f, -4f);

            Texture2D leftEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarLeft").Value;
            Texture2D rightEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarRight").Value;
            Texture2D rightEarRibbon = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarRight").Value;

            Vector2 forward = Vector2.UnitX;
            Vector2 up = Vector2.UnitY;

            Rectangle hairFrame = drawPlayer.bodyFrame;
            hairFrame.Y -= 336;

            if (hairFrame.Y == 56 || hairFrame.Y == 112 || hairFrame.Y == 168 || hairFrame.Y == 448 || hairFrame.Y == 504 || hairFrame.Y == 560)
                earDrawPosition -= up * 2f;

            Vector2 leftEarDrawPosition = earDrawPosition.Floor() + forward * (drawPlayer.direction == 1 ? -4f : 14f);
            Vector2 rightEarDrawPosition = leftEarDrawPosition + forward * drawPlayer.direction * 12f;

            // Calculate the base ear rotation.
            float earRotation = drawPlayer.direction * -0.16f;

            // Calculate the angular offset of ears based on twitch.
            float twitchAngleOffset = Utilities.Convert01To010(EasingCurves.Evaluate(EasingCurves.Cubic, EasingType.Out, vanityPlayer.EarTwitchAnimationCompletion)) * drawPlayer.direction * 0.26f;

            float leftEarRotation = earRotation - twitchAngleOffset;
            float rightEarRotation = earRotation + twitchAngleOffset * 0.04f;

            // Calculate the size of the ears.
            Vector2 earScale = Vector2.One * 0.5f;

            float squish = Utilities.Cos01(drawPlayer.Center.X * 0.017f) * Utilities.Cos01(drawPlayer.Center.X * 0.011f) * Utilities.InverseLerp(7f, 21f, drawPlayer.velocity.Length()) * 0.25f;
            earScale.X *= 1f - squish * 0.6f;
            earScale.Y *= 1f + squish * 1.3f;

            // Draw ears.
            SpriteEffects earDirection = drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            ApplyPixelation(leftEar);
            Main.spriteBatch.Draw(leftEar, leftEarDrawPosition, null, Color.White, leftEarRotation, new Vector2(leftEar.Width * 0.5f, leftEar.Height), earScale, earDirection, 0);

            ApplyPixelation(rightEar);
            Main.spriteBatch.Draw(rightEar, rightEarDrawPosition, null, Color.White, rightEarRotation, new Vector2(rightEar.Width * 0.5f, rightEar.Height), earScale, earDirection, 0);

            // Draw feelers.
            Vector2 feelerCenter = rightEarDrawPosition - up * 4f;
            Vector2 velocityOffset = -vanityPlayer.VelocityMovingAverage * new Vector2(0.022f, 0.011f);
            Vector2 feelerEndOffset = Vector2.UnitY * MathF.Abs(vanityPlayer.VelocityMovingAverage.X) * -0.6f;

            Vector2 leftFeelerDirection = -Vector2.UnitX * drawPlayer.direction + velocityOffset;
            Vector2 rightFeelerDirection = Vector2.UnitX * drawPlayer.direction * 0.7f + velocityOffset;
            DrawRibbonFeeler(feelerCenter, leftFeelerDirection, feelerEndOffset, Color.White, drawPlayer.whoAmI);
            DrawRibbonFeeler(feelerCenter, rightFeelerDirection, feelerEndOffset, Color.White, drawPlayer.whoAmI + 1000);

            // Draw the ribbon again to ensure that it layers over the feelers.
            ApplyPixelation(rightEarRibbon);
            Main.spriteBatch.Draw(rightEarRibbon, rightEarDrawPosition, null, Color.White, rightEarRotation, new Vector2(rightEarRibbon.Width * 0.5f, rightEarRibbon.Height), earScale, earDirection, 0);
        }

        private static void DrawRibbonFeeler(Vector2 center, Vector2 direction, Vector2 endOffset, Color lightColor, int identifier)
        {
            if (RibbonTarget is null)
                return;

            Vector2 ribbonTargetArea = new(200f);
            RibbonTarget.Request((int)ribbonTargetArea.X, (int)ribbonTargetArea.Y, identifier, () =>
            {
                ManagedShader feelerShader = ShaderManager.GetShader("SylvVanity.LucilleFeelerShader");
                feelerShader.TrySetParameter("feelerColorStart", 0.61f);
                feelerShader.TrySetParameter("colorSpacingFactor", 1.9f);
                feelerShader.TrySetParameter("pixelationFactor", 40f);
                feelerShader.TrySetParameter("outlineColor", new Color(109, 102, 112).ToVector4());
                feelerShader.Apply();

                PrimitiveSettings settings = new(FeelerWidthFunction, FeelerColorFunction, Shader: feelerShader, UseUnscaledMatrix: true,
                    ProjectionAreaWidth: Main.instance.GraphicsDevice.Viewport.Width, ProjectionAreaHeight: Main.instance.GraphicsDevice.Viewport.Height);

                Vector2 targetCenter = ribbonTargetArea * 0.5f + Main.screenPosition;
                Vector2[] feelerDrawPositions = new Vector2[16];
                for (int i = 0; i < feelerDrawPositions.Length; i++)
                {
                    float completionRatio = i / (float)(feelerDrawPositions.Length - 1f);
                    float verticalOffsetInterpolant = Utilities.InverseLerp(0.1f, 0.5f, completionRatio);
                    Vector2 verticalOffset = Vector2.UnitY * MathF.Sin(MathHelper.Pi * completionRatio * 2f - Main.GlobalTimeWrappedHourly * 3.2f + direction.X * 2f) * verticalOffsetInterpolant * 2.6f;

                    feelerDrawPositions[i] = targetCenter + direction * i * 1.9f + verticalOffset + endOffset * completionRatio;
                }

                PrimitiveRenderer.RenderTrail(feelerDrawPositions, settings, 40);
            });

            if (!RibbonTarget.TryGetTarget(identifier, out RenderTarget2D? target) || target is null)
                return;

            ManagedShader pixelationShader = ShaderManager.GetShader("Luminance.PixelationShader");
            pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 0.8f / target.Size());
            pixelationShader.Apply();
            Main.spriteBatch.Draw(target, center, null, lightColor, 0f, target.Size() * 0.5f, 1f, 0, 0f);
        }

        private static float FeelerWidthFunction(float completionRatio) => 2.8f;

        private static Color FeelerColorFunction(float completionRatio) => new(232, 229, 245);

        private static void ApplyPixelation(Texture2D texture)
        {
            ManagedShader pixelationShader = ShaderManager.GetShader("Luminance.PixelationShader");
            pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 2f / texture.Size());
            pixelationShader.Apply();
        }
    }
}
