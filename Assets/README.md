# 🎮 Local Multiplayer Project (Unity 6 + Input System)

Welcome! This is the completed Unity project featured in the Local Multiplayer Tutorial by Dan @ Faktory Studios Games. It demonstrates how to support up to 4 local players using Unity 6 and the new Input System.

---

## ✅ Features

* 🕹️ Supports 1–4 local players
* 🎮 Mix of controller and shared keyboard input
* 🔁 Manual player joining using `PlayerInputManager` in JoinPlayersManually mode
* 🧍‍♂️ Simple `PlayerController` script using `CharacterController`
* 🧠 Input bindings and player actions (move + jump)
* 🧷 Clean scene hierarchy and modular setup

---

## 🧪 Requirements

* Unity 6.0.0 or later
* Input System package (pre-installed with Unity 6 templates)
* Works on Windows, macOS, and gamepad-supported platforms

---

## 🚀 How to Use

1. Open the project in Unity 6+
2. Load the scene: `Scenes/LocalMultiplayerScene.unity`
3. Press Play
4. Use `Right Control`, `Space`, or the south gamepad button to join a player
5. Move around using assigned inputs (WASD, Arrow keys, or left joystick)

---

## 🎓 Linked Tutorial

Watch the full video tutorial here:
📺 https://youtu.be/u3KoWI92blE

---

## 🧠 Notes

* The first player is joined via keyboard manually in `LocalMultiplayerManager.cs`
* Gamepads can join by pressing a button (auto-detected)
* The `PlayerController.cs` handles movement and jumping using the new Input System

---

## 🧰 Files to Explore

* `Scripts/PlayerController.cs`
* `Scripts/PlaerInputManager.cs`
* `Input/MultiplayerInput.inputactions`
* `Prefabs/Player.prefab`
* `Scenes/LocalMultiplayerScene.unity`

---

## 💬 Questions?

Join the community on Discord:
🔗 https://discord.gg/rnRAVAkRAJ

Or support the project on Patreon:
❤️ patreon.com/FaktoryStudiosGames
