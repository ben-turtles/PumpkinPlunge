using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starter.SubsceneSplitscreen {
    public class StarterPawn : Pawn {

        [Header("References")]
        public ButtonPanel buttonPanel;

        private float density = 500f;
        private float submerged = 1f;
        private float dragC = 0.5f;
        public bool inWater = false;

        public void SetMaterial(Material material)
        {
            GetComponent<MeshRenderer>().material = material;
        }

        void FixedUpdate()
        {
            if (inWater)
            {
                // Rigidbody body = GetComponent<Rigidbody>();
                // float m = TankWater.density * Physics.gravity.magnitude * submerged;
                // Debug.Log("IN WATER!" + m);
                // Vector3 buoyantForce = Vector3.up * m;
                // body.AddForce(buoyantForce, ForceMode.Force);
                // body.AddForce(-body.velocity * dragC, ForceMode.Force);
            }
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonA) {
                // Activate panel
                buttonPanel.Activate();
            }

            if (context.action.name == PawnAction.ButtonL)
            {
                // Select left type
                buttonPanel.MoveLeft();
            }
            else if (context.action.name == PawnAction.ButtonR)
            {
                // Move panel indicator right
                buttonPanel.MoveRight();
            }
        }

        // protected override void OnActionReleased(InputAction.CallbackContext context) {
        //     if (context.action.name == PawnAction.ButtonB) {
        //         // Stop shooting
        //     }
        // }
    }
}