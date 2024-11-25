using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace SylvVanity
{
    public class SylvVanityPlayer : ModPlayer
    {
        /// <summary>
        /// A 0-1 animation completion interpolation for ear twitches.
        /// </summary>
        public float EarTwitchAnimationCompletion
        {
            get;
            set;
        }

        /// <summary>
        /// The moving average of this player's velocity. Used for the motion of the ribbons.
        /// </summary>
        public Vector2 VelocityMovingAverage
        {
            get;
            set;
        }

        public override void PreUpdateMovement()
        {
            // Randomly start the ear twich animation.
            if (Player.velocity.Length() <= 1f && Main.rand.NextBool(120) && EarTwitchAnimationCompletion <= 0f)
            {
                EarTwitchAnimationCompletion = 0.01f;
                Player.eyeHelper.BlinkBecausePlayerGotHurt();
            }

            // Update animations.
            if (EarTwitchAnimationCompletion > 0f)
                EarTwitchAnimationCompletion += 0.05f;
            if (EarTwitchAnimationCompletion >= 1f)
                EarTwitchAnimationCompletion = 0f;

            VelocityMovingAverage = Vector2.Lerp(VelocityMovingAverage, Player.velocity, 0.3f).MoveTowards(Player.velocity, 0.5f);
        }
    }
}
