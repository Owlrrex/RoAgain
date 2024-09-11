using OwlLogging;
using Shared;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

namespace Client
{
    public class GridEntityMover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private GridEntity _entity;
        private int _currentTargetCornerIndex = -1;
        private NavMeshAgent _nmAgent;
        private GridComponent _grid;

        [SerializeField]
        private Transform _modelAnchor;
        private GameObject _model;
        public GameObject Model => _model;

        [SerializeField]
        protected TMP_Text _entityNameText;

        [SerializeField]
        protected bool _showOnHover;

        private LocalizedStringText _entityNameLocText;

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

            if (_entityNameText == null)
            {
                OwlLogger.LogWarning($"BattleEntityModelMain has no unitNameText!", GameComponent.UI);
            }
            else
            {
                _entityNameText.canvas.worldCamera = PlayerMain.Instance.WorldUiCamera;
            }

            if(_entityNameText != null)
                _entityNameText.TryGetComponent(out _entityNameLocText);

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

            switch (_entity)
            {
                case ACharacterEntity rChar:
                    SetupForCharacterEntity(rChar);
                    break;
                case ClientBattleEntity bEntity:
                    SetupForBattleEntity(bEntity);
                    break;
                case GridEntity gEntity:
                    SetupForGridEntity(gEntity);
                    break;
                default:
                    OwlLogger.LogError($"Unrecognized entity type for entity {_entity.Id} when creating Mover!", GameComponent.Other);
                    return;
            }

            UpdateMovementSpeed();
            SnapToCoordinates(_entity.Coordinates);
            OnEntityPathUpdated(_entity, null, _entity.Path);

            if (_showOnHover)
                OnPointerExit(null);
        }

        private void SetupForCharacterEntity(ACharacterEntity cEntity)
        {
            GameObject prefab = GetCharacterModelPrefab(cEntity);
            if (prefab == null)
                return;
            _model = Instantiate(prefab, _modelAnchor);
            if(_model.TryGetComponent(out CursorModifierComponent modifierComponent))
            {
                // It's possible to freely attack players atm, but this won't stay, so we don't give the attack-cursor
                // TODO: Check PvP state to give Attack-cursor, maybe also check shop for "shop"-cursor
                modifierComponent.TargetType = CursorChanger.HoverTargetType.Normal;
            }
        }

        private void SetupForBattleEntity(ClientBattleEntity bEntity)
        {
            GameObject prefab = GetMobModelPrefab(bEntity);
            if (prefab == null)
                return;
            _model = Instantiate(prefab, _modelAnchor);
            if (_model.TryGetComponent(out CursorModifierComponent modifierComponent))
            {
                // Currently, we assume a non-character battleEntity to be a mob = attackable.
                // That will change eventually
                modifierComponent.TargetType = CursorChanger.HoverTargetType.Attack;
            }
        }

        private void SetupForGridEntity(GridEntity gEntity)
        {
            GameObject prefab = GetGenericModelPrefab(gEntity);
            if (prefab == null)
                return;
            _model = Instantiate(prefab, _modelAnchor);
            if (_model.TryGetComponent(out CursorModifierComponent modifierComponent))
            {
                // Currently, we assume a non-battle grid-entity to be an NPC = talkable
                // This will change eventually
                modifierComponent.TargetType = CursorChanger.HoverTargetType.Attack;
            }
        }

        private GameObject GetCharacterModelPrefab(ACharacterEntity character)
        {
            JobTableData jobData = JobTable.GetDataForId(character.JobId);
            return jobData?.ModelPrefab;
        }

        private GameObject GetMobModelPrefab(ClientBattleEntity bEntity)
        {
            return ModelTable.GetDataForId(bEntity.ModelId)?.Prefab;
        }

        private GameObject GetGenericModelPrefab(GridEntity gEntity)
        {
            return ModelTable.GetDataForId(gEntity.ModelId)?.Prefab;
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

            if (_entityNameText != null)
            {
                if (_entity.NameOverride != null)
                {
                    _entityNameText.text = _entity.NameOverride;
                    if(_entityNameLocText != null)
                    {
                        _entityNameLocText.SetLocalizedString(LocalizedStringId.INVALID);
                    }
                }
                else if (_entity.LocalizedNameId != LocalizedStringId.INVALID
                    && _entityNameLocText != null)
                {
                    _entityNameLocText.SetLocalizedString(_entity.LocalizedNameId);
                }
            }
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_showOnHover)
                return;

            _entityNameText.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_showOnHover)
                return;

            _entityNameText.gameObject.SetActive(false);
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
