//1.2pre
using UnityEngine;

namespace DestructionEffects
{
    public class DeGaplessParticleEmitter : MonoBehaviour
    {
        public bool Emit;

        public float MaxDistance = 1.1f;

        public Part Part;

        public KSPParticleEmitter PEmitter;

        public Rigidbody Rb;

        private void Start()
        {
            PEmitter = gameObject.GetComponent<KSPParticleEmitter>();
            PEmitter.emit = false;

            if (Part != null)
            {
                Debug.Log("Part " + Part.partName + "'s explosionPotential: " + Part.explosionPotential);
            }

            MaxDistance = PEmitter.minSize / 3;
        }

        private void FixedUpdate()
        {
            if (!Emit) return;

            var velocity = Part?.GetComponent<Rigidbody>().velocity ?? Rb.velocity;
            var originalLocalPosition = gameObject.transform.localPosition;
            var originalPosition = gameObject.transform.position;
            var startPosition = gameObject.transform.position + velocity * Time.fixedDeltaTime;
            var originalGapDistance = Vector3.Distance(originalPosition, startPosition);
            var intermediateSteps = originalGapDistance / MaxDistance;

            PEmitter.EmitParticle();
            gameObject.transform.position = Vector3.MoveTowards(
                gameObject.transform.position,
                startPosition,
                MaxDistance);
            for (var i = 1; i < intermediateSteps; i++)
            {
                PEmitter.EmitParticle();
                gameObject.transform.position = Vector3.MoveTowards(
                    gameObject.transform.position,
                    startPosition,
                    MaxDistance);
            }
            gameObject.transform.localPosition = originalLocalPosition;
        }

        public void EmitParticles()
        {
            var velocity = Part?.GetComponent<Rigidbody>().velocity ?? Rb.velocity;
            var originalLocalPosition = gameObject.transform.localPosition;
            var originalPosition = gameObject.transform.position;
            var startPosition = gameObject.transform.position + velocity * Time.fixedDeltaTime;
            var originalGapDistance = Vector3.Distance(originalPosition, startPosition);
            var intermediateSteps = originalGapDistance / MaxDistance;

            //gameObject.transform.position = startPosition;
            for (var i = 0; i < intermediateSteps; i++)
            {
                PEmitter.EmitParticle();
                gameObject.transform.position = Vector3.MoveTowards(
                    gameObject.transform.position,
                    startPosition,
                    MaxDistance);
            }
            gameObject.transform.localPosition = originalLocalPosition;
        }
    }
}