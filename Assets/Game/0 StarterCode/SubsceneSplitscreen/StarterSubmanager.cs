using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.iOS;

namespace Starter.SubsceneSplitscreen {
    /// <summary>
    /// Manages the gameplay for a single player's subscene in a splitscreen minigame.
    /// Communicates with StarterGameManager.
    /// </summary>
    public class StarterSubmanager : MonoBehaviour {
        public int playerIndex { get; private set; }
        [SerializeField] List<Material> materials = new();
        
        [Header("References")]
        [SerializeField] private StarterPawn player;
        [SerializeField] private GameObject trapdoorPrefab;
        [SerializeField] private List<GameObject> sideWalls;
        [SerializeField] private GameObject roof;
        [SerializeField] private new GameObject camera;
        public TextMeshProUGUI resultsText;
        public TextMeshProUGUI earlyFinishText;

        private Queue<Trapdoor> trapdoors;
        private int trapdoorStartingCount;
        private int trapdoorsOpened;
        private float trapdoorTotalHeight;
        private bool updateCameraPosition;
        private Vector3 targetCameraPosition;
        private Vector3 cameraVelocity;
        private int finishedAt = -1;

        // Constants
        private static float TRAPDOOR_SPACING = 5;
        private static float ROOF_OFFSET = 0;
        private static float CAMERA_OFFSET_Y = -5;
        private static float CAMERA_VELOCITY_Y = 10;
        private static float CAMERA_MOVE_TIME = 1f;
        private static float CAMERA_STOP_MARGIN = 0.01f;
        private static float SCORE_ANIMATE_DELAY = 0.05f;
        private static float SCORE_EXTRA_DELAY = 0.5f;

        private void Awake()
        {
            playerIndex = StarterGameManager.instance.subscenes.Count;
            StarterGameManager.instance.subscenes.Add(this);
        }

        void Update()
        {
            if (updateCameraPosition)
            {
                // Push camera to target
                Vector3 startPosition = camera.transform.position;
                Vector3 newPosition = Vector3.SmoothDamp(
                    startPosition, targetCameraPosition, ref cameraVelocity, CAMERA_MOVE_TIME
                );
                camera.transform.position = newPosition;
                if ((newPosition - startPosition).magnitude <= CAMERA_STOP_MARGIN)
                {
                    updateCameraPosition = false;
                }
            }
        }

        private Vector3 GetCameraPosition()
        {
            return new(
                camera.transform.position.x,
                trapdoorTotalHeight - ((StarterGameManager.instance.trapdoorCreateCount - trapdoors.Count) * TRAPDOOR_SPACING) + CAMERA_OFFSET_Y,
                camera.transform.position.z
            );
        }

        private void UpdateCamera() {
            cameraVelocity = -CAMERA_VELOCITY_Y * Vector3.up;
            targetCameraPosition = GetCameraPosition();
            updateCameraPosition = true;
        }

        public bool TryOpenTrapdoor(TrapdoorType openType)
        {
            if (trapdoors.Count == 0)
            {
                // No more trapdoors
                return false;
            }
            // Open first trapdoor if matches
            if (trapdoors.Peek().type == openType)
            {
                trapdoorsOpened++;
                Trapdoor opening = trapdoors.Dequeue();
                opening.OpenLatch();
                UpdateCamera();
                if (trapdoors.Count == 0)
                {
                    // No more trapdoors, store time finished
                    finishedAt = StarterGameManager.currentTime;
                }
                return true;
            }
            return false;
        }

        private void Start() {
            player.SetMaterial(materials[playerIndex]);

            // Create all trapdoors
            trapdoors = new();
            trapdoorsOpened = 0;
            List<TrapdoorType> trapdoorLayout = StarterGameManager.instance.GetTrapdoorLayout();
            trapdoorStartingCount = trapdoorLayout.Count;
            trapdoorTotalHeight = trapdoorStartingCount * TRAPDOOR_SPACING;
            for (int i = 0; i < trapdoorStartingCount; i++)
            {
                GameObject trapdoorObject = Instantiate(trapdoorPrefab);
                Trapdoor trapdoor = trapdoorObject.GetComponent<Trapdoor>();
                Rigidbody trapdoorBody = trapdoor.latch.GetComponent<Rigidbody>();
                trapdoorBody.constraints = RigidbodyConstraints.FreezePosition;
                float atHeight = trapdoorTotalHeight - ((i + 1) * TRAPDOOR_SPACING);
                trapdoorObject.transform.position = new(
                    trapdoorObject.transform.position.x,
                    atHeight,
                    trapdoorObject.transform.position.z
                );
                trapdoorBody.constraints = RigidbodyConstraints.None;
                trapdoor.type = trapdoorLayout[i];
                trapdoors.Enqueue(trapdoor);

                // Have trapdoor ignore walls so it doesn't get stuck
                foreach (GameObject sideWall in sideWalls)
                {
                    Physics.IgnoreCollision(trapdoor.latch.GetComponent<Collider>(), sideWall.GetComponent<Collider>());
                }
            }

            // Generate walls based on trapdoors
            foreach (GameObject sideWall in sideWalls)
            {
                sideWall.transform.localScale = new(
                    sideWall.transform.localScale.x,
                    sideWall.transform.localScale.y + trapdoorTotalHeight + ROOF_OFFSET,
                    sideWall.transform.localScale.z
                );
                sideWall.transform.position = new(
                    sideWall.transform.position.x,
                    sideWall.transform.position.y + ((trapdoorTotalHeight + ROOF_OFFSET) * 0.5f),
                    sideWall.transform.position.z
                );
            }
            roof.transform.position = new(
                roof.transform.position.x,
                trapdoorTotalHeight + ROOF_OFFSET + (roof.transform.localScale.y * 0.5f),
                roof.transform.position.z
            );

            // Position player + camera by first trapdoor
            Trapdoor firstTrapdoor = trapdoors.Peek();
            player.transform.position = new(
                firstTrapdoor.latch.transform.position.x,
                firstTrapdoor.transform.position.y + (firstTrapdoor.latch.transform.localScale.y * 0.5f)
                + (player.transform.localScale.y * 0.5f),
                firstTrapdoor.transform.position.z
            );
            player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionZ;
            camera.transform.position = GetCameraPosition();
        }

        private Color GetScoreWeightColor(float weight)
        {
            List<Color> weightedColors = StarterGameManager.instance.resultsWeightColors;
            return weightedColors[(int)Mathf.Floor(Mathf.Lerp(0, weightedColors.Count - 1, weight))];
        }

        private IEnumerator AnimateDropDisplay(int dropped, int extra)
        {
            int n = 0;
            int trapdoorCount = trapdoorStartingCount;
            while (n <= dropped)
            {
                resultsText.text = $"{n}/{trapdoorCount}";
                resultsText.color = GetScoreWeightColor(((float)n) / trapdoorCount);
                n += 1;
                yield return new WaitForSeconds(SCORE_ANIMATE_DELAY);
            }
            if (extra > 0)
            {
                yield return new WaitForSeconds(SCORE_EXTRA_DELAY);
                earlyFinishText.text = $"+{extra} Early Finish!";
                earlyFinishText.color = GetScoreWeightColor(1);
            }
        }
        
        public int GetAndDisplayScore() {
            // Return player score
            int rawScore = trapdoorsOpened;
            int extra = finishedAt < 0 ? 0 : Math.Max(0, finishedAt);
            StartCoroutine(AnimateDropDisplay(rawScore, extra));
            return rawScore + extra;
        }
    }
}