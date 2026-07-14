using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CorpseMechanism.Tests.PlayMode
{
    public sealed class InfrastructurePlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator TwoDimensionalComponents_RemainConfiguredAcrossPlayModeFrames()
        {
            GameObject testObject = new GameObject("Infrastructure2DSmokeTest");
            Rigidbody2D body = testObject.AddComponent<Rigidbody2D>();
            BoxCollider2D boxCollider = testObject.AddComponent<BoxCollider2D>();

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            boxCollider.size = new Vector2(2f, 1f);

            yield return null;
            yield return new WaitForFixedUpdate();

            bool bodyIsConfigured =
                body.bodyType == RigidbodyType2D.Kinematic &&
                Mathf.Approximately(body.gravityScale, 0f);
            Vector2 colliderSize = boxCollider.size;

            Object.Destroy(testObject);
            yield return null;

            Assert.Multiple(() =>
            {
                Assert.That(bodyIsConfigured, Is.True);
                Assert.That(colliderSize, Is.EqualTo(new Vector2(2f, 1f)));
                Assert.That(testObject == null, Is.True);
            });
        }
    }
}
