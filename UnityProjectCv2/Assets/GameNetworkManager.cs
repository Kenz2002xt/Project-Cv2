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
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = "MainRoom",
            SceneManager = sceneManager
        });

        if (result.Ok)
            Debug.Log("Joined Shared Game Session!");
        else
            Debug.LogError($"Failed to start Fusion: {result.ShutdownReason}");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Only configure cameras for the local player
        if (player == runner.LocalPlayer)
        {
            var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);

            // Sort cameras by name to get a predictable order
            cams = cams.OrderBy(c => c.name).ToArray();

            // Find the index of this player based on their PlayerId order
            var allPlayers = runner.ActivePlayers.OrderBy(p => p.RawEncoded).ToList();
            int playerIndex = allPlayers.IndexOf(player);

            // If index invalid, fallback to 0
            if (playerIndex < 0) playerIndex = 0;

            Debug.Log($"Local Player {player.PlayerId} detected with index {playerIndex}");

            // Disable all cameras locally (does not affect other clients)
            foreach (var cam in cams)
                cam.enabled = false;

            // Enable only the camera for this local player
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

    // --- Empty Callbacks (required by Fusion) ---
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