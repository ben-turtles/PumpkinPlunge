using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
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
        [Header("Parameters")]
        [SerializeField] private float trapdoorSpacing = 5;
        [SerializeField] private float roofPositionOffset = 0;
        [SerializeField] private float cameraOffsetY = -3f;
        [SerializeField] private float cameraMoveVelocity = 10;
        [SerializeField] private float cameraMoveTime = 1f;
        [SerializeField] private float scoreResultsIncreaseDelay = 0.05f;
        [SerializeField] private float scoreResultsStartDelay = 0.5f;
        [SerializeField] private float cameraStartZoom = 3;
        [SerializeField] private float cameraStartZoomOutTime = 2.3f;
        [SerializeField] private float cameraEndFocusZoom = 6;
        [SerializeField] private float cameraEndFocusHeightOffset = 0.25f;

        private Queue<Trapdoor> trapdoors;
        private int trapdoorStartingCount;
        private int trapdoorsOpened;
        private float trapdoorTotalHeight;
        private float cameraStartHeight;
        private bool updateCameraPosition;
        private Vector3 targetCameraPosition;
        private Vector3 cameraVelocity;
        private int finishedAt = -1;
        private bool cameraAtEnd;
        private bool cameraDidEndDrop;
        private bool cameraStartedEndDrop;

        private void Awake()
        {
            playerIndex = StarterGameManager.instance.subscenes.Count;
            StarterGameManager.instance.subscenes.Add(this);
        }

        void Update()
        {
            if (cameraAtEnd)
            {
                if (!cameraDidEndDrop && !cameraStartedEndDrop)
                {
                    cameraStartedEndDrop = true;
                    targetCameraPosition = GetCameraPosition(trapdoorsOpened);
                }
                else if (cameraDidEndDrop)
                {
                    targetCameraPosition = player.transform.position +
                        new Vector3(0, cameraEndFocusHeightOffset, -cameraEndFocusZoom);
                }
            }
            if (cameraAtEnd || updateCameraPosition)
            {
                // Push camera to target
                Vector3 startPosition = camera.transform.position;
                Vector3 newPosition = Vector3.SmoothDamp(
                    startPosition, targetCameraPosition, ref cameraVelocity, cameraMoveTime
                );
                camera.transform.position = newPosition;
                if ((newPosition - startPosition).magnitude <= StarterGameManager.STOP_MARGIN)
                {
                    if (cameraStartedEndDrop)
                    {
                        cameraDidEndDrop = true;
                    }
                    else if (!cameraAtEnd)
                    {
                        updateCameraPosition = false;
                    }
                }
            }
        }

        private float GetCameraHeight(int overrideOpened)
        {
            int opened = overrideOpened >= 0 ? overrideOpened : trapdoorsOpened;
            if (opened == 0)
            {
                return cameraStartHeight;
            }
            return cameraStartHeight - (opened * trapdoorSpacing) + cameraOffsetY;
        }

        private Vector3 GetCameraPosition(int overrideOpened = -1)
        {
            return new(camera.transform.position.x, GetCameraHeight(overrideOpened), camera.transform.position.z);
        }

        private void UpdateCamera() {
            cameraVelocity = -cameraMoveVelocity * Vector3.up;
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
                    // No more trapdoors, finished!
                    player.buttonPanel.Hide();
                    finishedAt = StarterGameManager.currentTime;
                    cameraAtEnd = true;
                    cameraDidEndDrop = false;
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
            trapdoorTotalHeight = trapdoorStartingCount * trapdoorSpacing;
            for (int i = 0; i < trapdoorStartingCount; i++)
            {
                GameObject trapdoorObject = Instantiate(trapdoorPrefab);
                Trapdoor trapdoor = trapdoorObject.GetComponent<Trapdoor>();
                Rigidbody trapdoorBody = trapdoor.latch.GetComponent<Rigidbody>();
                trapdoorBody.constraints = RigidbodyConstraints.FreezePosition;
                float atHeight = trapdoorTotalHeight - ((i + 1) * trapdoorSpacing);
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
                    sideWall.transform.localScale.y + trapdoorTotalHeight + roofPositionOffset,
                    sideWall.transform.localScale.z
                );
                sideWall.transform.position = new(
                    sideWall.transform.position.x,
                    sideWall.transform.position.y + ((trapdoorTotalHeight + roofPositionOffset) * 0.5f),
                    sideWall.transform.position.z
                );
            }
            roof.transform.position = new(
                roof.transform.position.x,
                trapdoorTotalHeight + roofPositionOffset + (roof.transform.localScale.y * 0.5f),
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
            cameraStartHeight = player.transform.position.y;
            player.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionZ;
            StartCoroutine(CameraStartRoutine());
        }

        private float CameraStartEasing(float t)
        {
            return Mathf.Pow(t, 2);  
        }

        private IEnumerator CameraStartRoutine()
        {
            Vector3 endPos = GetCameraPosition();
            Quaternion endRotation = camera.transform.rotation;
            Vector3 startPos = player.transform.position + new Vector3(0, 0, -cameraStartZoom);
            camera.transform.position = startPos;
            camera.transform.LookAt(player.transform);
            Quaternion startRotation = camera.transform.rotation;
            yield return new WaitForSeconds(1f);
            float timer = 0f;
            while (timer < cameraStartZoomOutTime)
            {
                float alpha = CameraStartEasing(timer / cameraStartZoomOutTime);
                camera.transform.position = Vector3.Lerp(startPos, endPos, alpha);
                camera.transform.rotation = Quaternion.Lerp(startRotation, endRotation, alpha);
                yield return null;
                timer += Time.deltaTime;
            }
            camera.transform.position = endPos;
            camera.transform.rotation = endRotation;
            player.buttonPanel.Show();
        }

        public void TriggerEnd()
        {
            player.buttonPanel.Hide();
            cameraAtEnd = true;
            cameraStartedEndDrop = true;
            cameraDidEndDrop = true;
        }

        private Color GetScoreWeightColor(float weight)
        {
            List<Color> weightedColors = StarterGameManager.instance.resultsWeightColors;
            float weightLerp = Mathf.Lerp(0, weightedColors.Count - 1, weight);
            int index = Mathf.FloorToInt(weightLerp);
            if (index == 0)
            {
                return weightedColors[0];
            }
            if (index == weightedColors.Count - 1)
            {
                return weightedColors[weightedColors.Count - 1];
            }
            float alpha = weightLerp - index;
            return Color.Lerp(weightedColors[index], weightedColors[index + 1], alpha);
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
                yield return new WaitForSeconds(scoreResultsIncreaseDelay);
            }
            if (extra > 0)
            {
                yield return new WaitForSeconds(scoreResultsStartDelay);
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