using BepInEx;
using GameNetcodeStuff;
using UnityEngine;
using System;
using UnityEngine.AI;
using System.IO;

namespace LethalMod
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PathFinder : MonoBehaviour
    {
        NavMeshAgent agent;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        public Vector3[] DrawPath(Vector3 start, Vector3 target, Color color)
        {
            var path = new NavMeshPath();
            if (agent.isOnNavMesh == false) {
              agent.Warp(start);
            }
            agent.nextPosition = start;
            agent.CalculatePath(target, path);
            var line = this.GetComponent<LineRenderer>();
            if( line == null )
            {
              line = this.gameObject.AddComponent<LineRenderer>();
              line.material = new Material( Shader.Find( "Sprites/Default" ) ) { color = color };
              line.SetWidth( 0.5f, 0.5f );
              line.SetColors( color, color );
            }

            line.SetVertexCount( path.corners.Length );

            for( int i = 0; i < path.corners.Length; i++ )
            {
              line.SetPosition( i, path.corners[ i ] );
            }

            Debug.Log($"Found {path.corners.Length} waypoints");

            return path.corners;
        }
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private EnemyAI[] enemies;
        private PlayerControllerB local_player;
        private GrabbableObject[] grabbable_objects;
        private Camera camera;

        private readonly float entity_update_interval = 5f;
        private float entity_update_timer;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            enemies = [];
            grabbable_objects = [];
        }

        public void Update()
        {
            EntityUpdate();
        }

        public void OnGUI()
        {
            foreach (var go in grabbable_objects)
            {
                esp(go.transform.position, Color.green, go.transform.name);
            }

            foreach (var enemy in enemies)
            {
                esp(enemy.transform.position, Color.red, enemy.transform.name);
            }

        }

        private void EntityUpdate()
        {
            if (entity_update_timer <= 0f)
            {
                enemies = FindObjectsOfType<EnemyAI>();
                grabbable_objects = FindObjectsOfType<GrabbableObject>();

                // You have to open menu to get local player lol
                local_player = HUDManager.Instance?.localPlayer;
                if (local_player != null) {
                    camera = local_player.gameplayCamera;
                    if (camera.gameObject.GetComponent<PathFinder>() == null) {
                        Logger.LogWarning($"Attaching PathFinder to local player");
                        camera.gameObject.AddComponent<PathFinder>(); 
                    }
                    // if (NavMesh.SamplePosition(local_player.transform.position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas)) {
                    //     if (camera.gameObject.GetComponent<NavMeshAgent>() == null) {
                    //         Logger.LogWarning($"Attaching NavMeshAgent to local player");
                    //         var agent = camera.gameObject.AddComponent<NavMeshAgent>();
                    //         agent.enabled = false;
                    //         agent.isStopped = true;
                    //     }
                    // }
                    // else
                    // {
                    //     Logger.LogError("No valid NavMesh found. Make sure a NavMesh is baked in the scene.");
                    // }
                    entity_update_timer = entity_update_interval;
                }
            }

            entity_update_timer -= Time.deltaTime;
        }

        private Vector3 world_to_screen(Vector3 world)
        {
            Vector3 screen = camera.WorldToViewportPoint(world);

            screen.x *= Screen.width;
            screen.y *= Screen.height;

            screen.y = Screen.height - screen.y;

            return screen;
        }

        private float distance(Vector3 world_position)
        {
            return Vector3.Distance(camera.transform.position, world_position);
        }

        private void esp(Vector3 entity_position, Color color, String label)
        {
            if (camera == null)
            {
                Logger.LogDebug($"not in-game; camera is null");
                return;
            }

            Vector3 entity_screen_pos = world_to_screen(entity_position);

            if (entity_screen_pos.z < 0 || Math.Abs(entity_position.y - local_player.transform.position.y) > 50)
            {
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
                box_height,
                color, box_thickness);
                camera.gameObject.GetComponent<PathFinder>().DrawPath(camera.transform.position, entity_position, color);
                //render.draw_line(new Vector2(Screen.width / 2, Screen.height),
                //new Vector2(entity_screen_pos.x, entity_screen_pos.y + box_height / 2), color, 2f);
            }
        }
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
