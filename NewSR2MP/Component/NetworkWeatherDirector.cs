
using Il2CppMonomiPark.SlimeRancher.Weather;
using NewSR2MP.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewSR2MP.Component
{
    [RegisterTypeInIl2Cpp(false)]
    public class NetworkWeatherDirector : MonoBehaviour
    {
        WeatherRegistry dir;

        void Start()
        {
            dir = GetComponent<WeatherRegistry>();
        }

        public float timer = 0;
        
        void Update()
        {
            // Только хост отправляет погоду
            if (!ServerActive())
                return;
            
            timer += Time.unscaledDeltaTime;

            if (timer > WeatherTimer)
            {

                if (latestMessage == null)
                {
                    latestMessage = new WeatherSyncPacket(dir._model);
                    return;
                }

                if (latestMessage.timeStarted + WeatherSyncPacket.BUG_CHECK < Time.unscaledTime)
                {
                    latestMessage = null;
                    timer = 0;
                    return;
                }
                
                if (!latestMessage.initializedPacket)
                    return;

                MultiplayerManager.NetworkSend(latestMessage);

                latestMessage = null;
                timer = 0;
            }
        }

        private WeatherSyncPacket latestMessage;
    }
}
