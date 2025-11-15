using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starter.SubsceneSplitscreen {
    public class ButtonPanel : MonoBehaviour {
        [Header("References")]
        public StarterSubmanager submanager;
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject indicator;
        [SerializeField] private GameObject buttonPrefab;
        // Constants
        private static float INDICATOR_VELOCITY = 1;
        private static float INDICATOR_MOVE_TIME = 0.5f;
        private static float INDICATOR_STOP_MARGIN = 0.01f;
        private static float BUTTON_PANEL_PADDING_X = 10;
        private static float BUTTON_PANEL_PADDING_Y = 10;
        private static float BUTTON_PANEL_X_EXTRA = 0;
        private static float BUTTON_PANEL_Y_EXTRA = 10;

        public static bool canUsePanel = false;
        private int selectedTypeIndex;
        private TrapdoorType selectedTrapdoorType => StarterGameManager.instance.trapdoorTypes[selectedTypeIndex];
        private Button selectedButton => buttons[selectedTrapdoorType];
        private Dictionary<TrapdoorType, Button> buttons = new();
        private bool updateIndicatorPosition;
        private Vector3 targetIndicatorPosition;
        private Vector3 indicatorVelocity;
        void Start()
        {
            // Move panel object
            int typeCount = StarterGameManager.instance.trapdoorTypes.Count;
            Vector3 buttonSize = buttonPrefab.transform.localScale;
            float panelThickness = panel.transform.localScale.z;
            float panelWidth = (buttonSize.x * typeCount) + (BUTTON_PANEL_PADDING_X * (typeCount + 1));
            float panelHeight = buttonSize.z + (BUTTON_PANEL_PADDING_Y * 2) + BUTTON_PANEL_Y_EXTRA;
            float canvasHeight = GetComponent<RectTransform>().rect.height * 0.5f;
            panel.transform.localScale = new(panelWidth + BUTTON_PANEL_X_EXTRA, panelHeight, panelThickness);
            panel.transform.localPosition = new(
                panel.transform.localPosition.x,
                -canvasHeight + (0.5f * (panelHeight - BUTTON_PANEL_Y_EXTRA)),
                panel.transform.localPosition.z
            );

            // Make buttons
            for (int i = 0; i < typeCount; i++)
            {
                TrapdoorType type = StarterGameManager.instance.trapdoorTypes[i];
                GameObject buttonObject = Instantiate(buttonPrefab, gameObject.transform);
                buttonObject.transform.localPosition = new(
                    (0.5f * (buttonSize.x - panelWidth)) + BUTTON_PANEL_PADDING_X + (i * (buttonSize.x + BUTTON_PANEL_PADDING_X)),
                    (0.5f * BUTTON_PANEL_Y_EXTRA) + panel.transform.localPosition.y,
                    -((0.5f * panelThickness) + buttonSize.y)
                );
                Button button = buttonObject.GetComponent<Button>();
                button.type = type;
                buttons.Add(type, button);
            }

            // Move indicator
            indicator.transform.position = GetIndicatorPosition();
            indicator.SetActive(true);
        }

        void Update()
        {
            if (updateIndicatorPosition)
            {
                // Move indicator to target
                Vector3 startPosition = indicator.transform.position;
                Vector3 newPosition = Vector3.SmoothDamp(
                    startPosition, targetIndicatorPosition, ref indicatorVelocity, INDICATOR_MOVE_TIME
                );
                indicator.transform.position = newPosition;
                if ((newPosition - startPosition).magnitude <= INDICATOR_STOP_MARGIN)
                {
                    indicator.transform.position = targetIndicatorPosition;
                    updateIndicatorPosition = false;
                }
            }
        }
        private Vector3 GetIndicatorPosition()
        {
            return selectedButton.transform.position;
        }
        
        void UpdateIndicator()
        {
            targetIndicatorPosition = GetIndicatorPosition();
            indicatorVelocity = INDICATOR_VELOCITY * (targetIndicatorPosition - indicator.transform.position).normalized;
            updateIndicatorPosition = true;
        }

        public void MoveLeft()
        {
            if (!canUsePanel)
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
            if (!canUsePanel)
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
            if (!canUsePanel)
            {
                return;
            }
            submanager.TryOpenTrapdoor(selectedTrapdoorType);
        }
    }
}