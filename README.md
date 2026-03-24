Anomaly Corridor Game — Don't Be Late
A first-person Unity game inspired by The Exit 8, developed as an intermodular project for the DAM degree at INS Vidal i Barraquer (Tarragona).
The player walks through a corridor, memorises every detail, and must decide each round: is something different, or not? Advance if correct — restart if wrong.

🎮 Gameplay
 - Genre: First-person psychological / observation
 - Controls: WASD to move · Shift to run · Mouse to look · ESC to pause
 - Objective: Complete 4 rounds by correctly identifying anomalies

🛠️ Built With
 - Unity 6.2 (6000.2.8f1) · C#
 - CharacterController — first-person movement & collision
 - TextMeshPro — in-game UI and corridor signs
 - PlayerPrefs — full save/load system (position, level, time, active anomaly)
 - SceneManager — 3 scenes: Main Menu · Corridor · Game Over

✨ Key Features
 - Infinite corridor loop — seamless teleportation system (invisible to the player)
 - 15 anomaly types — 5 obvious (flickering lights, extra doors...) + 10 subtle (mirror signs, missing pillars, 3D audio anomaly...)
 - Modular architecture — GameManager, AnomalyManager, CorridorLoopController, PlayerController each with a single responsibility
 - Complete save system — saves position, orientation, level, elapsed time and active anomaly state
 - Personal best timer — tracks best completion time across sessions
