using Il2CppMonomiPark.SlimeRancher.UI.Map;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Creates and manages a map icon for a network player
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkPlayerMapIcon : MonoBehaviour
    {
        public NetworkPlayerMapIcon(System.IntPtr ptr) : base(ptr) { }

        private NetworkPlayer networkPlayer;
        private GameObject mapIconObject;
        private RectTransform mapIconRect;
        private Image mapIconImage;
        private MapUI mapUI;
        private bool isInitialized = false;

        public void Awake()
        {
            networkPlayer = GetComponent<NetworkPlayer>();
            if (networkPlayer == null)
            {
                SRMP.Error("NetworkPlayerMapIcon: No NetworkPlayer component found!");
                return;
            }
        }

        public void Start()
        {
            try
            {
                // Отложенная инициализация, чтобы MapUI успел загрузиться
                MelonCoroutines.Start(InitializeMapIcon());
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to start map icon initialization: {ex}");
            }
        }

        private System.Collections.IEnumerator InitializeMapIcon()
        {
            // Ждем пока MapUI будет готов
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
                SRMP.Debug("NetworkPlayerMapIcon: MapUI not found after waiting - player icon will not be shown on map");
                yield break;
            }

            try
            {
                CreateMapIcon();
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to create map icon: {ex}");
            }
        }

        private void CreateMapIcon()
        {
            // Находим канвас карты
            var mapCanvas = mapUI.transform.Find("MapCanvas");
            if (mapCanvas == null)
            {
                SRMP.Error("MapCanvas not found!");
                return;
            }

            // Создаем иконку игрока
            mapIconObject = new GameObject($"NetworkPlayerIcon_{networkPlayer.id}");
            mapIconObject.transform.SetParent(mapCanvas, false);

            // Добавляем RectTransform
            mapIconRect = mapIconObject.AddComponent<RectTransform>();
            mapIconRect.sizeDelta = new Vector2(20f, 20f);
            mapIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            mapIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapIconRect.pivot = new Vector2(0.5f, 0.5f);

            // Добавляем Image компонент
            mapIconImage = mapIconObject.AddComponent<Image>();
            
            // Устанавливаем цвет иконки
            Color iconColor;
            if (networkPlayer.id == ushort.MaxValue)
            {
                iconColor = Color.cyan; // Хост - голубой
            }
            else if (networkPlayer.id == currentPlayerID)
            {
                iconColor = Color.green; // Локальный игрок - зеленый
            }
            else
            {
                iconColor = Color.yellow; // Другие игроки - желтый
            }
            mapIconImage.color = iconColor;

            // Создаем простую круглую иконку
            CreateCircleSprite();

            isInitialized = true;
            SRMP.Debug($"Created map icon for player {networkPlayer.id}");
        }

        private void CreateCircleSprite()
        {
            // Создаем текстуру для круглой иконки
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f)
            );

            mapIconImage.sprite = sprite;
        }

        public void LateUpdate()
        {
            if (!isInitialized || mapIconObject == null || mapUI == null)
                return;

            try
            {
                UpdateIconPosition();
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to update map icon position: {ex}");
            }
        }

        private void UpdateIconPosition()
        {
            if (networkPlayer == null || networkPlayer.transform == null)
                return;

            // Получаем мировую позицию игрока
            Vector3 worldPosition = networkPlayer.transform.position;

            // Преобразуем мировую позицию в позицию на карте
            // Это упрощенная версия - может потребоваться корректировка
            Vector2 mapPosition = WorldToMapPosition(worldPosition);

            // Обновляем позицию иконки
            if (mapIconRect != null)
            {
                mapIconRect.anchoredPosition = mapPosition;
            }
        }

        private Vector2 WorldToMapPosition(Vector3 worldPos)
        {
            // Упрощенное преобразование мировых координат в координаты карты
            // Масштаб может потребоваться настройка в зависимости от размера карты
            float scale = 1f; // Можно настроить
            return new Vector2(worldPos.x * scale, worldPos.z * scale);
        }

        public void OnDestroy()
        {
            try
            {
                if (mapIconObject != null)
                {
                    Destroy(mapIconObject);
                }
            }
            catch { }
        }
    }
}

