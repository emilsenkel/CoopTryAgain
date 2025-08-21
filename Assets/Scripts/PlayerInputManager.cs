using UnityEngine;
using UnityEngine.InputSystem;

using System.Collections.Generic;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool wasdJoined = false;
    private bool arrowsJoined = false;

    [SerializeField] private int maxGamepads = 4; // Max number of gamepads allowed (adjust as needed)
private List<Gamepad> joinedGamepads = new List<Gamepad>(); // Track joined gamepads

    void Update()
    {
        if (Keyboard.current == null) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return; // Skip if no spawn points set

        if (!wasdJoined && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab,
                controlScheme: "WASD",
                pairWithDevice: Keyboard.current);

            player.GetComponent<Renderer>().material.color = GetRandomColor();
            player.GetComponent<PlayerController>().SetLabel("WASD Keyboard");

            if (spawnPoints.Length > 0 )
            {
                player.transform.position = spawnPoints[0].position;
            }

            wasdJoined = true;
        }

        if (!arrowsJoined && Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            var player = PlayerInput.Instantiate(playerPrefab,
                controlScheme: "Arrows",
                pairWithDevice: Keyboard.current);

            player.GetComponent<Renderer>().material.color = GetRandomColor();
            player.GetComponent<PlayerController>().SetLabel("Arrows Keyboard");

            if (spawnPoints.Length > 1)
            {
                player.transform.position = spawnPoints[1].position;
            }

            arrowsJoined = true;
        }

        foreach (var gamePad in Gamepad.all)
{
    if (gamePad.buttonSouth.wasPressedThisFrame && !joinedGamepads.Contains(gamePad) && joinedGamepads.Count < maxGamepads)
    {
        var player = PlayerInput.Instantiate(playerPrefab,
            controlScheme: "Gamepad",
            pairWithDevice: gamePad);

        player.GetComponent<Renderer>().material.color = GetRandomColor();
        player.GetComponent<PlayerController>().SetLabel($"Gamepad {joinedGamepads.Count + 1}");

        // Assign spawn point based on how many gamepads are already joined
        int spawnIndex = 2 + joinedGamepads.Count; // Keyboard uses 0 and 1, gamepads start at 2
        if (spawnPoints.Length > spawnIndex)
        {
            player.transform.position = spawnPoints[spawnIndex].position;
        }

        joinedGamepads.Add(gamePad); // Mark this gamepad as joined
    }
}
    }

    private static Color GetRandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
}
