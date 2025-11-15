using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starter.SubsceneSplitscreen {
    public class StarterPawn : Pawn {

        [Header("References")]
        [SerializeField] private ButtonPanel buttonPanel;

        public void SetMaterial(Material material)
        {
            GetComponent<MeshRenderer>().material = material;
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