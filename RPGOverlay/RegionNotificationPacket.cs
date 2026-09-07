using ProtoBuf;

namespace RPGOverlay;

[ProtoContract]
public class RegionNotificationPacket
{
    [ProtoMember(1)]
    public string Zone;

    [ProtoMember(2)]
    public int RegionX;

    [ProtoMember(3)]
    public int RegionZ;

    [ProtoMember(4)]
    public int Level;
}
