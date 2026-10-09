using System;

namespace CampusLife.Core.Simulation
{
    /// <summary>
    /// Chooses which nearby interactable the prompt should target. Closer
    /// objects in front of the player win; objects behind the player are only
    /// chosen when very close.
    /// </summary>
    public static class InteractionScoring
    {
        public const float BehindReachFactor = 0.45f;

        /// <returns>A score where higher is better, or a negative value if out of reach.</returns>
        public static float Score(float distance, float range, float angleFromForwardDegrees)
        {
            if (range <= 0f || distance < 0f || distance > range) return -1f;
            float angle = Math.Abs(angleFromForwardDegrees);
            if (angle > 100f && distance > range * BehindReachFactor) return -1f;
            float distanceTerm = 1f - distance / range;            // 0..1
            float facingTerm = 1f - Math.Min(angle, 180f) / 180f;   // 0..1
            return distanceTerm * 0.6f + facingTerm * 0.4f;
        }
    }
}
