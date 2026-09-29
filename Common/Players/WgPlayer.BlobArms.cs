using Terraria;

namespace WgMod.Common.Players;

public partial class WgPlayer
{
    const ulong BlobArmSwingDuration = 65;

    bool _blobArmSwing;
    ulong _blobArmSwingStart;

    internal bool BlobArmSwingActive
        => _blobArmSwing && Main.GameUpdateCount - _blobArmSwingStart < BlobArmSwingDuration;

    internal int BlobArmSwingFrame
    {
        get
        {
            float time = (Main.GameUpdateCount - _blobArmSwingStart) * 0.2f % 13f;
            return (int)(6f + time) switch
            {
                6 => 3,
                7 or 8 or 9 or 10 => 4,
                11 or 12 or 13 => 3,
                14 => 5,
                15 or 16 => 6,
                17 => 5,
                18 or 19 => 3,
                _ => 3
            };
        }
    }

    internal void TriggerBlobArmSwing()
    {
        if (!BlobArmSwingActive)
        {
            _blobArmSwing = true;
            _blobArmSwingStart = Main.GameUpdateCount;
        }
    }
}
