using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Displays waypoints for all players on the map
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class MultiplayerWaypointMapIcon : MonoBehaviour
    {
        public MultiplayerWaypointMapIcon(System.IntPtr ptr) : base(ptr) { }

        private MapUI mapUI;
        private Dictionary<ushort, GameObject> waypointIcons;

        public void Start()
        {
            waypointIcons = new Dictionary<ushort, GameObject>();
            MelonCoroutines.Start(InitializeWaypointDisplay());
        }

        private System.Collections.IEnumerator InitializeWaypointDisplay()
        {
            // Wait for MapUI to be ready
            int attempts = 0;
            while (mapUI == null && attempts < 100)
            {
                try
                {
                    mapUI = Object.FindObjectOfType<MapUI>();
                }
                catch { }
                
                attempts++;
                yield return new WaitForSeconds(0.2f);
            }

            if (mapUI == null)
            {
                SRMP.Debug("MultiplayerWaypointMapIcon: MapUI not found");
                yield break;
            }

            SRMP.Log("Multiplayer waypoint map display initialized");
        }

        public void Update()
        {
            if (mapUI == null || !mapUI.isActiveAndEnabled) return;
            if (MultiplayerWaypointManager.Instance == null) return;

            // Update or create waypoint icons
            var allWaypoints = MultiplayerWaypointManager.Instance.GetAllWaypoints();
            foreach (var kvp in allWaypoints)
            {
                ushort playerID = kvp.Key;
                var waypoint = kvp.Value;

                if (!waypoint.isActive)
                {
                    RemoveWaypointIcon(playerID);
                    continue;
                }

                // Skip local player's waypoint (handled by game's MapDirector)
                if (playerID == currentPlayerID)
                {
                    RemoveWaypointIcon(playerID);
                    continue;
                }

                // Create or update waypoint icon
                if (!waypointIcons.ContainsKey(playerID) || waypointIcons[playerID] == null)
                {
                    CreateWaypointIcon(playerID, waypoint);
                }
                else
                {
                    UpdateWaypointIcon(playerID, waypoint);
                }
            }

            // Remove icons for waypoints that no longer exist
            var keysToRemove = new List<ushort>();
            foreach (var iconKvp in waypointIcons)
            {
                if (!allWaypoints.ContainsKey(iconKvp.Key) || !allWaypoints[iconKvp.Key].isActive)
                {
                    keysToRemove.Add(iconKvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                RemoveWaypointIcon(key);
            }
        }

        private void CreateWaypointIcon(ushort playerID, WaypointData waypoint)
        {
            if (mapUI == null) return;

            try
            {
                // Find map canvas
                var mapCanvas = mapUI.transform.Find("MapCanvas");
                if (mapCanvas == null) return;

                var mapRoot = mapCanvas.Find("Map");
                if (mapRoot == null) return;

                // Create waypoint icon GameObject
                var waypointIcon = new GameObject($"MultiplayerWaypoint_{playerID}");
                waypointIcon.transform.SetParent(mapRoot, false);

                // Add RectTransform
                var rectTransform = waypointIcon.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(30f, 30f);

                // Add Image component
                var image = waypointIcon.AddComponent<Image>();
                
                // Create waypoint marker sprite (triangle pointing down)
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                var pixels = new Color32[32 * 32];
                
                Color32 waypointColor = GetWaypointColor(playerID);
                
                // Draw triangle
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        // Triangle pointing down
                        if (y < 24 && x >= (16 - y/2) && x <= (16 + y/2))
                        {
                            pixels[y * 32 + x] = waypointColor;
                        }
                        else
                        {
                            pixels[y * 32 + x] = new Color32(0, 0, 0, 0);
                        }
                    }
                }
                
                texture.SetPixels32(pixels);
                texture.Apply();

                var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
                image.sprite = sprite;

                waypointIcons[playerID] = waypointIcon;

                SRMP.Debug($"Created waypoint icon for player {playerID} on map");
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to create waypoint icon: {ex}");
            }
        }

        private void UpdateWaypointIcon(ushort playerID, WaypointData waypoint)
        {
            if (!waypointIcons.TryGetValue(playerID, out var icon) || icon == null) return;

            try
            {
                var rectTransform = icon.GetComponent<RectTransform>();
                if (rectTransform == null) return;

                // Simply position at world position - map will handle scaling
                rectTransform.anchoredPosition = new Vector2(waypoint.position.x, waypoint.position.z);
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to update waypoint icon: {ex}");
            }
        }

        private void RemoveWaypointIcon(ushort playerID)
        {
            if (waypointIcons.TryGetValue(playerID, out var icon) && icon != null)
            {
                Destroy(icon);
                waypointIcons.Remove(playerID);
            }
        }

        private Color32 GetWaypointColor(ushort playerID)
        {
            if (playerID == ushort.MaxValue)
            {
                // Host - green
                return new Color32(0, 255, 0, 255);
            }
            else
            {
                // Clients - red  
                return new Color32(255, 0, 0, 255);
            }
        }

        public void OnDestroy()
        {
            if (waypointIcons != null)
            {
                foreach (var icon in waypointIcons.Values)
                {
                    if (icon != null)
                    {
                        Destroy(icon);
                    }
                }
                waypointIcons.Clear();
            }
        }
    }
}

