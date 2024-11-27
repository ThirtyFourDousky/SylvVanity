using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace SylvVanity
{
    public class ShaderSystem : ModSystem
    {
        /// <summary>
        /// The mod's pixelation shader.
        /// </summary>
        public static Asset<Effect>? PixelationShader
        {
            get;
            private set;
        }

        /// <summary>
        /// The mod's feeler ribbon shader.
        /// </summary>
        public static Asset<Effect>? FeelerRibbonShader
        {
            get;
            private set;
        }

        public override void OnModLoad()
        {
            PixelationShader = ModContent.Request<Effect>("SylvVanity/Assets/Shaders/PixelationShader");
            FeelerRibbonShader = ModContent.Request<Effect>("SylvVanity/Assets/Shaders/LucilleFeelerShader");
        }
    }
}
