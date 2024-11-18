using OwlLogging;
using Shared;

namespace Server
{
    // TODO: Make all these command send command error & Feedback messages to Client
    public class HealChatCommand : AChatCommand
    {
        // Args[1]: [Optional] Heal mode - 0 = both, 1 = hp, 2 = sp, default = 0
        // Args[2]: [Optional] Name of character to heal, default = sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 3))
                return -1;

            int healMode = 0;
            string targetName = sender.NameOverride;

            if(args.Length >= 2)
            {
                if (!int.TryParse(args[1], out healMode))
                {
                    OwlLogger.Log($"Invalid healMode given: {healMode}", GameComponent.ChatCommands);
                    return -2;
                }

                if(args.Length == 3)
                {
                    targetName = args[2];
                }
            }

            // Resolve target name
            CharacterRuntimeData target = sender;
            if (targetName != sender.NameOverride)
            {
                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use HealChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -3;
                }
            }

            // Execute effect
            MapInstance targetMap = target.GetMapInstance();
            if(targetMap == null)
            {
                OwlLogger.LogError($"Couldn't find target map {target.MapId}!", GameComponent.ChatCommands);
                return -4;
            }
            BattleModule battleModule = targetMap.BattleModule;

            switch(healMode)
            {
                case 0:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                case 1:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    break;
                case 2:
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                default:
                    OwlLogger.Log($"Invalid heal mode {healMode}!", GameComponent.ChatCommands);
                    return -5;
            }

            return 0;
        }
    }

    public class HealIdChatCommand : AChatCommand
    {
        // Args[1]: [Optional] Heal mode - 0 = both, 1 = hp, 2 = sp, default = 0
        // Args[2]: [Optional] EntityId of entity to heal, default = sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 3))
                return -1;

            int healMode = 0;
            int targetId = sender.Id;
            ServerBattleEntity target = sender;

            if (args.Length >= 2)
            {
                if (!int.TryParse(args[1], out healMode))
                {
                    OwlLogger.Log($"Invalid healMode given: {healMode}", GameComponent.ChatCommands);
                    return -2;
                }

                if (args.Length == 3)
                {
                    if (!int.TryParse(args[2], out targetId))
                    {
                        OwlLogger.Log($"Invalid TargetId given: {targetId}", GameComponent.ChatCommands);
                        return -3;
                    }
                }
            }

            // Resolve target id
            if(targetId != sender.Id)
            {
                target = AServer.Instance.MapModule.FindEntityOnAllMaps(targetId) as ServerBattleEntity;
                if (target == null)
                {
                    OwlLogger.Log($"Can't find target with id {targetId}", GameComponent.ChatCommands);
                    return -4;
                }
            }

            // Execute effect
            MapInstance targetMap = target.GetMapInstance();
            if (targetMap == null)
            {
                OwlLogger.Log($"Couldn't find target map {target.MapId}!", GameComponent.ChatCommands);
                return -4;
            }
            BattleModule battleModule = targetMap.BattleModule;

            switch (healMode)
            {
                case 0:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                case 1:
                    battleModule.ChangeHp(target, target.MaxHp.Total, sender);
                    break;
                case 2:
                    battleModule.ChangeSp(target, target.MaxSp.Total);
                    break;
                default:
                    OwlLogger.Log($"Invalid heal mode {healMode}!", GameComponent.ChatCommands);
                    return -5;
            }
            return 0;
        }
    }

    public class KillChatCommand : AChatCommand
    {
        // Args[1]: [Optional] Name of character to kill, default = sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 2))
                return -1;

            string targetName = sender.NameOverride;

            if (args.Length == 2)
            {
                targetName = args[1];
            }

            // Resolve target name
            CharacterRuntimeData target = sender;
            if (targetName != sender.NameOverride)
            {
                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use HealChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -3;
                }
            }

            // Execute effect
            MapInstance targetMap = target.GetMapInstance();
            if (targetMap == null)
            {
                OwlLogger.LogError($"Couldn't find target map {target.MapId}!", GameComponent.ChatCommands);
                return -4;
            }
            BattleModule battleModule = targetMap.BattleModule;

            battleModule.ChangeHp(target, -target.MaxHp.Total, sender);
            return 0;
        }
    }

    public class KillIdChatCommand : AChatCommand
    {
        // Args[1]: [Optional] EntityId of entity to kill, default = sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 2))
                return -1;

            int targetId = sender.Id;

            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], out targetId))
                {
                    OwlLogger.Log($"Invalid TargetId given: {targetId}", GameComponent.ChatCommands);
                    return -3;
                }
            }

            ServerBattleEntity target = sender;
            // Resolve target id
            if(targetId != sender.Id)
            {
                target = AServer.Instance.MapModule.FindEntityOnAllMaps(targetId) as ServerBattleEntity;
                if (target == null)
                {
                    OwlLogger.Log($"Can't find target with id {targetId}", GameComponent.ChatCommands);
                    return -4;
                }
            }

            // Execute effect
            MapInstance targetMap = target.GetMapInstance();
            if (targetMap == null)
            {
                OwlLogger.Log($"Couldn't find target map {target.MapId}!", GameComponent.ChatCommands);
                return -4;
            }
            BattleModule battleModule = targetMap.BattleModule;

            battleModule.ChangeHp(target, -target.MaxHp.Total, sender);
            return 0;
        }
    }

    public class ReloadSkillDbChatCommand : AChatCommand
    {
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1))
                return -1;

            SkillStaticDataDatabase.Reload();
            return 0;
        }
    }

    public class ChangeJobChatCommand : AChatCommand
    {
        // Args[1]: Id of the job to switch to
        // Args[2]: [Optional] Include full skill reset? (default: 1 = Yes)
        // Args[3]: [Optional] Name of character to switch (default: Sender)
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 2, 4))
                return -1;

            // Read & validate jobId
            JobId targetJobId;
            string targetJobStr = args[1];
            if(!int.TryParse(targetJobStr, out int targetJobInt))
            {
                OwlLogger.LogF("Invalid JobId format passed to ChangeJobChatCommand: {0}", targetJobStr, GameComponent.ChatCommands);
                return -2;
            }

            targetJobId = (JobId)targetJobInt;

            if(!System.Enum.IsDefined(typeof(JobId), targetJobId) 
                || targetJobId == JobId.Unknown)
            {
                OwlLogger.LogF("Invalid JobId passed to ChangeJobChatCommand: {0}", targetJobId, GameComponent.ChatCommands);
                return -3;
            }

            bool shouldReset = true;
            if(args.Length >= 3)
            {
                bool canParse = int.TryParse(args[2], out int shouldResetInt);
                if(!canParse)
                {
                    OwlLogger.LogF("Invalid shouldReset value passed into ChangeJobChatCommand: {0}", args[2], GameComponent.ChatCommands);
                    return -5;
                }
                shouldReset = shouldResetInt != 0;
            }

            // Read & Validate target
            CharacterRuntimeData target = sender;

            if(args.Length >= 4)
            {
                string targetName = args[3];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use ChangeJobChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -4;
                }
            }

            // Execute effect
            AServer.Instance.JobModule.ChangeJob(target, targetJobId, shouldReset);
            return 0;
        }
    }

    public class SkillResetChatCommand : AChatCommand
    {
        // args[1]: [Optional] Name of character to reset. default: Sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 2))
                return -1;

            
            CharacterRuntimeData target = sender;
            if (args.Length >= 2)
            {
                string targetName = args[2];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use ChangeJobChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -4;
                }
            }

            target.GetMapInstance().SkillModule.SkillReset(target);

            return 0;
        }
    }

    public class StatResetChatCommand : AChatCommand
    {
        // args[1]: [Optional] Name of character to reset. default: Sender
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 2))
                return -1;

            CharacterRuntimeData target = sender;
            if (args.Length >= 2)
            {
                string targetName = args[2];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use ChangeJobChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -4;
                }
            }

            AServer.Instance.JobModule.StatReset(target);

            return 0;
        }
    }

    public class BaseLevelChatCommand : AChatCommand
    {
        // args[1]: Level difference to apply
        // args[2]: [Optional] Target character name. Default: Sender name
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 2, 3))
                return -1;

            if (!int.TryParse(args[1], out int levelDiff))
            {
                OwlLogger.Log($"Tried to use BaseLevelChatCommand with invalid leveldiff {args[1]}.", GameComponent.ChatCommands);
                return -2;
            }

            CharacterRuntimeData target = sender;
            if (args.Length >= 3)
            {
                string targetName = args[2];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use BaseLevelChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -4;
                }
            }

            AServer.Instance.ExpModule.LevelUpBase(target, levelDiff);

            return 0;
        }
    }

    public class JobLevelChatCommand : AChatCommand
    {
        // args[1]: Level difference to apply
        // args[2]: [Optional] Target character name. Default: Sender name
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 2, 3))
                return -1;

            if (!int.TryParse(args[1], out int levelDiff))
            {
                OwlLogger.Log($"Tried to use JobLevelChatCommand with invalid leveldiff {args[1]}.", GameComponent.ChatCommands);
                return -2;
            }

            CharacterRuntimeData target = sender;
            if (args.Length >= 3)
            {
                string targetName = args[2];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    OwlLogger.Log($"Tried to use JobLevelChatCommand on target {targetName}, which wasn't found.", GameComponent.ChatCommands);
                    return -4;
                }
            }

            AServer.Instance.ExpModule.LevelUpJob(target, levelDiff);

            return 0;
        }
    }

    public class CreateItemExactCommant : AChatCommand
    {
        // args[1]: Exact itemType to be created
        // args[2]: Amount to be created
        // args[3]: [Optional] Name of character who's inventory to create it in. Default: Sender name
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 3, 4))
                return -1;

            if (!long.TryParse(args[1], out long targetItemTypeId))
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                    MessageLocId = new(213)
                });
                OwlLogger.Log($"Invalid ItemTypeId {args[1]}.", GameComponent.ChatCommands);
                return -2;
            }

            if (!int.TryParse(args[2], out int amount)
                || amount == 0)
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                    MessageLocId = new(214)
                });
                OwlLogger.Log($"Invalid Amount {args[2]}.", GameComponent.ChatCommands);
                return -3;
            }

            CharacterRuntimeData target = sender;
            if (args.Length >= 4)
            {
                string targetName = args[3];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    sender.Connection.Send(new LocalizedChatMessagePacket()
                    {
                        ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                        MessageLocId = new(215)
                    });
                    OwlLogger.LogF("Tried to use CreateItemExactCommand on target {0}, which wasn't found.", targetName, GameComponent.ChatCommands);
                    return -4;
                }
            }

            int result;
            if (amount > 0)
                result = AServer.Instance.InventoryModule.AddItemsToCharacterInventory(target, targetItemTypeId, amount);
            else
                result = AServer.Instance.InventoryModule.RemoveItemsFromCharacterInventory(target, targetItemTypeId, -amount, true);
            
            if(result == -2)
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                    MessageLocId = new(216)
                });
                OwlLogger.LogF("Tried to use CreateItemExactCommand on target {0}, but not enough weight capacity!", target.NameOverride, GameComponent.ChatCommands);
            }
            else if(result != 0)
            {
                OwlLogger.LogF("Generic error while using CreateItemExactCommand: {0}", result, GameComponent.ChatCommands);
            }
            else
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_FEEDBACK,
                    MessageLocId = new(217)
                });
                // This will somewhat-duplicate item-telemetry, but this way we can more easily filter for GM activity
                OwlLogger.LogF("{0} used chatcommand to create {1}x ItemType {2} on character {3}",
                    sender.NameOverride, amount, targetItemTypeId, target.NameOverride, GameComponent.ChatCommands);
            }

            return result;
        }
    }

    public class ClearInventoryCommand : AChatCommand
    {
        // args[1]: [Optional] Target character name. Default: Sender name
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 1, 2))
                return -1;

            CharacterRuntimeData target = sender;
            if (args.Length >= 4)
            {
                string targetName = args[3];

                target = FindPlayerByName(targetName);
                if (target == null)
                {
                    sender.Connection.Send(new LocalizedChatMessagePacket()
                    {
                        ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                        MessageLocId = new(215)
                    });
                    OwlLogger.LogF("Tried to use ClearInventoryCommand on target {0}, which wasn't found.", targetName, GameComponent.ChatCommands);
                    return -2;
                }
            }

            Inventory inventory = AServer.Instance.InventoryModule.GetOrLoadInventory(target.InventoryId);
            int totalResult = 0;
            foreach(var kvp in inventory.ItemStacksByTypeId)
            {
                int partResult = AServer.Instance.InventoryModule.RemoveItemsFromCharacterInventory(target, kvp.Value.ItemType.TypeId, kvp.Value.ItemCount, true);
                if(partResult != 0)
                {
                    OwlLogger.LogError($"Failed to delete itemtype {kvp.Key} from inventory of character {target.NameOverride}", GameComponent.ChatCommands);
                }
                totalResult += partResult;
            }

            if(totalResult == 0)
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_FEEDBACK,
                    MessageLocId = new(218)
                });
                OwlLogger.LogF("Character {0} has cleared inventory of Character {1}", sender.NameOverride, target.NameOverride, GameComponent.ChatCommands);
            }

            return totalResult;
        }
    }

    public class EquipmentRequestChatCommand : AChatCommand
    {
        // args[1]: ItemType to equip
        // args[2]: Slot/s to equip into
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 3, 3))
                return -1;

            if (!long.TryParse(args[1], out long itemTypeId))
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                    MessageLocId = new(225)
                });
                OwlLogger.LogF("Tried to equip ItemType {0}, which doesn't exist", args[1], GameComponent.ChatCommands);
                return -2;
            }

            if(!int.TryParse(args[2], out int slotsInt))
            {
                sender.Connection.Send(new LocalizedChatMessagePacket()
                {
                    ChannelTag = DefaultChannelTags.COMMAND_ERROR,
                    MessageLocId = new(226)
                });
                OwlLogger.LogF("Tried to equip into EquipmentSlot, which isn't valid", args[2], GameComponent.ChatCommands);
                return -2;
            }

            EquipmentSlot targetSlots = (EquipmentSlot)slotsInt;
            AServer.Instance.EquipmentModule.ReceiveCharacterEquipRequest(sender, sender, targetSlots, itemTypeId);

            return 0;
        }
    }

    public class TemplateChatCommand : AChatCommand
    {
        // args[1]: Does something.
        // args[2]: [Optional] Does something else. Default: 0
        public override int Execute(CharacterRuntimeData sender, string[] args)
        {
            if (!VerifyArgCount(args, 2, 3))
                return -1;

            return 0;
        }
    }

    public abstract class AChatCommand
    {
        /// <summary>
        /// Executes a chat command's logic
        /// </summary>
        /// <param name="args">[0] = command word, after that the command's arguments</param>
        /// <returns>0 = success</returns>
        public abstract int Execute(CharacterRuntimeData sender, string[] args);

        protected bool VerifyArgCount(string[] args, int expectedCount)
        {
            if(args.Length != expectedCount)
            {
                OwlLogger.Log($"Too many arguments for {GetType().Name}: {expectedCount} expected, {args.Length-1} given!", GameComponent.ChatCommands);
                return false;
            }

            return true;
        }

        protected bool VerifyArgCount(string[] args, int minCount, int maxCount)
        {
            if (args.Length > maxCount)
            {
                OwlLogger.Log($"Too many arguments for {GetType().Name} - up to {maxCount} expected, {args.Length - 1} given!", GameComponent.ChatCommands);
                return false;
            }

            if (args.Length < minCount)
            {
                OwlLogger.Log($"Not enough arguments for{GetType().Name} - at least {minCount} expected, {args.Length - 1} given!", GameComponent.ChatCommands);
                return false;
            }

            return true;
        }

        protected CharacterRuntimeData FindPlayerByName(string name)
        {
            foreach (CharacterRuntimeData charData in AServer.Instance.LoggedInCharacters)
            {
                if (charData.NameOverride == name)
                {
                    return charData;
                }
            }
            return null;
        }
    }
}
