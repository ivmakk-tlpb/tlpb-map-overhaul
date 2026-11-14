namespace MapOverhaul.Game
{
    // The game manager of the running game and its network mode. The mod is off in a network game.
    internal static class Session
    {
        private static GameManager manager;
        private static NetworkManager network;
        private static bool multiplayerLogged;

        internal static GameManager Manager()
        {
            if (manager != null) return manager;
            manager = UnityEngine.Object.FindObjectOfType<GameLoop>()?.gameManager?.TryCast<GameManager>();
            network = manager?.networkManager?.TryCast<NetworkManager>();
            return manager;
        }

        // True in a network game, as a client or a host. The first such check of a session writes one log line.
        internal static bool Multiplayer()
        {
            Manager();
            if (network == null || !network.IsNetworkedGame()) return false;
            if (!multiplayerLogged)
            {
                multiplayerLogged = true;
                Plugin.Log.LogInfo("Map Overhaul is off in multiplayer games.");
            }
            return true;
        }

        // Forgets the managers at a world unload, so the next world finds them again.
        internal static void Clear()
        {
            manager = null;
            network = null;
        }

        internal static void Reset()
        {
            Clear();
            multiplayerLogged = false;
        }
    }
}
