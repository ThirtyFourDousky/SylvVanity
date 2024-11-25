using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SylvVanity.Content.Items
{
    public class LucilleHairLayer : PlayerDrawLayer
    {
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
                }
            }
        }
    }
}
