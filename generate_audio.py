import wave, struct, math, os, random

os.makedirs('Assets/Audio', exist_ok=True)
sample_rate = 44100

def create_wav(filename, duration, gen_sample_fn):
    n_samples = int(sample_rate * duration)
    with wave.open(filename, 'w') as wav:
        wav.setnchannels(1) # mono
        wav.setsampwidth(2) # 16-bit
        wav.setframerate(sample_rate)
        data = bytearray()
        for i in range(n_samples):
            t = i / sample_rate
            sample = gen_sample_fn(t, duration)
            sample = max(-1.0, min(1.0, sample))
            ival = int(sample * 32767)
            data += struct.pack('<h', ival)
        wav.writeframes(data)
    print(f"Generated: {filename}")

# 1. SHOOT SFX: Hook launch whoosh / cable spool sound
def shoot_fn(t, dur):
    freq = 280 + 350 * (t / dur)
    env = math.sin(math.pi * (t / dur)) ** 0.5 * (1.0 - t / dur)
    noise = (random.random() * 2.0 - 1.0) * 0.3
    tone = math.sin(2 * math.pi * freq * t) * 0.7 + noise
    return tone * env * 0.65

# 2. CATCH SFX: Solid latch / metallic clink
def catch_fn(t, dur):
    env = math.exp(-t * 24)
    f1 = 440
    f2 = 880
    f3 = 1760
    noise = (random.random() * 2.0 - 1.0) * math.exp(-t * 30) * 0.4
    tone = (math.sin(2 * math.pi * f1 * t) * 0.5 + 
            math.sin(2 * math.pi * f2 * t) * 0.3 + 
            math.sin(2 * math.pi * f3 * t) * 0.2 + noise)
    return tone * env * 0.8

# 3. COLLECT SFX: Bright coin chime (B5 -> E6)
def collect_fn(t, dur):
    env = math.exp(-t * 7)
    if t < 0.09:
        tone = math.sin(2 * math.pi * 987.77 * t) + 0.3 * math.sin(2 * math.pi * 1975.5 * t)
    else:
        t2 = t - 0.09
        tone = math.sin(2 * math.pi * 1318.51 * t2) + 0.4 * math.sin(2 * math.pi * 2637.0 * t2)
    return tone * env * 0.75

# 4. BOMB EXPLOSION SFX: Deep rumble boom + white noise blast
def bomb_fn(t, dur):
    env = math.exp(-t * 5.0)
    freq = max(35.0, 160.0 * math.exp(-t * 6.0))
    bass = math.sin(2 * math.pi * freq * t) * 0.6
    noise = (random.random() * 2.0 - 1.0) * math.exp(-t * 8.0) * 0.7
    return (bass + noise) * env * 0.9

# 5. WIN SFX: Uplifting 4-note victory jingle (C5 -> E5 -> G5 -> C6)
def win_fn(t, dur):
    notes = [523.25, 659.25, 783.99, 1046.50]
    note_dur = dur / 4.0
    idx = min(int(t / note_dur), 3)
    t_note = t - idx * note_dur
    freq = notes[idx]
    env = math.exp(-t_note * 4.5)
    tone = math.sin(2 * math.pi * freq * t_note) + 0.25 * math.sin(4 * math.pi * freq * t_note)
    return tone * env * 0.7

# 6. GAMEOVER SFX: Descending wobble
def gameover_fn(t, dur):
    notes = [440.0, 392.0, 349.23, 261.63]
    note_dur = dur / 4.0
    idx = min(int(t / note_dur), 3)
    t_note = t - idx * note_dur
    freq = notes[idx]
    wobble = 1.0 + 0.08 * math.sin(2 * math.pi * 12 * t)
    env = math.exp(-t_note * 3.5)
    tone = math.sin(2 * math.pi * (freq * wobble) * t_note)
    return tone * env * 0.7

if __name__ == '__main__':
    create_wav('Assets/Audio/sfx_shoot.wav', 0.25, shoot_fn)
    create_wav('Assets/Audio/sfx_catch.wav', 0.2, catch_fn)
    create_wav('Assets/Audio/sfx_collect.wav', 0.4, collect_fn)
    create_wav('Assets/Audio/sfx_bomb.wav', 0.65, bomb_fn)
    create_wav('Assets/Audio/sfx_win.wav', 0.7, win_fn)
    create_wav('Assets/Audio/sfx_gameover.wav', 0.75, gameover_fn)
    print("All game audio assets generated successfully!")
