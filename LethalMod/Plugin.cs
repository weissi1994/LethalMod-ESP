using BepInEx;
using GameNetcodeStuff;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace LethalMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Dictionary<Type, List<Component>> objectCache = new Dictionary<Type, List<Component>>();
        private float cacheRefreshInterval = 1.5f;
        private bool isESPEnabled = true;
        private bool isEnemyESPEnabled = true;
        private bool isPartialESPEnabled = false;

        private float lastToggleTime = 0f;
        private const float toggleCooldown = 0.5f;

        #region Keypress logic
        private const int VK_ESP = 0x33; // 3
        private const int VK_ESP_ENEMY = 0x34; // 4
        private const int VK_ESP_PARTIAL = 0x35; // 5
        private const int VK_OPEN_DOOR = 0x36; // 6
        private const int VK_CLOSE_DOORS = 0x37; // 7
        private const int VK_OPEN_DOORS = 0x38; // 8

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private bool IsKeyDown(int keyCode)
        {
            return (GetAsyncKeyState(keyCode) & 0x8000) > 0;
        }
        #endregion

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            StartCoroutine(CacheRefreshRoutine());
        }

        #region Cache
        IEnumerator CacheRefreshRoutine()
        {
            while (true)
            {
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
        }

        void CacheObjects<T>() where T : Component
        {
            objectCache[typeof(T)] = new List<Component>(FindObjectsOfType<T>());
        }
        #endregion

        public void Update()
        {
            bool isESPKeyDown = IsKeyDown(VK_ESP);
            bool isEnemyESPKeyDown = IsKeyDown(VK_ESP_ENEMY);
            bool isPartialESPKeyDown = IsKeyDown(VK_ESP_PARTIAL);
            bool isOpenDoorKeyDown = IsKeyDown(VK_OPEN_DOOR);
            bool isCloseDoorsKeyDown = IsKeyDown(VK_CLOSE_DOORS);
            bool isOpenDoorsKeyDown = IsKeyDown(VK_OPEN_DOORS);

            if (isESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isESPEnabled = !isESPEnabled;
                lastToggleTime = Time.time;
            }

            if (isEnemyESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isEnemyESPEnabled = !isEnemyESPEnabled;
                lastToggleTime = Time.time;
            }

            if (isPartialESPKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
                isPartialESPEnabled = !isPartialESPEnabled;
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
            var label_text_tmp = isESPEnabled == true ? "On" : "Off";
            GUI.contentColor = isESPEnabled == true ? Color.green : Color.red;
            GUI.Label(new Rect(10f, 25f, 200f, 30f), $"3 - ESP is: {label_text_tmp}");
            if (isESPEnabled)
            {
                ProcessObjects<Terminal>((terminal, vector) => "SHIP TERMINAL ");
                ProcessObjects<Landmine>((landmine, vector) => "LANDMINE ");
                ProcessObjects<Turret>((turret, vector) => "TURRET ");
                ProcessObjects<SteamValveHazard>((valve, vector) => "Steam Valve ");
                ProcessObjects<GrabbableObject>((grabbableObject, vector) => grabbableObject.itemProperties.itemName + " - " + grabbableObject.scrapValue + "\n");
                ProcessObjects<EntranceTeleport>((entrance, vector) => entrance.isEntranceToBuilding ? " Entrance " : " Exit ");
                ProcessPlayers();
            }

            label_text_tmp = isEnemyESPEnabled == true ? "On" : "Off";
            GUI.contentColor = isEnemyESPEnabled == true ? Color.green : Color.red;
            GUI.Label(new Rect(10f, 40f, 200f, 30f), $"4 - Enemy ESP is: {label_text_tmp}");
            if (isEnemyESPEnabled)
                ProcessEnemies();

            label_text_tmp = isPartialESPEnabled == true ? "On" : "Off";
            GUI.contentColor = isPartialESPEnabled == true ? Color.green : Color.red;
            GUI.Label(new Rect(10f, 55f, 200f, 30f), $"5 - Incomplete Path ESP is: {label_text_tmp}");

            GUI.contentColor = Color.white;
            GUI.Label(new Rect(10f, 70f, 200f, 30f), $"6 - Open nearest big door");
            GUI.Label(new Rect(10f, 85f, 200f, 30f), $"7 - Close all big doors");
            GUI.Label(new Rect(10f, 100f, 200f, 30f), $"8 - Open/Unlock all doors");
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
                        Vector3 tmp = obj.transform.position;
                        tmp.y = tmp.y - 1.5f;
                        DrawPath(tmp, GameNetworkManager.Instance.localPlayerController.transform.position, GetColorForObject<T>(), 2f);
                    }
                    else if (obj is GrabbableObject && GameNetworkManager.Instance.localPlayerController.isInsideFactory)
                    {
                        Vector3 target_pos = obj.transform.position;
                        if (obj.name == "Apparatus")
                            target_pos.y = target_pos.y - 3;
                        DrawPath(target_pos, GameNetworkManager.Instance.localPlayerController.transform.position, GetColorForObject<T>(), 2f);
                    }
                }
            }
        }

        private void ProcessPlayers()
        {
            if (!objectCache.TryGetValue(typeof(PlayerControllerB), out var cachedPlayers))
                return;

            foreach (PlayerControllerB player in cachedPlayers.Cast<PlayerControllerB>())
            {
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
                    DrawLabel(screen, label, Color.green, distance);
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
                        DrawLabel(screen, label, Color.red, distance);
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
                    return Color.cyan;
                case "GrabbableObject":
                    return Color.blue;
                case "Landmine":
                    return Color.red;
                case "Turret":
                    return Color.red;
                case "SteamValveHazard":
                    return Color.magenta;
                case "Terminal":
                    return Color.magenta;
                default:
                    return Color.white;
            }
        }

        private void DrawPath(Vector3 target, Vector3 start, Color color, float width)
        {
            if (GameNetworkManager.Instance.localPlayerController == null)
            {
                //Debug.LogWarning($"Not possible to do pathfinding here");
                return;
            }
            NavMeshAgent agent = GameNetworkManager.Instance.localPlayerController.gameObject.GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = GameNetworkManager.Instance.localPlayerController.gameObject.AddComponent<NavMeshAgent>();
            }
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            var path = new NavMeshPath();
            agent.transform.position = start;
            agent.nextPosition = start;
            agent.enabled = false;
            agent.enabled = true;
            agent.CalculatePath(target, path);
            if (path.corners.Length < 3) //if the path has 1 or no corners, there is no need
                return;
            Vector2 previous;
            Vector2 next;
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                previous = world_to_screen(path.corners[1]);
                for (int i = 2; i < path.corners.Length - 1; i++)
                {
                    var screen_pos = world_to_screen(path.corners[i]);
                    next = new Vector2(screen_pos.x, screen_pos.y);
                    render.draw_line(previous, next, color, width);
                    previous = next;
                }
                Vector3 end_pos = world_to_screen(target);
                render.draw_line(previous, end_pos, color, width);
            }
            else if (isPartialESPEnabled && path.status == NavMeshPathStatus.PathPartial)
            {
                previous = world_to_screen(path.corners[1]);
                for (int i = 2; i < path.corners.Length - 1; i++)
                {
                    var screen_pos = world_to_screen(path.corners[i]);
                    next = new Vector2(screen_pos.x, screen_pos.y);
                    render.draw_line(previous, next, Color.yellow, width);
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
