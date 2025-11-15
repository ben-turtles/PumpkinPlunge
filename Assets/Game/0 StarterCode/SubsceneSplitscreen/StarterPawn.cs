using System;
using System.Collections;
using System.Collections.Generic;
using Game.MinigameFramework.Scripts.Framework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starter.SubsceneSplitscreen {
    public class StarterPawn : Pawn {
        Vector2 _moveInput = Vector2.zero;

        [SerializeField] private StarterSubmanager submanager;
        private int selectedTypeIndex;
        private TrapdoorType selectedTrapdoorType => submanager.trapdoorTypes[selectedTypeIndex];

        void Update()
        {
            // TODO: Implement movement
        }
        
        void UpdateTypeIndex()
        {
            
        }

        protected override void OnActionPressed(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.Move) {
                _moveInput = context.ReadValue<Vector2>();
            }

            if (context.action.name == PawnAction.ButtonA) {
                // Open trapdoor
                submanager.TryOpenTrapdoor(selectedTrapdoorType);
            }

            if (context.action.name == PawnAction.ButtonL)
            {
                // Select left type
                selectedTypeIndex -= 1;
                if (selectedTypeIndex < 0)
                {
                    selectedTypeIndex = submanager.trapdoorTypes.Count - 1;
                }
                UpdateTypeIndex();
            }
            else if (context.action.name == PawnAction.ButtonR)
            {
                // Select right type
                selectedTypeIndex += 1;
                if (selectedTypeIndex >= submanager.trapdoorTypes.Count)
                {
                    selectedTypeIndex = 0;
                }
                UpdateTypeIndex();
            }
        }

        protected override void OnActionReleased(InputAction.CallbackContext context) {
            if (context.action.name == PawnAction.ButtonB) {
                // Stop shooting
            }
        }
    }
}