using CampusLife.Core.Narrative;
using CampusLife.Core.Player;
using CampusLife.Core.Simulation;
using CampusLife.Core.Social;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class MotionMathTests
    {
        const float Eps = 1e-4f;

        [Test]
        public void DeadZoneRemovesDriftAndRescales()
        {
            Assert.AreEqual(0f, MotionMath.ApplyDeadZone(new Planar(0.05f, 0.05f), 0.12f).Magnitude, Eps);
            Assert.AreEqual(1f, MotionMath.ApplyDeadZone(new Planar(0f, 1f), 0.12f).Magnitude, Eps);
            Assert.AreEqual(0.5f, MotionMath.ApplyDeadZone(new Planar(0.56f, 0f), 0.12f).Magnitude, Eps);
        }

        [Test]
        public void MovementIsRelativeToCamera()
        {
            var forwardNorth = MotionMath.CameraRelative(new Planar(0, 1), 0f);
            Assert.AreEqual(0f, forwardNorth.X, Eps);
            Assert.AreEqual(1f, forwardNorth.Y, Eps);

            var forwardEast = MotionMath.CameraRelative(new Planar(0, 1), 90f);
            Assert.AreEqual(1f, forwardEast.X, Eps);
            Assert.AreEqual(0f, forwardEast.Y, Eps);

            var rightWhenFacingEast = MotionMath.CameraRelative(new Planar(1, 0), 90f);
            Assert.AreEqual(0f, rightWhenFacingEast.X, Eps);
            Assert.AreEqual(-1f, rightWhenFacingEast.Y, Eps);
        }

        [Test]
        public void SpeedAcceleratesAndBrakes()
        {
            var s = new LocomotionSettings();
            Assert.AreEqual(s.RunSpeed, MotionMath.TargetSpeed(1f, true, s), Eps);
            Assert.AreEqual(s.WalkSpeed * 0.5f, MotionMath.TargetSpeed(0.5f, false, s), Eps);
            float v = MotionMath.StepSpeed(0f, s.RunSpeed, 0.1f, s);
            Assert.AreEqual(s.Acceleration * 0.1f, v, Eps);
            Assert.AreEqual(0f, MotionMath.StepSpeed(1f, 0f, 1f, s), Eps);
        }

        [Test]
        public void AnglesWrapTheShortWay()
        {
            Assert.AreEqual(20f, MotionMath.DeltaAngle(350f, 10f), Eps);
            Assert.AreEqual(-20f, MotionMath.DeltaAngle(10f, 350f), Eps);
            Assert.AreEqual(360f, MotionMath.MoveTowardsAngle(350f, 10f, 10f), Eps);
            Assert.AreEqual(90f, MotionMath.Heading(new Planar(1, 0)), Eps);
        }

        [Test]
        public void SmoothingIsFrameRateIndependent()
        {
            float oneStep = MotionMath.SmoothingFactor(10f, 0.1f);
            float twoSteps = 1f - (1f - MotionMath.SmoothingFactor(10f, 0.05f)) * (1f - MotionMath.SmoothingFactor(10f, 0.05f));
            Assert.AreEqual(oneStep, twoSteps, Eps);
            Assert.AreEqual(1f, MotionMath.SmoothingFactor(0f, 0.1f), Eps);
        }
    }

    public class CameraOrbitTests
    {
        [Test]
        public void PitchAndZoomAreClamped()
        {
            var s = new CameraSettings();
            var orbit = new CameraOrbit(0, 10, 4, s);
            orbit.Look(0, -1000, s); // mouse down raises the camera (looks further down at the player)
            Assert.AreEqual(s.MaxPitch, orbit.Pitch, 1e-4);
            orbit.Look(0, 1000, s);
            Assert.AreEqual(s.MinPitch, orbit.Pitch, 1e-4);
            orbit.Zoom(100, s);
            Assert.AreEqual(s.MinDistance, orbit.Distance, 1e-4);
            orbit.Zoom(-100, s);
            Assert.AreEqual(s.MaxDistance, orbit.Distance, 1e-4);
        }

        [Test]
        public void InvertYFlipsPitch()
        {
            var s = new CameraSettings();
            var normal = new CameraOrbit(0, 10, 4, s);
            normal.Look(0, 5, s);
            s.InvertY = true;
            var inverted = new CameraOrbit(0, 10, 4, s);
            inverted.Look(0, 5, s);
            Assert.Less(normal.Pitch, 10f);
            Assert.Greater(inverted.Pitch, 10f);
        }

        [Test]
        public void YawWrapsAndSensitivityScales()
        {
            var s = new CameraSettings { Sensitivity = 2f };
            var orbit = new CameraOrbit(350, 0, 4, s);
            orbit.Look(10, 0, s);
            Assert.AreEqual(10f, orbit.Yaw, 1e-4);
        }

        [Test]
        public void CollisionPullsCameraInFrontOfWalls()
        {
            var s = new CameraSettings { CollisionPadding = 0.2f };
            Assert.AreEqual(5f, CameraOrbit.ResolveCollision(5f, null, s), 1e-4);
            Assert.AreEqual(2.8f, CameraOrbit.ResolveCollision(5f, 3f, s), 1e-4);
            Assert.AreEqual(0.3f, CameraOrbit.ResolveCollision(5f, 0.1f, s), 1e-4);
            Assert.AreEqual(2f, CameraOrbit.ResolveCollision(2f, 4f, s), 1e-4);
        }
    }

    public class InteractionScoringTests
    {
        [Test]
        public void PrefersCloseObjectsInFront()
        {
            float front = InteractionScoring.Score(1.5f, 2.5f, 5f);
            float side = InteractionScoring.Score(1.5f, 2.5f, 80f);
            float far = InteractionScoring.Score(2.4f, 2.5f, 5f);
            Assert.Greater(front, side);
            Assert.Greater(front, far);
            Assert.Less(InteractionScoring.Score(3f, 2.5f, 0f), 0f);
            Assert.Less(InteractionScoring.Score(2f, 2.5f, 170f), 0f, "Far behind the player is ignored");
            Assert.Greater(InteractionScoring.Score(0.8f, 2.5f, 170f), 0f, "Very close behind is allowed");
        }
    }

    public class SocialAndProfileTests
    {
        [Test]
        public void RelationshipValuesAreClampedAndMemoriesDeduplicated()
        {
            var book = new RelationshipBook();
            var rel = book.Get("npc");
            rel.ChangeAffinity(500);
            Assert.AreEqual(100, rel.Affinity);
            rel.ChangeTrust(-500);
            Assert.AreEqual(-100, rel.Trust);
            Assert.IsTrue(rel.Remember("helped", MemoryKind.Favor, 10));
            Assert.IsFalse(rel.Remember("helped", MemoryKind.Favor, 20));
            Assert.AreEqual(1, rel.Memories.Count);
            var restored = RelationshipBook.FromData(book.ToData());
            Assert.AreEqual(100, restored.Get("npc").Affinity);
            Assert.AreEqual(MemoryKind.Favor, restored.Get("npc").FindMemory("helped").kind);
        }

        [Test]
        public void ObjectivesFollowFlags()
        {
            var tracker = new ObjectiveTracker(new[]
            {
                new ObjectiveDefinition("a", "A", "done_a"),
                new ObjectiveDefinition("b", "B", "done_b", null, "done_a"),
            });
            var flags = new NarrativeFlags();
            Assert.AreEqual("a", tracker.Current(flags).Id);
            flags.Set("done_a");
            Assert.AreEqual("b", tracker.Current(flags).Id);
            flags.Set("done_b");
            Assert.IsNull(tracker.Current(flags));
            Assert.AreEqual(2, tracker.CompletedCount(flags));
        }

        [Test]
        public void ProfileValidationAndSanitizing()
        {
            Assert.IsNotEmpty(CharacterOptions.Validate(new PlayerProfileData { displayName = "A" }));
            Assert.IsEmpty(CharacterOptions.Validate(new PlayerProfileData { displayName = "Ama" }));
            var a = CharacterOptions.Sanitize(new AppearanceData { skinTone = -1, hairStyle = 999, height = 3f });
            Assert.AreEqual(CharacterOptions.SkinTones.Length - 1, a.skinTone);
            Assert.Less(a.hairStyle, CharacterOptions.HairStyles.Length);
            Assert.AreEqual(1.08f, a.height, 1e-5);
        }
    }
}
