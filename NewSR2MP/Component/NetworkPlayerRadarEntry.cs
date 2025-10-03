using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.UI;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Radar entry for displaying network players on the compass/radar
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkPlayerRadarEntry : MonoBehaviour
    {
        public NetworkPlayerRadarEntry(System.IntPtr ptr) : base(ptr) { }

        private NetworkPlayer networkPlayer;
        public bool isLocalPlayer = false;

        public void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
        }

        // IRadarEntry implementation - these properties are accessed by the game's radar system
        
        // World position of this radar entry
        public Vector3 WorldPosition => transform.position;
        
        // Scene group for this entry (can be null)
        public SceneGroup SceneGroup => null;
        
        // Whether this entry is valid and should be displayed
        public bool IsValid
        {
            get
            {
                // Return true so player icons are shown on the MAP
                // Compass visibility is controlled by InstantiateCompassMarkerPrefab
                return (ServerActive() || ClientActive()) && networkPlayer != null;
            }
        }
        
        // How the entry should behave when outside the compass bounds
        public RadarCompassOverflowMode OverflowMode => RadarCompassOverflowMode.CLAMP;

        // Create radar marker prefab (for map view)
        public GameObject InstantiateRadarMarkerPrefab(Transform parent)
        {
            // Return null to use default marker for map display
            // The PlayerDisplayOnMap component handles the actual map icon
            return null;
        }

        // Create compass marker prefab (for compass view)
        public GameObject InstantiateCompassMarkerPrefab(Transform parent)
        {
            // Return null to NOT show player icons on compass
            // Only waypoints will appear on compass
            return null;
        }
    }
}

