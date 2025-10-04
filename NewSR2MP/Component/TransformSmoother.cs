using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewSR2MP.Component
{
    [RegisterTypeInIl2Cpp(false)]
    public class TransformSmoother : MonoBehaviour
    {
        public void SetRigidbodyState(bool enabled)
        {
            if (GetComponent<Rigidbody>() != null)
                GetComponent<Rigidbody>().constraints =
                    enabled 
                        ? RigidbodyConstraints.None 
                        : RigidbodyConstraints.FreezeAll;
        }

        
        void Start()
        {
            if (GetComponent<NetworkPlayer>() != null)
            {
                thisPlayer = GetComponent<NetworkPlayer>();
            }
            if (GetComponent<NetworkActor>() != null)
            {
                thisActor = GetComponent<NetworkActor>();
            }
            
            // Инициализируем последнюю позицию для расчета скорости
            lastReceivedPos = transform.position;
            lastReceivedTime = Time.time;
            receivedVelocity = Vector3.zero;
        }

        public NetworkPlayer thisPlayer;
        public NetworkActor thisActor;
        
        /// <summary>
        /// Next rotation. The future rotation, this is the rotation the transform is smoothing to.
        /// </summary>
        public Vector3 nextRot;

        /// <summary>
        /// Next position. The future position, this is the position the transform is smoothing to.
        /// </summary>
        public Vector3 nextPos;

        /// <summary>
        /// Interpolation Period. the speed at which the transform is smoothed.
        /// </summary>
        public float interpolPeriod = PlayerTimer;

        // ===== НОВАЯ СИСТЕМА ЭКСТРАПОЛЯЦИИ =====
        
        /// <summary>
        /// Последняя полученная позиция из сети
        /// </summary>
        private Vector3 lastReceivedPos;
        
        /// <summary>
        /// Время получения последней позиции
        /// </summary>
        private float lastReceivedTime;
        
        /// <summary>
        /// Скорость объекта, полученная из сети
        /// </summary>
        private Vector3 receivedVelocity;
        
        /// <summary>
        /// Использовать ли физическую экстраполяцию (с гравитацией)
        /// </summary>
        private bool usePhysicsExtrapolation = true;
        
        /// <summary>
        /// Сила гравитации для экстраполяции
        /// </summary>
        private Vector3 gravity = new Vector3(0, -9.81f, 0);
        
        /// <summary>
        /// Максимальное расстояние экстраполяции (защита от ошибок)
        /// </summary>
        private const float MAX_EXTRAPOLATION_DISTANCE = 50f;
        
        /// <summary>
        /// Коэффициент сглаживания для экстраполяции
        /// </summary>
        private float extrapolationSmoothing = 0.15f;

        public Vector3 currPos => transform.position;
        private float positionTime;

        public Vector3 currRot => transform.eulerAngles;

        private uint frame;
        
        /// <summary>
        /// Устанавливает новую целевую позицию и скорость из сетевого пакета
        /// </summary>
        public void SetNetworkTarget(Vector3 position, Vector3 rotation, Vector3 velocity)
        {
            // Вычисляем время с последнего обновления
            float deltaTime = Time.time - lastReceivedTime;
            
            // Сохраняем новые данные
            lastReceivedPos = position;
            lastReceivedTime = Time.time;
            receivedVelocity = velocity;
            
            // Обновляем целевые значения
            nextPos = position;
            nextRot = rotation;
            
            // Определяем нужна ли физическая экстраполяция
            // Если скорость больше порога - используем физику
            usePhysicsExtrapolation = velocity.sqrMagnitude > 0.1f;
        }
        
        public void Update()
        {
            if (thisActor) SetRigidbodyState(thisActor.IsOwned);
            
            if (thisActor && thisActor.IsOwned) return;

            // ===== НОВАЯ ЛОГИКА ИНТЕРПОЛЯЦИИ С ЭКСТРАПОЛЯЦИЕЙ =====
            
            float timeSinceLastUpdate = Time.time - lastReceivedTime;
            
            // ЭКСТРАПОЛЯЦИЯ: Предсказываем позицию на основе скорости и физики
            Vector3 extrapolatedPos = nextPos;
            
            if (usePhysicsExtrapolation && receivedVelocity.sqrMagnitude > 0.01f)
            {
                // Проверяем есть ли Rigidbody с гравитацией
                bool hasGravity = false;
                if (TryGetComponent<Rigidbody>(out var rb))
                {
                    hasGravity = rb.useGravity;
                }
                
                // Физическая экстраполяция: позиция = начальная + скорость*время + 0.5*гравитация*время^2
                extrapolatedPos = lastReceivedPos + receivedVelocity * timeSinceLastUpdate;
                
                if (hasGravity)
                {
                    // Добавляем гравитацию к экстраполяции
                    extrapolatedPos += 0.5f * gravity * timeSinceLastUpdate * timeSinceLastUpdate;
                }
                
                // Ограничиваем экстраполяцию чтобы объекты не улетали слишком далеко
                float extrapolationDistance = Vector3.Distance(lastReceivedPos, extrapolatedPos);
                if (extrapolationDistance > MAX_EXTRAPOLATION_DISTANCE)
                {
                    // Если экстраполяция завела слишком далеко, используем последнюю известную позицию
                    extrapolatedPos = lastReceivedPos;
                }
            }
            
            // ИНТЕРПОЛЯЦИЯ: Плавно двигаемся к экстраполированной позиции
            float t = 1.0f - ((positionTime - Time.unscaledTime) / interpolPeriod);
            t = Mathf.Clamp01(t);
            
            // Используем более плавную интерполяцию (SmoothDamp style)
            Vector3 targetPosition = Vector3.Lerp(nextPos, extrapolatedPos, extrapolationSmoothing);
            
            // Плавное движение к целевой позиции
            transform.position = Vector3.Lerp(currPos, targetPosition, t);
            
            // Интерполяция вращения
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(currRot), Quaternion.Euler(nextRot), t);

            positionTime = Time.unscaledTime + interpolPeriod;
        }

        void OnDisable()
        {
            if (TryGetComponent<Rigidbody>(out var rb))
                rb.velocity = GetComponent<NetworkActorOwnerToggle>().savedVelocity;
        }
    }
}
