
namespace Shared
{
    public enum PickupState
    {
        Unknown,
        JustDropped,
        OnGround,
        PickedUp,
        AboutToDisappear
    }

    public class PickupEntity : GridEntity
    {
        public long ItemTypeId;
        public int Count;
        public int OwnerEntityId;
        public TimerFloat LifeTime = new();
        public PickupState State;

        public PickupEntity(Coordinate coordinates, long itemTypeId, int count, float lifeTime, int ownerEntityId = 0)
            : base(coordinates, LocalizedStringId.INVALID, -1, 1)
        {
            ItemTypeId = itemTypeId;
            Count = count;
            OwnerEntityId = ownerEntityId;
            LifeTime.Initialize(lifeTime);
            State = PickupState.JustDropped;
        }

        public override bool BlocksStanding()
        {
            return false;
        }

        public override Packet ToDataPacket()
        {
            return new PickupDataPacket()
            {
                Amount = Count,
                Coordinates = Coordinates,
                ItemTypeId = ItemTypeId,
                OwnerCharacterId = OwnerEntityId,
                PickupId = Id,
                RemainingTime = LifeTime.RemainingValue,
                State = State
            };
        }

        public override Packet ToRemovedPacket()
        {
            PickupRemovedPacket packet = new() { PickupId = Id };
            if (State == PickupState.AboutToDisappear)
                packet.PickedUpEntityId = PickupRemovedPacket.PICKUP_ENTITY_TIMEOUT;
            else
                packet.PickedUpEntityId = OwnerEntityId;
            return packet;
        }
    }
}
