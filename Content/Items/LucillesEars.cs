using Luminance.Common.Easings;
using Luminance.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SylvVanity.Content.Items
{
    [AutoloadEquip(EquipType.Head)]
    public class LucillesEars : ModItem
    {
        public override void SetStaticDefaults()
        {
            if (Main.netMode != NetmodeID.Server)
                ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = true;

            On_LegacyPlayerRenderer.DrawPlayerFull += RenderEarsWrapper;
        }

        private void RenderEarsWrapper(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer)
        {
            orig(self, camera, drawPlayer);

            Item headItem = drawPlayer.armor[0];

            if (drawPlayer.armor[10].type > ItemID.None)
                headItem = drawPlayer.armor[10];

            if (headItem.type == ModContent.ItemType<LucillesEars>())
            {
                string equipSlotName = headItem.ModItem.Name;
                int equipSlot = EquipLoader.GetEquipSlot(Mod, equipSlotName, EquipType.Head);

                if (!drawPlayer.dead && equipSlot == drawPlayer.head)
                {
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                    DrawEars(drawPlayer);
                    Main.spriteBatch.End();
                }
            }
        }

        private static void DrawEars(Player drawPlayer)
        {
            if (!drawPlayer.TryGetModPlayer(out SylvVanityPlayer vanityPlayer))
                return;

            Vector2 earDrawPosition = drawPlayer.TopLeft - Main.screenPosition + Vector2.UnitY * drawPlayer.gfxOffY;
            earDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);
            Vector2 headVect = new(drawPlayer.legFrame.Width * 0.5f, drawPlayer.legFrame.Height * 0.4f);
            earDrawPosition += new Vector2(drawPlayer.direction == 1 ? 1f : -11f, -4f);
            earDrawPosition += drawPlayer.headPosition + headVect;
            earDrawPosition.Y = MathF.Round(earDrawPosition.Y / 2f) * 2f;

            Texture2D leftEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarLeft").Value;
            Texture2D rightEar = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEarRight").Value;

            Rectangle hairFrame = drawPlayer.bodyFrame;
            hairFrame.Y -= 336;

            if (hairFrame.Y == 56 || hairFrame.Y == 112 || hairFrame.Y == 168 || hairFrame.Y == 448 || hairFrame.Y == 504 || hairFrame.Y == 560)
                earDrawPosition.Y -= 2f;

            Vector2 leftEarDrawPosition = earDrawPosition.Floor() + Vector2.UnitX * (drawPlayer.direction == 1 ? -4f : 14f);
            Vector2 rightEarDrawPosition = leftEarDrawPosition + Vector2.UnitX * drawPlayer.direction * 12f;

            // Calculate the base ear rotation.
            float earRotation = drawPlayer.headRotation + drawPlayer.direction * -0.16f;

            // Calculate the angular offset of ears based on twitch.
            float twitchAngleOffset = Utilities.Convert01To010(EasingCurves.Evaluate(EasingCurves.Cubic, EasingType.Out, vanityPlayer.EarTwitchAnimationCompletion)) * drawPlayer.direction * 0.26f;

            float leftEarRotation = earRotation - twitchAngleOffset;
            float rightEarRotation = earRotation + twitchAngleOffset * 0.04f;

            // Calculate the size of the ears.
            Vector2 earScale = Vector2.One * 0.46f;

            float squish = Utilities.Cos01(drawPlayer.Center.X * 0.017f) * Utilities.InverseLerp(7f, 25f, drawPlayer.velocity.Length()) * 0.25f;
            earScale.X *= 1f - squish * 0.6f;
            earScale.Y *= 1f + squish * 1.3f;

            // Draw ears.
            SpriteEffects earDirection = drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(leftEar, leftEarDrawPosition, null, Color.White, leftEarRotation, new Vector2(leftEar.Width * 0.5f, leftEar.Height), earScale, earDirection, 0);
            Main.spriteBatch.Draw(rightEar, rightEarDrawPosition, null, Color.White, rightEarRotation, new Vector2(rightEar.Width * 0.5f, rightEar.Height), earScale, earDirection, 0);
        }

        public override void SetDefaults()
        {
            Item.width = 66;
            Item.height = 78;
            Item.scale = 0.5f;
            Item.rare = ItemRarityID.Pink;
            Item.vanity = true;
        }
    }
}
