using ProtoBuf;

namespace RPGOverlay;

[ProtoContract]
public class RegionNotificationPacket
{
    [ProtoMember(1)]
    public string Text;
}
