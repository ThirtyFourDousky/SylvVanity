using Luminance.Core.Graphics;
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
            Vector2 earDrawPosition = drawPlayer.TopLeft - Main.screenPosition + Vector2.UnitY * drawPlayer.gfxOffY;
            earDrawPosition += new Vector2((drawPlayer.width - drawPlayer.bodyFrame.Width) / 2f, drawPlayer.height - drawPlayer.bodyFrame.Height + 4f);
            Vector2 headVect = new(drawPlayer.legFrame.Width * 0.5f, drawPlayer.legFrame.Height * 0.4f);
            earDrawPosition += new Vector2(drawPlayer.direction == 1 ? 1f : -11f, -4f);
            earDrawPosition += drawPlayer.headPosition + headVect;

            Texture2D ears = ModContent.Request<Texture2D>("SylvVanity/Content/Items/LucillesEars").Value;
            ManagedShader pixelationShader = ShaderManager.GetShader("Luminance.PixelationShader");
            pixelationShader.TrySetParameter("pixelationFactor", Vector2.One * 2f / ears.Size());
            pixelationShader.Apply();

            earDrawPosition.Y = MathF.Round(earDrawPosition.Y / 2f) * 2f;

            Rectangle hairFrame = drawPlayer.bodyFrame;
            hairFrame.Y -= 336;

            if (hairFrame.Y == 56 || hairFrame.Y == 112 || hairFrame.Y == 168 || hairFrame.Y == 448 || hairFrame.Y == 504 || hairFrame.Y == 560)
                earDrawPosition.Y -= 2f;

            // Draw ears.
            float earRotation = drawPlayer.headRotation + drawPlayer.direction * -0.16f;
            Main.spriteBatch.Draw(ears, earDrawPosition.Floor(), null, Color.White, earRotation, new Vector2(headVect.X, ears.Height), 0.37f, drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
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
