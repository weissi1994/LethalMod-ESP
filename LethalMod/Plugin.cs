using BepInEx;
using GameNetcodeStuff;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.IO;
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

        private float lastToggleTime = 0f;
        private const float toggleCooldown = 0.5f; 

        #region Keypress logic
        private const int VK_INSERT = 0x2D;

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
        }

        void CacheObjects<T>() where T : Component
        {
          objectCache[typeof(T)] = new List<Component>(FindObjectsOfType<T>());
        }
        #endregion

        public void Update()
        {
            bool isKeyDown = IsKeyDown(VK_INSERT);

            if (isKeyDown && Time.time - lastToggleTime > toggleCooldown)
            {
              isESPEnabled = !isESPEnabled;
              lastToggleTime = Time.time;
            }
        }

        public void OnGUI()
        {
            if (isESPEnabled) {
                ProcessObjects<EntranceTeleport>((entrance, vector) => entrance.isEntranceToBuilding ? " Entrance " : " Exit ");
                ProcessObjects<Landmine>((landmine, vector) => "LANDMINE ");
                ProcessObjects<Turret>((turret, vector) => "TURRET ");
                ProcessObjects<Terminal>((terminal, vector) => "SHIP TERMINAL ");
                ProcessObjects<SteamValveHazard>((valve, vector) => "Steam Valve ");
                ProcessPlayers();

                ProcessObjects<GrabbableObject>((grabbableObject, vector) => grabbableObject.itemProperties.itemName + " ");

                ProcessEnemies();

                // foreach (var entry in entries)
                // {
                //     var tmp = entry.transform.position;
                //     tmp.y = tmp.y - 1;
                //     esp(tmp, Color.blue, entry.transform.name);
                // }
            }
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
            if (obj is GrabbableObject GO && (GO.isPocketed || GO.isHeld))
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
              esp(obj.transform.position, screen, GetColorForObject<T>(), label);
            }
          }
        }

        private void ProcessPlayers()
        {
          if (!objectCache.TryGetValue(typeof(PlayerControllerB), out var cachedPlayers))
            return;

          foreach (PlayerControllerB player in cachedPlayers.Cast<PlayerControllerB>())
          {
            if (player.isPlayerDead || player.IsLocalPlayer || player.playerUsername == GameNetworkManager.Instance.localPlayerController.playerUsername || player.disconnectedMidGame)
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
            if (WorldToScreen(GameNetworkManager.Instance.localPlayerController.gameplayCamera,
                enemyAI.eye.transform.position, out screen))
            {
              string label;
              if (string.IsNullOrWhiteSpace(enemyAI.enemyType.enemyName))
              {
                label = "Unknown Enemy ";
              }
              else
                label = enemyAI.enemyType.enemyName + " ";
              float distance = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, enemyAI.eye.transform.position);
              distance = (float)Math.Round(distance);
              DrawLabel(screen, label, Color.red, distance);
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
              return Color.yellow;
            case "Terminal":
              return Color.magenta;
            default:
              return Color.white;
          }
        }

        private void esp(Vector3 entity_position, Vector3 entity_screen_pos, Color color, String label)
        {
            if (GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
            {
                Logger.LogDebug($"not in-game; camera is null");
                return;
            }

            float distance_to_entity = distance(entity_position);
            float box_width = 300 / distance_to_entity;
            float box_height = 300 / distance_to_entity;

            float box_thickness = 3f;

            if (entity_screen_pos.x > 0 && entity_screen_pos.x < Screen.width && entity_screen_pos.y > 0 && entity_screen_pos.y < Screen.height)
            {
                render.draw_string(
                new Vector2(entity_screen_pos.x - box_width, entity_screen_pos.y - box_height), $"{label} - {(int)distance_to_entity}",
                color, false);
                render.draw_box_outline(
                new Vector2(entity_screen_pos.x - box_width / 2, entity_screen_pos.y - box_height / 2), box_width,
                box_height, color, box_thickness);
                draw_path(entity_position, GameNetworkManager.Instance.localPlayerController.transform.position, color, 2f);
                //render.draw_line(new Vector2(Screen.width / 2, Screen.height),
                //new Vector2(entity_screen_pos.x, entity_screen_pos.y + box_height / 2), color, 2f);
            }
        }

        private void draw_path(Vector3 target, Vector3 start, Color color, float width)
        {
          NavMeshAgent agent = GameNetworkManager.Instance.localPlayerController.gameObject.GetComponent<NavMeshAgent>();
          if (agent == null) {
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
          Vector2 previous = new Vector2(Screen.width / 2, Screen.height);
          Vector2 next;
          switch (path.status)
          {
              case NavMeshPathStatus.PathComplete:
                  for (int i = 0; i < path.corners.Length - 1; i++) {
                      var screen_pos = world_to_screen(path.corners[i]);
                      next = new Vector2(screen_pos.x, screen_pos.y);
                      render.draw_line(previous, next, color, width);
                      previous = next;
                  }
                  Vector3 end_pos = world_to_screen(target);
                  render.draw_line(previous, end_pos, color, width);
                  break;
              case NavMeshPathStatus.PathPartial:
                  Debug.LogWarning($"will only be able to move partway");
                  for (int i = 0; i < path.corners.Length - 1; i++) {
                      var screen_pos = world_to_screen(path.corners[i]);
                      next = new Vector2(screen_pos.x, screen_pos.y);
                      render.draw_line(previous, next, Color.yellow, width);
                      previous = next;
                  }
                  break;
              default:
                  Debug.LogError($"There is no valid path to reach.");
                  break;
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

        public static void draw_box(Vector2 position, Vector2 size, Color color, bool centered = true)
        {
            Color = color;
            draw_box(position, size, centered);
        }

        public static void draw_box(Vector2 position, Vector2 size, bool centered = true)
        {
            var upperLeft = centered ? position - size / 2f : position;
            GUI.DrawTexture(new Rect(position, size), Texture2D.whiteTexture, ScaleMode.StretchToFill);
            Color = Color.white;
        }

        public static void draw_string(Vector2 position, string label, Color color, bool centered = true)
        {
            Color = color;
            draw_string(position, label, centered);
        }

        public static void draw_string(Vector2 position, string label, bool centered = true)
        {
            var content = new GUIContent(label);
            var size = StringStyle.CalcSize(content);
            var upperLeft = centered ? position - size / 2f : position;
            GUI.Label(new Rect(upperLeft, size), content);
        }

        public static Texture2D lineTex;

        public static void draw_line(Vector2 pointA, Vector2 pointB, Color color, float width)
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

        public static void draw_box(float x, float y, float w, float h, Color color, float thickness)
        {
            draw_line(new Vector2(x, y), new Vector2(x + w, y), color, thickness);
            draw_line(new Vector2(x, y), new Vector2(x, y + h), color, thickness);
            draw_line(new Vector2(x + w, y), new Vector2(x + w, y + h), color, thickness);
            draw_line(new Vector2(x, y + h), new Vector2(x + w, y + h), color, thickness);
        }

        public static void draw_box_outline(Vector2 Point, float width, float height, Color color, float thickness)
        {
            draw_line(Point, new Vector2(Point.x + width, Point.y), color, thickness);
            draw_line(Point, new Vector2(Point.x, Point.y + height), color, thickness);
            draw_line(new Vector2(Point.x + width, Point.y + height), new Vector2(Point.x + width, Point.y), color, thickness);
            draw_line(new Vector2(Point.x + width, Point.y + height), new Vector2(Point.x, Point.y + height), color,
                thickness);
        }
    }
}
