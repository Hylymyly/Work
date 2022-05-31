using UnityEngine;
using System;

namespace TrafficSimulation{
    [Serializable]
    public enum DriveType{
        RearWheelDrive,
        FrontWheelDrive,
        AllWheelDrive
    }

    [Serializable]
    public enum UnitType{
        KMH,
        MPH
    }

    public class WheelDrive : MonoBehaviour
    {
        [Tooltip("Притяжение")]
        public float downForce = 100f;

        [Tooltip("Максимальный угол поворота колес")]
        public float maxAngle = 30f;

        [Tooltip("Скорость при повороте")]
        public float steeringLerp = 5f;
        
        [Tooltip("Максимальная скорость при повороте")]
        public float steeringSpeedMax = 20f;

        [Tooltip("Крутящий момент")]
        public float maxTorque = 300f;

        [Tooltip("ТОрмозной момент")]
        public float brakeTorque = 30000f;

        [Tooltip("Unit Type")]
        public UnitType unitType;

        [Tooltip("Минимальная скорость при движении")]
        public float minSpeed = 5;

        [Tooltip("Максимальная скорость")]
        public float maxSpeed = 50;

        [Tooltip("Колеса")]
        public GameObject leftWheelShape;
        public GameObject rightWheelShape;

        [Tooltip("Анимирование")]
        public bool animateWheels = true;
        
        [Tooltip("Прив")]
        public DriveType driveType;

        private WheelCollider[] wheels;
        private float currentSteering = 0f;

        void OnEnable(){
            wheels = GetComponentsInChildren<WheelCollider>();

            for (int i = 0; i < wheels.Length; ++i) 
            {
                var wheel = wheels [i];

                // Create wheel shapes only when needed.
                if (leftWheelShape != null && wheel.transform.localPosition.x < 0)
                {
                    var ws = Instantiate (leftWheelShape);
                    ws.transform.parent = wheel.transform;
                }
                else if(rightWheelShape != null && wheel.transform.localPosition.x > 0){
                    var ws = Instantiate(rightWheelShape);
                    ws.transform.parent = wheel.transform;
                }

                wheel.ConfigureVehicleSubsteps(10, 1, 1);
            }
        }

        public void Move(float _acceleration, float _steering, float _brake)
        {

            float nSteering = Mathf.Lerp(currentSteering, _steering, Time.deltaTime * steeringLerp);
            currentSteering = nSteering;

            Rigidbody rb = this.GetComponent<Rigidbody>();

            float angle = maxAngle * nSteering;
            float torque = maxTorque * _acceleration;

            float handBrake = _brake > 0 ? brakeTorque : 0;

            foreach (WheelCollider wheel in wheels){
                // Steer front wheels only
                if (wheel.transform.localPosition.z > 0) wheel.steerAngle = angle;

                if (wheel.transform.localPosition.z < 0) wheel.brakeTorque = handBrake;

                if (wheel.transform.localPosition.z < 0 && driveType != DriveType.FrontWheelDrive) wheel.motorTorque = torque;

                if (wheel.transform.localPosition.z >= 0 && driveType != DriveType.RearWheelDrive) wheel.motorTorque = torque;

                // Update visual wheels if allowed
                if(animateWheels){
                    Quaternion q;
                    Vector3 p;
                    wheel.GetWorldPose(out p, out q);

                    Transform shapeTransform = wheel.transform.GetChild (0);
                    shapeTransform.position = p;
                    shapeTransform.rotation = q;
                }
            }


            //Apply speed
            float s = GetSpeedUnit(rb.velocity.magnitude);
            if(s > maxSpeed) rb.velocity = GetSpeedMS(maxSpeed) * rb.velocity.normalized;

            
            //Apply downforce
            rb.AddForce(-transform.up * downForce * rb.velocity.magnitude);
        }

        public float GetSpeedMS(float _s){
            return unitType == UnitType.KMH ? _s / 3.6f : _s / 2.237f;
        }

        public float GetSpeedUnit(float _s){
            return unitType == UnitType.KMH ? _s * 3.6f : _s * 2.237f;
        }
    }   
}
