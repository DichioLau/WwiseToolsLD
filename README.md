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

| Tool                      | Description                                                                                                                                      | Video                                     | Status      |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------- | ----------- |
| `AKLD_EventMultiBox`      | Define and visualize multiple trigger areas from a single GameObject to trigger Wwise events.                                                    | [Episode 1](https://youtu.be/WdFs3uQ-2k8) | ✅ Available |
| `AKLD_HeartbeatModulator` | Modulate RTPCs with a heartbeat-shaped curve synced to music bars/beats and weighted by proximity zones.                                         | —                                         | ✅ Available |
| `AKLD_DevMixer`           | Control global Wwise RTPCs from the Inspector (0–100 knob mapped to \[min..max], per-RTPC mute/solo, master mute). Editor-only for fast testing. | —                                         | ✅ Available |
| `AKLD_SOTemplate`         | ScriptableObject framework to centralize Wwise references (Events, RTPCs, Switches, States) and call them with full autocomplete in code.        | —                                         | ✅ Available |
| `AKLD_TimerRTPC`          | Control a Wwise RTPC over time using customizable intervals, sum/subtract operations, and manual configuration of start/target values.           | —                                         | ✅ Available |
| `AKLD_DistanceBetweenObjects`| Calculates distance between two objects (X, Y, Z, or 3D) and maps it to a Wwise RTPC with optional remap curve, inverse mode, and debug gizmos. | —                                         | ✅ Available |
| `AKLD_DBMultipleGO`          | Multi-target distance system: selects the closest target (with hysteresis, dwell time, direction bias, and crossfade) and maps distance to RTPC.| —                                         | ✅ Available |
| *(more coming soon)*      |                                                                                                                                                  |                                           | 🔜           |

---
> 📦 **Note**: All tools (scripts + demo scenes) are already included in the **Unity Package** inside the `PACK` folder.  
> Import the package into your project to get access to all components and example scenes.
---

## 🎮 Demo Scenes

To quickly test each tool, the package includes a set of Unity demo scenes:

| Scene Name        | Tool Demonstrated            |
| ----------------- | ---------------------------- |
| `1_BoxTrigger`    | `AKLD_EventMultiBox`         |
| `2_HeartModulator`| `AKLD_HeartbeatModulator`    |
| `3_DevMixer`      | `AKLD_DevMixer`              |
| `4_SOTemplate`    | `AKLD_SOTemplate` (+ Hotkeys)|
| `5_TimerRTPC`     | `AKLD_TimerRTPC`             |
| `6_DistanceBetweenGO`  | `AKLD_DistanceBetweenObjects` |
| `7_DistanceBetweenMultGO` | `AKLD_DBMultipleGO`        |


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

## 🔷 AKLD_DevMixerGroup

### 🎯 What is it?
Editor-only dev mixer (Unity + Wwise) to control **global RTPCs** from the Inspector.
Perfect for QA/dev when menus aren’t ready or you need quick audio tweaks.

- 0–100 **knob** remapped to **[min..max]** per RTPC
- **Per-RTPC mute** and **solo** + **Master Mute** for the whole group
- Applies on **Update** and **OnValidate** (avoids redundant RTPC sets)
- Plug-and-play with **AK.Wwise.RTPC** assets

### 🚀 Use Cases
- Fast **music/SFX/VO** level changes during testing
- **A/B** mix checks without UI
- Temporarily **kill music** or isolate one RTPC while debugging

### 🧰 How to use
1) Add `AKLD_DevMixerGroup` to any GameObject.
2) In **Dev Mixers**, add entries and assign **AK.Wwise.RTPC** (global).
3) For each entry:
   - Set **min / max** (RTPC range, e.g., 0–100 or 0–1).
   - Adjust the **Knob (0–100)** (auto-mapped to `[min..max]`).
   - Toggle **Mute / Solo** as needed.
4) Use **Master Mute** to force **min** on all entries.
5) Press **Play** and tweak live (also applies immediately when moving sliders in the Inspector).

> Note: This component is **Editor-only** by design (`#if UNITY_EDITOR`).  
> If you need it in builds, remove the guard and keep editor calls wrapped in `#if UNITY_EDITOR`.

### 🩺 Troubleshooting
- **No change in game?** Ensure those RTPCs are truly **global** and used in your Wwise mix.
- **Values look off?** Verify **min/max** matches the RTPC scale (e.g., 0–1 vs 0–100).
- **Solo not working?** If **any** entry is soloed, all **non-solo** entries are muted.
- **Component missing in “Add Component”?** Class must be `public` `MonoBehaviour`, file name must match, script not under an `Editor/` folder, and there must be **no compile errors**.

---
## 🔷 AKLD_SOTemplate

### 🎯 What is it?
A **ScriptableObject–based framework** to centralize Wwise references (Events, RTPCs, Switches, States) inside Unity.  
It is designed to give programmers and audio teams a **shared workflow**:

- 🧑‍💻 **For programmers**: every Wwise call becomes an **autocompletable method** in code.  
  No more guessing strings, typing raw IDs, or risking typos.  
- 🎚️ **For audio teams**: all mappings (which Event, State, RTPC, or Switch corresponds to each method) can be **changed freely in the asset Inspector**.  
  No code changes required.  
- 🔄 **For collaboration**: programmers write stable, type-safe code once, while sound designers and composers can iterate and remap sounds at will.  

### 🚀 Why is this useful?
Traditionally, integrating Wwise into Unity forces programmers to:
- Call audio with raw strings like `"Play_Footstep"`  
- Depend on knowing exact event/RTPC names  
- Modify code whenever audio assets change  

With `AKLD_SOTemplate` + its **code generator**, you instead:
1. Define the logical names once inside the ScriptableObject asset.  
2. Run the generator → it creates C# methods with those names.  
3. Call those methods directly in code with full **IntelliSense/autocomplete**.  
4. Re-map Wwise assets later in the Inspector without touching code.  

### 🧰 How to use
1. Create a new asset via `Assets → Create → AudioLD`.  
2. Add entries for:
   - **Events** → Wwise Events you want to trigger  
   - **RTPCs** → Wwise RTPCs (always global)  
   - **Switches** → Wwise Switches  
   - **States** → Wwise States  
3. Run **Tools → AKLD → Generate Autocomplete** to build the `.Auto.cs` file.  
4. In your scripts, add a reference to the asset and call methods directly.

### 📜 Example

csharp
public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private AKLD_SOTemplate audio;

    void Step()
    {
        audio.Footstep(gameObject);       // Event
        audio.Speed(gameObject, 50f);     // RTPC (global)
        audio.Surface(gameObject);        // Switch
        audio.CombatState(gameObject);    // State
    }
}

### 🧪 Demo: AudioHotkeys

To quickly test your setup, the package includes `AudioHotkeys.cs`, a simple script that maps keyboard keys to template calls.  
This lets you verify your setup without writing custom logic:

- **1 2 3 4** → Trigger example *Events* (`EventNumber1–4`)  
- **Q W E R** → Change *music layers* via *States* (`Layer1–4`)  
- **M** → Post the *Music* event  
- **A S D** → Adjust the *MusicValue* RTPC (global):  
  - **A** = 100 (loud)  
  - **S** = 50 (medium)  
  - **D** = 10 (quiet)
 
---

## 🔷 AKLD_TimerRTPC

### 🎯 What is it?
A MonoBehaviour that lets you control **one shared Wwise RTPC** over time by using **multiple 3D boxes** in the scene.  
When the tracked object enters a box, the RTPC begins to change in timed steps (incrementing or decrementing).  
When the object exits, the RTPC moves toward a defined exit target value. Only one box has ownership at a time, so values never jump when switching areas.

- 📦 Multiple oriented boxes (position, rotation, size, color)
- ➕➖ Step-based **increment/decrement** on enter
- 🎯 Optional **target values** for enter and exit
- 🌍 Apply RTPC globally or to specific GameObjects (configurable TargetMode)
- 👁️ Full Scene gizmos + handles to move, rotate, and scale boxes visually
- 🧪 Shared runtime value ensures **smooth continuity** across all boxes

### 🚀 Use Cases
- Gradually increase/decrease an RTPC when the player enters or leaves an area (e.g., filter, intensity, ambience layer).
- Smooth fade to a target value once leaving a defined zone.
- Centralize control of a **single RTPC** across multiple trigger regions without risk of jumps.

### 🧰 How to use
1. Add the `AKLD_TimerRTPC` component to a GameObject.  
2. Assign the **RTPC** you want to control.  
3. Set global parameters:
   - `minValue` / `maxValue` (range clamp)  
   - `initialValue` (starting RTPC value)  
   - `targetMode` (Global, ThisGameObject, ObjectToCheck, OtherGameObject)  
4. Add **boxes** to the `Boxes` list and configure each:  
   - **Enter**: operation (Sum/Subtract), increment size & interval, optional target.  
   - **Exit**: operation, target value, increment size & interval.  
5. Use Scene view handles to position, rotate, and scale each box.  
6. Press Play and watch the RTPC smoothly transition as you enter/exit areas.

> ⚠️ Notes  
> • Only one box controls the RTPC at a time (first-come-wins).  
> • Enter without target = keep stepping until hitting min/max.  
> • Enter/Exit with target = step with `MoveTowards` until reaching the goal.  
> • Requires `AkGameObj` if applied to per-object RTPCs.  

### 🧪 Demo Scene
Open **`5_TimerRTPC`** for a ready-to-test setup, with boxes, gizmos, and values preconfigured.

---

## 🔷 AKLD_DistanceBetweenObjects

### 🎯 What is it?
A MonoBehaviour that calculates the **distance between two objects** (X, Y, Z, or full 3D)  
and maps that distance into a **Wwise RTPC** value.  

- 📏 Axis selection → X, Y, Z, or 3D distance  
- 🔄 Two behavior modes: **NearIsMin** (closer = lower RTPC) or **NearIsMax** (closer = higher RTPC)  
- 🎚️ Optional remap with **AnimationCurve** and configurable `[inputMin..inputMax] → [outputMin..outputMax]`  
- 👁️ Scene gizmos with line, spheres, axis projection (optional), and distance label  

### 🚀 Use Cases
- Control **filter cutoff**, **reverb send**, or **music intensity** based on distance between objects.  
- Implement dynamic SFX (e.g., volume or pitch) that scale with how close two entities are.  
- Quick prototyping of **distance-based behaviors** without complex trigger volumes.  

### 🧰 How to use
1. Add the `AKLD_DistanceBetweenObjects` component to a GameObject.  
2. Assign **Object1** and **Object2** (any Transforms).  
3. Assign the **Wwise RTPC** you want to drive.  
4. Configure:  
   - `axisDistance`: X, Y, Z, or All (3D).  
   - `inputMin / inputMax`: expected Unity distances.  
   - `outputMin / outputMax`: target RTPC range.  
   - `distanceMode`: choose NearIsMin or NearIsMax.  
   - `remapCurve`: shape how the RTPC responds (linear, log, exp, custom).  
5. Enable **Gizmos** in the Scene to visualize distance lines and debug labels.  
6. Press Play: the RTPC updates in real time as the objects move.  

### 🧪 Demo Scene
Open **`6_DistanceBetweenGO`** to test distance mapping between two cubes.  
Move them closer/further to see gizmos update and hear the RTPC value in Wwise.

---

## 🔷 AKLD_DBMultipleGO

### 🎯 What is it?
A MonoBehaviour that measures distance from a **main object (A)** to **multiple targets**  
and drives a single **Wwise RTPC** without jumps.  

- 🎯 Selects one **“owner” target** with **Nearest Sticky logic**: hysteresis + dwell time + direction bias  
- 🔄 Smooth transitions with **crossfade** and optional **rate limit** (MaxChangePerSecond)  
- 📏 Each target has its own **[inputMin..inputMax]** distance range for normalization  
- 🎚️ Remap distance via **AnimationCurve** into a global `[outputMin..outputMax]` RTPC range  
- 👁️ Scene gizmos:  
  - **Red** = target out of range  
  - **Green** = in range  
  - **Yellow** = current controlling target  
  - Labels show distance, normalized t, RTPC value, and target range  

### 🚀 Use Cases
- Complex setups where **one RTPC** should follow the closest relevant object  
  (e.g., nearest hazard, NPC, sound source).  
- Dynamic **music/sfx intensity** that shifts focus smoothly between multiple objects.  
- Prototyping **multi-zone distance logic** without custom trigger scripting.  

### 🧰 How to use
1. Add the `AKLD_DBMultipleGO` component to a GameObject.  
2. Assign **Object A** (the moving object to track).  
3. In **Targets**, add entries:  
   - Assign the **target Transform**  
   - Set per-target `inputMin / inputMax` (Unity distance range)  
4. Assign the **Wwise RTPC** to control.  
5. Configure global parameters:  
   - `distanceMode`: NearIsMin or NearIsMax  
   - `remapCurve` + `outputMin / outputMax` for RTPC mapping  
   - `hysteresisPercent`, `minDwellTime`, `crossfadeTime` for smooth ownership changes  
   - `maxChangePerSecond` for global smoothing  
6. Enable **Gizmos** to debug lines, colors, and labels.  
7. Press Play: as Object A moves, ownership shifts smoothly between nearest targets.  

### 🧪 Demo Scene
Open **`7_DistanceBetweenMultGO`** to test with multiple cubes.  
Move Object A around and watch gizmos switch ownership (yellow)  
while Wwise receives a continuous, smoothed RTPC value.

---

## 📬 Contact

Have questions, ideas, or a collab in mind? Let’s talk! 🙌

- 💌 Email: **lautarodichio@hotmail.com**  
- 💼 LinkedIn: **https://www.linkedin.com/in/lautaro-dichio/**
- 🗣️ Languages: English & Spanish (AR)
- 📍 Buenos Aires (UTC−3)

I try to reply as soon as I can ✨

---

## 📦 Requirements
- **Unity** with **Wwise Unity Integration** installed  
- Scripts can be dropped into any project folder (no special setup required)

---

## 📝 License — AKLD Tools Simple License 

- ✅ Intended for audio folks, devs, technical designers, composers, programmers, etc.
- ✅ Free to use inside your games/apps (commercial or not).
- ✅ You can modify them within your project/team.
- 🙌 Feedback, shoutouts, and credits are appreciated (not required).
- ❌ You may NOT monetize the Tools themselves (no selling, paywalling, renting, or paid access to the scripts).
- ❌ You may NOT resell/repackage/redistribute them as standalone assets, asset packs, templates, plugins, SDKs, libraries, or similar.
- ❌ Do NOT upload them to marketplaces or repos as “assets” on their own.

Copyright (c) 2025 Lautaro Dichio

Permission is granted to use and modify these tools for creating and shipping interactive
applications (including commercial games). Redistribution is allowed only when the tools are
embedded in your application in a way that users cannot extract or reuse them as assets.

You may not monetize, resell, sublicense, repackage, or redistribute the tools by themselves,
including (but not limited to) asset packs, templates, plugins, SDKs, libraries, paid bundles,
or tool collections, whether free or paid.

THE TOOLS ARE PROVIDED “AS IS” WITHOUT WARRANTY OF ANY KIND.
*/

---
