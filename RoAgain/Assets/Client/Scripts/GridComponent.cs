using OwlLogging;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Shared;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Client
{
    public class GridComponent : MonoBehaviour
    {
        public GridData Data { get; private set; }

        public int CellSize = 1;

        public string EditorMapId;
#if UNITY_EDITOR
        public List<Vector3> GizmoDots { get; private set; } = new();

        public Coordinate EditorBounds;
        public float GridVisualizationHeight = 0.0f;

        public static float CellWalkableCheckRadius = 0.1f;

        private Vector3[] _gridLineSegments;
        private Coordinate _lastEditorBounds;
        private float _lastVisHeight;
        private float _lastCellSize;
        private Vector3 _lastWorldPos;
        private Quaternion _lastWorldRot;
        private Vector3 _lastWorldSize;

        private static Vector3 _queriedCellCenter;
        private static Vector3 _cellQueryMarkerSize;

        private void BuildGridLineSegments()
        {
            if (_gridLineSegments != null
                && _lastWorldPos == transform.position
                && _lastWorldRot == transform.rotation
                && _lastWorldSize == transform.lossyScale
                && _lastEditorBounds == EditorBounds
                && _lastVisHeight == GridVisualizationHeight
                && _lastCellSize == CellSize)
            {
                return;
            }

            _lastWorldPos = transform.position;
            _lastWorldRot = transform.rotation;
            _lastWorldSize = transform.lossyScale;
            _lastEditorBounds = EditorBounds;
            _lastVisHeight = GridVisualizationHeight;
            _lastCellSize = CellSize;

            Vector3 horiLineEndOffset = new(EditorBounds.X * CellSize, 0, 0);
            Vector3 vertLineEndOffset = new(0, 0, EditorBounds.Y * CellSize);
            int gridLineSegmentIndex = 0;
            _gridLineSegments = new Vector3[(EditorBounds.X + EditorBounds.Y + 2) * 2];

            // Build points in local space
            for (int x = 0; x < EditorBounds.X; x++)
            {
                Vector3 start = new(x * CellSize, GridVisualizationHeight, 0);
                Vector3 end = start + vertLineEndOffset;
                _gridLineSegments[gridLineSegmentIndex++] = start;
                _gridLineSegments[gridLineSegmentIndex++] = end;
            }

            for (int y = 0; y < EditorBounds.Y; y++)
            {
                Vector3 start = new(0, GridVisualizationHeight, y * CellSize);
                Vector3 end = start + horiLineEndOffset;
                _gridLineSegments[gridLineSegmentIndex++] = start;
                _gridLineSegments[gridLineSegmentIndex++] = end;
            }

            // Boundary Line: last hori
            Vector3 horiStart = new(0, GridVisualizationHeight, EditorBounds.Y * CellSize);
            Vector3 horiEnd = horiStart + horiLineEndOffset;
            _gridLineSegments[gridLineSegmentIndex++] = horiStart;
            _gridLineSegments[gridLineSegmentIndex++] = horiEnd;

            // Boundary Line: last vert
            Vector3 vertStart = new(EditorBounds.X * CellSize, GridVisualizationHeight, 0);
            Vector3 vertEnd = vertStart + vertLineEndOffset;
            _gridLineSegments[gridLineSegmentIndex++] = vertStart;
            _gridLineSegments[gridLineSegmentIndex++] = vertEnd;

            // transform into world space
            for (int i = 0; i < _gridLineSegments.Length; i++)
            {
                if (_gridLineSegments[i] != null )
                    _gridLineSegments[i] = transform.TransformPoint(_gridLineSegments[i]);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 dot in GizmoDots)
            {
                Gizmos.DrawWireSphere(dot, 0.1f);
            }

            BuildGridLineSegments();

            if (_gridLineSegments != null
                && _gridLineSegments.Length > 0
                && _gridLineSegments.Length % 2 == 0)
            {
                Color prev = Handles.color;
                Handles.color = Color.black;
                Handles.DrawLines(_gridLineSegments);
                Handles.color = prev;
            }

            if(_queriedCellCenter != Vector3.zero)
            {
                Color prev = Handles.color;
                Handles.color = Color.red;
                Handles.DrawWireCube(_queriedCellCenter, _cellQueryMarkerSize);
                Handles.color = prev;
            }
        }

        [MenuItem("Map/Check Coordinates _g")]
        private static void ScheduleCheckGridCoordinates()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void CheckGridCoordinates()
        {
            GridComponent gridComponent = null;
            PrefabStage pStage = PrefabStageUtility.GetCurrentPrefabStage();
            Scene scene = SceneManager.GetActiveScene();
            if (pStage != null)
            {
                scene = pStage.scene;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GridComponent candidate = root.GetComponentInChildren<GridComponent>();
                if (candidate != null)
                {
                    if (gridComponent != null)
                    {
                        OwlLogger.LogError("Multiple GridComponents found in Scene - Coodinate tracking may not work!", GameComponent.Editor);
                        break;
                    }
                    gridComponent = candidate;
                }
            }

            if(gridComponent == null)
            {
                OwlLogger.LogError("Can't check Coordinates without a GridComponent in the scene!", GameComponent.Editor);
                return;
            }

            if(string.IsNullOrEmpty(gridComponent.EditorMapId))
            {
                OwlLogger.LogWarning("The EditorMapId in GridComponent has to be set to perform Coordinate-check!", GameComponent.Editor);
                return;
            }

            if (gridComponent.Data == null)
            {
                gridComponent.Initialize(gridComponent.EditorMapId);
            }

            Vector2 mousePos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            //Debug.Log($"{mousePos}");
            RaycastHit hit;
            PhysicsScene pScene = scene.GetPhysicsScene();
            if(!pScene.Raycast(ray.origin, ray.direction, out hit, float.PositiveInfinity, LayerMask.GetMask(new string[] { "ClickableTerrain" })))
                return;

            Coordinate coordinates = gridComponent.FreePosToGridCoords(hit.point);
            if (coordinates == GridData.INVALID_COORDS)
            {
                Debug.LogWarning("Hovered coordinates: Not in Map");
                _queriedCellCenter = Vector3.zero;
                return;
            }

            Vector3 center = gridComponent.CoordsToWorldPosition(coordinates);
            Debug.LogWarning("Hovered coordinates: " + coordinates);
            _queriedCellCenter = center;
            _cellQueryMarkerSize.x = gridComponent.CellSize;
            _cellQueryMarkerSize.y = 0;
            _cellQueryMarkerSize.z = gridComponent.CellSize;
            gridComponent.GridVisualizationHeight = center.y;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            CheckGridCoordinates();
        }
#endif

        // Start is called before the first frame update
        void Start()
        {
            if (CellSize <= 0)
            {
                OwlLogger.LogError("Grid CellSize must be a positive number!", GameComponent.Grid);
                CellSize = 1;
            }

            // tmp
            //Initialize(DebugBounds);
            //GridData.Path path = GridData.FindPath(new(10, 10), new(5, 1));
            //foreach (Vector2Int finalPathCoord in path.AllCells)
            //{
            //    Instantiate(FinalPathIndicator, CoordsToWorldPosition(finalPathCoord), Quaternion.identity);
            //}
        }

        public int Initialize(string mapId)
        {
            Data = GridData.LoadMapFromFiles(mapId);
            if(Data == null)
            {
                OwlLogger.LogError($"Initialization of GridComponent for map {mapId} failed!", GameComponent.Grid);
                return -1;
            }
            return 0;
        }

        public bool IsInitialized()
        {
            return Data != null;
        }

        public Coordinate FreePosToGridCoords(Vector3 freePos)
        {
            Vector3 localFreePos = transform.worldToLocalMatrix.MultiplyPoint(freePos);
            Coordinate candidateCoords = new(
                Mathf.CeilToInt(localFreePos.x / CellSize),
                Mathf.CeilToInt(localFreePos.z / CellSize)
            );

            if (!Data.AreCoordinatesValid(candidateCoords))
            {
                return GridData.INVALID_COORDS;
            }

            if (Data.GetDataAtCoords(candidateCoords).IsVoidCell())
            {
                return GridData.INVALID_COORDS;
            }

            return candidateCoords;
        }

        public Vector3 CoordsToWorldPosition(Coordinate coords)
        {
            if (!Data.AreCoordinatesValid(coords))
                return Vector3.negativeInfinity;

            float halfCell = CellSize / 2.0f;

            Vector3 position = Vector3.zero;
            position.x = CellSize * (coords.X - 1) + halfCell;
            position.z = CellSize * (coords.Y - 1) + halfCell;
            position.y = Data.GetDataAtCoords(coords).CellHeight;

            return transform.localToWorldMatrix.MultiplyPoint(position);
        }

        public Vector3 SnapPositionToGrid(Vector3 freePos)
        {
            Coordinate coords = FreePosToGridCoords(freePos);
            if (coords == GridData.INVALID_COORDS)
            {
                return Vector3.negativeInfinity;
            }

            Vector3 snappedPos = CoordsToWorldPosition(coords);
            if (snappedPos == Vector3.negativeInfinity)
            {
                return Vector3.negativeInfinity;
            }

            return snappedPos;
        }

        public Quaternion GridDirectionToWorldRotation(GridData.Direction direction)
        {
            if (direction == GridData.Direction.Unknown)
                return Quaternion.identity;

            Vector3 horiLookDirection = direction switch
            {
                GridData.Direction.North => transform.forward,
                GridData.Direction.NorthEast => transform.forward + transform.right,
                GridData.Direction.East => transform.right,
                GridData.Direction.SouthEast => -transform.forward + transform.right,
                GridData.Direction.South => -transform.forward,
                GridData.Direction.SouthWest => -transform.forward - transform.right,
                GridData.Direction.West => -transform.right,
                GridData.Direction.NorthWest => transform.forward + -transform.right,
                _ => Vector3.zero
            };

            return Quaternion.LookRotation(horiLookDirection, transform.up);
        }
    }
}

