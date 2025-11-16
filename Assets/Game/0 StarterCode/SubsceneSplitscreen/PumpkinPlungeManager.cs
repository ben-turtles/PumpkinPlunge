using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starter.PumpkinPlunge {
    /// <summary>
    /// Manages minigame state across separate individual subscenes for each player.
    /// Communicates with each StarterSubmanager.
    /// </summary>
    public class PumpkinPlungeManager : MonoBehaviour {
        [HideInInspector] public List<PumpkinPlungeSubmanager> subscenes = new();

        [Header("Game Parameters")]
        public float timer = 30;
        public float timerWarning = 5;
        public Color timerWarningColor = Color.red;
        public int trapdoorCreateCount = 30;
        public List<TrapdoorType> trapdoorTypes;
        public List<Color> resultsWeightColors;
        public List<string> countdownMessages;
        public Color countdownColorStart = Color.white;
        public Color countdownColorEnd = new(1, 1, 1, 0);
        public float countdownStartFontSize = 100;
        public float countdownEndFontSize = 300;
        public float countdownMessageDuration = 1.5f;
        public float countdownColorAlphaOffset = 0.5f;
        public float gameCountdownStartDelay = 1f;
        public float gameCountdownCompleteDelay = 0.2f;
        public float resultsStartDelay = 2f;
        public float resultsFinishDelay = 6f;
        [SerializeField] private float cameraStartZoomOutTime = 2.3f;

        [Header("References")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private GameObject timerFrame;
        [SerializeField] private TextMeshProUGUI timerText;

        public static float STOP_MARGIN = 0.01f;

        public static PumpkinPlungeManager instance;
        public static int currentTime;
        private List<TrapdoorType> trapdoorLayout;
        public static float startCameraAlpha;
        public static bool gameStarted;


        void Start()
        {
            StartCoroutine(StartRoutine());
        }

        private void Awake() {
            if (instance == null) {
                instance = this;
                GenerateTrapdoorLayout();
            }
            else {
                Destroy(gameObject);
            }
        }

        public List<TrapdoorType> GetTrapdoorLayout()
        {
            return trapdoorLayout;
        }

        private void GenerateTrapdoorLayout()
        {
            // Selectively choose random trapdoor types
            trapdoorLayout = new();
            List<TrapdoorType> pickOptions = null;
            int pickCounter = 0;
            int RANDOM_LOOP_COUNT = 4;
            for (int i = 0; i < trapdoorCreateCount; i++)
            {
                pickCounter++;
                if (pickCounter >= RANDOM_LOOP_COUNT || pickOptions == null || pickOptions.Count == 0)
                {
                    pickCounter = 0;
                    pickOptions = new();
                    foreach (TrapdoorType type in trapdoorTypes)
                    {
                        pickOptions.Add(type);
                        pickOptions.Add(type);
                    }
                }
                int index = Random.Range(0, pickOptions.Count);
                trapdoorLayout.Add(pickOptions[index]);
                pickOptions.RemoveAt(index);
            }
        }

        IEnumerator CountdownEffectRoutine(string text, bool hideOnFinish = false)
        {
            float timer = 0f;
            countdownText.text = text;
            countdownText.ForceMeshUpdate();
            while (timer < countdownMessageDuration)
            {
                float fontAlpha = timer / countdownMessageDuration;
                float colorAlpha = (fontAlpha - countdownColorAlphaOffset) / (1 - countdownColorAlphaOffset);
                countdownText.fontSize = Mathf.Lerp(countdownStartFontSize, countdownEndFontSize, fontAlpha);
                countdownText.color = Color.Lerp(countdownColorStart, countdownColorEnd, colorAlpha);
                yield return null;
                timer += Time.deltaTime;
            }
            if (hideOnFinish)
            {
                countdownText.gameObject.SetActive(false);
            }
            else
            {
                countdownText.fontSize = countdownEndFontSize;
                countdownText.color = countdownColorEnd;
                countdownText.text = text;
                countdownText.ForceMeshUpdate();
            }
        }

        private float CameraStartEasing(float t)
        {
            return Mathf.Pow(t, 2);  
        }

        IEnumerator StartCameraRoutine()
        {
            yield return new WaitForSeconds(1f);
            float timer = 0f;
            while (timer < cameraStartZoomOutTime)
            {
                startCameraAlpha = CameraStartEasing(timer / cameraStartZoomOutTime);
                foreach (PumpkinPlungeSubmanager subscene in subscenes)
                {
                    subscene.UpdateStartCameraAlpha();
                }
                yield return null;
                timer += Time.deltaTime;
            }
            startCameraAlpha = 1;
            foreach (PumpkinPlungeSubmanager subscene in subscenes)
            {
                subscene.UpdateStartCameraAlpha();
            }
        }

        IEnumerator StartRoutine()
        {
            ButtonPanel.canUsePanel = false;
            timerFrame.SetActive(false);
            yield return new WaitForSeconds(gameCountdownStartDelay);
            StartCoroutine(StartCameraRoutine());
            for (int i = 0; i < countdownMessages.Count; i++)
            {
                string text = countdownMessages[i];
                yield return StartCoroutine(CountdownEffectRoutine(text));
            }
            gameStarted = true;
            foreach (PumpkinPlungeSubmanager subscene in subscenes)
            {
                subscene.UpdatePanel();
            }
            yield return new WaitForSeconds(gameCountdownCompleteDelay);
            ButtonPanel.canUsePanel = true;
            StartCoroutine(TimerRoutine());
        }

        IEnumerator TimerRoutine()
        {
            float timeLeft = timer;
            Image timerImage = timerFrame.GetComponent<Image>();
            timerFrame.SetActive(true);
            while (timeLeft > 0)
            {
                currentTime = Mathf.CeilToInt(timeLeft);
                timerText.text = currentTime.ToString();
                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
                if (timeLeft <= timerWarning && timerImage.color != timerWarningColor)
                {
                    timerImage.color = timerWarningColor;
                }
            }
            timerText.text = "0";
            StartCoroutine(EndRoutine());
        }
        
        IEnumerator EndRoutine()
        {
            ButtonPanel.canUsePanel = false;
            timerFrame.SetActive(false);
            foreach (PumpkinPlungeSubmanager subscene in subscenes) {
                subscene.TriggerEnd();
            }
            StartCoroutine(CountdownEffectRoutine("TIME!", true));
            yield return new WaitForSeconds(resultsFinishDelay);

            // Get scores
            List<int> scores = new();
            foreach (PumpkinPlungeSubmanager subscene in subscenes) {
                scores.Add(subscene.GetAndDisplayScore());
            }
            yield return new WaitForSeconds(6f);

            // End minigame
            MinigameManager.Ranking ranking = new();
            ranking.DetermineRankingFromScores(scores);
            MinigameManager.instance.EndMinigame(ranking);
        }
    }
}