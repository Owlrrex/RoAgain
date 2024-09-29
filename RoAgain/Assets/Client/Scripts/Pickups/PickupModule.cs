using OwlLogging;
using Shared;

namespace Client
{
    public class PickupData
    {
        public int PickupId;
        public long ItemTypeId;
        public int Amount;
        public float RemainingTime;
        public int OwnerCharacterId;
        public PickupState State;
        public Coordinate Coordinates;

        public static PickupData FromPacket(PickupDataPacket packet)
        {
            return new()
            {
                PickupId = packet.PickupId,
                ItemTypeId = packet.ItemTypeId,
                Amount = packet.Amount,
                RemainingTime = packet.RemainingTime,
                OwnerCharacterId = packet.OwnerCharacterId,
                State = packet.State,
                Coordinates = packet.Coordinates
            };
        }
    }

    // to be used when we split up the Client-MapModule into smaller sub-objects
    public class PickupModule
    {

    }
}


