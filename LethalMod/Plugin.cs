using BepInEx;
using BepInEx.Configuration;
using GameNetcodeStuff;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;


namespace LethalMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Dictionary<Type, List<Component>> objectCache = new Dictionary<Type, List<Component>>();
        private Dictionary<Type, Dictionary<Component, NavMeshPath>> pathCache = new Dictionary<Type, Dictionary<Component, NavMeshPath>>();
        private float cacheRefreshInterval = 2.5f;

        private static ConfigEntry<bool> isUIEnabled;
        private static ConfigEntry<bool> isESPEnabled;
        private static ConfigEntry<bool> isEnemyESPEnabled;
        private static ConfigEntry<bool> isItemsESPEnabled;
        private static ConfigEntry<bool> isPlayerESPEnabled;
        private static ConfigEntry<bool> isDoorsESPEnabled;
        private static ConfigEntry<bool> isPartialESPEnabled;

        private ConfigEntry<int> config_FontSize;
        private ConfigEntry<int> config_MaxTrackedItems;
        private ConfigEntry<int> config_MaxTrackedEnemies;

        // RGB color configs (0-255 range)
        private ConfigEntry<int> config_ColorEnemyR;
        private ConfigEntry<int> config_ColorEnemyG;
        private ConfigEntry<int> config_ColorEnemyB;

        private ConfigEntry<int> config_ColorPlayerR;
        private ConfigEntry<int> config_ColorPlayerG;
        private ConfigEntry<int> config_ColorPlayerB;

        private ConfigEntry<int> config_ColorItemR;
        private ConfigEntry<int> config_ColorItemG;
        private ConfigEntry<int> config_ColorItemB;

        private ConfigEntry<int> config_ColorExitR;
        private ConfigEntry<int> config_ColorExitG;
        private ConfigEntry<int> config_ColorExitB;

        private ConfigEntry<int> config_ColorHazardR;
        private ConfigEntry<int> config_ColorHazardG;
        private ConfigEntry<int> config_ColorHazardB;

        private ConfigEntry<int> config_ColorTerminalR;
        private ConfigEntry<int> config_ColorTerminalG;
        private ConfigEntry<int> config_ColorTerminalB;

        private ConfigEntry<int> config_ColorPartialR;
        private ConfigEntry<int> config_ColorPartialG;
        private ConfigEntry<int> config_ColorPartialB;

        // Cached colors
        private Color enemyColor;
        private Color playerColor;
        private Color itemColor;
        private Color exitColor;
        private Color hazardColor;
        private Color terminalColor;
        private Color partialColor;

        private float lastToggleTime = 0f;
        private const float toggleCooldown = 0.5f;

        #region Keypress logic
        public static string[] keybinds;
        private ConfigEntry<string> config_KeyUI;
        private ConfigEntry<string> config_KeyESP;
        private ConfigEntry<string> config_KeyESPEnemies;
        private ConfigEntry<string> config_KeyESPPlayers;
        private ConfigEntry<string> config_KeyESPDoors;
        private ConfigEntry<string> config_KeyESPItems;
        private ConfigEntry<string> config_KeyESPPartial;
        private ConfigEntry<string> config_KeyOpenCloseDoor;
        private ConfigEntry<string> config_KeyOpenAllDoors;
        private ConfigEntry<string> config_KeyCloseAllDoors;

        private bool IsKeyDown(string key)
        {
            // Allow unbinding keys by setting them to empty/whitespace
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                // Attempt to get the key control - may be null for invalid key names
                var keyControl = ((InputControl)Keyboard.current)[key];
                if (keyControl == null)
                    return false;

                return InputControlExtensions.IsPressed(keyControl, 0f);
            }
            catch (System.Exception)
            {
                // Silently ignore invalid key names
                return false;
            }
        }
        #endregion

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Loading {PluginInfo.PLUGIN_GUID} - {PluginInfo.PLUGIN_VERSION}.");
            ConfigFile();
            StartCoroutine(CacheRefreshRoutine());
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void ConfigFile()
        {
            isUIEnabled = Config.Bind("UI", "Enable UI", true, "Enable UI?");
            config_FontSize = Config.Bind("UI", "Font Size", 12, "Font size for ESP labels (default: 12, recommended for 4K: 18-24)");
            isESPEnabled = Config.Bind("ESP", "Enable ESP", true, "Enable ESP?");
            isEnemyESPEnabled = Config.Bind("ESP", "Enable Enemy ESP", true, "Enable Enemy ESP?");
            isItemsESPEnabled = Config.Bind("ESP", "Enable Items ESP", true, "Enable Items ESP?");
            isPlayerESPEnabled = Config.Bind("ESP", "Enable Players ESP", true, "Enable Players ESP?");
            isDoorsESPEnabled = Config.Bind("ESP", "Enable Doors ESP", true, "Enable Doors ESP?");
            isPartialESPEnabled = Config.Bind("ESP", "Enable Partial ESP", false, "Enable Partial ESP?");

            // Performance settings
            config_MaxTrackedItems = Config.Bind("Performance", "Max Tracked Items", -1, "Maximum number of items to track (-1 = unlimited, recommended for large maps: 50-100)");
            config_MaxTrackedEnemies = Config.Bind("Performance", "Max Tracked Enemies", -1, "Maximum number of enemies to track (-1 = unlimited, recommended for large maps: 20-30)");

            keybinds = new string[10];
            config_KeyESP = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Enable ESP", "3", (ConfigDescription)null);
            keybinds[0] = config_KeyESP.Value.Replace(" ", "");
            config_KeyESPEnemies = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle Enemy ESP", "4", (ConfigDescription)null);
            keybinds[1] = config_KeyESPEnemies.Value.Replace(" ", "");
            config_KeyESPPlayers = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle Player ESP", "5", (ConfigDescription)null);
            keybinds[2] = config_KeyESPPlayers.Value.Replace(" ", "");
            config_KeyESPDoors = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle Door ESP", "6", (ConfigDescription)null);
            keybinds[3] = config_KeyESPDoors.Value.Replace(" ", "");
            config_KeyESPItems = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle Items ESP", "7", (ConfigDescription)null);
            keybinds[4] = config_KeyESPItems.Value.Replace(" ", "");
            config_KeyESPPartial = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle incomplete paths", "8", (ConfigDescription)null);
            keybinds[5] = config_KeyESPPartial.Value.Replace(" ", "");
            config_KeyOpenCloseDoor = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Open closest door to player", "f", (ConfigDescription)null);
            keybinds[6] = config_KeyOpenCloseDoor.Value.Replace(" ", "");
            config_KeyOpenAllDoors = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Open all doors", "c", (ConfigDescription)null);
            keybinds[7] = config_KeyOpenAllDoors.Value.Replace(" ", "");
            config_KeyCloseAllDoors = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Close all doors", "x", (ConfigDescription)null);
            keybinds[8] = config_KeyCloseAllDoors.Value.Replace(" ", "");
            config_KeyUI = ((BaseUnityPlugin)this).Config.Bind<string>("Keybindings", "Toggle UI", "p", (ConfigDescription)null);
            keybinds[9] = config_KeyUI.Value.Replace(" ", "");

            // Color configurations (RGB 0-255)
            config_ColorEnemyR = Config.Bind("Colors", "Enemy Red", 255, "Red component (0-255) for Enemy ESP");
            config_ColorEnemyG = Config.Bind("Colors", "Enemy Green", 0, "Green component (0-255) for Enemy ESP");
            config_ColorEnemyB = Config.Bind("Colors", "Enemy Blue", 0, "Blue component (0-255) for Enemy ESP");

            config_ColorPlayerR = Config.Bind("Colors", "Player Red", 0, "Red component (0-255) for Player ESP");
            config_ColorPlayerG = Config.Bind("Colors", "Player Green", 255, "Green component (0-255) for Player ESP");
            config_ColorPlayerB = Config.Bind("Colors", "Player Blue", 0, "Blue component (0-255) for Player ESP");

            config_ColorItemR = Config.Bind("Colors", "Item Red", 0, "Red component (0-255) for Item ESP");
            config_ColorItemG = Config.Bind("Colors", "Item Green", 0, "Green component (0-255) for Item ESP");
            config_ColorItemB = Config.Bind("Colors", "Item Blue", 255, "Blue component (0-255) for Item ESP");

            config_ColorExitR = Config.Bind("Colors", "Exit Red", 0, "Red component (0-255) for Exit ESP");
            config_ColorExitG = Config.Bind("Colors", "Exit Green", 255, "Green component (0-255) for Exit ESP");
            config_ColorExitB = Config.Bind("Colors", "Exit Blue", 255, "Blue component (0-255) for Exit ESP");

            config_ColorHazardR = Config.Bind("Colors", "Hazard Red", 255, "Red component (0-255) for Hazard ESP (Landmine/Turret/SteamValve)");
            config_ColorHazardG = Config.Bind("Colors", "Hazard Green", 0, "Green component (0-255) for Hazard ESP (Landmine/Turret/SteamValve)");
            config_ColorHazardB = Config.Bind("Colors", "Hazard Blue", 0, "Blue component (0-255) for Hazard ESP (Landmine/Turret/SteamValve)");

            config_ColorTerminalR = Config.Bind("Colors", "Terminal Red", 255, "Red component (0-255) for Terminal ESP");
            config_ColorTerminalG = Config.Bind("Colors", "Terminal Green", 0, "Green component (0-255) for Terminal ESP");
            config_ColorTerminalB = Config.Bind("Colors", "Terminal Blue", 255, "Blue component (0-255) for Terminal ESP");

            config_ColorPartialR = Config.Bind("Colors", "Partial Path Red", 255, "Red component (0-255) for Partial Path ESP");
            config_ColorPartialG = Config.Bind("Colors", "Partial Path Green", 255, "Green component (0-255) for Partial Path ESP");
            config_ColorPartialB = Config.Bind("Colors", "Partial Path Blue", 0, "Blue component (0-255) for Partial Path ESP");

            // Initialize cached colors
            UpdateColors();
        }

        private void UpdateColors()
        {
            enemyColor = new Color(config_ColorEnemyR.Value / 255f, config_ColorEnemyG.Value / 255f, config_ColorEnemyB.Value / 255f);
            playerColor = new Color(config_ColorPlayerR.Value / 255f, config_ColorPlayerG.Value / 255f, config_ColorPlayerB.Value / 255f);
            itemColor = new Color(config_ColorItemR.Value / 255f, config_ColorItemG.Value / 255f, config_ColorItemB.Value / 255f);
            exitColor = new Color(config_ColorExitR.Value / 255f, config_ColorExitG.Value / 255f, config_ColorExitB.Value / 255f);
            hazardColor = new Color(config_ColorHazardR.Value / 255f, config_ColorHazardG.Value / 255f, config_ColorHazardB.Value / 255f);
            terminalColor = new Color(config_ColorTerminalR.Value / 255f, config_ColorTerminalG.Value / 255f, config_ColorTerminalB.Value / 255f);
            partialColor = new Color(config_ColorPartialR.Value / 255f, config_ColorPartialG.Value / 255f, config_ColorPartialB.Value / 255f);
        }

        #region Cache
        IEnumerator CacheRefreshRoutine()
        {
            Logger.LogInfo("Starting background caching");
            while (true)
            {
                Logger.LogDebug($"Refreshing object cache.");
                RefreshCache();
                yield return new WaitForSeconds(cacheRefreshInterval);
            }
        }

        void RefreshCache()
        {
            objectCache.Clear();
            CacheObjects<EntranceTeleport>();
            CacheObjects<GrabbableObject>();
            CacheObjects<Landmine>();
            CacheObjects<Turret>();
            CacheObjects<Terminal>();
            CacheObjects<PlayerControllerB>();
            CacheObjects<SteamValveHazard>();
            CacheObjects<EnemyAI>();
            CacheObjects<TerminalAccessibleObject>();
            CacheObjects<DoorLock>();
            pathCache.Clear();
            try
            {
                if (GameNetworkManager.Instance?.localPlayerController?.gameplayCamera != null)
                {
                    CachePaths<EntranceTeleport>();
                    CachePaths<GrabbableObject>();
                }
            }
            catch (Exception e)
            {
                // Use debug level to avoid spam during game state transitions (lobby, loading, etc.)
                Logger.LogDebug($"Failed to calculate paths: {e.Message}");
            }
            //CachePaths<PlayerControllerB>();
            //CachePaths<EnemyAI>();
        }

        void CacheObjects<T>() where T : Component
        {
            var allObjects = FindObjectsOfType<T>();
            List<Component> objectsToCache = new List<Component>(allObjects);

            // Apply distance-based filtering if limits are configured
            if (GameNetworkManager.Instance?.localPlayerController != null)
            {
                int limit = -1;
                bool shouldLimit = false;

                // Check if this type has a limit configured
                if (typeof(T) == typeof(GrabbableObject) && config_MaxTrackedItems.Value > 0)
                {
                    limit = config_MaxTrackedItems.Value;
                    shouldLimit = true;
                }
                else if (typeof(T) == typeof(EnemyAI) && config_MaxTrackedEnemies.Value > 0)
                {
                    limit = config_MaxTrackedEnemies.Value;
                    shouldLimit = true;
                }

                // Sort by distance and take only the closest N objects
                if (shouldLimit && allObjects.Length > limit)
                {
                    Vector3 playerPos = GameNetworkManager.Instance.localPlayerController.transform.position;
                    objectsToCache = allObjects
                        .OrderBy(obj => Vector3.Distance(playerPos, obj.transform.position))
                        .Take(limit)
                        .Cast<Component>()
                        .ToList();
                    Logger.LogDebug($"Limited {typeof(T)} from {allObjects.Length} to {objectsToCache.Count} closest objects");
                }
            }

            objectCache[typeof(T)] = objectsToCache;
            if (objectCache[typeof(T)].Count > 0)
                Logger.LogInfo($"Cached {objectCache[typeof(T)].Count} objects of type {typeof(T)}.");
        }

        void CachePaths<T>() where T : Component
        {
            if (GameNetworkManager.Instance == null)
                return;
            pathCache[typeof(T)] = new Dictionary<Component, NavMeshPath>();
            if (GameNetworkManager.Instance.localPlayerController == null)
                return;

            if (!objectCache.TryGetValue(typeof(T), out var cachedObjects))
                return;

            NavMeshAgent agent = GameNetworkManager.Instance.localPlayerController.gameObject.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                Logger.LogInfo("Attaching NavMeshAgent to Player");
                agent = GameNetworkManager.Instance.localPlayerController.gameObject.AddComponent<NavMeshAgent>();
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.updateUpAxis = false;
            }
            agent.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
            agent.nextPosition = GameNetworkManager.Instance.localPlayerController.transform.position;
            agent.enabled = false;
            agent.enabled = true;

            // Check if agent is properly placed on NavMesh before attempting path calculations
            if (!agent.isOnNavMesh)
            {
                Logger.LogDebug($"NavMeshAgent is not on NavMesh, skipping path calculation for {typeof(T)}");
                return;
            }

            NavMeshPath path;

            foreach (T obj in cachedObjects.Cast<T>())
            {
                try
                {
                    // Skip if object is null or destroyed
                    if (obj == null || obj.transform == null)
                        continue;

                    path = new NavMeshPath();
                    Vector3 target_pos = obj.transform.position;

                    if (obj is EntranceTeleport)
                        target_pos.y = target_pos.y - 1.5f;
                    if (obj is GrabbableObject && obj.name.Contains("Apparatus"))
                        target_pos.y = target_pos.y - 3;

                    // CalculatePath can fail if NavMesh is not loaded or agent is not valid
                    agent.CalculatePath(target_pos, path);

                    if (path.corners.Length < 3) //if the path has 1 or no corners, there is no need
                        continue;
                    if (path.status == NavMeshPathStatus.PathComplete)
                    {
                        pathCache[typeof(T)][obj] = path;
                    }
                    else if (isPartialESPEnabled.Value && path.status == NavMeshPathStatus.PathPartial)
                    {
                        pathCache[typeof(T)][obj] = path;
                    }
                }
                catch (Exception)
                {
                    // Silently ignore errors during path calculation (NavMesh might not be loaded, agent might not be valid, etc.)
                    // This prevents log spam during game state transitions
                    continue;
                }
            }
            if (pathCache[typeof(T)].Count > 0)
                Logger.LogInfo($"Cached {pathCache[typeof(T)].Count} ESP Paths for type {typeof(T)}.");
        }
        #endregion

        public void Update()
        {
            bool isESPKeyDown = IsKeyDown(keybinds[0]);
            bool isEnemyESPKeyDown = IsKeyDown(keybinds[1]);
            bool isPlayerESPKeyDown = IsKeyDown(keybinds[2]);
            bool isDoorsESPKeyDown = IsKeyDown(keybinds[3]);
            bool isItemsESPKeyDown = IsKeyDown(keybinds[4]);
            bool isPartialESPKeyDown = IsKeyDown(keybinds[5]);
            bool isOpenDoorKeyDown = IsKeyDown(keybinds[6]);
            bool isOpenDoorsKeyDown = IsKeyDown(keybinds[7]);
            bool isCloseDoorsKeyDown = IsKeyDown(keybinds[8]);
            bool isUIKeyDown = IsKeyDown(keybinds[9]);

            if (isUIKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isUIEnabled.Value = !isUIEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isESPEnabled.Value = !isESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isEnemyESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isEnemyESPEnabled.Value = !isEnemyESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isDoorsESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isDoorsESPEnabled.Value = !isDoorsESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isItemsESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isItemsESPEnabled.Value = !isItemsESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isPlayerESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isPlayerESPEnabled.Value = !isPlayerESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isPartialESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isPartialESPEnabled.Value = !isPartialESPEnabled.Value;
                lastToggleTime = Time.time;
            }

            if (isOpenDoorKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                TerminalAccessibleObject closest = null;
                foreach (TerminalAccessibleObject obj in objectCache[typeof(TerminalAccessibleObject)])
                {
                    if (obj.isBigDoor)
                    {
                        if (closest == null || distance(closest.transform.position) > distance(obj.transform.position))
                        {
                            closest = obj;
                        }
                    }
                }
                if (closest)
                {
                    closest.SetDoorOpen(true);
                }

                lastToggleTime = Time.time;
            }

            if (isCloseDoorsKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                foreach (TerminalAccessibleObject obj in objectCache[typeof(TerminalAccessibleObject)])
                {
                    if (obj.isBigDoor)
                    {
                        obj.SetDoorOpen(false);
                    }
                }

                lastToggleTime = Time.time;
            }

            if (isOpenDoorsKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                foreach (TerminalAccessibleObject obj in objectCache[typeof(TerminalAccessibleObject)])
                {
                    if (obj.isBigDoor)
                    {
                        obj.SetDoorOpen(true);
                    }
                }

                foreach (DoorLock obj in objectCache[typeof(DoorLock)])
                {
                    obj.UnlockDoorSyncWithServer();
                }

                lastToggleTime = Time.time;
            }
        }

        public void OnGUI()
        {
            // Save original font size and apply configured size
            int originalFontSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = config_FontSize.Value;

            if (isUIEnabled.Value)
            {
                var label_text_tmp = isUIEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(10f, 10f, 200f, 30f), $"{keybinds[9]} - UI is: {label_text_tmp}");

                label_text_tmp = isESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 25f, 200f, 30f), $"{keybinds[0]} - ESP is: {label_text_tmp}");

                label_text_tmp = isEnemyESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true && isEnemyESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 40f, 200f, 30f), $"{keybinds[1]} - Enemy ESP is: {label_text_tmp}");

                label_text_tmp = isPlayerESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true && isPlayerESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 55f, 200f, 30f), $"{keybinds[2]} - Player ESP is: {label_text_tmp}");

                label_text_tmp = isDoorsESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true && isDoorsESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 70f, 200f, 30f), $"{keybinds[3]} - Doors ESP is: {label_text_tmp}");

                label_text_tmp = isItemsESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true && isItemsESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 85f, 200f, 30f), $"{keybinds[4]} - Items ESP is: {label_text_tmp}");

                label_text_tmp = isPartialESPEnabled.Value == true ? "On" : "Off";
                GUI.contentColor = isESPEnabled.Value == true && isPartialESPEnabled.Value == true ? Color.green : Color.red;
                GUI.Label(new Rect(10f, 100f, 200f, 30f), $"{keybinds[5]} - Incomplete Path ESP is: {label_text_tmp}");

                GUI.contentColor = Color.white;
                GUI.Label(new Rect(10f, 115f, 200f, 30f), $"{keybinds[6]} - Open nearest big door");
                GUI.Label(new Rect(10f, 130f, 200f, 30f), $"{keybinds[7]} - Open/Unlock all doors");
                GUI.Label(new Rect(10f, 145f, 200f, 30f), $"{keybinds[8]} - Close all big doors");
            }

            if (isESPEnabled.Value)
            {
                //Logger.LogDebug($"Rendering ESP.");
                ProcessObjects<Terminal>((terminal, vector) => "SHIP TERMINAL ");
                ProcessObjects<SteamValveHazard>((valve, vector) => "Steam Valve ");
                if (isItemsESPEnabled.Value)
                    ProcessObjects<GrabbableObject>((grabbableObject, vector) => grabbableObject.itemProperties.itemName + " - " + grabbableObject.scrapValue + "\n");
                if (isPlayerESPEnabled.Value)
                    ProcessPlayers();
                if (isDoorsESPEnabled.Value)
                    ProcessObjects<EntranceTeleport>((entrance, vector) => entrance.isEntranceToBuilding ? " Entrance " : " Exit ");
                if (isEnemyESPEnabled.Value)
                {
                    ProcessEnemies();
                    ProcessObjects<Landmine>((landmine, vector) => "LANDMINE ");
                    ProcessObjects<Turret>((turret, vector) => "TURRET ");
                }
            }

            // Restore original font size
            GUI.skin.label.fontSize = originalFontSize;
        }

        private Vector3 world_to_screen(Vector3 world)
        {
            Vector3 screen = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToViewportPoint(world);

            screen.x *= Screen.width;
            screen.y *= Screen.height;

            screen.y = Screen.height - screen.y;

            return screen;
        }

        private float distance(Vector3 world_position)
        {
            return Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, world_position);
        }
        #region ESP Drawing

        public static bool WorldToScreen(Camera camera, Vector3 world, out Vector3 screen)
        {
            screen = camera.WorldToViewportPoint(world);

            screen.x *= Screen.width;
            screen.y *= Screen.height;

            screen.y = Screen.height - screen.y;

            return screen.z > 0;
        }

        private void ProcessObjects<T>(Func<T, Vector3, string> labelBuilder) where T : Component
        {
            if (!objectCache.TryGetValue(typeof(T), out var cachedObjects))
                return;

            foreach (T obj in cachedObjects.Cast<T>())
            {
                try
                {
                    if (GameNetworkManager.Instance.localPlayerController == null || obj == null)
                    {
                        continue;
                    }
                    if (obj is GrabbableObject GO && (GO.isPocketed || GO.isHeld || (GO.itemProperties.itemName == "Gift" && !GO.gameObject.GetComponent<Renderer>().isVisible)))
                    {
                        continue;
                    }

                    if (obj is GrabbableObject GO2 && GO2.itemProperties.itemName is "clipboard" or "Sticky note")
                    {
                        continue;
                    }

                    if (obj is SteamValveHazard valve && valve.triggerScript.interactable == false)
                    {
                        continue;
                    }

                    Vector3 screen;
                    if (WorldToScreen(GameNetworkManager.Instance.localPlayerController.gameplayCamera,
                        obj.transform.position, out screen))
                    {
                        string label = labelBuilder(obj, screen);
                        float distance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, obj.transform.position);
                        distance = (float)Math.Round(distance);
                        DrawLabel(screen, label, GetColorForObject<T>(), distance);
                        if (obj is EntranceTeleport)
                        {
                            DrawPath(obj, GetColorForObject<T>(), 2f);
                        }
                        else if (obj is GrabbableObject && GameNetworkManager.Instance.localPlayerController.isInsideFactory)
                        {
                            DrawPath(obj, GetColorForObject<T>(), 2f);
                        }
                    }
                }
                catch (NullReferenceException e)
                {
                    Logger.LogError($"Failed to render {typeof(T)}:\n{e.Message}");
                    continue;
                }
            }
        }

        private void ProcessPlayers()
        {
            if (!objectCache.TryGetValue(typeof(PlayerControllerB), out var cachedPlayers))
                return;

            foreach (PlayerControllerB player in cachedPlayers.Cast<PlayerControllerB>())
            {
                try
                {
                    if (GameNetworkManager.Instance.localPlayerController == null || player == null)
                    {
                        continue;
                    }
                    if (player.isPlayerDead || player.IsLocalPlayer || player.playerUsername == GameNetworkManager.Instance.localPlayerController.playerUsername || player.disconnectedMidGame || player.playerUsername.Contains("Player #"))
                    {
                        continue;
                    }

                    Vector3 screen;
                    if (WorldToScreen(GameNetworkManager.Instance.localPlayerController.gameplayCamera,
                        player.transform.position, out screen))
                    {
                        string label = player.playerUsername + " ";
                        float distance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, player.transform.position);
                        distance = (float)Math.Round(distance);
                        DrawLabel(screen, label, playerColor, distance);
                    }
                }
                catch (NullReferenceException e)
                {
                    Logger.LogError($"Failed to render {typeof(PlayerControllerB)}:\n{e.Message}");
                    continue;
                }
            }
        }

        private void ProcessEnemies()
        {

            if (!objectCache.TryGetValue(typeof(EnemyAI), out var cachedEnemies))
                return;

            Action<EnemyAI> processEnemy = enemyAI =>
            {
                Vector3 screen;
                if (GameNetworkManager.Instance.localPlayerController != null && enemyAI != null)
                {
                    if (WorldToScreen(GameNetworkManager.Instance.localPlayerController.gameplayCamera,
                      enemyAI.transform.position, out screen))
                    {
                        string label;
                        if (string.IsNullOrWhiteSpace(enemyAI.enemyType.enemyName))
                        {
                            label = "Unknown Enemy ";
                        }
                        else
                            label = enemyAI.enemyType.enemyName + " ";
                        float distance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, enemyAI.transform.position);
                        distance = (float)Math.Round(distance);
                        DrawLabel(screen, label, enemyColor, distance);
                        //DrawPath(enemyAI.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position, Color.red, 2f);
                    }
                }
            };

            foreach (EnemyAI enemyAI in cachedEnemies.Cast<EnemyAI>())
            {
                processEnemy(enemyAI);
            }
        }

        private void DrawLabel(Vector3 screenPosition, string text, Color color, float distance)
        {
            GUI.contentColor = color;
            GUI.Label(new Rect(screenPosition.x, screenPosition.y, 75f, 50f), text + distance + "m");
        }

        private Color GetColorForObject<T>()
        {
            switch (typeof(T).Name)
            {
                case "EntranceTeleport":
                    return exitColor;
                case "GrabbableObject":
                    return itemColor;
                case "Landmine":
                    return hazardColor;
                case "Turret":
                    return hazardColor;
                case "SteamValveHazard":
                    return hazardColor;
                case "Terminal":
                    return terminalColor;
                default:
                    return Color.white;
            }
        }

        private void DrawPath<T>(T obj, Color color, float width) where T : Component
        {
            if (!pathCache.TryGetValue(typeof(T), out var cachedObjects))
                return;
            if (!cachedObjects.TryGetValue(obj, out var cachedPath))
                return;

            Vector2 previous;
            Vector2 next;
            if (cachedPath.status == NavMeshPathStatus.PathComplete)
            {
                previous = world_to_screen(cachedPath.corners[1]);
                for (int i = 2; i < cachedPath.corners.Length - 1; i++)
                {
                    var screen_pos = world_to_screen(cachedPath.corners[i]);
                    next = new Vector2(screen_pos.x, screen_pos.y);
                    render.draw_line(previous, next, color, width);
                    previous = next;
                }
                Vector3 end_pos = world_to_screen(obj.transform.position);
                render.draw_line(previous, end_pos, color, width);
            }
            else if (isPartialESPEnabled.Value && cachedPath.status == NavMeshPathStatus.PathPartial)
            {
                previous = world_to_screen(cachedPath.corners[1]);
                for (int i = 2; i < cachedPath.corners.Length - 1; i++)
                {
                    var screen_pos = world_to_screen(cachedPath.corners[i]);
                    next = new Vector2(screen_pos.x, screen_pos.y);
                    render.draw_line(previous, next, partialColor, width);
                    previous = next;
                }
            }
        }
        #endregion
    }

    public class render : MonoBehaviour
    {
        public static GUIStyle StringStyle { get; set; } = new GUIStyle(GUI.skin.label);

        public static Color Color
        {
            get { return GUI.color; }
            set { GUI.color = value; }
        }

        public static Texture2D lineTex;

        public static void draw_line(Vector2 pointA, Vector2 pointB, Color color, float width)
        {
            if ((pointA.x > 0 && pointA.x < Screen.width && pointA.y > 0 && pointA.y < Screen.height) ||
                (pointB.x > 0 && pointB.x < Screen.width && pointB.y > 0 && pointB.y < Screen.height))
            {
                Matrix4x4 matrix = GUI.matrix;
                if (!lineTex)
                    lineTex = new Texture2D(1, 1);

                Color color2 = GUI.color;
                GUI.color = color;
                float num = Vector3.Angle(pointB - pointA, Vector2.right);

                if (pointA.y > pointB.y)
                    num = -num;

                GUIUtility.ScaleAroundPivot(new Vector2((pointB - pointA).magnitude, width),
                    new Vector2(pointA.x, pointA.y + 0.5f));
                GUIUtility.RotateAroundPivot(num, pointA);
                GUI.DrawTexture(new Rect(pointA.x, pointA.y, 1f, 1f), lineTex);
                GUI.matrix = matrix;
                GUI.color = color2;
            }
        }
    }
}
