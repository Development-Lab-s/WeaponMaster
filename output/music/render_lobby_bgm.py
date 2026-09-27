"""Render an original, sample-free lobby cue. Requires Python 3 and numpy.

All note tails and room reflections wrap around the exact musical loop.
Run from any directory; outputs are relative to this file's project root.
"""

from __future__ import annotations

import json
import math
import struct
import wave
from dataclasses import dataclass
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
ASSETS = ROOT / "Assets/WorkFolder/BK/06_Audio/BGM"
NAME = "Lobby_Quiet_Courtyard_80BPM"
SR = 44100
BPM = 80
BEAT = 60 / BPM
BARS = 32
BEATS = BARS * 4
FRAMES = round(BEATS * BEAT * SR)
RNG = np.random.default_rng(260926)


@dataclass
class Note:
    voice: str
    beat: float
    pitch: int
    length: float
    velocity: float
    pan: float = 0


NOTES: list[Note] = []


def add(voice, beat, pitch, length, velocity, pan=0):
    NOTES.append(Note(voice, beat, pitch, length, velocity, pan))


# D major, with added ninths and sixths for a calm, open-air waiting room.
# The bass supplies each root, leaving soft extensions in the upper parts.
HARMONY = [
    (38, [54, 57, 61, 64], [62, 66, 69, 73]),  # Dmaj9
    (35, [54, 57, 59, 62], [59, 62, 66, 69]),  # Bm7
    (43, [54, 57, 59, 62], [59, 62, 66, 69]),  # Gmaj9
    (33, [52, 57, 59, 61], [61, 64, 69, 71]),  # Aadd9
    (42, [52, 57, 61, 66], [61, 64, 66, 69]),  # F#m7
    (35, [54, 57, 59, 62], [59, 62, 66, 69]),  # Bm7
    (40, [54, 55, 59, 62], [59, 62, 66, 67]),  # Em9
    (33, [54, 57, 59, 61], [61, 64, 66, 69]),  # A6/9
]

# Original, unhurried two-bar phrases. Rests are intentional.
MELODY_A = [
    [(.50, 69, 1.15), (2.25, 73, 1.20)],
    [(0, 74, 1.90), (2.75, 69, .85)],
    [(.75, 71, 1.30), (2.75, 69, .85)],
    [(0, 64, 2.50)],
    [(.50, 66, 1.20), (2.25, 69, 1.10)],
    [(0, 74, 1.75), (2.5, 73, .65), (3.25, 71, .50)],
    [(.50, 69, 1.0), (2, 67, 1.50)],
    [(0, 66, 1.30), (2, 64, 1.20)],
]
MELODY_B = [
    [(.50, 78, 1.30), (2.5, 76, 1.10)],
    [(0, 74, 2.20)],
    [(.75, 74, .95), (2.25, 71, 1.20)],
    [(0, 73, 1.70), (2.75, 69, .70)],
    [(.50, 73, 1.30), (2.5, 76, 1.10)],
    [(0, 74, 1.50), (2.25, 71, 1.10)],
    [(.50, 69, 1.10), (2.25, 67, 1.20)],
    [(0, 66, 1.50), (2.5, 64, .90)],
]


def compose():
    for bar in range(BARS):
        start = bar * 4
        section, phrase = divmod(bar, 8)
        root, chord, arp = HARMONY[phrase]
        density = [.86, .95, 1.0, .82][section]

        add("bass", start, root, 3.75, .16 * density, 0)
        if section in (1, 2):
            add("bass", start + 2.70, root + 12, 1.1, .038, 0)

        # Rolled electric-piano voicings: soft attacks, long decays.
        for key, pitch in enumerate(chord):
            add("keys", start + .03 + key * .028, pitch + 12,
                3.6, .060 * density, -.24 + key * .12)

        # Sparse, softly plucked answers with room between notes.
        slots, indexes = [1.05, 2.08, 3.10], [0, 2, 1]
        if section in (1, 2):
            slots, indexes = [.55, 1.55, 2.55, 3.55], [0, 2, 1, 3]
        for index, (at, tone) in enumerate(zip(slots, indexes)):
            add("pluck", start + at, arp[tone], .70,
                (.037 if index % 2 else .045) * density, .32)

        # Slow, warm pad throughout; the middle gradually opens up.
        for index, pitch in enumerate(chord[:3]):
            add("strings", start - .12, pitch, 4.05,
                (.030 if section in (1, 2) else .024),
                [-.58, .05, .57][index])

        melody = MELODY_B[phrase] if section == 2 else MELODY_A[phrase]
        voice = "woodwind" if section == 2 else "keys"
        for at, pitch, length in melody:
            if section == 3 and phrase in (2, 4):
                continue
            add(voice, start + at, pitch, length,
                .066 if voice == "woodwind" else .11 * density, -.10)

        if section == 1 and phrase in (1, 3, 5):
            add("mallet", start + 3.15, arp[1] + 12, .85, .030, .40)


def noise(length, smooth=1):
    result = RNG.standard_normal(length)
    if smooth > 1:
        result = np.convolve(result, np.ones(smooth) / smooth, mode="same")
    return result


def synth(note):
    duration = note.length * BEAT
    release = {"lute": .35, "pluck": .55, "bass": .55, "keys": 1.30,
               "woodwind": .36, "mallet": .90, "strings": .95}.get(note.voice, .08)
    t = np.arange(round((duration + release) * SR), dtype=np.float64) / SR
    freq = 440 * 2 ** ((note.pitch - 69) / 12)
    phase = 2 * np.pi * freq * t
    sig = np.zeros_like(t)

    if note.voice == "keys":
        # Mellow electric-piano body with a restrained, bell-like tine.
        for harmonic, amp, decay in [(1, .90, 2.4), (2, .22, 1.25),
                                      (3, .080, .65), (4, .023, .24)]:
            sig += amp * np.sin(phase * harmonic) * np.exp(-t / decay)
        sig += .075 * np.sin(phase * 1.0018) * np.exp(-t / 1.8)
        sig += .035 * np.sin(phase * 6.01) * np.exp(-t / .10)
        attack = .018
    elif note.voice in ("lute", "pluck"):
        # Damped string partials, with a little body resonance and pick noise.
        bright = note.voice == "pluck"
        for harmonic in range(1, 13):
            if freq * harmonic > SR * .44:
                break
            amp = (1 / harmonic ** (1.55 if bright else 1.8))
            amp *= .74 + .26 * math.cos(harmonic * .85)
            decay = (.66 if bright else .83) / (1 + harmonic * .18)
            sig += amp * np.sin(phase * harmonic + .09 * harmonic) * np.exp(-t / decay)
        sig += .025 * noise(len(t), 3) * np.exp(-t / .013)
        sig += .06 * np.sin(2 * np.pi * (freq * 2.003) * t) * np.exp(-t / .42)
        attack = .004
    elif note.voice == "bass":
        sig = (np.sin(phase) + .25 * np.sin(phase * 2) + .065 * np.sin(phase * 3))
        sig *= np.exp(-t / 1.80)
        sig += .018 * noise(len(t), 22) * np.exp(-t / .021)
        attack = .035
    elif note.voice == "woodwind":
        vibrato = .004 * np.sin(2 * np.pi * 4.8 * t) * (1 - np.exp(-t / .22))
        phase = np.cumsum(2 * np.pi * freq * (1 + vibrato) / SR)
        sig = .77 * np.sin(phase) + .20 * np.sin(phase * 2) + .09 * np.sin(phase * 3)
        sig += .021 * noise(len(t), 7)
        sig *= .94 + .06 * np.sin(2 * np.pi * 2.6 * t)
        attack = .085
    elif note.voice == "mallet":
        for ratio, amp, decay in [(1, .85, .48), (3.99, .19, .12), (9.97, .025, .05)]:
            if freq * ratio < SR * .44:
                sig += amp * np.sin(phase * ratio) * np.exp(-t / decay)
        sig += .02 * noise(len(t), 4) * np.exp(-t / .009)
        attack = .003
    elif note.voice == "strings":
        for detune in (-.0018, .0016):
            for harmonic in range(1, 8):
                sig += (np.sin(phase * harmonic * (1 + detune)
                               + .12 * np.sin(2 * np.pi * 4.1 * t))
                        / harmonic ** 2.7 / 2)
        sig *= .94 + .06 * np.sin(2 * np.pi * .72 * t)
        attack = .60
    elif note.voice == "drum":
        base = 70 if note.pitch == 36 else 118 if note.pitch == 41 else 151
        drum_phase = 2 * np.pi * (base * t + 30 * .022 * (1 - np.exp(-t / .022)))
        sig = (np.sin(drum_phase) * np.exp(-t / .13)
               + .24 * np.sin(drum_phase * 1.59) * np.exp(-t / .065)
               + .10 * noise(len(t), 11) * np.exp(-t / .024))
        attack = .002
    elif note.voice == "rim":
        sig = (.48 * np.sin(2 * np.pi * 940 * t) * np.exp(-t / .012)
               + .30 * np.sin(2 * np.pi * 1480 * t) * np.exp(-t / .008)
               + .27 * noise(len(t), 3) * np.exp(-t / .013))
        attack = .001
    else:
        raw = noise(len(t))
        smooth = np.convolve(raw, np.ones(9) / 9, mode="same")
        sig = (raw - smooth) * np.exp(-t / .028)
        attack = .005

    envelope = np.minimum(t / attack, 1)
    # Smooth note-off, including a true zero at the end of every tail.
    off = np.clip((t - duration) / release, 0, 1)
    envelope *= .5 + .5 * np.cos(np.pi * off)
    edge = min(len(t), round(.010 * SR))
    envelope[-edge:] *= np.linspace(1, 0, edge)
    return (sig * envelope * note.velocity).astype(np.float32)


def mix_wrapped(target, signal, start, pan):
    start %= FRAMES
    gains = np.array([math.cos((pan + 1) * np.pi / 4),
                      math.sin((pan + 1) * np.pi / 4)], dtype=np.float32)
    end = min(FRAMES - start, len(signal))
    target[start:start + end] += signal[:end, None] * gains
    if end < len(signal):
        target[:len(signal) - end] += signal[end:, None] * gains


def render():
    dry = np.zeros((FRAMES, 2), np.float32)
    send = np.zeros_like(dry)
    for note in NOTES:
        sig = synth(note)
        frame = round(note.beat * BEAT * SR)
        mix_wrapped(dry, sig, frame, note.pan)
        amount = {"woodwind": .32, "strings": .50, "mallet": .36,
                  "keys": .32, "lute": .14, "pluck": .25, "bass": .065,
                  "drum": .065, "rim": .12, "shaker": .035}[note.voice]
        mix_wrapped(send, sig * amount, frame, note.pan)

    # Circular FIR room response: the next loop begins with the previous tail.
    wet = np.zeros_like(dry)
    smooth_send = (send + np.roll(send, 1, axis=0) * 2 + np.roll(send, 2, axis=0)) / 4
    for index in range(40):
        delay = .031 + index * .049 + .007 * math.sin(index * 2.17)
        gain = .36 * math.exp(-delay / .65)
        reflected = np.roll(smooth_send, round(delay * SR), axis=0)
        if index % 2:
            reflected = reflected[:, ::-1]
        wet += reflected * gain
    mix = dry + wet
    mix -= mix.mean(axis=0)
    mix = np.tanh(mix * 1.18) / 1.18
    rms = float(np.sqrt(np.mean(mix ** 2)))
    peak = float(np.max(np.abs(mix)))
    gain = min(10 ** (-20 / 20) / rms, 10 ** (-3 / 20) / peak)
    mix *= gain
    # Triangular dither at one 16-bit LSB.
    dither = (RNG.random(mix.shape, dtype=np.float32)
              - RNG.random(mix.shape, dtype=np.float32)) / 65536
    pcm = np.round(np.clip(mix + dither, -.9999, .9999) * 32767).astype('<i2')
    return mix, pcm


def write_wav(path, pcm):
    with wave.open(str(path), "wb") as file:
        file.setnchannels(2)
        file.setsampwidth(2)
        file.setframerate(SR)
        file.writeframes(pcm.tobytes())


def vlq(value):
    result = [value & 127]
    while (value := value >> 7):
        result.insert(0, (value & 127) | 128)
    return bytes(result)


def midi_track(events):
    data, previous = bytearray(), 0
    for tick, priority, payload in sorted(events, key=lambda item: (item[0], item[1])):
        data.extend(vlq(tick - previous))
        data.extend(payload)
        previous = tick
    data.extend(vlq(BEATS * 480 - previous) + b'\xff\x2f\x00')
    return b'MTrk' + struct.pack('>I', len(data)) + data


def write_midi(path):
    tempo = round(60_000_000 / BPM).to_bytes(3, 'big')
    tracks = [midi_track([(0, 0, b'\xff\x51\x03' + tempo),
                          (0, 1, b'\xff\x58\x04\x04\x02\x18\x08')])]
    voices = {"keys": (0, 4), "pluck": (1, 24), "bass": (2, 32),
              "woodwind": (3, 74), "mallet": (4, 12), "strings": (5, 48),
              "drum": (9, None), "rim": (9, None), "shaker": (9, None)}
    for voice, (channel, program) in voices.items():
        name = voice.encode('ascii')
        events = [(0, 0, b'\xff\x03' + vlq(len(name)) + name)]
        if program is not None:
            events.append((0, 1, bytes([0xC0 + channel, program])))
        for note in NOTES:
            if note.voice != voice:
                continue
            begin = max(0, round(note.beat * 480))
            end = min(BEATS * 480, round((note.beat + note.length) * 480))
            velocity = max(1, min(110, round(note.velocity * 300)))
            events += [(begin, 3, bytes([0x90 + channel, note.pitch, velocity])),
                       (end, 2, bytes([0x80 + channel, note.pitch, 0]))]
        tracks.append(midi_track(events))
    path.write_bytes(b'MThd' + struct.pack('>IHHH', 6, 1, len(tracks), 480) + b''.join(tracks))


def main():
    ASSETS.mkdir(parents=True, exist_ok=True)
    compose()
    print(f"Rendering {len(NOTES)} notes, {FRAMES / SR:.1f} seconds", flush=True)
    mix, pcm = render()
    write_wav(ASSETS / f"{NAME}.wav", pcm)
    # Preview includes eight bars of the opening and eight of the variation.
    section_frames = round(8 * 4 * BEAT * SR)
    preview = np.concatenate([pcm[:section_frames], pcm[section_frames * 2:section_frames * 3]]).copy()
    fade = round(.08 * SR)
    join = section_frames
    preview[join - fade:join] = (preview[join - fade:join] * np.linspace(1, 0, fade)[:, None]).astype('<i2')
    preview[join:join + fade] = (preview[join:join + fade] * np.linspace(0, 1, fade)[:, None]).astype('<i2')
    end_fade = round(1.0 * SR)
    preview[-end_fade:] = (preview[-end_fade:] * np.linspace(1, 0, end_fade)[:, None]).astype('<i2')
    write_wav(OUT / f"{NAME}_Preview.wav", preview)
    # One junction, suitable for auditioning the last two and first two bars.
    two_bars = round(8 * BEAT * SR)
    write_wav(OUT / f"{NAME}_LoopCheck.wav", np.concatenate([pcm[-two_bars:], pcm[:two_bars]]))
    write_midi(OUT / f"{NAME}.mid")

    peak = float(np.max(np.abs(mix)))
    rms = float(np.sqrt(np.mean(mix ** 2)))
    seam = float(np.max(np.abs(mix[0] - mix[-1])))
    local = np.concatenate([mix[-1000:], mix[:1000]])
    derivative_rms = float(np.sqrt(np.mean(np.diff(local, axis=0) ** 2)))
    result = {
        "title": "Quiet Courtyard", "bpm": BPM, "key": "D major",
        "bars": BARS, "seconds": FRAMES / SR, "sample_rate": SR,
        "channels": 2, "bits": 16, "note_events": len(NOTES),
        "peak_dbfs": 20 * math.log10(peak), "rms_dbfs": 20 * math.log10(rms),
        "clipped_samples": int(np.sum(np.abs(pcm.astype(np.int32)) >= 32767)),
        "loop_boundary_delta": seam, "local_sample_delta_rms": derivative_rms,
        "loop_boundary_delta_dbfs": 20 * math.log10(max(seam, 1e-12)),
        "stereo_correlation": float(np.corrcoef(mix[::10].T)[0, 1]),
        "finite": bool(np.isfinite(mix).all()),
        "master": str(ASSETS / f"{NAME}.wav"),
        "render_seed": 260926,
    }
    assert result["finite"] and result["clipped_samples"] == 0
    assert seam < .02, f"Unexpected loop discontinuity: {seam}"
    (OUT / "render_report.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    print(json.dumps(result, indent=2), flush=True)


if __name__ == "__main__":
    main()
