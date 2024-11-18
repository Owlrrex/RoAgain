using OwlLogging;
using Shared;
using System;
using System.Collections.Generic;

namespace Server
{
    [Serializable]
    public class EquipmentSetPersistent
    {
        public DictionarySerializationWrapper<EquipmentSlot, long> EquippedTypes = new();
    }

    public class EquipmentSetRuntime : EquipmentSet<EquippableItemType>
    {
        public EquipmentType GetDefaultWeaponType(out EquipmentSlot usedSlot, out bool isTwoHanded)
        {
            isTwoHanded = IsTwoHanding();
            if (HasItemEquippedInSlot(EquipmentSlot.Mainhand))
                usedSlot = GetGroupedSlots(EquipmentSlot.Mainhand);
            else if (HasItemEquippedInSlot(EquipmentSlot.Offhand))
                usedSlot = GetGroupedSlots(EquipmentSlot.Offhand);
            else
                usedSlot = EquipmentSlot.TwoHand; // Unarmed attacks count as two-handing for now
                
            return GetItemType(EquipmentSlot.Mainhand)?.EquipmentType ?? EquipmentType.Unarmed;
        }
    }

    /// <summary>
    /// Provides functions to manipulate what items characters have equipped.
    /// </summary>
    public class EquipmentModule
    {
        private InventoryModule _invModule;
        private ItemTypeModule _itemTypeModule;

        public int Initialize(InventoryModule invModule, ItemTypeModule itemTypeModule) // InvModule: To check item ownership ItemTypeModule: To resolve equipment effects
        {
            if (invModule == null)
            {
                OwlLogger.LogError("Can't initialize EquipmentModule with null InventoryModule!", GameComponent.Items);
                return -1;
            }

            if (itemTypeModule == null)
            {
                OwlLogger.LogError("Can't initialize EquipmentModule with null ItemTypeModule!", GameComponent.Items);
                return -1;
            }

            _invModule = invModule;
            _itemTypeModule = itemTypeModule;

            return 0;
        }

        public void Shutdown()
        {
            _invModule = null;
            _itemTypeModule = null;
        }

        public bool CanEquip(ServerBattleEntity owner, EquipmentSlot occupiedSlots, ItemType type)
        {
            if (type is not EquippableItemType equipType)
                return false;

            if (owner is CharacterRuntimeData character)
            {
                Inventory charInv = _invModule.GetOrLoadInventory(character.InventoryId);
                if (!charInv.HasItemTypeExact(type.TypeId))
                    return false;

                EquipmentSlot slotsThatNeedUnequip = 0;
                foreach (EquipmentSlot slot in new EquipmentSlotIterator(occupiedSlots))
                {
                    if (character.EquipSet.HasItemEquippedInSlot(slot))
                        slotsThatNeedUnequip |= character.EquipSet.GetGroupedSlots(slot);
                }

                if(slotsThatNeedUnequip != 0
                    && !CanUnequip(owner, slotsThatNeedUnequip))
                    return false;
            }

            HashSet<EquipmentSlot> equippableSlots = equipType.GetEquippableSlots(owner);
            if (!equippableSlots.Contains(occupiedSlots))
                return false;

            // TODO: Debuffs that block equipment

            return true;
        }

        // If there's ever a variant of this function for ServerBattleEntities, it'll have a lot of copied code from this function
        // Could split that up ahead of time to maximise reuse of code
        /// <summary>
        /// Equips an itemtype into a slot on an Equipset.
        /// This function only performs basic error checking, assuming checks specific to the Equipset's owner have already been performed
        /// </summary>
        /// <param name="equipSet">Equipset to modify</param>
        /// <param name="targetSlots">Slot to equip to</param>
        /// <param name="itemType">ItemType to equip</param>
        /// <returns>EquipType bitmask for slots that were changed by the operation, negative errorcode otherwise </returns>
        public int Equip(CharacterRuntimeData character, EquipmentSlot targetSlots, long itemTypeId)
        {
            if (character == null)
            {
                OwlLogger.LogError($"Can't equip itemtype {itemTypeId} to slot {targetSlots} on null character!", GameComponent.Items);
                return -1;
            }

            ItemType type = _itemTypeModule.GetOrLoadItemType(itemTypeId);
            // Intentionally don't notify yet - equipping might still fail, in which case we don't need to notify since we don't store a reference to the new type
            if (type == null)
            {
                OwlLogger.LogError($"Can't load itemType {itemTypeId} for equipping!", GameComponent.Items);
                return -2;
            }

            if (type is not EquippableItemType equipType)
            {
                OwlLogger.LogError($"Can't equip itemType {itemTypeId} - is not equippable!", GameComponent.Items);
                return -4;
            }

            EquipmentSlot predictedChangedSlots = character.EquipSet.GetGroupedSlots(targetSlots);
            Dictionary<EquipmentSlot, EquippableItemType> oldItemTypes = new();
            HashSet<EquippableItemType> uniqueUnqeuippedItemTypes = new();
            foreach (EquipmentSlot oldSlot in new EquipmentSlotIterator(predictedChangedSlots))
            {
                EquippableItemType unequippedType = character.EquipSet.GetItemType(oldSlot);
                if (unequippedType == null)
                    continue;

                oldItemTypes.Add(oldSlot, unequippedType);
                if (uniqueUnqeuippedItemTypes.Add(unequippedType))
                    _itemTypeModule.NotifyItemTypeUsed(unequippedType.TypeId);
            }

            foreach (EquippableItemType unequippedType in uniqueUnqeuippedItemTypes)
            {
                RemovePermanentStats(character, unequippedType);
            }

            if (!CanEquip(character, targetSlots, type))
            {
                OwlLogger.LogF("Character {0} not allowed to equip itemtype {1} in slot {2}", character.Id, itemTypeId, targetSlots, GameComponent.Items, LogSeverity.Verbose);
                character.Connection.Send(new LocalizedChatMessagePacket() { ChannelTag = DefaultChannelTags.GENERIC_ERROR, MessageLocId = new(221) });

                // Give back the stats we took away to make the CanEquip check accurate
                foreach(EquippableItemType unequippedType in uniqueUnqeuippedItemTypes)
                {
                    ApplyPermanentStats(character, unequippedType);
                }
                return 0;
            }

            int result = Equip(character.EquipSet, targetSlots, equipType);

            if (result < 0)
            {
                OwlLogger.LogError($"Character {character.Id} failed to equip item {itemTypeId} in slot {targetSlots}!", GameComponent.Items);
                character.Connection.Send(new LocalizedChatMessagePacket() { ChannelTag = DefaultChannelTags.GENERIC_ERROR, MessageLocId = new(221) });
                return -3;
            }

            ApplyPermanentStats(character, equipType);

            // TODO: Update sets

            foreach (var kvp in oldItemTypes)
            {
                character.EquipSet.EquipmentChanged?.Invoke(kvp.Key, kvp.Value);
            }

            foreach (EquippableItemType oldType in uniqueUnqeuippedItemTypes)
            {
                _itemTypeModule.NotifyItemTypeUseEnded(oldType.TypeId);
            }

            // Slots that were empty to being with may not have been notified by Unequip()
            character.NetworkQueue.EquipmentChanged(targetSlots, character);

            return 0;
        }

        /// <summary>
        /// Equips an itemtype into onre or multiple slots on an Equipset.
        /// Automatically unequips items that occupy one of the targetSlots
        /// This function only performs basic error checking, assuming checks specific to the Equipset's owner have already been performed
        /// This function marks the itemType as "used" if the equipping is successful
        /// </summary>
        /// <param name="equipSet">Equipset to modify</param>
        /// <param name="targetSlots">Slot/s to equip to</param>
        /// <param name="itemType">ItemType to equip</param>
        /// <returns>EquipType bitmask for slots that were changed by the operation, negative errorcode otherwise </returns>
        private int Equip(EquipmentSetRuntime equipSet, EquipmentSlot targetSlots, EquippableItemType itemType)
        {
            if (equipSet == null)
            {
                OwlLogger.LogError($"Can't equip itemtype {itemType.TypeId} to slot {targetSlots} on null equipset!", GameComponent.Items);
                return -1;
            }

            if (targetSlots == EquipmentSlot.Unknown)
            {
                OwlLogger.LogError($"Can't equip itemType {itemType.TypeId} to unknown slot!", GameComponent.Items);
                return -1;
            }

            int unequipResult = Unequip(equipSet, targetSlots);
            if (unequipResult < 0)
            {
                OwlLogger.LogError($"Failed to unequip item in slot {targetSlots} when trying to equip new item {itemType.TypeId}!", GameComponent.Items);
                return -2;
            }
            EquipmentSlot unequippedSlots = (EquipmentSlot)unequipResult;

            // TODO: Call Equip-script - before or after the Equip happens?

            equipSet.SetItemType(targetSlots, itemType);

            // Lock itemtype once per slot, for parity with unequip()
            foreach(EquipmentSlot tmp in new EquipmentSlotIterator(targetSlots))
                _itemTypeModule.NotifyItemTypeUsed(itemType.TypeId);

            return (int)unequippedSlots;
        }

        public bool CanUnequip(ServerBattleEntity owner, EquipmentSlot slot)
        {
            if (owner is not CharacterRuntimeData character)
                return false;

            // TODO: Buffs that block unequipping

            bool result = false;
            foreach (EquipmentSlot s in new EquipmentSlotIterator(slot))
            {
                result |= character.EquipSet.HasItemEquippedInSlot(s);
            }

            return result;
        }

        /// <summary>
        /// Equips an itemtype into a slot on an Equipset.
        /// This function only performs basic error checking, assuming checks specific to the Equipset's owner have already been performed
        /// </summary>
        /// <param name="equipSet">Equipset to modify</param>
        /// <param name="targetSlots">Slot to equip to</param>
        /// <param name="notifyItemType">Should the ItemTypes of unequipped items be notified that their usage has ended?</param>
        /// <returns>EquipType bitmask for slots that were changed by the operation, negative errorcode otherwise </returns>
        public int Unequip(CharacterRuntimeData character, EquipmentSlot targetSlots)
        {
            if (character == null)
            {
                OwlLogger.LogError($"Can't unequip slot {targetSlots} on null character!", GameComponent.Items);
                return -1;
            }

            if (targetSlots == EquipmentSlot.Unknown)
            {
                OwlLogger.LogError($"Can't unequip unknown equipment slot on character {character.Id}!", GameComponent.Items);
                return -3;
            }

            if (!CanUnequip(character, targetSlots))
            {
                OwlLogger.LogF("Character {0} not allowed to unquip item in slot {1}", character.Id, targetSlots, GameComponent.Items, LogSeverity.Verbose);
                character.Connection.Send(new LocalizedChatMessagePacket() { ChannelTag = DefaultChannelTags.GENERIC_ERROR, MessageLocId = new(222) });
                return 0;
            }

            EquipmentSlot predictedChangedSlots = character.EquipSet.GetGroupedSlots(targetSlots);
            Dictionary<EquipmentSlot, EquippableItemType> oldItemTypes = new();
            HashSet<EquippableItemType> uniqueUnqeuippedItemTypes = new();
            foreach (EquipmentSlot oldSlot in new EquipmentSlotIterator(predictedChangedSlots))
            {
                EquippableItemType type = character.EquipSet.GetItemType(oldSlot);
                if (type == null)
                    continue; 
                oldItemTypes.Add(oldSlot, type);
                if (uniqueUnqeuippedItemTypes.Add(type))
                    _itemTypeModule.NotifyItemTypeUsed(type.TypeId); // Keep this itemtype alive until we've performed all our post-unequip operations
            }

            // TODO: Call Unequip-script - before or after the unequip happens?

            int result = Unequip(character.EquipSet, targetSlots);

            if (result < 0)
            {
                OwlLogger.LogError($"Character {character.Id} failed to unequip item in slot {targetSlots}!", GameComponent.Items);
                character.Connection.Send(new LocalizedChatMessagePacket() { ChannelTag = DefaultChannelTags.GENERIC_ERROR, MessageLocId = new(222) });
                return -2;
            }

            foreach (var kvp in oldItemTypes)
            {
                character.EquipSet.EquipmentChanged?.Invoke(kvp.Key, kvp.Value);
                // Send notification to client for "<Item> unequipped" message? Since this is not part of the EquipmentChangedPacket
            }

            foreach (EquippableItemType type in uniqueUnqeuippedItemTypes)
            {
                RemovePermanentStats(character, type);
                _itemTypeModule.NotifyItemTypeUseEnded(type.TypeId); // can free this type again, now that the stats have been removed & EqupimentChanged has been broadcast
            }

            character.NetworkQueue.EquipmentChanged(targetSlots, character);

            // TODO: Update Sets

            return result;
        }

        /// <summary>
        /// Unequips all items that cover one or more of the given slots.
        /// This function assumes checks specific to the Equipset's owner have already been performed
        /// This function does notify the unequipped item type that its use has ended, so if you want to continue using it, you have to register your use separately
        /// </summary>
        /// <param name="equipSet">Equipset to modify</param>
        /// <param name="targetSlots">Slot/s to unequip from</param>
        /// <returns>EquipType bitmask for slots that were changed by the operation, negative errorcode otherwise </returns>
        private int Unequip(EquipmentSetRuntime equipSet, EquipmentSlot targetSlots)
        {
            if (equipSet == null)
            {
                OwlLogger.LogError($"Can't unequip slot {targetSlots} on null equipset!", GameComponent.Items);
                return -1;
            }

            foreach (EquipmentSlot slot in new EquipmentSlotIterator(targetSlots))
            {
                EquippableItemType oldType = equipSet.GetItemType(slot);
                if(oldType != null)
                    _itemTypeModule.NotifyItemTypeUseEnded(oldType.TypeId);
            }

            EquipmentSlot groupedSlots = equipSet.GetGroupedSlots(targetSlots);
            equipSet.SetItemType(groupedSlots, null);
            return (int)groupedSlots;
        }

        public EquipmentSetRuntime LoadEquipSet(EquipmentSetPersistent persData)
        {
            if (persData == null)
            {
                OwlLogger.LogError("Can't load equipset for null PersistentData!", GameComponent.Items);
                return null;
            }

            EquipmentSetRuntime equipSet = new();

            foreach (var entry in persData.EquippedTypes.entries)
            {
                if (_itemTypeModule.GetOrLoadItemType(entry.value) is not EquippableItemType type)
                {
                    OwlLogger.LogError($"Failed to load ItemType {entry.value} for Equipset, or it's not equippable!", GameComponent.Items);
                    continue;
                }

                Equip(equipSet, entry.key, type);
            }

            return equipSet;
        }

        public void UnloadEquipSet(EquipmentSetRuntime equipSet)
        {
            if (equipSet == null)
            {
                OwlLogger.LogError("Can't unload null equipset!", GameComponent.Items);
                return;
            }

            for (EquipmentSlot slot = EquipmentSlot.HeadUpper; slot <= EquipmentSlot.MAX; slot++)
            {
                EquippableItemType type = equipSet.GetItemType(slot);
                if (type != null)
                {
                    _itemTypeModule.NotifyItemTypeUseEnded(type.TypeId);
                }
            }
        }

        private void ApplyPermanentStats(ServerBattleEntity owner, EquippableItemType itemType)
        {
            foreach (SimpleStatEntry entry in itemType.SimpleStatEntries)
            {
                owner.AddStat(entry.Type, entry.Change);
            }

            if (owner is not CharacterRuntimeData charOwner)
                return;

            foreach (ConditionalStatEntry entry in itemType.ConditionalStatEntries)
            {
                charOwner.AddConditionalStat(entry.Type, entry.ConditionalChange);
            }
        }

        private void RemovePermanentStats(ServerBattleEntity owner, EquippableItemType itemType)
        {
            foreach (SimpleStatEntry entry in itemType.SimpleStatEntries)
            {
                owner.RemoveStat(entry.Type, entry.Change);
            }

            if (owner is not CharacterRuntimeData charOwner)
                return;

            foreach (ConditionalStatEntry entry in itemType.ConditionalStatEntries)
            {
                charOwner.RemoveConditionalStat(entry.Type, entry.ConditionalChange);
            }
        }

        private void UpdateSetBonuses()
        {
            // TODO
        }

        public void ReceiveCharacterEquipRequest(CharacterRuntimeData sender, GridEntity owner, EquipmentSlot slot, long itemTypeId)
        {
            if (!IsAllowedToChangeEquipment(sender, owner))
            {
                OwlLogger.LogError($"Received EquipRequest from characterId {sender.CharacterId} to owner {owner.Id} - permission denied.", GameComponent.Items);
                return;
            }

            if (owner is not CharacterRuntimeData charOwner)
            {
                OwlLogger.LogError($"Received EquipRequest from characterId {sender.CharacterId} to owner {owner.Id} - owner doesn't support equipment!", GameComponent.Items);
                return;
            }

            if (itemTypeId == ItemConstants.ITEM_TYPE_ID_INVALID)
                Unequip(charOwner, slot);
            else
                Equip(charOwner, slot, itemTypeId);
        }

        /// <summary>
        /// This function allows for checking situations like manipulating your pet's or follower's equipment
        /// </summary>
        /// <param name="sender">The character sending the request</param>
        /// <param name="owner">The entity who's equipment should be changed</param>
        /// <returns>Is changing of equipment by this character allowed?</returns>
        public bool IsAllowedToChangeEquipment(CharacterRuntimeData sender, GridEntity owner)
        {
            return sender.Id == owner.Id;
        }
    }
}
