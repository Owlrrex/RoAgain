using OwlLogging;
using Server;
using System.Collections.Generic;
using UnityEngine;

// This class manages a single server-side instance of a map
public class ServerMapInstance
{
    public GridData Grid { get; private set; }
    public string MapId { get; private set; }

    public BattleModule BattleModule { get; private set; }

    public MapMobManager MobManager { get; private set; }

    public SkillModule SkillModule { get; private set; }

    private HashSet<GridEntity> _newVisibleEntityBuffer = new();
    private HashSet<GridEntity> _oldVisibleEntityBuffer = new();
    private HashSet<GridEntity> _noLongerVisibleEntityBuffer = new();

    private HashSet<CellEffectGroup> _newVisibleEffectBuffer = new();
    private HashSet<CellEffectGroup> _oldVisibleEffectBuffer = new();
    private HashSet<CellEffectGroup> _noLongerVisibleEffectBuffer = new();


    // This loads the actual map data in the future, I guess?
    public int Initialize(string mapId, ExperienceModule expModule)
    {
        MapId = mapId;
        int loadError = LoadGridFromFile(MapId);
        if (loadError != 0)
            return loadError;

        BattleModule = new();
        BattleModule.Initialize(this);

        MobManager = new();
        MobManager.Initialize(this, expModule);

        SkillModule = new();
        SkillModule.Initialize(this);

        // Pre-place stuff
        if(mapId == "test_map")
        {
            // CellEffect test: A warp somewhere
            GridShape shape = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(8, 30),
                SourceBoundsMax = new(12, 35)
            };
            WarpCellEffectGroup group = new();
            group.Create(Grid, shape, "test_map2", new(5, 6));
        }
        else
        {
            // CellEffect test: A warp somewhere
            GridShape hillToBridge = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(41, 20),
                SourceBoundsMax = new(43, 21)
            };
            WarpCellEffectGroup group = new();
            group.Create(Grid, hillToBridge, "test_map2", new(10, 41));

            GridShape bridgeToHill = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(8, 40),
                SourceBoundsMax = new(9, 42)
            };
            group = new WarpCellEffectGroup();
            group.Create(Grid, bridgeToHill, "test_map2", new(42, 19));

            GridShape bridgeToMap = new RectangleBoundsGridShape()
            {
                IncludeVoid = false,
                SourceBoundsMin = new(46, 40),
                SourceBoundsMax = new(47, 42)
            };
            group = new WarpCellEffectGroup();
            group.Create(Grid, bridgeToMap, "test_map", new(5, 5));
        }

        return loadError;
    }

    private int LoadGridFromFile(string mapid) 
    {
        if (Grid != null)
            DetachFromGrid();

        Grid = GridData.LoadMapFromFiles(mapid);
        if(Grid == null)
        {
            return -1;
        }

        SetupWithGrid();

        return 0;
    }

    private void SetupWithGrid()
    {
        Grid.EntityPlaced += OnEntityPlaced;
        Grid.EntityRemoved += OnEntityRemoved;
    }

    private void DetachFromGrid()
    {
        Grid.EntityPlaced -= OnEntityPlaced;
        Grid.EntityRemoved -= OnEntityRemoved;
    }

    private void OnEntityPlaced(GridEntity entity, Vector2Int coords)
    {
        entity.MapId = MapId;
        if (entity is CharacterRuntimeData data)
        {
            //UpdateVisibleEntities(data);
        }
        // No need to send packet here, will be discovered as part of the VisibleEntities-update automatically
    }

    private void OnEntityRemoved(GridEntity entity, Vector2Int coords)
    {
        EntityRemovedPacket packet = new()
        {
            EntityId = entity.Id,
        };

        foreach(CharacterRuntimeData obs in Grid.GetOccupantsInRangeSquareLowAlloc<CharacterRuntimeData>(coords, GridData.MAX_VISION_RANGE))
        {
            obs.Connection.Send(packet);
        }
    }

    public void Update(float deltaTime)
    {
        UpdateEntityMovementStupid(deltaTime);

        foreach (var entity in Grid.GetAllOccupants())
        {
            if(entity is ServerBattleEntity sbe)
                sbe.Update?.Invoke(sbe, deltaTime);
        }

        SkillModule?.UpdateSkillExecutions(deltaTime);
        BattleModule?.UpdateRegenerations(deltaTime);
        Grid?.UpdateCellEffects(deltaTime);
        MobManager?.UpdateMobSpawns(deltaTime);
    }

    // We do as little clever "filtering" here as reasonable, leave optimization for later
    private void UpdateEntityMovementStupid(float deltaTime)
    {
        List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);
        ICollection<GridEntity> allEntities = Grid.GetAllOccupants();
        List<CharacterRuntimeData> characters = new(); // TODO: Reusable list to reduce allocations
        foreach(GridEntity entity in allEntities)
        {
            if(entity is CharacterRuntimeData)
                characters.Add(entity as CharacterRuntimeData);
        }

        foreach (CharacterRuntimeData character in characters)
        {
            UpdateVisibleEntities(character);
            UpdateVisibleCellGroups(character);
        }

        // Entities in newly placed effects will be handled in AddEffect/UpdateCellEffects
        foreach (GridEntity entity in movedEntities)
        {
            GridCellData oldCell = Grid.GetDataAtCoords(entity.LastUpdateCoordinates);
            GridCellData newCell = Grid.GetDataAtCoords(entity.Coordinates);

            List<CellEffectGroup> oldEffects = oldCell.GetCellEffects();
            List<CellEffectGroup> newEffects = newCell.GetCellEffects();

            List<CellEffectGroup> leftEffects = new();
            List<CellEffectGroup> stayedEffects = new();
            List<CellEffectGroup> enteredEffects = new();
            
            bool[] oldEffectsStayed = new bool[oldEffects.Count];

            for(int i = 0; i < newEffects.Count; i++)
            {
                if (oldEffects.Contains(newEffects[i]))
                {
                    stayedEffects.Add(newEffects[i]);
                    oldEffectsStayed[i] = true;
                }
                else
                    enteredEffects.Add(newEffects[i]);
            }

            for (int i = 0; i < oldEffects.Count; i++)
            {
                if (!oldEffectsStayed[i])
                    leftEffects.Add(oldEffects[i]);
            }

            foreach(CellEffectGroup leftEffect in  leftEffects)
            {
                leftEffect.EntityLeft(entity);
            }

            foreach(CellEffectGroup enteredEffect in enteredEffects)
            {
                enteredEffect.EntityEntered(entity);
            }
        }
    }

    // This function tries to filter beforehand which characters to update visibility for, but it doesn't work well yet
    private void UpdateEntityMovementBrokenFilter(float deltaTime)
    {
        List<GridEntity> movedEntities = Grid.UpdateEntityMovment(deltaTime);
        HashSet<CharacterRuntimeData> charactersToUpdate = new();

        foreach (GridEntity movedEntity in movedEntities)
        {
            charactersToUpdate.UnionWith(Grid.GetOccupantsInRangeSquareLowAlloc<CharacterRuntimeData>(movedEntity.Coordinates, GridData.MAX_VISION_RANGE));
        }

        foreach (CharacterRuntimeData character in charactersToUpdate)
        {
            UpdateVisibleEntities(character);
        }
    }

    private void UpdateVisibleEntities(CharacterRuntimeData character)
    {
        List<GridEntity> newAllVisible = character.RecalculateVisibleEntities(ref _newVisibleEntityBuffer, ref _oldVisibleEntityBuffer, ref _noLongerVisibleEntityBuffer);
        character.VisibleEntities.Clear();
        character.VisibleEntities.AddRange(newAllVisible);

        foreach (GridEntity newEntity in _newVisibleEntityBuffer)
        {
            // For a unit entering update-range (could be first time): Send full data update
            character.NetworkQueue.GridEntityDataUpdate(newEntity);
        }

        foreach (GridEntity entity in _oldVisibleEntityBuffer)
        {
            if (entity.HasNewPath)
            {
                // For unit only updating its path: Send only path update
                character.NetworkQueue.GridEntityPathUpdate(entity);
            }
        }
    }

    private void UpdateVisibleCellGroups(CharacterRuntimeData character)
    {
        character.VisibleCellEffectGroups = character.RecalculateVisibleCellEffectGroups(ref _newVisibleEffectBuffer, ref _oldVisibleEffectBuffer, ref _noLongerVisibleEffectBuffer);

        foreach(CellEffectGroup newGroup in _newVisibleEffectBuffer)
        {
            CellEffectGroupPlacedPacket packet = new()
            {
                GroupId = newGroup.Id,
                Shape = newGroup.Shape,
                Type = newGroup.Type
            };
            character.Connection.Send(packet);
        }

        foreach(CellEffectGroup removedGroup in _noLongerVisibleEffectBuffer)
        {
            CellEffectGroupRemovedPacket packet = new()
            { GroupId = removedGroup.Id };
            character.Connection.Send(packet);
        }
    }

    public void Shutdown()
    {
        BattleModule?.Shutdown();

        MobManager?.Shutdown();

        BattleModule = null;
        MobManager = null;
        Grid = null;
        MapId = null;
    }
}
