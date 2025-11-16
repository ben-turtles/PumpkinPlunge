using UnityEngine;

namespace Starter.PumpkinPlunge {
    public class TankWater : MonoBehaviour {
        [SerializeField] private PumpkinPawn player;

        private Collider playerCollider => player.collider;
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