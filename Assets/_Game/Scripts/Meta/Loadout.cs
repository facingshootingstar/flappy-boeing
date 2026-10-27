using System;
using System.Collections.Generic;

[Serializable]
public class Loadout
{
    public string planeId = "boeing";
    public List<string> perks = new List<string>();

    public static Loadout FromSave()
    {
        return new Loadout
        {
            planeId = SaveSystem.Data != null && !string.IsNullOrEmpty(SaveSystem.Data.selectedPlane)
                ? SaveSystem.Data.selectedPlane
                : "boeing",
            perks = new List<string>()
        };
    }
}
