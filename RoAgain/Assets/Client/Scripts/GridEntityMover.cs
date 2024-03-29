using OwlLogging;
using System.IO;
using UnityEngine;
using UnityEngine.AI;

namespace Client
{
    public class GridEntityMover : MonoBehaviour
    {
        private GridEntity _entity;
        private int _currentTargetCornerIndex = -1;
        private NavMeshAgent _nmAgent;
        private GridComponent _grid;

        [SerializeField]
        private Transform _modelAnchor;
        private GameObject _model;
        public GameObject Model => _model;

        public void Initialize(GridEntity entity, GridComponent grid)
        {
            if (entity == null)
            {
                OwlLogger.LogError("Can't initialize GridEntityMover with null entity", GameComponent.Other);
                return;
            }

            if (grid == null)
            {
                OwlLogger.LogError($"Can't initialize GridEntityMover with null grid", GameComponent.Other);
                return;
            }

            _entity = entity;
            _grid = grid;
            _nmAgent = GetComponent<NavMeshAgent>();
            if (_nmAgent == null)
            {
                OwlLogger.LogError($"EntityMover can't find NavMeshMover!", GameComponent.Other);
                return;
            }
            _nmAgent.autoBraking = false;
            _nmAgent.acceleration = float.MaxValue;

            _entity.PathUpdated += OnEntityPathUpdated;
            _entity.PathFinished += OnEntityPathFinished;

            GameObject prefab = GetPrefabForEntity();
            if(prefab == null)
            {
                OwlLogger.LogError($"Can't find prefab for entity {_entity.Id} when creating Mover!", GameComponent.Other);
            }
            else
            {
                _model = Instantiate(prefab, _modelAnchor);
            }

            UpdateMovementSpeed();
            SnapToCoordinates(_entity.Coordinates);
            OnEntityPathUpdated(_entity, null, _entity.Path);
        }

        private GameObject GetPrefabForEntity()
        {
            switch(_entity)
            {
                case ACharacterEntity rChar:
                    return GetCharacterModelPrefab(rChar);
                case ClientBattleEntity bEntity:
                    return GetMobModelPrefab(bEntity);
                case GridEntity gEntity:
                    return GetGenericModelPrefab(gEntity);
                default:
                    return null;
            }
        }

        private GameObject GetCharacterModelPrefab(ACharacterEntity character)
        {
            JobTableEntry jobData = JobTable.GetDataById(character.JobId);
            if (jobData == null)
                return null;

            return jobData.ModelPrefab;
        }

        private GameObject GetMobModelPrefab(ClientBattleEntity bEntity)
        {
            return ModelTable.GetPrefabForType(bEntity.ModelId);
        }

        private GameObject GetGenericModelPrefab(GridEntity gEntity)
        {
            return ModelTable.GetPrefabForType(gEntity.ModelId);
        }

        // Update is called once per frame
        void Update()
        {
            if (_entity == null || _nmAgent == null || _grid == null)
                return;

            UpdateMovementSpeed();
            UpdatePathing();
            // for debugging, causes MouseCursor-desync
            //SnapToCoordinates(_entity.Coordinates);
        }

        private void UpdateMovementSpeed()
        {
            float requiredVelocity = _grid.CellSize * _entity.Movespeed.Value;   
            _nmAgent.speed = requiredVelocity;
            // Other nmAgent values to modify?
        }

        private void UpdatePathing()
        {
            if (_entity.HasFinishedPath())
            {
                if (_currentTargetCornerIndex > -1)
                    Reset();
                return;
            }

            bool shouldStop = _entity is ClientBattleEntity bEntity && bEntity.IsCasting();
            _nmAgent.isStopped = shouldStop;
            if (shouldStop)
                return;

            if (_entity.Path == null || _entity.Path.Corners.Count == 0 || _currentTargetCornerIndex == -1)
                return;

            if (_entity.Coordinates == _entity.Path.Corners[_currentTargetCornerIndex])
            {
                if(_currentTargetCornerIndex < _entity.Path.Corners.Count-1)
                {
                    // More corners
                    SetCornerIndexTarget(_currentTargetCornerIndex + 1);
                }
            }
        }

        public void SnapToCoordinates(Vector2Int newCoords)
        {
            Vector3 target = _grid.CoordsToWorldPosition(_entity.Coordinates);
            //gameObject.transform.position = target;
            _nmAgent.Warp(target);
        }

        private void SetCornerIndexTarget(int index)
        {
            if (index < 0 || index >= _entity.Path.Corners.Count)
            {
                OwlLogger.LogError($"EntityMover for {_entity.Id} can't move to invalid corner idx {index}!", GameComponent.Other);
                return;
            }

            _currentTargetCornerIndex = index;
            OwlLogger.Log($"EntityMover for {_entity.Id} starts moving to Corner idx {_currentTargetCornerIndex}: {_entity.Path.Corners[_currentTargetCornerIndex]}", GameComponent.Other, LogSeverity.VeryVerbose);
            Vector3 target = _grid.CoordsToWorldPosition(_entity.Path.Corners[_currentTargetCornerIndex]);
            _nmAgent.destination = target;
        }

        private void Reset()
        {
            _currentTargetCornerIndex = -1;
        }

        private void OnEntityPathUpdated(GridEntity entity, GridData.Path oldPath, GridData.Path newPath)
        {
            if(entity != _entity || newPath != _entity.Path)
            {
                OwlLogger.LogError($"GridEntityMover received EntityPathUpdated for different entity/path!", GameComponent.Other);
                return;
            }

            Reset();

            if (_entity.Path == null
                || _entity.Path.AllCells.Count == 0
                || _entity.HasFinishedPath())
            {
                // REceived path is already done = a stationary update
                // make sure position matches
                _currentTargetCornerIndex = -1;
                _nmAgent.Warp(_grid.CoordsToWorldPosition(_entity.Coordinates));
                return;
            }

            int nextCornerIndex = 0;
            for (int i = 0; i <= _entity.PathCellIndex; i++)
            {
                Vector2Int cell = _entity.Path.AllCells[i];
                if (cell == _entity.Path.Corners[nextCornerIndex])
                {
                    nextCornerIndex++;
                }
            }
            if (nextCornerIndex < _entity.Path.Corners.Count)
            {
                SetCornerIndexTarget(nextCornerIndex);
            }
        }

        private void OnEntityPathFinished(GridEntity entity)
        {
            if (entity != _entity)
            {
                OwlLogger.LogError($"GridEntityMover received EntityPathFinished for different entity!", GameComponent.Other);
                return;
            }

            Reset();
            // Align to updated unit position when a path starts or finishes
            _nmAgent.Warp(_grid.CoordsToWorldPosition(_entity.Coordinates));
        }

        public int Shutdown()
        {
            if(_entity != null)
            {
                _entity.PathUpdated -= OnEntityPathUpdated;
                _entity.PathFinished -= OnEntityPathFinished;
            }

            _entity = null;
            _grid = null;
            _nmAgent = null;
            _currentTargetCornerIndex = -1;
            return 0;
        }
    }
}
