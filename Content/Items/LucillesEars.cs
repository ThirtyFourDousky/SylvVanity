using Terraria;
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
            {
                ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = true;
                ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
            }
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
