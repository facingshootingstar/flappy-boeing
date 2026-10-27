using UnityEngine;

[CreateAssetMenu(menuName = "Sky Arcade/Mini Game Def")]
public class MiniGameDef : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public string sceneName;
    public int unlockPilotLevel;
    public int unlockCoinCost;
    public string materialDrop;
    [TextArea]
    public string description;
}
