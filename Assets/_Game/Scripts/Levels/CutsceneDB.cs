using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Transition { Cut, Iris, Fade, PageTurn }

/// <summary>One frame of a cutscene. Art is a file name under Assets/_Game/Art/story without the extension.</summary>
public class Shot
{
    public string art, text, sfx, music;
    public float dur = 4f;
    public Vector2 panFrom, panTo;
    public float zoomFrom = 1.08f, zoomTo = 1.16f;
    public Transition inT, outT;
    public Action custom;
}

public class Cutscene
{
    public string id;
    public Shot[] shots;
}

/// <summary>
/// Story beats. Text stays inside the pixel font: A-Z 0-9 and ! ? . : - + / ' % and space.
/// Stars at or above <see cref="GoldStars"/> are the bright ending. Below that the beacon only flickers.
/// </summary>
public static class CutsceneDB
{
    public const int GoldStars = 60;
    public const string Title = "POE: THE LAST MAIL RUN";

    static readonly string[] PipLines =
    {
        "FLAP LIKE YOU MEAN THE LETTER.",
        "STACK THEM. THE BOAT IS ITCHY.",
        "NICE NIGHT FOR A BUMP.",
        "THOSE ROCKETS ARE RUDE.",
        "BIG BIRD. SMALL COLLAR. GO.",
        "GREASE THE WHEELS OLD BOY.",
        "CATCH THE BOX. NOT THE SEA.",
        "WIND IS A LIAR. STACK ANYWAY.",
        "DARK PIPES. BRIGHT EYES.",
        "THAT SHIP IS A BULLY.",
        "LAND IT. THE GALE CAN WAIT.",
        "DO NOT SHAKE HANDS WITH BOLTS.",
        "FILL THE QUOTA. THEN FLEX.",
        "NIGHT ROCKETS CHEAT. YOU DON'T.",
        "THE TOWER SHOOTS. YOU DODGE.",
        "SIP THE HOSE. DON'T BITE IT.",
        "NIGHT LANDING. EYES OPEN.",
        "TALLER STACK. SAME TRICK.",
        "BOLTS HAVE NO MANNERS.",
        "FOUR BOSSES. ONE PIP. EASY.",
        "NIGHT CRATES. DAY HERO.",
        "THE STORM BLINKS. YOU DON'T.",
        "HOSE IN THE WIND. HERO STUFF.",
        "LAST CHIMNEYS. MAKE THEM COUNT.",
        "EAT THE DARK. SPIT OUT LIGHT.",
    };

    static readonly string[] Cards =
    {
        "THANK YOU POE! - THE MILLER FAMILY",
        "THE MEDICINE IS PACKED. - DAWN CLINIC",
        "NIGHT FLIGHT SAVED US. - THE OWL MILL",
        "YOU DODGED EVERY ROCKET. - PIP",
        "THE EAGLE IS FREE. - GRANNY WREN",
        "SOFT LANDING. WARM BREAD. - THE DOCK",
        "WE CAUGHT THE CRATES. - BUBBA",
        "WIND AND ALL YOU STACKED IT. - CREW",
        "NIGHT PIPES DID NOT STOP YOU. - PIP",
        "THE HARBOR IS OPEN. - SUNNY SEA",
        "YOU LANDED IN THE GALE. - STORM POST",
        "THE BOLTS MISSED. - THE BEACON",
        "QUOTA FILLED. BELLIES TOO. - THE MESS",
        "DARK SKY. CLEAN DODGE. - PIP",
        "YOU FLEW THE EYE TOGETHER. - EAGLE",
        "THE HOSE HELD. - BUBBA",
        "NIGHT STRIP. GENTLE WHEELS. - FIELD",
        "TALL STACK. SHORT FUSE. - THE DOCK",
        "EVERY BOLT COUNTED. - THE LOOKOUT",
        "THE TOWER IS DOWN. - GRANNY WREN",
        "NIGHT DROPS ALL CAUGHT. - BUBBA",
        "THE STORM BLINKED FIRST. - PIP",
        "WINDY HOSE. STEADY HAND. - BUBBA",
        "LAST PIPES. LAST LIGHT. - DAWN",
        "THE BEACON CAN SEE US. - THE ISLES",
    };

    public static readonly Cutscene[] All = Build();

    public static Cutscene Get(string id)
    {
        if (string.IsNullOrEmpty(id) || All == null) return null;
        for (int i = 0; i < All.Length; i++)
            if (All[i] != null && All[i].id == id) return All[i];
        return null;
    }

    /// <summary>World-open id for the first level of a world, else null. Levels 1, 6, 11, 16, 21.</summary>
    public static string EnterId(int levelId)
    {
        if (levelId < 1 || (levelId - 1) % LevelDB.PerWorld != 0) return null;
        return "world" + ((levelId - 1) / LevelDB.PerWorld + 1);
    }

    public static string BossInId(int levelId) => BossId(levelId, "_in");
    public static string BossOutId(int levelId) => BossId(levelId, "_out");

    static string BossId(int levelId, string suffix)
    {
        if (levelId < 1 || levelId % LevelDB.PerWorld != 0) return null;
        int n = levelId / LevelDB.PerWorld - 1;
        return n >= 0 && n <= 4 ? "boss" + n + suffix : null;
    }

    public static string EndingId(int stars) => stars >= GoldStars ? "ending_gold" : "ending_normal";

    public static string PipLine(int levelId)
    {
        int i = levelId - 1;
        return i >= 0 && i < PipLines.Length ? PipLines[i] : "STILL GOT SOME SKY IN YOU.";
    }

    public static string PostcardId(int levelId) => levelId >= 1 && levelId <= Cards.Length ? "pc" + levelId.ToString("00") : null;

    public static string PostcardLine(int levelId)
    {
        int i = levelId - 1;
        return i >= 0 && i < Cards.Length ? Cards[i] : "";
    }

    static Shot S(string art, string text, float dur, string sfx = null, string music = null,
        Transition inn = Transition.Cut, Transition outT = Transition.Cut)
    {
        return new Shot
        {
            art = art, text = text, dur = dur, sfx = sfx, music = music, inT = inn, outT = outT,
            panFrom = new Vector2(-0.2f, -0.05f), panTo = new Vector2(0.22f, 0.08f),
            zoomFrom = 1.08f, zoomTo = 1.16f,
        };
    }

    static Shot C(Shot s, Action a) { s.custom = a; return s; }

    static void Projector() { if (FilmFilter.I) FilmFilter.I.scratch = 0.9f; }

    static void FlockBeat() { CutscenePlayer.I?.PlayBeat(FlockBoom()); }

    static IEnumerator FlockBoom()
    {
        yield return new WaitForSecondsRealtime(0.85f);
        AudioManager.Sfx("explosion", 0.75f);
        CutscenePlayer.I?.Nudge(0.2f, 0.45f);
    }

    static void EngineBeat() { CutscenePlayer.I?.PlayBeat(Engine()); }

    static IEnumerator Engine()
    {
        AudioManager.Sfx("ready", 0.55f, 0.02f, 0.03f, 0.7f);
        yield return new WaitForSecondsRealtime(1.35f);
        yield return new WaitForSecondsRealtime(1.15f);
        AudioManager.Sfx("wallop", 0.9f);
        CutscenePlayer.I?.Nudge(0.16f, 0.4f);
    }

    static void TitleCard()
    {
        AudioManager.Sfx("super", 0.85f);
        CutscenePlayer.I?.Banner(Title);
    }

    static void Slam() { CutscenePlayer.I?.Nudge(0.18f, 0.35f); }

    static Cutscene[] Build()
    {
        var list = new List<Cutscene>
        {
            new Cutscene { id = "intro", shots = new[]
            {
                C(S(null, "", 4f, null, null, Transition.Iris), Projector),
                S("intro_isles", "ONCE THE SKYWARD ISLES WERE JOINED BY A THOUSAND LETTERS A DAY.", 10f, null, "music_flappy"),
                S("intro_letters", "EVERY LETTER WAS A LIGHT.", 6f, "score"),
                S("intro_citadel", "BUT CITADEL AIR WANTED THE SKY FOR ITSELF.", 8f, null, "music_boss"),
                C(S("intro_flock", "THE IRON FLOCK GROUNDED EVERY LITTLE PLANE.", 8f, "thunder"), FlockBeat),
                S("intro_hangar", "ALL BUT ONE.", 6f, "swoop"),
                S("intro_wren", "YOU'RE ALL WE'VE GOT. OLD BOY.", 7f),
                S("intro_pip", "PLEASE. WE NEED MEDICINE. - DAWN VALLEY", 8f, "flap"),
                C(S("intro_wake", "", 7f), EngineBeat),
                C(S("intro_logo", "", 6f, "card", "music_flappy"), TitleCard),
                S("intro_isles", "", 5f, null, "music_flappy", Transition.Cut, Transition.Iris),
            }},
            W("world1",
                S("w1_quiet", "DAWN VALLEY WAKES TO A QUIET SKY.", 7f, null, "music_flappy", Transition.Fade),
                S("w1_pip", "PLEASE. WE NEED MEDICINE.", 7f, "flap"),
                S("w1_wren", "STILL GOT SOME SKY IN YOU.", 6f)),
            W("world2",
                S("w2_port", "SUNNY SEA IS LOCKED. THE BOATS CANNOT LEAVE.", 7f, null, "music_flappy", Transition.Fade),
                S("w2_bubba", "I... I CAN DROP SUPPLIES. IF YOU CATCH THEM.", 7f),
                S("w2_crates", "CATCH EVERY CRATE BEFORE THE SEA TAKES IT.", 6f, "score")),
            W("world3",
                S("w3_storm", "THE BEACON IS FADING. THE STORMS ARE WINNING.", 7f, "thunder", "music_boss", Transition.Fade),
                S("w3_wren", "STILL GOT SOME SKY IN YOU.", 7f),
                S("w3_guns", "THOSE GUNS WHIP THE GALE. FLY THE EYE.", 6f)),
            W("world4",
                S("w4_hop", "SUNDOWN IS A LONG HOP. STAY ON THE HOSE.", 7f, null, "music_flappy", Transition.Fade),
                S("w4_bubba", "I'LL KEEP YOU FED. YOU KEEP US ALIVE.", 7f),
                S("w4_gilt", "GILT ONCE FLEW THE MAIL. THEY TOOK HIS WINGS.", 6f, "card", "music_boss")),
            W("world5",
                S("w5_escort", "PIP. BUBBA. EAGLE. NOBODY FLIES ALONE.", 7f, "flap", "music_flappy", Transition.Fade),
                S("w5_climb", "STARLIGHT IS A CLIMB OF LETTERS.", 7f),
                S("w5_machine", "THE BLACKOUT SITS ON THE BEACON.", 6f, "thunder", "music_boss")),
        };
        Boss(list, "boss0", "card_eagle", "TEMPEST EAGLE - THE CAGED KING", "GILT: HA-HA. THAT BIRD WORKS FOR ME.",
            "b0_free", "THE COLLAR SNAPS. THE EAGLE BOWS.", "parry",
            "b0_desk", "GILT SLAMS THE DESK. THE SKY IS NOT YOURS.", "wallop", true);
        Boss(list, "boss1", "card_wake", "IRONWAKE - THE HARBOR LOCK", "GILT: NO BOAT LEAVES MY HARBOR.",
            "b1_sink", "IRONWAKE GOES UNDER.", "explosion",
            "b1_boats", "THE FISHING BOATS SAIL. FLAGS WAVE.", "victory", false);
        Boss(list, "boss2", "card_citadel", "SUNDOWN CITADEL - THE STORM GUNS", "GILT: I WILL BLOW THE LIGHT OUT.",
            "b2_eye", "THE GUNS GO QUIET. THE EAGLE DIVES IN.", "swoop",
            "b2_carry", "TWO WINGS. ONE STORM. POE HOLDS ON.", "flap", false);
        Boss(list, "boss3", "card_flock", "IRON FLOCK - THE LAST SWARM", "GILT: HA-HA. FOUR GENERALS. ONE OLD PLANE.",
            "b3_fall", "THE TOWER FALLS.", "explosion",
            "b3_balloon", "GILT RUNS FOR STARLIGHT IN A BALLOON.", "swoop", false);
        Boss(list, "boss4", "card_black", "THE BLACKOUT - EATER OF LIGHT", "GILT: I DRAINED THE BEACON. NOW I DRAIN YOU.",
            "b4_crack", "THE MACHINE CRACKS. LIGHT POURS OUT.", "super",
            "b4_dark", "GILT FALLS WITH HIS OWN DARK.", "knockout", true);
        list.Add(new Cutscene { id = "ending_normal", shots = new[]
        {
            S("end_dim", "THE BEACON FLICKERS. IT HOLDS.", 6f, null, "music_flappy", Transition.Fade),
            S("end_wren", "IT'S A START. THERE ARE STILL LETTERS OUT THERE.", 7f),
            S("end_left", "GO BACK. THE DARK STILL HAS MAIL IN IT.", 6f, "score"),
        }});
        list.Add(new Cutscene { id = "ending_gold", shots = new[]
        {
            S("end_rise", "EVERY LETTER RISES TO THE BEACON.", 6f, "score", "music_flappy", Transition.Fade),
            S("end_gold", "THE LIGHT COMES BACK GOLD.", 7f, "super"),
            S("end_paint", "POE IS PAINTED NEW. GILT SORTS THE MAIL.", 7f, "victory"),
        }});
        list.Add(new Cutscene { id = "postcredits", shots = new[]
        {
            S("end_strange", "A STRANGE LETTER. THE STAMP IS NOT ON ANY MAP.", 7f, "score", "music_boss", Transition.PageTurn),
        }});
        return list.ToArray();
    }

    static Cutscene W(string id, Shot a, Shot b, Shot c) => new Cutscene { id = id, shots = new[] { a, b, c } };

    static void Boss(List<Cutscene> list, string id, string card, string name, string taunt,
        string outA, string lineA, string sfxA, string outB, string lineB, string sfxB, bool slamOut)
    {
        list.Add(new Cutscene { id = id + "_in", shots = new[]
        {
            S(card, name, 4.5f, "card", "music_boss", Transition.Iris),
            S(card, taunt, 4.5f, "roar"),
        }});
        var last = S(outB, lineB, 5f, sfxB);
        if (slamOut) last = C(last, Slam);
        list.Add(new Cutscene { id = id + "_out", shots = new[] { S(outA, lineA, 5f, sfxA), last } });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void SelfCheck()
    {
        string why = Audit();
        if (why != null) Debug.LogError("[CutsceneDB] " + why);
    }

    /// <summary>Null when the catalogue is legal. The message names the first broken line.</summary>
    public static string Audit()
    {
        if (All == null || All.Length == 0) return "empty";
        var ids = new HashSet<string>();
        float intro = 0f;
        const string sfxOk = " explosion roar card iris swoop dash wallop thunder super shoot hurt victory score ready parry knockout hit flap ex death ";
        for (int i = 0; i < All.Length; i++)
        {
            var c = All[i];
            if (c == null || string.IsNullOrEmpty(c.id) || c.shots == null || c.shots.Length == 0) return "bad cutscene";
            if (!ids.Add(c.id)) return "duplicate " + c.id;
            for (int s = 0; s < c.shots.Length; s++)
            {
                var shot = c.shots[s];
                if (shot == null || shot.dur <= 0f) return c.id + " dur";
                if (!Legal(shot.text)) return c.id + " text";
                if (!string.IsNullOrEmpty(shot.sfx) && sfxOk.IndexOf(" " + shot.sfx + " ", StringComparison.Ordinal) < 0) return c.id + " sfx " + shot.sfx;
                if (!string.IsNullOrEmpty(shot.music) && shot.music != "music_flappy" && shot.music != "music_boss") return c.id + " music";
                if (c.id == "intro") intro += shot.dur;
            }
        }
        if (intro < 74f || intro > 76f) return "intro dur " + intro;
        string[] need =
        {
            "intro", "world1", "world2", "world3", "world4", "world5",
            "boss0_in", "boss0_out", "boss1_in", "boss1_out", "boss2_in", "boss2_out",
            "boss3_in", "boss3_out", "boss4_in", "boss4_out",
            "ending_normal", "ending_gold", "postcredits",
        };
        for (int i = 0; i < need.Length; i++)
            if (!ids.Contains(need[i])) return "missing " + need[i];
        if (PipLines.Length != 25 || Cards.Length != 25) return "lines";
        for (int i = 0; i < 25; i++)
        {
            if (!Legal(PipLines[i]) || PipLines[i].Length > 42) return "pip " + (i + 1);
            if (!Legal(Cards[i]) || Cards[i].Length > 48) return "card " + (i + 1);
        }
        if (!Legal(Title)) return "title";
        if (EnterId(1) != "world1" || EnterId(6) != "world2" || EnterId(5) != null) return "enter";
        if (BossInId(5) != "boss0_in" || BossOutId(25) != "boss4_out" || BossInId(4) != null) return "boss id";
        if (EndingId(59) != "ending_normal" || EndingId(60) != "ending_gold") return "ending";
        return null;
    }

    public static bool Legal(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch != ' ' && PixelText.Charset.IndexOf(ch) < 0) return false;
        }
        return true;
    }
}
