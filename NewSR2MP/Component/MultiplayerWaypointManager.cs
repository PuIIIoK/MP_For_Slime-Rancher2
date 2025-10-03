using Il2CppMonomiPark.SlimeRancher.Map;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Manages waypoints for all players in multiplayer
    /// </summary>
    public class MultiplayerWaypointManager
    {
        // Singleton instance
        private static MultiplayerWaypointManager _instance;
        public static MultiplayerWaypointManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MultiplayerWaypointManager();
                }
                return _instance;
            }
        }

        // Хранилище waypoints: playerID -> WaypointData
        private Dictionary<ushort, WaypointData> playerWaypoints;

        // Визуальные маяки waypoints в мире
        private Dictionary<ushort, GameObject> waypointBeacons;

        private MultiplayerWaypointManager()
        {
            // Initialize dictionaries in constructor
            playerWaypoints = new Dictionary<ushort, WaypointData>();
            waypointBeacons = new Dictionary<ushort, GameObject>();
            
            SRMP.Log("MultiplayerWaypointManager initialized");
        }

        /// <summary>
        /// Устанавливает waypoint для игрока
        /// </summary>
        public void SetWaypoint(ushort playerID, Vector3 position, MapType mapType)
        {
            if (playerWaypoints == null) return;
            
            if (!playerWaypoints.ContainsKey(playerID))
            {
                playerWaypoints[playerID] = new WaypointData();
            }

            playerWaypoints[playerID].position = position;
            playerWaypoints[playerID].mapType = mapType;
            playerWaypoints[playerID].isActive = true;

            // Создаем или обновляем визуальный маяк
            UpdateWaypointBeacon(playerID);

            SRMP.Debug($"Set waypoint for player {playerID} at {position}");
        }

        /// <summary>
        /// Удаляет waypoint игрока
        /// </summary>
        public void ClearWaypoint(ushort playerID)
        {
            if (playerWaypoints != null && playerWaypoints.ContainsKey(playerID))
            {
                playerWaypoints[playerID].isActive = false;
            }

            // Удаляем визуальный маяк
            RemoveWaypointBeacon(playerID);

            SRMP.Debug($"Cleared waypoint for player {playerID}");
        }

        /// <summary>
        /// Получает waypoint игрока
        /// </summary>
        public WaypointData GetWaypoint(ushort playerID)
        {
            if (playerWaypoints != null && playerWaypoints.TryGetValue(playerID, out var waypoint))
            {
                return waypoint;
            }
            return null;
        }

        /// <summary>
        /// Получает все активные waypoints
        /// </summary>
        public Dictionary<ushort, WaypointData> GetAllWaypoints()
        {
            if (playerWaypoints == null) return new Dictionary<ushort, WaypointData>();
            return new Dictionary<ushort, WaypointData>(playerWaypoints);
        }

        /// <summary>
        /// Создает или обновляет визуальный маяк waypoint в мире
        /// </summary>
        private void UpdateWaypointBeacon(ushort playerID)
        {
            if (playerWaypoints == null || waypointBeacons == null) return;
            
            if (!playerWaypoints.TryGetValue(playerID, out var waypoint) || !waypoint.isActive)
                return;

            // Удаляем старый маяк если есть
            RemoveWaypointBeacon(playerID);

            // Создаем новый маяк
            GameObject beacon = new GameObject($"WaypointBeacon_{playerID}");
            beacon.transform.position = waypoint.position;

            // Добавляем компонент визуализации маяка
            var beaconComponent = beacon.AddComponent<WaypointBeaconVisual>();
            beaconComponent.Initialize(playerID, GetWaypointColor(playerID));

            waypointBeacons[playerID] = beacon;
            Object.DontDestroyOnLoad(beacon);

            SRMP.Debug($"Created waypoint beacon for player {playerID}");
        }

        /// <summary>
        /// Удаляет визуальный маяк waypoint
        /// </summary>
        private void RemoveWaypointBeacon(ushort playerID)
        {
            if (waypointBeacons == null) return;
            
            if (waypointBeacons.TryGetValue(playerID, out var beacon))
            {
                if (beacon != null)
                {
                    Object.Destroy(beacon);
                }
                waypointBeacons.Remove(playerID);
            }
        }

        /// <summary>
        /// Получает цвет waypoint для игрока
        /// </summary>
        private Color GetWaypointColor(ushort playerID)
        {
            if (playerID == ushort.MaxValue)
            {
                // Хост - зеленый
                return Color.green;
            }
            else
            {
                // Клиенты - красный
                return Color.red;
            }
        }

        /// <summary>
        /// Очищает все waypoints (при отключении от сервера)
        /// </summary>
        public void ClearAll()
        {
            if (waypointBeacons != null)
            {
                foreach (var beacon in waypointBeacons.Values)
                {
                    if (beacon != null)
                    {
                        Object.Destroy(beacon);
                    }
                }
                waypointBeacons.Clear();
            }
            
            if (playerWaypoints != null)
            {
                playerWaypoints.Clear();
            }

            SRMP.Log("Cleared all waypoints");
        }

    }

    /// <summary>
    /// Данные waypoint игрока
    /// </summary>
    public class WaypointData
    {
        public Vector3 position;
        public MapType mapType;
        public bool isActive;
    }
}

