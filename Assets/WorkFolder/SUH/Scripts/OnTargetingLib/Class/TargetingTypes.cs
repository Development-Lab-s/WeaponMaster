namespace WorkFolder.SUH.Scripts.OnTargetingLib.Class
{
    public enum TargetZone
    {
        Center,
        Head,
        Torso,
        LeftArm,
        RightArm,
        Legs,
    }

    public enum TargetLostReason
    {
        None,
        ManualCancel,
        OutOfRange,
        OutsideScreen,
        Occluded,
        Disabled,
        Despawned,
    }

    public enum TargetCycleDirection
    {
        Left = -1,
        Right = 1
    }
}