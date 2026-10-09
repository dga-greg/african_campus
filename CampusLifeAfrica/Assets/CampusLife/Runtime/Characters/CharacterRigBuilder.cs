using CampusLife.Core.Player;
using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.Characters
{
    /// <summary>
    /// Builds a modular stylised humanoid from shared primitive meshes. Every
    /// appearance option is cosmetic only. Root origin is at the feet; +Z is forward.
    /// </summary>
    public static class CharacterRigBuilder
    {
        static readonly Color EyeWhite = new Color(0.96f, 0.95f, 0.92f);
        static readonly Color EyeDark = new Color(0.08f, 0.06f, 0.05f);
        static readonly Color KenteGold = new Color(0.98f, 0.76f, 0.10f);
        static readonly Color KenteGreen = new Color(0.06f, 0.50f, 0.28f);
        static readonly Color KenteRed = new Color(0.78f, 0.12f, 0.12f);

        public static CharacterVisual Build(Transform parent, AppearanceData appearance, MaterialLibrary mats, string name = "Visual")
        {
            var a = CharacterOptions.Sanitize(appearance);
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var v = root.gameObject.AddComponent<CharacterVisual>();

            Color skin = MaterialLibrary.FromRgb(CharacterOptions.SkinTones[a.skinTone]);
            Color hair = MaterialLibrary.FromRgb(CharacterOptions.HairColors[a.hairColor]);
            Color top = MaterialLibrary.FromRgb(CharacterOptions.ClothingColors[a.topColor]);
            Color bottom = MaterialLibrary.FromRgb(CharacterOptions.ClothingColors[a.bottomColor]);
            Color shoes = MaterialLibrary.FromRgb(CharacterOptions.ClothingColors[a.shoeColor]);
            var skinMat = mats.Get(skin, 0.35f);
            var topMat = mats.Get(top);
            var bottomMat = mats.Get(bottom);
            var shoeMat = mats.Get(shoes * 0.85f, 0.4f);
            var hairMat = mats.Get(hair, 0.25f);

            root.localScale = Vector3.one * a.height;

            v.Body = Pivot("Body", root, new Vector3(0f, 0f, 0f));

            // Legs: hip pivot → thigh, knee pivot → shin + shoe.
            v.LegLeft = BuildLeg("LegL", v.Body, -0.1f, bottomMat, skinMat, shoeMat, out v.KneeLeft);
            v.LegRight = BuildLeg("LegR", v.Body, 0.1f, bottomMat, skinMat, shoeMat, out v.KneeRight);

            // Pelvis and torso.
            Part("Pelvis", PrimitiveType.Cube, v.Body, new Vector3(0, 0.93f, 0), new Vector3(0.34f, 0.18f, 0.22f), bottomMat);
            v.Torso = Pivot("Torso", v.Body, new Vector3(0, 1.0f, 0));
            float torsoWidth = a.topStyle == 3 ? 0.46f : 0.4f;
            Part("Chest", PrimitiveType.Capsule, v.Torso, new Vector3(0, 0.24f, 0), new Vector3(torsoWidth, 0.3f, 0.25f), topMat);
            BuildTopDetails(a, v.Torso, mats, top);

            // Arms: shoulder pivot → upper arm, elbow pivot → forearm + hand.
            v.ArmLeft = BuildArm("ArmL", v.Torso, -1f, torsoWidth, topMat, skinMat, a.topStyle == 2, out v.ForearmLeft);
            v.ArmRight = BuildArm("ArmR", v.Torso, 1f, torsoWidth, topMat, skinMat, a.topStyle == 2, out v.ForearmRight);

            // Neck and head with a simple expressive face.
            Part("Neck", PrimitiveType.Cylinder, v.Torso, new Vector3(0, 0.5f, 0), new Vector3(0.1f, 0.06f, 0.1f), skinMat);
            v.Head = Pivot("Head", v.Torso, new Vector3(0, 0.56f, 0));
            Part("Skull", PrimitiveType.Sphere, v.Head, new Vector3(0, 0.14f, 0), new Vector3(0.25f, 0.29f, 0.26f), skinMat);
            Part("Nose", PrimitiveType.Sphere, v.Head, new Vector3(0, 0.12f, 0.125f), new Vector3(0.05f, 0.05f, 0.05f), skinMat);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("EyeWhite", PrimitiveType.Sphere, v.Head, new Vector3(0.05f * side, 0.17f, 0.105f), new Vector3(0.055f, 0.045f, 0.03f), mats.Get(EyeWhite, 0.6f));
                Part("Pupil", PrimitiveType.Sphere, v.Head, new Vector3(0.05f * side, 0.17f, 0.118f), new Vector3(0.028f, 0.032f, 0.015f), mats.Get(EyeDark, 0.8f));
                Part("Brow", PrimitiveType.Cube, v.Head, new Vector3(0.05f * side, 0.215f, 0.112f), new Vector3(0.06f, 0.012f, 0.012f), hairMat);
                Part("Ear", PrimitiveType.Sphere, v.Head, new Vector3(0.125f * side, 0.14f, 0f), new Vector3(0.035f, 0.06f, 0.04f), skinMat);
            }
            Part("Mouth", PrimitiveType.Cube, v.Head, new Vector3(0, 0.065f, 0.112f), new Vector3(0.06f, 0.012f, 0.01f), mats.Get(skin * 0.55f));
            BuildHair(a, v.Head, hairMat, mats.Get(top * 0.9f + Color.white * 0.05f));
            BuildAccessory(a, v, mats);

            v.Props = Pivot("Props", v.ForearmRight, new Vector3(0, -0.3f, 0.05f));
            return v;
        }

        public static Transform Pivot(string name, Transform parent, Vector3 localPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        public static Transform Part(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Material mat, Vector3? euler = null)
        {
            var go = MeshCache.CreateVisual(name, type, parent, mat);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            var r = go.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            r.receiveShadows = true;
            return go.transform;
        }

        static Transform BuildLeg(string name, Transform body, float x, Material trousers, Material skin, Material shoe, out Transform knee)
        {
            var hip = Pivot(name, body, new Vector3(x, 0.9f, 0));
            Part("Thigh", PrimitiveType.Capsule, hip, new Vector3(0, -0.22f, 0), new Vector3(0.15f, 0.24f, 0.15f), trousers);
            knee = Pivot("Knee", hip, new Vector3(0, -0.44f, 0));
            Part("Shin", PrimitiveType.Capsule, knee, new Vector3(0, -0.21f, 0), new Vector3(0.13f, 0.23f, 0.13f), trousers);
            Part("Shoe", PrimitiveType.Cube, knee, new Vector3(0, -0.42f, 0.05f), new Vector3(0.12f, 0.08f, 0.26f), shoe);
            return hip;
        }

        static Transform BuildArm(string name, Transform torso, float side, float torsoWidth, Material sleeve, Material skin, bool longSleeves, out Transform elbow)
        {
            var shoulder = Pivot(name, torso, new Vector3(side * (torsoWidth * 0.5f + 0.06f), 0.42f, 0));
            Part("UpperArm", PrimitiveType.Capsule, shoulder, new Vector3(0, -0.15f, 0), new Vector3(0.1f, 0.16f, 0.1f), sleeve);
            elbow = Pivot("Elbow", shoulder, new Vector3(0, -0.3f, 0));
            Part("Forearm", PrimitiveType.Capsule, elbow, new Vector3(0, -0.13f, 0), new Vector3(0.085f, 0.14f, 0.085f), longSleeves ? sleeve : skin);
            Part("Hand", PrimitiveType.Sphere, elbow, new Vector3(0, -0.3f, 0), new Vector3(0.085f, 0.1f, 0.06f), skin);
            return shoulder;
        }

        static void BuildTopDetails(AppearanceData a, Transform torso, MaterialLibrary mats, Color top)
        {
            switch (a.topStyle)
            {
                case 1: // Kente-inspired trim bands (original pattern)
                    Color[] band = { KenteGold, KenteGreen, KenteRed, KenteGold };
                    for (int i = 0; i < band.Length; i++)
                        Part("Trim", PrimitiveType.Cube, torso, new Vector3(0, 0.1f + i * 0.035f, 0.125f), new Vector3(0.36f, 0.025f, 0.02f), mats.Get(band[i]));
                    Part("Placket", PrimitiveType.Cube, torso, new Vector3(0, 0.33f, 0.125f), new Vector3(0.03f, 0.22f, 0.02f), mats.Get(KenteGold));
                    break;
                case 2: // Hoodie: hood and pocket
                    Part("Hood", PrimitiveType.Sphere, torso, new Vector3(0, 0.47f, -0.1f), new Vector3(0.3f, 0.16f, 0.16f), mats.Get(top * 0.92f));
                    Part("Pocket", PrimitiveType.Cube, torso, new Vector3(0, 0.12f, 0.125f), new Vector3(0.24f, 0.09f, 0.02f), mats.Get(top * 0.85f));
                    break;
                case 3: // Smock (fugu): woven vertical stripes and a wider cut
                    Color stripe = top.grayscale > 0.5f ? new Color(0.12f, 0.12f, 0.16f) : new Color(0.94f, 0.92f, 0.86f);
                    for (int i = -2; i <= 2; i++)
                        Part("Stripe", PrimitiveType.Cube, torso, new Vector3(i * 0.08f, 0.22f, 0.128f), new Vector3(0.018f, 0.42f, 0.015f), mats.Get(stripe));
                    break;
            }
        }

        static void BuildHair(AppearanceData a, Transform head, Material hair, Material wrap)
        {
            switch (a.hairStyle)
            {
                case 0: // Low cut
                    Part("Hair", PrimitiveType.Sphere, head, new Vector3(0, 0.19f, -0.01f), new Vector3(0.262f, 0.22f, 0.27f), hair);
                    break;
                case 1: // Short afro
                    Part("Hair", PrimitiveType.Sphere, head, new Vector3(0, 0.22f, -0.015f), new Vector3(0.31f, 0.27f, 0.31f), hair);
                    break;
                case 2: // Big afro
                    Part("Hair", PrimitiveType.Sphere, head, new Vector3(0, 0.26f, -0.03f), new Vector3(0.44f, 0.38f, 0.42f), hair);
                    break;
                case 3: // Braids
                case 4: // Locs
                {
                    bool locs = a.hairStyle == 4;
                    Part("Cap", PrimitiveType.Sphere, head, new Vector3(0, 0.2f, -0.012f), new Vector3(0.27f, 0.23f, 0.28f), hair);
                    int strands = locs ? 9 : 12;
                    float thickness = locs ? 0.045f : 0.03f;
                    for (int i = 0; i < strands; i++)
                    {
                        float ang = Mathf.Lerp(-110f, 110f, i / (float)(strands - 1)) * Mathf.Deg2Rad;
                        var pos = new Vector3(Mathf.Sin(ang) * 0.12f, 0.02f, -Mathf.Cos(ang) * 0.12f);
                        Part("Strand", PrimitiveType.Cylinder, head, pos, new Vector3(thickness, locs ? 0.17f : 0.2f, thickness), hair);
                    }
                    break;
                }
                case 5: // Bantu knots
                    Part("Cap", PrimitiveType.Sphere, head, new Vector3(0, 0.2f, -0.012f), new Vector3(0.265f, 0.22f, 0.275f), hair);
                    Vector3[] knots = { new Vector3(0, 0.33f, 0), new Vector3(-0.08f, 0.3f, 0.05f), new Vector3(0.08f, 0.3f, 0.05f), new Vector3(-0.09f, 0.29f, -0.07f), new Vector3(0.09f, 0.29f, -0.07f) };
                    foreach (var k in knots) Part("Knot", PrimitiveType.Sphere, head, k, Vector3.one * 0.075f, hair);
                    break;
                case 6: // Headwrap
                    Part("Wrap", PrimitiveType.Sphere, head, new Vector3(0, 0.25f, -0.03f), new Vector3(0.3f, 0.3f, 0.32f), wrap);
                    Part("WrapTop", PrimitiveType.Sphere, head, new Vector3(0, 0.36f, -0.02f), new Vector3(0.24f, 0.14f, 0.2f), wrap, new Vector3(-15f, 0, 0));
                    break;
                // case 7: bald — nothing
            }
        }

        static void BuildAccessory(AppearanceData a, CharacterVisual v, MaterialLibrary mats)
        {
            switch (a.accessory)
            {
                case 1: // Glasses
                {
                    var frame = mats.Get(new Color(0.08f, 0.08f, 0.1f), 0.7f);
                    for (int side = -1; side <= 1; side += 2)
                        Part("Lens", PrimitiveType.Cube, v.Head, new Vector3(0.05f * side, 0.17f, 0.13f), new Vector3(0.075f, 0.055f, 0.008f), frame);
                    Part("Bridge", PrimitiveType.Cube, v.Head, new Vector3(0, 0.175f, 0.13f), new Vector3(0.03f, 0.01f, 0.008f), frame);
                    break;
                }
                case 2: // Beaded bracelet
                {
                    Color[] beads = { KenteGold, KenteRed, KenteGreen, KenteGold, KenteRed, KenteGreen };
                    for (int i = 0; i < beads.Length; i++)
                    {
                        float ang = i / (float)beads.Length * Mathf.PI * 2f;
                        Part("Bead", PrimitiveType.Sphere, v.ForearmLeft, new Vector3(Mathf.Cos(ang) * 0.045f, -0.24f, Mathf.Sin(ang) * 0.045f), Vector3.one * 0.028f, mats.Get(beads[i], 0.6f));
                    }
                    break;
                }
                case 3: // Backpack
                    Part("Backpack", PrimitiveType.Cube, v.Torso, new Vector3(0, 0.25f, -0.2f), new Vector3(0.3f, 0.36f, 0.14f), mats.Get(new Color(0.15f, 0.2f, 0.3f)));
                    break;
                case 4: // Cap
                {
                    var capMat = mats.Get(MaterialLibrary.FromRgb(CharacterOptions.ClothingColors[a.bottomColor]) * 0.9f);
                    Part("Cap", PrimitiveType.Sphere, v.Head, new Vector3(0, 0.25f, -0.01f), new Vector3(0.28f, 0.18f, 0.29f), capMat);
                    Part("Brim", PrimitiveType.Cube, v.Head, new Vector3(0, 0.25f, 0.15f), new Vector3(0.2f, 0.015f, 0.12f), capMat);
                    break;
                }
            }
        }
    }
}
