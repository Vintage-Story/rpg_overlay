using Vintagestory.API.Common;

namespace RPGOverlay;

#pragma warning disable CA2211
public static partial class Configuration
{
    internal static void Load(ICoreAPI api)
    {
        LoadBase(api);
    }
}
