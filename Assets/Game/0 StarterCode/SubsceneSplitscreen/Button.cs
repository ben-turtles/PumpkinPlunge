using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Starter.SubsceneSplitscreen {
    public class Button : MonoBehaviour {
        [Header("References")]
        public GameObject indicatorSymbol;
        [Header("Properties")]
        public TrapdoorType type;
        private float PUSH_PERCENT = 0.85f;
        private float PUSH_MAX_TIME = 0.2f;
        private float WRONG_COOLDOWN = 2f;

        private static bool buttonPushed = false;
        private static bool wrongCooldown = false;
        private static bool canPush => !buttonPushed && !wrongCooldown;
        private bool newPushCall = false;
        private bool positionInit = false;
        private float inPositionZ;
        private float spawnPositionZ;
        private float buttonMoveOffset => PUSH_PERCENT * transform.localScale.y;

        void Start()
        {
            UpdateType();
            buttonPushed = false;
        }

        public void MadeWrongChoice()
        {
            StartCoroutine(WrongChoiceRoutine());
        }

        IEnumerator WrongChoiceRoutine()
        {
            wrongCooldown = true;
            yield return new WaitForSeconds(WRONG_COOLDOWN);
            wrongCooldown = false;
        }

        public bool Push()
        {
            if (!positionInit)
            {
                positionInit = true;
                spawnPositionZ = transform.localPosition.z;
                inPositionZ = spawnPositionZ + buttonMoveOffset;
            }
            if (!canPush)
            {
                return false;
            }
            buttonPushed = true;
            StartCoroutine(PushRoutine());
            return true;
        }

        IEnumerator PushRoutine()
        {
            if (!newPushCall)
            {
                newPushCall = true;
                float startPositionZ = transform.localPosition.z;
                float timer = 0f;
                float pushTime = PUSH_MAX_TIME;
                while (timer < pushTime)
                {
                    float alpha = timer / pushTime;
                    transform.localPosition = new Vector3(
                        transform.localPosition.x,
                        transform.localPosition.y,
                        Mathf.Lerp(startPositionZ, inPositionZ, alpha)
                    );
                    yield return null;
                    timer += Time.deltaTime;
                }
                if (pushTime < PUSH_MAX_TIME)
                {
                    yield return new WaitForSeconds(PUSH_MAX_TIME - pushTime);
                }
                buttonPushed = false;
                newPushCall = false;
                timer = 0f;
                while (timer < PUSH_MAX_TIME && !newPushCall)
                {
                    float alpha = timer / PUSH_MAX_TIME;
                    transform.localPosition = new Vector3(
                        transform.localPosition.x,
                        transform.localPosition.y,
                        Mathf.Lerp(inPositionZ, spawnPositionZ, alpha)
                    );
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }

        void UpdateType()
        {
            GetComponent<MeshRenderer>().material = type.material;
            SpriteRenderer symbolRenderer = indicatorSymbol.GetComponent<SpriteRenderer>();
            symbolRenderer.sprite = type.symbol;
            symbolRenderer.color = type.symbolColor;
            indicatorSymbol.transform.localPosition += new Vector3(0, 0, type.symbolOffset);
        }
    }
}