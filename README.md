# 🎧 Wwise Tools by Lautaro Dichio
_Professional tools for audio implementation in Unity + Wwise_

> 🧰 A growing collection of open-source tools for technical sound designers.
> This repository includes all scripts featured in the video series **Need for Solution**.

---

## 🧠 What is this repository?

This is the official toolkit for the educational and production-ready tools I'm building as a Technical Sound Designer.

All tools are designed for **Unity + Wwise**, and focus on solving real problems that come up during implementation — whether in prototyping, production, or teaching environments.

Each script is:
- Lightweight and customizable  
- Editor-friendly (custom inspectors and scene gizmos)  
- Built with both teaching and real-world use in mind

This repo will grow over time. Every time a new **Need for Solution** video is released, the corresponding tool will be added or updated here.

---

## 🎬 About *Need for Solution*

**Need for Solution** is a devlog-style video series where I present free tools built to address specific problems in game audio implementation.

Each episode includes:
- A real-world challenge
- A simple and flexible solution
- A downloadable Unity script you can use freely

📺 Watch the series:  
Episode 1: [AKLD_EventMultiBox](https://youtu.be/WdFs3uQ-2k8)

---

## 🧩 Included Tools

| Tool | Description | Video | Status |
|------|-------------|-------|--------|
| `AKLD_EventMultiBox` | Define and visualize multiple trigger areas from a single GameObject to trigger Wwise events. | [Episode 1](https://youtu.be/WdFs3uQ-2k8) | ✅ Available |
| `AKLD_HeartbeatModulator` | Modulate RTPCs with a heartbeat-shaped curve synced to music bars/beats and weighted by proximity zones. | — | ✅ Available |
| *(more coming soon)* |  |  | 🔜 |

---

## 🔷 AKLD_EventMultiBox (Episode 1)

### 🎯 What is it?
A MonoBehaviour that lets you define **multiple trigger areas** on a single GameObject. Each area can trigger Wwise events when a specified object enters or exits.

- 📦 Multiple customizable boxes (position, size, rotation, color)
- 🧩 Works with any GameObject (not just the player)
- 🎵 Supports music **state changes**, SFX, or ambiences
- 👁️ Visual debug with gizmos for all boxes
- 🧪 Debug logging for each entry and exit event

### 🚀 Use Cases
- Trigger **music state transitions** (e.g., vertical remixing)
- Build flexible **SFX zones** (including one-shots and loops)
- Visualize and debug multiple boxes easily in large levels

### 🧰 How to use
1. Add the `AKLD_EventMultiBox` component to a GameObject.  
2. Set the `objectToCheck` — this is the object that will trigger the events.  
3. Customize your areas:  
   - Adjust size, color, and rotation  
   - Assign Wwise events on enter and exit  
   - Enable/disable debug logs  

---

## 🔷 AKLD_HeartbeatModulator

### 🎯 What is it?
A MonoBehaviour that **modulates one or more Wwise RTPCs** using a heartbeat-shaped curve. The modulation is **synchronized to musical bars/beats** and **scaled by player proximity** to one or more **modulation zones**.

**Output formula per RTPC**  
`value = initialValue + (curve * proximity * amplitude)`  
- `curve` ∈ **[−0.5, +0.5]** from the selected heartbeat shape  
- `proximity` ∈ **[0, 1]** from the top-down radial bake (“pizza”)  
- `amplitude` is a per-RTPC depth multiplier

### 💡 Features
- 🧭 **Per-zone focus**: Each zone has its own Collider **and** its own Focus transform for radial baking  
- 🍕 **Top-down “pizza” bake (XZ)** with sector interpolation and optional auto-rebake  
- 🫀 Built-in shapes (**FastPulse**, **CalmBeat**, **StressSpike**) + **Custom** curve (expects −0.5..+0.5)  
- 🎚️ Per-RTPC **amplitude** and **initialValue** for precise layering  
- ⏱️ Sync by **BPM/pulsesPerBeat** or **cycles-per-beat (Frequency)**  
- 🧪 Visualizer bars + gizmos for zones/foci (Editor-only)  
- 🧰 Works alongside other Wwise systems (RTPCs, States, etc.)

### 🚀 Use Cases
- Drive **music layers** or filters with a musical/pulsing motion near points of interest  
- Subtle environmental **ambience modulation** that increases near hotspots  
- Stress/heartbeat feedback that scales with proximity and music timing

### 🧰 How to use
1. Add `AKLD_HeartbeatModulator` to a suitable GameObject (e.g., audio manager).  
2. Assign **`musicEvent`** (Wwise) and **`musicEventPosition`** (Transform) for musical sync.  
3. Set **`objectToCheck`** (the listener or actor used for proximity).  
4. Add **Modulation Zones**:
   - For each zone, set a **Collider** and an optional **Focus** (falls back to the global focus if null).  
   - Click **“Bake Pizza (Top-Down XZ)”** in the Inspector, or enable **`autoRebakeIfMoved`**.  
5. Choose a **Heartbeat Shape** or set a **Custom Curve** (range −0.5..+0.5).  
6. Configure **Sync**: `BPM` + `pulsesPerBeat` **or** `Frequency`.  
7. Add entries to **RTPCs**: set the Wwise RTPC, `initialValue`, and `amplitude`.  
8. Press Play and monitor the **Visualizer** + gizmos.  

> ⚠️ Notes  
> • The script does **not** clamp output by default. If your RTPC is strictly 0..1, clamp in Wwise or add a clamp.  
> • `progressSmoothed` is used in the output; use `progress01` if you prefer raw (unsmoothed) proximity.

### 🩺 Troubleshooting
- **No modulation?** Confirm the `musicEvent` is posting, zones are baked, and the player/target is inside a zone or within the fallback OBB.  
- **Pizza looks wrong?** Check each zone’s **Focus** position relative to its Collider; re-bake.  
- **Jittery proximity?** Lower `progressSmoothing` for more smoothing (less responsiveness).  
- **Performance concerns?** Reduce `sectorCount` or avoid frequent auto-rebakes.

---

## 📦 Requirements
- **Unity** with **Wwise Unity Integration** installed  
- Scripts can be dropped into any project folder (no special setup required)

---

## 📝 License
MIT — use freely in commercial and non-commercial projects. A credit is appreciated but not required.

---
