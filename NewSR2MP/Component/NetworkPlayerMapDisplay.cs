using Il2CppMonomiPark.SlimeRancher.Regions;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.World;
using MelonLoader;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Component that displays a network player on the map and compass
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkPlayerMapDisplay : MonoBehaviour
    {
        public NetworkPlayerMapDisplay(System.IntPtr ptr) : base(ptr) { }

        private PlayerDisplayOnMap displayComponent;
        private NetworkPlayer networkPlayer;
        private PlayerZoneTracker zoneTracker;
        private NetworkPlayerRadarEntry radarEntry;
        private NetworkPlayerMapIcon mapIcon;
        private bool isInitialized = false;

        public void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                SRMP.Error("NetworkPlayerMapDisplay: No NetworkPlayer component found!");
                return;
            }
        }

        public void Start()
        {
            try
            {
                InitializeMapDisplay();
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to initialize map display for player: {ex}");
            }
        }

        private void InitializeMapDisplay()
        {
            // Add PlayerDisplayOnMap component
            displayComponent = gameObject.AddComponent<PlayerDisplayOnMap>();
            
            // Add PlayerZoneTracker component
            zoneTracker = gameObject.AddComponent<PlayerZoneTracker>();
            
            // Add radar entry for compass display
            radarEntry = gameObject.AddComponent<NetworkPlayerRadarEntry>();
            if (radarEntry != null && networkPlayer != null)
            {
                radarEntry.isLocalPlayer = (networkPlayer.id == currentPlayerID);
            }
            
            // Add map icon for displaying player on the mini-map
            mapIcon = gameObject.AddComponent<NetworkPlayerMapIcon>();
            SRMP.Debug($"Added map icon for player {networkPlayer?.id}");
            
            // NOTE: World beacons removed as requested - they were distracting on player models
            
            // Link zone tracker to display component
            if (displayComponent != null && zoneTracker != null)
            {
                displayComponent._playerZoneTracker = zoneTracker;
                isInitialized = true;
                
                SRMP.Debug($"Initialized map display for player {networkPlayer?.id}");
            }
        }

        public void OnDestroy()
        {
            try
            {
                if (displayComponent != null)
                {
                    Destroy(displayComponent);
                }
                if (zoneTracker != null)
                {
                    Destroy(zoneTracker);
                }
                if (radarEntry != null)
                {
                    Destroy(radarEntry);
                }
                if (mapIcon != null)
                {
                    Destroy(mapIcon);
                }
            }
            catch { }
        }
    }
}

