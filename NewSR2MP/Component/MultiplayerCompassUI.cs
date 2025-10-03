using Il2CppMonomiPark.SlimeRancher.UI.HUD;
using Il2CppTMPro;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Adds multiplayer player coordinates to the compass UI
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class MultiplayerCompassUI : MonoBehaviour
    {
        public MultiplayerCompassUI(System.IntPtr ptr) : base(ptr) { }

        private CompassBarUI compassBarUI;
        private GameObject coordinatesPanel;
        private Il2CppSystem.Collections.Generic.Dictionary<int, TextMeshProUGUI> playerCoordinateTexts;
        
        private float updateTimer = 0f;
        private const float UpdateInterval = 0.5f; // Update twice per second

        public void Awake()
        {
            playerCoordinateTexts = new Il2CppSystem.Collections.Generic.Dictionary<int, TextMeshProUGUI>();
            compassBarUI = GetComponent<CompassBarUI>();
        }

        public void Start()
        {
            try
            {
                CreateCoordinatesPanel();
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Failed to create coordinates panel: {ex}");
            }
        }

        private void CreateCoordinatesPanel()
        {
            // Find the compass bar UI transform
            if (compassBarUI == null)
            {
                SRMP.Error("CompassBarUI not found!");
                return;
            }

            // Create a panel for player coordinates
            coordinatesPanel = new GameObject("MultiplayerCoordinates");
            coordinatesPanel.transform.SetParent(transform, false);

            var rectTransform = coordinatesPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f); // Top right
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-10f, -10f);
            rectTransform.sizeDelta = new Vector2(300f, 200f);

            SRMP.Debug("Multiplayer coordinates panel created");
        }

        public void LateUpdate()
        {
            updateTimer += Time.deltaTime;
            
            if (updateTimer >= UpdateInterval)
            {
                updateTimer = 0f;
                UpdatePlayerCoordinates();
            }
        }

        private void UpdatePlayerCoordinates()
        {
            if (coordinatesPanel == null) return;

            try
            {
                // Clear old text objects
                foreach (var kvp in playerCoordinateTexts)
                {
                    if (kvp.Value != null && kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
                playerCoordinateTexts.Clear();

                int index = 0;
                float yOffset = 0f;

                // Add all players
                foreach (var player in players)
                {
                    try
                    {
                        // Check if player or worldObject is null first
                        if (player == null || player.worldObject == null) 
                            continue;
                        
                        // Try to access gameObject - if destroyed, this will throw an IL2CPP exception
                        var playerGameObject = player.worldObject.gameObject;
                        if (playerGameObject == null) 
                            continue;

                        var playerPos = player.worldObject.transform.position;
                        var playerName = playerUsernamesReverse.ContainsKey(player.playerID) 
                            ? playerUsernamesReverse[player.playerID] 
                            : (player.playerID == ushort.MaxValue ? "Host" : $"Player {player.playerID}");

                        // Create text for this player
                        var textObj = new GameObject($"PlayerCoord_{player.playerID}");
                        textObj.transform.SetParent(coordinatesPanel.transform, false);

                        var textRect = textObj.AddComponent<RectTransform>();
                        textRect.anchorMin = new Vector2(0f, 1f);
                        textRect.anchorMax = new Vector2(1f, 1f);
                        textRect.pivot = new Vector2(0f, 1f);
                        textRect.anchoredPosition = new Vector2(5f, yOffset);
                        textRect.sizeDelta = new Vector2(-10f, 25f);

                        var textMesh = textObj.AddComponent<TextMeshProUGUI>();
                        textMesh.text = $"{playerName}: ({playerPos.x:F0}, {playerPos.y:F0}, {playerPos.z:F0})";
                        textMesh.fontSize = 14;
                        textMesh.color = player.playerID == currentPlayerID ? Color.green : Color.white;
                        textMesh.alignment = TextAlignmentOptions.Left;

                        // Add outline for better visibility
                        var outline = textObj.AddComponent<Outline>();
                        outline.effectColor = Color.black;
                        outline.effectDistance = new Vector2(1f, -1f);

                        playerCoordinateTexts.Add(player.playerID, textMesh);

                        yOffset -= 25f;
                        index++;
                    }
                    catch
                    {
                        // Silently skip destroyed or invalid player objects
                        // This happens when a player disconnects
                        continue;
                    }
                }
            }
            catch (System.Exception ex)
            {
                SRMP.Error($"Error updating player coordinates: {ex}");
            }
        }

        public void OnDestroy()
        {
            try
            {
                if (coordinatesPanel != null)
                {
                    Destroy(coordinatesPanel);
                }
            }
            catch { }
        }
    }
}

