# 🎧 Wwise Tools by Lautaro Dichio
_Professional tools for audio implementation in Unity + Wwise_

> 🧰 A growing collection of open-source tools for technical sound designers.
> This repository includes all scripts featured in the video series **Need for Solution**.

---

## 🧠 What is this repository?

This is the official toolkit for the educational and production-ready tools I'm building as a Technical Sound Designer.

All tools are designed for **Unity + Wwise**, and are focused on solving real problems that come up during implementation — whether in prototyping, production, or teaching environments.

Each script is:
- Lightweight and customizable
- Editor-friendly (custom inspectors and scene gizmos)
- Built with both teaching and real-world use in mind

This repo will grow over time. Every time a new **Need for Solution** video is released, the corresponding tool will be added or updated here.

---

## 🎬 About *Need for Solution*

**Need for Solution** is a devlog-style video series where I present free tools I've built to address specific problems in game audio implementation.

Each episode includes:
- A real-world challenge
- A simple and flexible solution
- A downloadable Unity script you can use freely

📺 Watch the series: [YouTube playlist](#)

---

## 🧩 Included Tools

| Tool | Description | Video | Status |
|------|-------------|--------|--------|
| `AKLD_EventMultiBox` | Define and visualize multiple trigger areas from a single GameObject to trigger Wwise events. | [Episode 1](#akld_eventmultibox) | ✅ Available |
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

- Trigger **music state transitions** (e.g. vertical remixing)
- Build flexible **SFX zones** (including one-shots and loops)
- Visualize and debug multiple boxes easily in large levels

### 🧰 How to use

```csharp
1. Add the `AKLD_EventMultiBox` component to a GameObject.
2. Set the `objectToCheck` — this is the object that will trigger the events.
3. Customize your areas:
   - Adjust size, color, and rotation
   - Assign Wwise events on enter and exit
   - Enable/disable debug logs

