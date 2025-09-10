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
| *(more coming soon)*      |                                                                                                                                                  |                                           | 🔜          |

---

## 🎮 Demo Scenes

To quickly test each tool, the package includes a set of Unity demo scenes:

| Scene Name        | Tool Demonstrated            |
| ----------------- | ---------------------------- |
| `1_BoxTrigger`    | `AKLD_EventMultiBox`         |
| `2_HeartMod`      | `AKLD_HeartbeatModulator`    |
| `3_DevMixer`      | `AKLD_DevMixer`              |
| `4_SOTemplate`    | `AKLD_SOTemplate` (+ Hotkeys)|

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
