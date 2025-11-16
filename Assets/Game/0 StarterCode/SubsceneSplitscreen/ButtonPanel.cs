using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Starter.SubsceneSplitscreen {
    public class ButtonPanel : MonoBehaviour {
        [Header("References")]
        public StarterSubmanager submanager;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject indicator;
        [SerializeField] private GameObject buttonPrefab;
        [Header("Parameters")]
        [SerializeField] private float panelButtonsPaddingX = 10;
        [SerializeField] private float panelButtonsPaddingY = 10;
        [SerializeField] private float panelExtraSizeX = 0;
        [SerializeField] private float panelExtraSizeY = 10;
        [SerializeField] private float panelMoveVelocity = 15;
        [SerializeField] private float panelMoveTime = 0.25f;
        [SerializeField] private float panelHiddenOffset = -60;
        [SerializeField] private float indicatorMoveVelocity = 1;
        [SerializeField] private float indicatorMoveTime = 0.5f;

        public static bool canUsePanel = false;
        private int selectedTypeIndex;
        private TrapdoorType selectedTrapdoorType => StarterGameManager.instance.trapdoorTypes[selectedTypeIndex];
        private Button selectedButton => buttons[selectedTrapdoorType];
        private Dictionary<TrapdoorType, Button> buttons = new();
        private bool updateIndicatorPosition;
        private Vector3 targetIndicatorPosition;
        private Vector3 indicatorVelocity;
        private bool updatePanelPosition;
        private Vector3 targetPanelPosition;
        private Vector3 panelVelocity;
        private float canvasHeight;
        private Vector3 buttonSize;
        private bool allowPanelInputs => canUsePanel && isPanelShown;
        private bool isPanelShown = false;

        void Start()
        {
            // Move panel object
            int typeCount = StarterGameManager.instance.trapdoorTypes.Count;
            buttonSize = buttonPrefab.transform.localScale;
            float panelThickness = panel.transform.localScale.z;
            float panelWidth = (buttonSize.x * typeCount) + (panelButtonsPaddingX * (typeCount + 1)) + panelExtraSizeX;
            float panelHeight = buttonSize.z + (panelButtonsPaddingY * 2) + panelExtraSizeY;
            Rect canvasRect = GetComponent<RectTransform>().rect;
            canvasHeight = canvasRect.height;
            panel.transform.localScale = new(panelWidth, panelHeight, panelThickness);
            Material material = panel.GetComponent<Renderer>().material;
            float materialFactor = 0.5f;
            material.mainTextureScale = materialFactor * new Vector2(
                1, canvasRect.height * panelHeight / (canvasRect.width * panelWidth)
            );

            // Make buttons
            for (int i = 0; i < typeCount; i++)
            {
                TrapdoorType type = StarterGameManager.instance.trapdoorTypes[i];
                Button button = Instantiate(buttonPrefab, gameObject.transform).GetComponent<Button>();
                button.type = type;
                buttons.Add(type, button);
            }

            // Toggle panel if game started
            isPanelShown = StarterGameManager.gameStarted;
            updatePanelPosition = false;
            SetPanelPosition(GetPanelPosition());

            // Move indicator
            indicator.transform.position = GetIndicatorPosition();
            indicator.SetActive(true);
        }

        void Update()
        {
            if (updatePanelPosition)
            {
                // Move indicator to target
                Vector3 startPosition = panel.transform.localPosition;
                Vector3 newPosition = Vector3.SmoothDamp(
                    startPosition, targetPanelPosition, ref panelVelocity, panelMoveTime
                );
                SetPanelPosition(newPosition);
                if ((newPosition - startPosition).magnitude <= StarterGameManager.STOP_MARGIN)
                {
                    SetPanelPosition(targetPanelPosition);
                    updatePanelPosition = false;
                }
            }
            else if (updateIndicatorPosition)
            {
                // Move indicator to target
                Vector3 startPosition = indicator.transform.position;
                Vector3 newPosition = Vector3.SmoothDamp(
                    startPosition, targetIndicatorPosition, ref indicatorVelocity, indicatorMoveTime
                );
                indicator.transform.position = newPosition;
                if ((newPosition - startPosition).magnitude <= StarterGameManager.STOP_MARGIN)
                {
                    indicator.transform.position = targetIndicatorPosition;
                    updateIndicatorPosition = false;
                }
            }
        }

        private void SetPanelPosition(Vector3 newPosition)
        {
            panel.transform.localPosition = newPosition;
            var values = buttons.Values;
            for (int i = 0; i < values.Count; i++)
            {
                Button button = values.ElementAt(i);
                button.transform.localPosition = new(
                    (0.5f * (buttonSize.x - panel.transform.localScale.x - panelExtraSizeX)) + panelButtonsPaddingX + (i * (buttonSize.x + panelButtonsPaddingX)),
                    (0.5f * panelExtraSizeY) + panel.transform.localPosition.y,
                    -((0.5f * panel.transform.localScale.z) + buttonSize.y)
                );
            }
            indicator.transform.position = GetIndicatorPosition();
        }

        private Vector3 GetPanelPosition()
        {
            return new(
                panel.transform.localPosition.x,
                0.5f * (-canvasHeight + (panel.transform.localScale.y - panelExtraSizeY))
                + (isPanelShown ? 0 : panelHiddenOffset),
                panel.transform.localPosition.z
            );
        }

        void UpdatePanelPosition()
        {
            targetPanelPosition = GetPanelPosition();
            panelVelocity = panelMoveVelocity * (targetPanelPosition - panel.transform.localPosition).normalized;
            updatePanelPosition = true;
        }

        public void Show()
        {
            isPanelShown = true;
            UpdatePanelPosition();
        }

        public void Hide()
        {
            isPanelShown = false;
            UpdatePanelPosition();
        }

        private Vector3 GetIndicatorPosition()
        {
            return selectedButton.transform.position;
        }
        
        void UpdateIndicator()
        {
            targetIndicatorPosition = GetIndicatorPosition();
            indicatorVelocity = indicatorMoveVelocity * (targetIndicatorPosition - indicator.transform.position).normalized;
            updateIndicatorPosition = true;
        }

        public void MoveLeft()
        {
            if (!allowPanelInputs)
            {
                return;
            }
            if (selectedTypeIndex > 0)
            {
                selectedTypeIndex -= 1;
            }
            UpdateIndicator();
        }

        public void MoveRight()
        {
            if (!allowPanelInputs)
            {
                return;
            }
            if (selectedTypeIndex < StarterGameManager.instance.trapdoorTypes.Count - 1)
            {
                selectedTypeIndex += 1;
            }
            UpdateIndicator();
        }

        public void Activate()
        {
            if (!allowPanelInputs)
            {
                return;
            }
            if (selectedButton.Push())
            {
                bool opened = submanager.TryOpenTrapdoor(selectedTrapdoorType);
                if (!opened)
                {
                    // Wrong trapdoor!
                    selectedButton.MadeWrongChoice();
                }
            }
        }
    }
}