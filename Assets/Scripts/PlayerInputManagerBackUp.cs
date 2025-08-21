using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private bool wasdJoined = false;
    private bool arrowsJoined = false;
    private bool gamepadJoined = false;

    void Update()
    {
        if (Keyboard.current == null) return;

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
            if (gamePad.buttonSouth.wasPressedThisFrame && !gamepadJoined)
            {
                var player = PlayerInput.Instantiate(playerPrefab,
                    controlScheme: "Gamepad",
                    pairWithDevice: gamePad);

                player.GetComponent<Renderer>().material.color = GetRandomColor();
                player.GetComponent<PlayerController>().SetLabel("Gamepad");

                gamepadJoined = true;
            }
        }
    }

    private static Color GetRandomColor()
    {
        return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
    }
}
