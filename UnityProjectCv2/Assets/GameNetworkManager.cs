using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Threading.Tasks;
using System.Collections.Generic;
using Fusion.Sockets;
using System.Linq;

public class GameNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner runner;

    private async void Start()
    {
        // Create and configure the NetworkRunner
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        Debug.Log("Starting Fusion session...");

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "MainRoom",
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            Debug.Log("Joined Shared Game Session!");

            // Only the host (master client) should spawn the GameManager
            if (runner.IsSharedModeMasterClient)
            {
                Debug.Log("Spawning GameManager prefab...");

                // Load and spawn GameManager from Resources
                var prefab = Resources.Load<GameObject>("GameManager");
                if (prefab == null)
                {
                    Debug.LogError("Could not find GameManager prefab in Resources!");
                }
                else
                {
                    runner.Spawn(prefab, Vector3.zero, Quaternion.identity);
                    Debug.Log("GameManager spawned successfully");
                }
            }
        }
        else
        {
            Debug.LogError($"Failed to start Fusion: {result.ShutdownReason}");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only configure cameras for the local player
        if (player == runner.LocalPlayer)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            // Sort cameras by name to get a predictable order
            cams = cams.OrderBy(c => c.name).ToArray();

            // Determine this player's index
            var allPlayers = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
            int playerIndex = allPlayers.IndexOf(player);
            if (playerIndex < 0) playerIndex = 0;

            Debug.Log($"Local Player {player.PlayerId} detected with index {playerIndex}");

            // Disable all cameras locally
            foreach (var cam in cams)
                cam.enabled = false;

            // Enable only the assigned camera
            if (playerIndex < cams.Length)
            {
                cams[playerIndex].enabled = true;
                Debug.Log($"Assigned Camera {cams[playerIndex].name} to Player {player.PlayerId}");
            }
            else
            {
                Debug.LogWarning($"No camera available for Player {player.PlayerId}");
            }
        }
    }

    // --- Required empty Fusion callbacks ---
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}