"""Synthesised 1930s-flavoured audio: SFX + two looping tunes (ragtime piano, swing big band) with vinyl crackle.
Writes 16-bit mono WAVs into Assets/_Game/Audio. Usage: python synth.py [--only name]"""
import argparse
import wave
from pathlib import Path
import numpy as np

SR = 22050
OUT = Path(__file__).resolve().parents[2] / "Assets" / "_Game" / "Audio"
rng = np.random.default_rng(1930)


# ------------------------------------------------------------------ primitives
def t_(d):
    return np.arange(int(SR * d)) / SR


def midi(n):
    return 440.0 * 2 ** ((n - 69) / 12)


NOTE = {"C": 0, "C#": 1, "Db": 1, "D": 2, "D#": 3, "Eb": 3, "E": 4, "F": 5, "F#": 6, "Gb": 6, "G": 7, "G#": 8,
        "Ab": 8, "A": 9, "A#": 10, "Bb": 10, "B": 11}


def n(name):
    """'C#5' -> midi number."""
    return NOTE[name[:-1]] + 12 * (int(name[-1]) + 1)


def phase(freq, d):
    f = np.broadcast_to(np.asarray(freq, dtype=float), (int(SR * d),))
    return 2 * np.pi * np.cumsum(f) / SR


def sine(freq, d):
    return np.sin(phase(freq, d))


def square(freq, d, duty=0.5):
    p = (phase(freq, d) / (2 * np.pi)) % 1.0
    return np.where(p < duty, 1.0, -1.0)


def saw(freq, d):
    p = (phase(freq, d) / (2 * np.pi)) % 1.0
    return 2 * p - 1


def noise(d):
    return rng.uniform(-1, 1, int(SR * d))


def lowpass(x, cutoff):
    """One-pole low-pass; cutoff may be an array (sweeps)."""
    c = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    a = 1 - np.exp(-2 * np.pi * c / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc += a[i] * (x[i] - acc)
        y[i] = acc
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def env(d, a=0.005, decay=None, hold=0.0, release=0.02):
    """Attack, optional exponential decay (time constant), short release to zero."""
    t = t_(d)
    e = np.minimum(1.0, t / max(a, 1e-4))
    if decay:
        e = e * np.exp(-np.maximum(0, t - a - hold) / decay)
    r = int(SR * release)
    if r and len(e) > r:
        e[-r:] *= np.linspace(1, 0, r)
    return e


def mix(*parts):
    L = max(len(p) for p in parts)
    out = np.zeros(L)
    for p in parts:
        out[:len(p)] += p
    return out


def at(buf, start, x):
    """Add x into buf at sample `start`, wrapping past the end (seamless loops)."""
    i = int(start) % len(buf)
    k = min(len(x), len(buf) - i)
    buf[i:i + k] += x[:k]
    if k < len(x):
        rest = x[k:]
        while len(rest):
            m = min(len(rest), len(buf))
            buf[:m] += rest[:m]
            rest = rest[m:]


def write(name, x, peak=0.89):
    x = np.asarray(x, dtype=float)
    m = np.max(np.abs(x)) or 1.0
    x = x / m * peak
    OUT.mkdir(parents=True, exist_ok=True)
    with wave.open(str(OUT / f"{name}.wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((x * 32767).astype("<i2").tobytes())
    print(f"wrote {name}.wav {len(x) / SR:.2f}s peak {peak}")


# ------------------------------------------------------------------ instruments
def piano(freq, d, vel=1.0):
    t = t_(d)
    x = sum(np.sin(2 * np.pi * freq * k * t * (1 + 0.0007 * k)) * (0.6 ** (k - 1)) for k in range(1, 6))
    x += 0.3 * np.sin(2 * np.pi * freq * 1.003 * t)   # honky-tonk detune
    return x * env(d, 0.003, decay=0.35 + 120 / (freq + 200), release=0.04) * vel


def brass(freq, d, vel=1.0, bright=2400):
    t = t_(d)
    vib = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.minimum(1, t / 0.15)
    x = saw(freq * vib, d) + 0.4 * square(freq * vib * 0.5, d, 0.3)
    e = env(d, 0.035, release=0.06)
    x = lowpass(x * e, bright * (0.5 + 0.5 * np.minimum(1, t / 0.06)))
    return x * vel


def bass(freq, d, vel=1.0):
    t = t_(d)
    x = np.sin(2 * np.pi * freq * t) + 0.35 * np.sin(4 * np.pi * freq * t) + 0.12 * np.sin(6 * np.pi * freq * t)
    return x * env(d, 0.004, decay=0.28, release=0.03) * vel


def brush(d=0.18, vel=1.0):
    return highpass(noise(d), 1800) * env(d, 0.01, decay=0.06) * vel


def ride(vel=1.0):
    d = 0.5
    t = t_(d)
    metal = sum(np.sin(2 * np.pi * f * t) for f in (3150, 4270, 5630, 7020)) * 0.15
    return (highpass(noise(d), 5000) * 0.5 + metal) * env(d, 0.002, decay=0.12) * vel


def kick(vel=1.0):
    d = 0.25
    t = t_(d)
    return np.sin(2 * np.pi * (55 + 90 * np.exp(-t / 0.03)) * t) * env(d, 0.002, decay=0.08) * vel


def vinyl(total):
    x = lowpass(noise(total), 900) * 0.012
    clicks = rng.random(len(x)) < 9 / SR
    x[clicks] += rng.uniform(-0.25, 0.25, clicks.sum())
    return x


def chord_notes(root, quality):
    iv = {"maj": (0, 4, 7), "min": (0, 3, 7), "7": (0, 4, 7, 10), "m7": (0, 3, 7, 10), "dim": (0, 3, 6, 9)}[quality]
    return [root + i for i in iv]


# ------------------------------------------------------------------ sfx
def sfx():
    d = 0.07
    write("shoot", square(np.linspace(1300, 520, int(SR * d)), d, 0.25) * env(d, 0.002, decay=0.03), 0.5)
    d = 0.05
    write("hit", highpass(noise(d), 2500) * env(d, 0.001, decay=0.012) + 0.4 * square(1800, d) * env(d, 0.001, decay=0.008), 0.45)
    d = 0.45
    write("hurt", mix(saw(np.linspace(620, 120, int(SR * d)), d) * env(d, 0.003, decay=0.18),
                      lowpass(noise(d), 1500) * env(d, 0.002, decay=0.08)), 0.8)
    d = 0.22
    write("dash", lowpass(noise(d), np.linspace(600, 4200, int(SR * d))) * env(d, 0.02, decay=0.09), 0.6)
    d = 0.9
    t = t_(d)
    ding = sum(np.sin(2 * np.pi * f * t) * a for f, a in ((1568, 1), (2349, 0.5), (3136, 0.35), (4186, 0.2)))
    write("parry", ding * env(d, 0.002, decay=0.22), 0.75)
    d = 0.35
    write("ex", mix(square(np.linspace(200, 900, int(SR * d)), d, 0.4) * env(d, 0.004, decay=0.12) * 0.6,
                    lowpass(noise(d), 2500) * env(d, 0.004, decay=0.15)), 0.75)
    d = 1.3
    t = t_(d)
    write("super", mix(saw(220 + 500 * t, d) * 0.5 * env(d, 0.05, release=0.3),
                       lowpass(noise(d), 3000) * env(d, 0.05, release=0.3) * 0.7,
                       sine(110, d) * env(d, 0.01, decay=0.5)), 0.85)
    d = 0.8
    t = t_(d)
    write("explosion", mix(lowpass(noise(d), np.linspace(2500, 200, int(SR * d))) * env(d, 0.002, decay=0.22),
                           np.sin(2 * np.pi * (40 + 70 * np.exp(-t / 0.05)) * t) * env(d, 0.002, decay=0.18)), 0.9)
    d = 1.0
    t = t_(d)
    growl = saw(105 * (1 + 0.04 * np.sin(2 * np.pi * 23 * t)), d) + 0.6 * saw(158, d)
    write("roar", lowpass(growl * (1 + 0.5 * noise(d)), 1400) * env(d, 0.06, decay=0.45), 0.85)
    d = 2.2
    t = t_(d)

    def bell(start):
        tt = np.maximum(0, t - start)
        return sum(np.sin(2 * np.pi * 760 * k * tt) * a for k, a in ((1, 1), (2.76, 0.5), (5.4, 0.25), (8.93, 0.12))) \
            * np.exp(-tt / 0.6) * (t >= start)
    write("knockout", bell(0) + bell(0.32), 0.85)
    for name, notes, dur in (("ready", [n("C4"), n("E4"), n("G4"), n("Bb4")], 0.55),
                             ("wallop", [n("F3"), n("C4"), n("F4"), n("A4"), n("C5")], 0.75)):
        x = sum(brass(midi(m), dur, bright=2600) for m in notes)
        if name == "wallop":
            x = mix(x, highpass(noise(1.0), 3000) * env(1.0, 0.002, decay=0.35) * 1.5, kick(2.0))
        write(name, x, 0.85)
    seq = [(n("G4"), 0.35), (n("F#4"), 0.35), (n("F4"), 0.35), (n("E4"), 1.1)]   # sad trombone
    x = np.zeros(0)
    for m, d in seq:
        tt = t_(d)
        wob = 1 + (0.03 * np.sin(2 * np.pi * 6 * tt) if d > 1 else 0)
        x = np.concatenate([x, brass(midi(m - 12) * wob, d, bright=1300)])
    write("death", x, 0.8)
    beat = 60 / 150
    fan = [(0, n("C4"), 0.5), (0.5, n("E4"), 0.5), (1, n("G4"), 0.5), (1.5, n("C5"), 1.5), (1.5, n("E5"), 1.5),
           (1.5, n("G4"), 1.5), (3.0, n("D5"), 0.25), (3.25, n("E5"), 2.0), (3.25, n("C5"), 2.0), (3.25, n("G4"), 2.0)]
    buf = np.zeros(int(SR * 3.8))
    for b, m, d in fan:
        x = brass(midi(m), d * beat + 0.1, bright=3000)
        i = int(b * beat * SR)
        buf[i:i + len(x)] += x[:len(buf) - i]
    write("victory", mix(buf, highpass(noise(2.0), 4000) * env(2.0, 0.002, decay=0.5) * 0.5), 0.85)
    d = 2.0
    write("thunder", mix(lowpass(noise(d), 260) * env(d, 0.01, decay=0.7) * 3,
                         highpass(noise(0.15), 1500) * env(0.15, 0.001, decay=0.04)), 0.9)
    d = 0.09
    write("flap", lowpass(noise(d), 1800) * env(d, 0.003, decay=0.03) + 0.3 * sine(np.linspace(300, 520, int(SR * d)), d) * env(d, 0.002, decay=0.03), 0.4)
    d = 0.2
    write("score", mix(square(1319, 0.07, 0.3) * env(0.07, 0.002, decay=0.04), np.concatenate([np.zeros(int(SR * 0.06)), square(1760, 0.14, 0.3) * env(0.14, 0.002, decay=0.05)])), 0.35)
    d = 0.5
    t = t_(d)
    write("card", sum(np.sin(2 * np.pi * f * t) * np.exp(-np.maximum(0, t - s) / 0.12) * (t >= s)
                      for f, s in ((1047, 0), (1319, 0.06), (1568, 0.12), (2093, 0.18))), 0.55)
    d = 0.12
    write("swoop", lowpass(noise(d), 2200) * env(d, 0.01, decay=0.05) + 0.4 * sine(np.linspace(900, 300, int(SR * d)), d) * env(d, 0.002, decay=0.05), 0.5)
    d = 0.5
    write("iris", lowpass(noise(d), np.linspace(300, 2500, int(SR * d))) * env(d, 0.1, decay=0.2), 0.45)


# ------------------------------------------------------------------ music
def render_song(bpm, bars, parts, swing=0.0):
    beat = 60 / bpm
    total = bars * 4 * beat
    buf = np.zeros(int(SR * total))

    def place(b, x):
        if swing:
            whole = np.floor(b)
            frac = b - whole
            if abs(frac - 0.5) < 1e-6:
                b = whole + 0.5 + swing   # push the off-beat eighth late
        at(buf, b * beat * SR, x)

    for fn in parts:
        fn(place, beat)
    return buf


def ragtime():
    chords = [[("C", "maj")], [("C", "maj")], [("G", "7")], [("C", "maj")],
              [("F", "maj")], [("C", "maj")], [("D", "7"), ("G", "7")], [("C", "maj")],
              [("A", "7")], [("D", "min")], [("G", "7")], [("C", "maj")],
              [("F", "maj"), ("F", "min")], [("C", "maj"), ("A", "7")], [("D", "7"), ("G", "7")], [("C", "maj"), ("G", "7")]]
    mel = """E5 0 .5|G5 .5 .5|C6 1 .5|A5 1.5 1|G5 2.5 .5|E5 3 1
D5 0 .5|E5 .5 .5|G5 1 .5|E5 1.5 1.5|D5 3 .5|C5 3.5 .5
B4 0 .5|D5 .5 .5|F5 1 .5|G5 1.5 1|F5 2.5 .5|D5 3 1
C5 0 1.5|E5 1.5 .5|G5 2 .5|C6 2.5 1.5
A5 0 .5|C6 .5 .5|A5 1 .5|F5 1.5 1|G5 2.5 .5|A5 3 1
G5 0 .5|E5 .5 .5|C5 1 .5|E5 1.5 1|G5 2.5 .5|E5 3 1
F#5 0 .5|A5 .5 .5|C6 1 1|B5 2 .5|G5 2.5 .5|F5 3 .5|D5 3.5 .5
C5 0 2|G4 2 .5|A4 2.5 .5|B4 3 .5|C5 3.5 .5
C#5 0 .5|E5 .5 .5|G5 1 .5|A5 1.5 1|G5 2.5 .5|E5 3 1
D5 0 .5|F5 .5 .5|A5 1 .5|D6 1.5 1|C6 2.5 .5|A5 3 1
B5 0 .5|G5 .5 .5|D5 1 .5|F5 1.5 1|E5 2.5 .5|D5 3 1
E5 0 .5|G5 .5 .5|C6 1 1|G5 2 1|E5 3 1
A5 0 .5|C6 .5 .5|F5 1 1|Ab5 2 .5|C6 2.5 .5|F5 3 1
G5 0 .5|E5 .5 .5|C5 1 1|C#5 2 .5|E5 2.5 .5|G5 3 1
F#5 0 .5|A5 .5 .5|D6 1 1|B5 2 .5|D6 2.5 .5|F5 3 1
E5 0 1.5|D5 1.5 .5|C5 2 1|B4 3 .5|D5 3.5 .5""".split("\n")

    def left(place, beat):
        for bar, cs in enumerate(chords):
            for half in range(2):
                root, q = cs[min(half, len(cs) - 1)]
                r = NOTE[root] + 36
                notes = chord_notes(NOTE[root] + 60, q)
                b0 = bar * 4 + half * 2
                place(b0, piano(midi(r if half == 0 else r + 7 if q != "7" else r), beat * 0.9, 0.55))
                place(b0 + 1, sum(piano(midi(m), beat * 0.45, 0.22) for m in notes))

    def right(place, beat):
        for bar, line in enumerate(mel):
            for item in line.split("|"):
                nm, b, d = item.split()
                place(bar * 4 + float(b), piano(midi(n(nm)), float(d) * beat * 0.95 + 0.05, 0.5))
                place(bar * 4 + float(b), piano(midi(n(nm) - 12), float(d) * beat * 0.95 + 0.05, 0.18))

    buf = render_song(132, 16, [left, right])
    write("music_flappy", buf + vinyl(len(buf) / SR) * np.max(np.abs(buf)), 0.7)


def swing():
    prog = ["Dm", "Dm", "Gm", "Gm", "Dm", "Dm", "A7", "A7", "Bb7", "A7", "Dm", "Dm", "Gm", "A7", "Dm", "A7"]

    def parse(c):
        root = c[:2] if len(c) > 1 and c[1] in "b#" else c[:1]
        q = c[len(root):]
        return NOTE[root], {"m": "min", "7": "7", "": "maj"}[q]

    mel = """D5 0 .66|F5 .66 .34|G5 1 .66|Ab5 1.66 .34|A5 2 1|F5 3 .66|D5 3.66 .34
A5 0 1.5|C6 2 .66|A5 2.66 .34|F5 3 1
G5 0 .66|Bb5 .66 .34|D6 1 .66|C6 1.66 .34|Bb5 2 1|G5 3 1
D5 0 2|E5 2 .66|F5 2.66 .34|G5 3 1
D5 0 .66|F5 .66 .34|G5 1 .66|Ab5 1.66 .34|A5 2 1|F5 3 .66|D5 3.66 .34
A5 0 .66|C6 .66 .34|D6 1 1|C6 2 .66|A5 2.66 .34|F5 3 1
E5 0 .66|G5 .66 .34|C#6 1 .66|Bb5 1.66 .34|A5 2 1|G5 3 1
A5 0 2|G5 2 .66|F5 2.66 .34|E5 3 1
D6 0 .66|C6 .66 .34|Bb5 1 .66|Ab5 1.66 .34|F5 2 1|D5 3 1
C#6 0 .66|A5 .66 .34|G5 1 .66|E5 1.66 .34|C#5 2 2
D5 0 .66|F5 .66 .34|G5 1 .66|Ab5 1.66 .34|A5 2 1|F5 3 .66|D5 3.66 .34
F5 0 .66|A5 .66 .34|D6 1 1|A5 2 2
Bb5 0 .66|A5 .66 .34|G5 1 .66|F5 1.66 .34|E5 2 1|G5 3 1
C#6 0 .66|E6 .66 .34|D6 1 1|C#6 2 .66|Bb5 2.66 .34|A5 3 1
D6 0 2|A5 2 .66|F5 2.66 .34|D5 3 1
E5 0 .66|G5 .66 .34|A5 1 .66|C#6 1.66 .34|E6 2 2""".split("\n")

    def bassline(place, beat):
        for bar, c in enumerate(prog):
            r, q = parse(c)
            nr, _ = parse(prog[(bar + 1) % len(prog)])
            root = 38 + (r - 2) % 12
            third = 3 if q == "min" else 4
            nxt = 38 + (nr - 2) % 12
            line = [root, root + 7, root + third + 12 if bar % 2 else root + 12, nxt + (1 if bar % 2 else -1)]
            for i, m in enumerate(line):
                place(bar * 4 + i, bass(midi(m), beat * 0.95, 0.9))

    def drums(place, beat):
        for bar in range(len(prog)):
            for b in (0, 1, 1.5, 2, 3, 3.5):
                place(bar * 4 + b, ride(0.35 if b % 1 == 0 else 0.22))
            for b in (1, 3):
                place(bar * 4 + b, brush(0.2, 0.45))
            for b in (0, 2):
                place(bar * 4 + b, kick(0.5))
                place(bar * 4 + b, brush(0.35, 0.12))

    def comp(place, beat):
        for bar, c in enumerate(prog):
            r, q = parse(c)
            notes = chord_notes(60 + (r - 2) % 12 - 2 if r > 7 else 60 + r, q if q != "maj" else "7")
            for b, d in ((0, 0.35), (1.5, 0.3)):
                place(bar * 4 + b, sum(piano(midi(m), d * beat + 0.08, 0.2) for m in notes))

    def horns(place, beat):
        for bar, line in enumerate(mel):
            for item in line.split("|"):
                nm, b, d = item.split()
                b, d = float(b), float(d)
                if abs(b % 1 - 0.66) < 0.01:
                    b = b - 0.66 + 0.5   # notate swung eighths as straight; render_song applies the swing
                place(bar * 4 + b, brass(midi(n(nm)), d * beat * 0.92 + 0.04, 0.42))
                place(bar * 4 + b, brass(midi(n(nm) - 5), d * beat * 0.92 + 0.04, 0.16, bright=1500))

    buf = render_song(168, 16, [bassline, drums, comp, horns], swing=0.16)
    write("music_boss", buf + vinyl(len(buf) / SR) * np.max(np.abs(buf)), 0.72)


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    if a.only in "sfx":
        sfx()
    if a.only in "music_flappy":
        ragtime()
    if a.only in "music_boss":
        swing()
