using UnityEngine;

namespace Starter.SubsceneSplitscreen {
    public class TankWater : MonoBehaviour {
        [SerializeField] private StarterPawn player;

        private Collider playerCollider => player.GetComponent<Collider>();
        public static float density = 2f;

        void OnTriggerEnter(Collider other)
        {
            if (other == playerCollider)
            {
                player.inWater = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other == playerCollider)
            {
                player.inWater = false;
            }
        }
    }
}