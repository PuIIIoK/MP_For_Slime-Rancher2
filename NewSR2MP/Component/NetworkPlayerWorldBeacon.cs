using MelonLoader;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Creates a visible beacon in the world to show player positions (like waypoint beacons)
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkPlayerWorldBeacon : MonoBehaviour
    {
        public NetworkPlayerWorldBeacon(System.IntPtr ptr) : base(ptr) { }

        private NetworkPlayer networkPlayer;
        private GameObject beaconObject;
        private GameObject beaconPillar;
        private Light beaconLight;
        private LineRenderer beaconLine;
        
        private const float BeaconHeight = 50f;
        private const float BeaconRadius = 0.5f;
        private const float PulseSpeed = 2f;
        
        public void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                SRMP.Error("NetworkPlayerWorldBeacon: No NetworkPlayer component found!");
                return;
            }
        }

        public void Start()
        {
            try
            {
                CreateBeacon();
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to create world beacon for player: {ex}");
            }
        }

        private void CreateBeacon()
        {
            // Create main beacon object
            beaconObject = new GameObject($"PlayerBeacon_{networkPlayer.id}");
            beaconObject.transform.SetParent(transform, false);
            beaconObject.transform.localPosition = Vector3.zero;

            // Create vertical light pillar using LineRenderer
            beaconLine = beaconObject.AddComponent<LineRenderer>();
            beaconLine.startWidth = BeaconRadius;
            beaconLine.endWidth = BeaconRadius * 0.3f;
            beaconLine.positionCount = 2;
            beaconLine.useWorldSpace = false;
            beaconLine.alignment = LineAlignment.View; // Always face camera
            
            // Set positions - from ground to sky
            beaconLine.SetPosition(0, Vector3.zero);
            beaconLine.SetPosition(1, new Vector3(0, BeaconHeight, 0));

            // Create material with emission for glow effect
            var beaconMaterial = new Material(Shader.Find("Sprites/Default"));
            beaconMaterial.SetInt("_ZWrite", 0);
            beaconMaterial.SetInt("_ZTest", 0); // Always render on top
            beaconMaterial.renderQueue = 3000;
            
            // Set color based on player
            Color beaconColor = GetPlayerColor();
            beaconMaterial.color = beaconColor;
            
            // Enable emission for glow
            beaconMaterial.EnableKeyword("_EMISSION");
            beaconMaterial.SetColor("_EmissionColor", beaconColor * 2f);
            
            beaconLine.material = beaconMaterial;
            beaconLine.startColor = beaconColor;
            beaconLine.endColor = new Color(beaconColor.r, beaconColor.g, beaconColor.b, 0.3f);

            // Add point light for additional glow
            var lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(beaconObject.transform, false);
            lightObj.transform.localPosition = new Vector3(0, 5f, 0);
            
            beaconLight = lightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = beaconColor;
            beaconLight.intensity = 3f;
            beaconLight.range = 20f;
            beaconLight.renderMode = LightRenderMode.ForcePixel;

            SRMP.Debug($"Created world beacon for player {networkPlayer.id}");
        }

        private Color GetPlayerColor()
        {
            // Get color based on player - host is cyan, clients are different colors
            if (networkPlayer.id == currentPlayerID)
            {
                return Color.green; // Local player
            }
            else if (networkPlayer.id == ushort.MaxValue)
            {
                return Color.cyan; // Host
            }
            else
            {
                // Generate color based on player ID
                float hue = (networkPlayer.id * 0.618033988749895f) % 1.0f; // Golden ratio for color distribution
                return Color.HSVToRGB(hue, 0.8f, 1.0f);
            }
        }

        public void Update()
        {
            if (beaconLight != null)
            {
                // Pulse the light intensity
                float pulse = Mathf.Sin(Time.time * PulseSpeed) * 0.3f + 0.7f;
                beaconLight.intensity = 3f * pulse;
            }

            // Update beacon position to follow player
            if (beaconObject != null)
            {
                beaconObject.transform.position = transform.position;
            }
        }

        public void OnDestroy()
        {
            try
            {
                if (beaconObject != null)
                {
                    Destroy(beaconObject);
                }
            }
            catch { }
        }
    }
}

