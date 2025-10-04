using System;
using System.Collections.Generic;
using MelonLoader;
using NewSR2MP.Packet;
using UnityEngine;

namespace NewSR2MP.Component
{
    /// <summary>
    /// Компонент для автоматической синхронизации инвентаря клиента с хостом
    /// Периодически отправляет инвентарь хосту для сохранения
    /// </summary>
    [RegisterTypeInIl2Cpp(false)]
    public class ClientInventorySync : MonoBehaviour
    {
        private float timer = 0f;

        void Update()
        {
            // Работает только на клиенте (не на хосте)
            if (!ClientActive() || ServerActive())
                return;

            timer += Time.deltaTime;

            if (timer >= ClientInventoryTimer)
            {
                timer = 0f;
                SendInventoryToHost();
            }
        }

        /// <summary>
        /// Отправляет текущий инвентарь клиента хосту для сохранения
        /// </summary>
        private void SendInventoryToHost()
        {
            try
            {
                if (sceneContext == null || sceneContext.PlayerState == null || sceneContext.PlayerState.Ammo == null)
                    return;

                var clientAmmo = sceneContext.PlayerState.Ammo;
                var inventoryData = new List<AmmoData>();

                int itemCount = 0;
                for (int i = 0; i < clientAmmo.Slots.Count; i++)
                {
                    var slot = clientAmmo.Slots[i];
                    int identId = (slot._id == null) ? -1 : GetIdentID(slot._id);

                    inventoryData.Add(new AmmoData
                    {
                        slot = i,
                        id = identId,
                        count = slot._count
                    });

                    if (slot._count > 0)
                        itemCount++;
                }

                // Отправляем только если есть предметы или слоты изменились
                if (itemCount > 0 || HasInventoryChanged(inventoryData))
                {
                    var packet = new ClientInventorySyncPacket
                    {
                        inventory = inventoryData
                    };

                    MultiplayerManager.NetworkSend(packet);
                    SRMP.Debug($"Auto-synced client inventory: {itemCount} items");

                    // Сохраняем последнее состояние
                    lastInventoryState = inventoryData;
                }
            }
            catch (Exception ex)
            {
                SRMP.Debug($"Failed to auto-sync inventory: {ex.Message}");
            }
        }

        private List<AmmoData> lastInventoryState = null;

        /// <summary>
        /// Проверяет изменился ли инвентарь с последней отправки
        /// </summary>
        private bool HasInventoryChanged(List<AmmoData> currentInventory)
        {
            if (lastInventoryState == null || lastInventoryState.Count != currentInventory.Count)
                return true;

            for (int i = 0; i < currentInventory.Count; i++)
            {
                if (lastInventoryState[i].id != currentInventory[i].id ||
                    lastInventoryState[i].count != currentInventory[i].count)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

