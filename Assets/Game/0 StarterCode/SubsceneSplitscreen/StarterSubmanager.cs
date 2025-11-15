using System.Collections.Generic;
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

        [Header("Subscene Parameters")]
        [SerializeField] private int trapdoorCreateCount = 30;
        [SerializeField] public List<TrapdoorType> trapdoorTypes;

        [Header("References")]
        [SerializeField] private StarterPawn player;
        [SerializeField] private GameObject trapdoorPrefab;
        [SerializeField] private List<GameObject> sideWalls;
        [SerializeField] private GameObject roof;
        [SerializeField] private Canvas buttonCanvas;
        [SerializeField] private GameObject buttonPanel;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private new GameObject camera;

        private Queue<Trapdoor> trapdoors;
        private int trapdoorsOpened => trapdoorCreateCount - trapdoors.Count;
        private float trapdoorTotalHeight;
        private bool updateCameraPosition;
        private Vector3 targetCameraPosition;
        private Vector3 cameraVelocity;

        // Constants
        private static float TRAPDOOR_SPACING = 6;
        private static float ROOF_OFFSET = 0;
        private static float CAMERA_OFFSET_Y = -2;
        private static float CAMERA_VELOCITY_Y = 10;
        private static float CAMERA_MOVE_TIME = 1f;
        private static float CAMERA_STOP_MARGIN = 0.01f;

        private void Awake()
        {
            playerIndex = StarterGameManager.instance.subscenes.Count;
            StarterGameManager.instance.subscenes.Add(this);
        }

        void Update()
        {
            if (updateCameraPosition)
            {
                // Tween camera to target
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

        public bool TryOpenTrapdoor(TrapdoorType openType)
        {
            if (trapdoors.Count == 0)
            {
                // No more trapdoors
                return false;
            }
            // Open first trapdoor if matches
            if (1 + 1 == 2)//trapdoors.Peek().type == openType)
            {
                Trapdoor opening = trapdoors.Dequeue();
                opening.OpenLatch();
                cameraVelocity = -CAMERA_VELOCITY_Y * Vector3.up;
                targetCameraPosition = GetCameraPosition();
                updateCameraPosition = true;
                return true;
            }
            return false;
        }

        private TrapdoorType PickRandomTrapdoorType()
        {
            return trapdoorTypes[Random.Range(0, trapdoorTypes.Count)];
        }

        private Vector3 GetCameraPosition()
        {
            return new(
                camera.transform.position.x,
                trapdoorTotalHeight - ((trapdoorsOpened + 1) * TRAPDOOR_SPACING) + CAMERA_OFFSET_Y,
                camera.transform.position.z
            );
        }

        private void Start() {
            player.GetComponent<MeshRenderer>().material = materials[playerIndex];

            // Create all trapdoors
            trapdoors = new();
            trapdoorTotalHeight = trapdoorCreateCount * TRAPDOOR_SPACING;
            for (int i = 0; i < trapdoorCreateCount; i++)
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
                trapdoor.type = PickRandomTrapdoorType();
                trapdoors.Enqueue(trapdoor);
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
        
        public int GetScore() {
            // Return player score
            return trapdoorsOpened;
        }
    }
}