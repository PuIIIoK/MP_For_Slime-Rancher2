using MelonLoader;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Visual beacon for waypoints in the world
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class WaypointBeaconVisual : MonoBehaviour
    {
        public WaypointBeaconVisual(System.IntPtr ptr) : base(ptr) { }

        private ushort playerID;
        private Color beaconColor;
        private LineRenderer beaconLine;
        private Light beaconLight;
        private GameObject lightObject;

        private const float BeaconHeight = 100f;
        private const float BeaconRadius = 1f;
        private const float PulseSpeed = 1.5f;

        public void Initialize(ushort playerId, Color color)
        {
            playerID = playerId;
            beaconColor = color;
            CreateBeacon();
        }

        private void CreateBeacon()
        {
            // Создаем вертикальный луч с помощью LineRenderer
            beaconLine = gameObject.AddComponent<LineRenderer>();
            beaconLine.startWidth = BeaconRadius;
            beaconLine.endWidth = BeaconRadius * 0.2f;
            beaconLine.positionCount = 2;
            beaconLine.useWorldSpace = true;
            beaconLine.alignment = LineAlignment.View;

            // Устанавливаем позиции - от земли вверх
            beaconLine.SetPosition(0, transform.position);
            beaconLine.SetPosition(1, transform.position + Vector3.up * BeaconHeight);

            // Создаем материал с эмиссией
            var beaconMaterial = new Material(Shader.Find("Sprites/Default"));
            beaconMaterial.SetInt("_ZWrite", 0);
            beaconMaterial.SetInt("_ZTest", 0); // Всегда рендерить поверх всего
            beaconMaterial.renderQueue = 3000;

            beaconMaterial.color = beaconColor;
            beaconMaterial.EnableKeyword("_EMISSION");
            beaconMaterial.SetColor("_EmissionColor", beaconColor * 3f);

            beaconLine.material = beaconMaterial;
            beaconLine.startColor = beaconColor;
            beaconLine.endColor = new Color(beaconColor.r, beaconColor.g, beaconColor.b, 0.2f);

            // Добавляем точечный свет
            lightObject = new GameObject("WaypointLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0, 2f, 0);

            beaconLight = lightObject.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = beaconColor;
            beaconLight.intensity = 5f;
            beaconLight.range = 30f;
            beaconLight.renderMode = LightRenderMode.ForcePixel;

            SRMP.Debug($"Created waypoint beacon visual for player {playerID}");
        }

        public void Update()
        {
            if (beaconLight != null)
            {
                // Пульсирующий эффект
                float pulse = Mathf.Sin(Time.time * PulseSpeed) * 0.3f + 0.7f;
                beaconLight.intensity = 5f * pulse;
            }

            // Обновляем позиции луча (если объект двигается)
            if (beaconLine != null)
            {
                beaconLine.SetPosition(0, transform.position);
                beaconLine.SetPosition(1, transform.position + Vector3.up * BeaconHeight);
            }
        }

        public void OnDestroy()
        {
            if (lightObject != null)
            {
                Destroy(lightObject);
            }
        }
    }
}

