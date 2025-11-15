using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Examples;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Starter.SubsceneSplitscreen {
    /// <summary>
    /// Manages minigame state across separate individual subscenes for each player.
    /// Communicates with each StarterSubmanager.
    /// </summary>
    public class StarterGameManager : MonoBehaviour {
        [HideInInspector] public List<StarterSubmanager> subscenes = new();

        [Header("Game Parameters")]
        public int countdownTimer = 3;
        public float timer = 30;
        public int trapdoorCreateCount = 30;
        public List<TrapdoorType> trapdoorTypes;
        public List<Color> resultsWeightColors;
        [Header("References")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private GameObject timerFrame;
        [SerializeField] private TextMeshProUGUI timerText;

        public static StarterGameManager instance;
        public static int currentTime;
        private List<TrapdoorType> trapdoorLayout;

        private static Color COUNTDOWN_COLOR_START = Color.white;
        private static Color COUNTDOWN_COLOR_END =
            new Color(COUNTDOWN_COLOR_START.r, COUNTDOWN_COLOR_START.g, COUNTDOWN_COLOR_START.b, 0);
        private static float COUNTDOWN_START_FONT_SIZE = 100;
        private static float COUNTDOWN_END_FONT_SIZE = 300;
        private static float COUNTDOWN_TIME = 1f;

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
            float alpha = 0;
            countdownText.text = text;
            countdownText.ForceMeshUpdate();
            while (alpha < 1)
            {
                alpha = timer / COUNTDOWN_TIME;
                countdownText.fontSize = Mathf.Lerp(COUNTDOWN_START_FONT_SIZE, COUNTDOWN_END_FONT_SIZE, alpha);
                countdownText.color = Color.Lerp(COUNTDOWN_COLOR_START, COUNTDOWN_COLOR_END, alpha);
                timer += Time.deltaTime;
                yield return null;
            }
            if (hideOnFinish)
            {
                countdownText.gameObject.SetActive(false);
            }
            else
            {
                countdownText.fontSize = COUNTDOWN_END_FONT_SIZE;
                countdownText.color = COUNTDOWN_COLOR_END;
                countdownText.text = text;
                countdownText.ForceMeshUpdate();
            }
        }

        IEnumerator StartRoutine()
        {
            ButtonPanel.canUsePanel = false;
            timerFrame.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            // for (int i = countdownTimer; i > 0; i--)
            // {
            //     string text = i.ToString();
            //     yield return StartCoroutine(CountdownEffectRoutine(text));
            // }
            StartCoroutine(CountdownEffectRoutine("PLUNGE!", true));
            yield return new WaitForSeconds(0.25f);

            ButtonPanel.canUsePanel = true;
            StartCoroutine(TimerRoutine());
        }

        IEnumerator TimerRoutine()
        {
            float timeLeft = timer;
            timerFrame.SetActive(true);
            while (timeLeft > 0)
            {
                currentTime = Mathf.CeilToInt(timeLeft);
                timerText.text = currentTime.ToString();
                yield return new WaitForSeconds(1f);
                timeLeft -= 1f;
            }
            timerText.text = "0";
            StartCoroutine(EndRoutine());
        }
        
        IEnumerator EndRoutine()
        {
            ButtonPanel.canUsePanel = false;
            timerFrame.SetActive(false);
            yield return new WaitForSeconds(2f);

            // Get scores
            List<int> scores = new();
            foreach (StarterSubmanager subscene in subscenes) {
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