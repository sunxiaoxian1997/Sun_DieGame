using System.Collections;
using System.Collections.Generic;
using CorpseMechanism.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class PlayerMovementPlayModeTests
    {
        private const int GroundingTimeoutSteps = 80;
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                if (createdObject != null)
                {
                    Object.Destroy(createdObject);
                }
            }

            _createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HorizontalInput_MovesRightAndLeft()
        {
            PlayerRig rig = CreatePlayer(new Vector2(0f, 2f), 0f);

            float startX = rig.Body.position.x;
            rig.Input.SetHorizontal(1f);
            yield return WaitForFixedSteps(3);
            float rightX = rig.Body.position.x;

            rig.Input.SetHorizontal(-1f);
            yield return WaitForFixedSteps(5);
            float leftX = rig.Body.position.x;

            Assert.That(rightX, Is.GreaterThan(startX + 0.05f));
            Assert.That(leftX, Is.LessThan(rightX - 0.05f));
        }

        [UnityTest]
        public IEnumerator GroundedJump_ProducesPositiveVerticalVelocity()
        {
            CreateGround();
            PlayerRig rig = CreatePlayer(new Vector2(0f, 1.5f), 2.5f);
            yield return WaitUntilGrounded(rig);

            rig.Input.SubmitJump();
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Body.velocity.y, Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator AirborneJumpRequest_DoesNotResetJumpVelocity()
        {
            CreateGround();
            PlayerRig rig = CreatePlayer(new Vector2(0f, 1.5f), 2.5f);
            yield return WaitUntilGrounded(rig);

            rig.Input.SubmitJump();
            yield return WaitForFixedSteps(2);
            Assert.That(rig.Probe.RefreshGroundedState(), Is.False);

            float velocityBeforeSecondRequest = rig.Body.velocity.y;
            rig.Input.SubmitJump();
            yield return new WaitForFixedUpdate();
            float velocityAfterSecondRequest = rig.Body.velocity.y;

            Assert.That(velocityAfterSecondRequest, Is.LessThan(rig.Motor.JumpSpeed - 0.25f));
            Assert.That(velocityAfterSecondRequest, Is.LessThan(velocityBeforeSecondRequest + 0.05f));
        }

        [UnityTest]
        public IEnumerator DisabledControl_StopsMovementClearsJumpAndCanBeRestored()
        {
            CreateGround();
            PlayerRig rig = CreatePlayer(new Vector2(0f, 1.5f), 2.5f);
            yield return WaitUntilGrounded(rig);

            rig.Input.SetHorizontal(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(rig.Body.velocity.x, Is.GreaterThan(0f));

            rig.Motor.SetControlEnabled(false);
            rig.Input.SubmitJump();
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Body.velocity.x, Is.EqualTo(0f).Within(0.001f));

            rig.Motor.SetControlEnabled(true);
            rig.Input.SetHorizontal(-1f);
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Body.velocity.x, Is.LessThan(0f));
            Assert.That(rig.Body.velocity.y, Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator JumpSubmittedBetweenPhysicsSteps_IsNotLost()
        {
            CreateGround();
            PlayerRig rig = CreatePlayer(new Vector2(0f, 1.5f), 2.5f);
            yield return WaitUntilGrounded(rig);

            yield return new WaitForFixedUpdate();
            rig.Input.SubmitJump();
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(rig.Body.velocity.y, Is.GreaterThan(1f));
            Assert.That(rig.Input.ConsumeJumpPressed(), Is.False);
        }

        private GameObject CreateGround()
        {
            GameObject ground = new GameObject("TestGround");
            ground.transform.position = Vector3.zero;
            BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(20f, 1f);
            _createdObjects.Add(ground);
            return ground;
        }

        private PlayerRig CreatePlayer(Vector2 position, float gravityScale)
        {
            GameObject player = new GameObject("TestPlayer");
            player.SetActive(false);
            player.transform.position = position;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = gravityScale;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            CapsuleCollider2D collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.8f, 1.6f);

            GroundProbe2D probe = player.AddComponent<GroundProbe2D>();
            TestPlayerInputSource input = new TestPlayerInputSource();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();

            probe.Configure(collider, 1 << 0, 0.12f);
            motor.Configure(body, probe, input, 5f, 7f);
            player.SetActive(true);
            Physics2D.SyncTransforms();

            _createdObjects.Add(player);
            return new PlayerRig(body, probe, motor, input);
        }

        private static IEnumerator WaitUntilGrounded(PlayerRig rig)
        {
            for (int step = 0; step < GroundingTimeoutSteps; step++)
            {
                yield return new WaitForFixedUpdate();
                if (rig.Probe.RefreshGroundedState() && Mathf.Abs(rig.Body.velocity.y) < 0.2f)
                {
                    yield break;
                }
            }

            Assert.Fail("Player did not become grounded within the test timeout.");
        }

        private static IEnumerator WaitForFixedSteps(int count)
        {
            for (int step = 0; step < count; step++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private readonly struct PlayerRig
        {
            public PlayerRig(
                Rigidbody2D body,
                GroundProbe2D probe,
                PlayerMotor2D motor,
                TestPlayerInputSource input)
            {
                Body = body;
                Probe = probe;
                Motor = motor;
                Input = input;
            }

            public Rigidbody2D Body { get; }

            public GroundProbe2D Probe { get; }

            public PlayerMotor2D Motor { get; }

            public TestPlayerInputSource Input { get; }
        }

        private sealed class TestPlayerInputSource : IPlayerInputSource
        {
            private readonly InputButtonLatch _jumpPressed = new InputButtonLatch();
            private bool _enabled = true;

            public float Horizontal { get; private set; }

            public bool ConsumeJumpPressed()
            {
                return _enabled && _jumpPressed.Consume();
            }

            public void SetInputEnabled(bool enabled)
            {
                _enabled = enabled;
                if (!enabled)
                {
                    Horizontal = 0f;
                    _jumpPressed.Clear();
                }
            }

            public void SetHorizontal(float horizontal)
            {
                Horizontal = _enabled ? Mathf.Clamp(horizontal, -1f, 1f) : 0f;
            }

            public void SubmitJump()
            {
                if (_enabled)
                {
                    _jumpPressed.Latch();
                }
            }
        }
    }
}
