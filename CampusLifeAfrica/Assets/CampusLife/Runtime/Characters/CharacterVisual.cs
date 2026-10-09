using UnityEngine;

namespace CampusLife.Runtime.Characters
{
    /// <summary>
    /// Bone references for a modular stylised character. The rig is built in
    /// code (see <see cref="CharacterRigBuilder"/>) so appearance options can
    /// be swapped at runtime; a skinned, artist-made rig can later implement
    /// the same pivots without changing gameplay code.
    /// </summary>
    public sealed class CharacterVisual : MonoBehaviour
    {
        public Transform Body;       // moves up/down for bob and sitting
        public Transform Torso;
        public Transform Head;
        public Transform ArmLeft, ArmRight;
        public Transform ForearmLeft, ForearmRight;
        public Transform LegLeft, LegRight;
        public Transform KneeLeft, KneeRight;
        public Transform Props;      // hand-held items such as a clipboard

        /// <summary>Hip height above the root when standing, used for sitting offsets.</summary>
        public float HipHeight = 0.9f;
    }
}
