using OwlLogging;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Client
{
    public class GridComponent : MonoBehaviour
    {
        public GridData Data { get; private set; }

        public int CellSize = 1;

#if UNITY_EDITOR
        public List<Vector3> GizmoDots { get; private set; } = new();

        public string EditorMapId;
        public Vector2Int EditorBounds;
        public float GridVisualizationHeight = 0.0f;

        public static float CellWalkableCheckRadius = 0.1f;

        private Vector3[] _gridLineSegments;
        private Vector2Int _lastEditorBounds;
        private float _lastVisHeight;
        private float _lastCellSize;
        private Vector3 _lastWorldPos;
        private Quaternion _lastWorldRot;
        private Vector3 _lastWorldSize;

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

            Vector3 horiLineEndOffset = new(EditorBounds.x * CellSize, 0, 0);
            Vector3 vertLineEndOffset = new(0, 0, EditorBounds.y * CellSize);
            int gridLineSegmentIndex = 0;
            _gridLineSegments = new Vector3[(EditorBounds.x + EditorBounds.y + 2) * 2];

            // Build points in local space
            for (int x = 0; x < EditorBounds.x; x++)
            {
                Vector3 start = new(x * CellSize, GridVisualizationHeight, 0);
                Vector3 end = start + vertLineEndOffset;
                _gridLineSegments[gridLineSegmentIndex++] = start;
                _gridLineSegments[gridLineSegmentIndex++] = end;
            }

            for (int y = 0; y < EditorBounds.y; y++)
            {
                Vector3 start = new(0, GridVisualizationHeight, y * CellSize);
                Vector3 end = start + horiLineEndOffset;
                _gridLineSegments[gridLineSegmentIndex++] = start;
                _gridLineSegments[gridLineSegmentIndex++] = end;
            }

            // Boundary Line: last hori
            Vector3 horiStart = new(0, GridVisualizationHeight, EditorBounds.y * CellSize);
            Vector3 horiEnd = horiStart + horiLineEndOffset;
            _gridLineSegments[gridLineSegmentIndex++] = horiStart;
            _gridLineSegments[gridLineSegmentIndex++] = horiEnd;

            // Boundary Line: last vert
            Vector3 vertStart = new(EditorBounds.x * CellSize, GridVisualizationHeight, 0);
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
            Data ??= new();

            Data = GridData.LoadMapFromFiles(mapId);
            if(Data == null)
            {
                OwlLogger.LogError($"Initialization of GridComponent for map {mapId} failed!", GameComponent.Grid);
                return -1;
            }
            return 0;
        }

        public Vector2Int FreePosToGridCoords(Vector3 freePos)
        {
            Vector3 localFreePos = transform.worldToLocalMatrix.MultiplyPoint(freePos);
            Vector2Int candidateCoords = Vector2Int.zero;
            candidateCoords.x = Mathf.CeilToInt(localFreePos.x / CellSize);
            candidateCoords.y = Mathf.CeilToInt(localFreePos.z / CellSize);

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

        public Vector3 CoordsToWorldPosition(Vector2Int coords)
        {
            if (!Data.AreCoordinatesValid(coords))
                return Vector3.negativeInfinity;

            float halfCell = CellSize / 2.0f;

            Vector3 position = Vector3.zero;
            position.x = CellSize * (coords.x - 1) + halfCell;
            position.z = CellSize * (coords.y - 1) + halfCell;
            position.y = Data.GetDataAtCoords(coords).CellHeight;

            return transform.localToWorldMatrix.MultiplyPoint(position);
        }

        public Vector3 SnapPositionToGrid(Vector3 freePos)
        {
            Vector2Int coords = FreePosToGridCoords(freePos);
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
    }
}

