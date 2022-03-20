using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSimulation {

    /*
        Торможение и интерполяция поворота
    */

    public struct Target{
        public int segment;
        public int waypoint;
    }

    public enum Status{
        GO,
        STOP,
        SLOW_DOWN
    }

    public class VehicleAI : MonoBehaviour
    {
        [Header("Traffic System")]
        [Tooltip("Current active traffic system")]
        public TrafficSystem trafficSystem;

        [Tooltip("Determine when the vehicle has reached its target. Can be used to \"anticipate\" earlier the next waypoint (the higher this number his, the earlier it will anticipate the next waypoint)")]
        public float waypointThresh = 6;


        [Header("Radar")]

        [Tooltip("Empty gameobject from where the rays will be casted")]
        public Transform raycastAnchor;

        [Tooltip("Length of the casted rays")]
        public float raycastLength = 5;

        [Tooltip("Spacing between each rays")]
        public int raySpacing = 2;

        [Tooltip("Number of rays to be casted")]
        public int raysNumber = 6;

        [Tooltip("If detected vehicle is below this distance, ego vehicle will stop")]
        public float emergencyBrakeThresh = 2f;

        [Tooltip("If detected vehicle is below this distance (and above, above distance), ego vehicle will slow down")]
        public float slowDownThresh = 4f;

        [HideInInspector] public Status vehicleStatus = Status.GO;

        private WheelDrive wheelDrive;
        private float initMaxSpeed = 0;
        private int pastTargetSegment = -1;
        private Target currentTarget;
        private Target futureTarget;

        void Start()
        {
            wheelDrive = this.GetComponent<WheelDrive>();

            if(trafficSystem == null)
                return;

            initMaxSpeed = wheelDrive.maxSpeed;
            SetWaypointVehicleIsOn();
        }

        void Update(){
            if(trafficSystem == null)
                return;

            WaypointChecker();
            MoveVehicle();
        }


        void WaypointChecker(){
            GameObject waypoint = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].gameObject;

            //Глобальная позиция точки с машиной
            Vector3 wpDist = this.transform.InverseTransformPoint(new Vector3(waypoint.transform.position.x, this.transform.position.y, waypoint.transform.position.z));

            //Переход к селд точке
            if(wpDist.magnitude < waypointThresh){
                currentTarget.waypoint++;
                if(currentTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                    pastTargetSegment = currentTarget.segment;
                    currentTarget.segment = futureTarget.segment;
                    currentTarget.waypoint = 0;
                }

                futureTarget.waypoint = currentTarget.waypoint + 1;
                if(futureTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                    futureTarget.waypoint = 0;
                    futureTarget.segment = GetNextSegmentId();
                }
            }
        }

        void MoveVehicle(){

            float acc = 1;
            float brake = 0;
            float steering = 0;
            wheelDrive.maxSpeed = initMaxSpeed;

            //поиск поворота
            Transform targetTransform = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform;
            Transform futureTargetTransform = trafficSystem.segments[futureTarget.segment].waypoints[futureTarget.waypoint].transform;
            Vector3 futureVel = futureTargetTransform.position - targetTransform.position;
            float futureSteering = Mathf.Clamp(this.transform.InverseTransformDirection(futureVel.normalized).x, -1, 1);

            //Проверка остановки машины на стопе
            if(vehicleStatus == Status.STOP){
                acc = 0;
                brake = 1;
                wheelDrive.maxSpeed = Mathf.Min(wheelDrive.maxSpeed / 2f, 5f);
            }
            else{
                
                //Уменьшитб ускорение
                if(vehicleStatus == Status.SLOW_DOWN){
                    acc = .3f;
                    brake = 0f;
                }

                if(futureSteering > .3f || futureSteering < -.3f){
                    wheelDrive.maxSpeed = Mathf.Min(wheelDrive.maxSpeed, wheelDrive.steeringSpeedMax);
                }

                //Препятствия
                float hitDist;
                GameObject obstacle = GetDetectedObstacles(out hitDist);

                //Проверка удара
                if(obstacle != null){

                    WheelDrive otherVehicle = null;
                    otherVehicle = obstacle.GetComponent<WheelDrive>();

                   
                    if(otherVehicle != null){
                        //есть ли машина спереди
                        float dotFront = Vector3.Dot(this.transform.forward, otherVehicle.transform.forward);

                        //Уменьшить скорость если машина спереди тормозит
                        if(otherVehicle.maxSpeed < wheelDrive.maxSpeed && dotFront > .8f){
                            float ms = Mathf.Max(wheelDrive.GetSpeedMS(otherVehicle.maxSpeed) - .5f, .1f);
                            wheelDrive.maxSpeed = wheelDrive.GetSpeedUnit(ms);
                        }
                        
                        //Расстояние между машинами слишком близко
                        if(hitDist < emergencyBrakeThresh && dotFront > .8f){
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        //Сдать назад
                        else if(hitDist < emergencyBrakeThresh && dotFront <= .8f){
                            acc = -.3f;
                            brake = 0f;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);

                            //Чек стороны машины
                            float dotRight = Vector3.Dot(this.transform.forward, otherVehicle.transform.right);
                            //Право
                            if(dotRight > 0.1f) steering = -.3f;
                            //Лево
                            else if(dotRight < -0.1f) steering = .3f;
                            else steering = -.7f;
                        }

                        //Замедлить, если машины близко
                        else if(hitDist < slowDownThresh){
                            acc = .5f;
                            brake = 0f;
                        }
                    }


                    else{
                        //Если слишком близко - 0
                        if(hitDist < emergencyBrakeThresh){
                            acc = 0;
                            brake = 1;
                            wheelDrive.maxSpeed = Mathf.Max(wheelDrive.maxSpeed / 2f, wheelDrive.minSpeed);
                        }

                        //Либо уменьшить скорость
                         else if(hitDist < slowDownThresh){
                            acc = .5f;
                            brake = 0f;
                        }
                    }
                }

                //Движение по пути
                if(acc > 0f){
                    Vector3 desiredVel = trafficSystem.segments[currentTarget.segment].waypoints[currentTarget.waypoint].transform.position - this.transform.position;
                    steering = Mathf.Clamp(this.transform.InverseTransformDirection(desiredVel.normalized).x, -1f, 1f);
                }

            }

            //Едем
            wheelDrive.Move(acc, steering, brake);
        }


        GameObject GetDetectedObstacles(out float _hitDist){
            GameObject detectedObstacle = null;
            float minDist = 1000f;

            float initRay = (raysNumber / 2f) * raySpacing;
            float hitDist =  -1f;
            for(float a=-initRay; a<=initRay; a+=raySpacing){
                CastRay(raycastAnchor.transform.position, a, this.transform.forward, raycastLength, out detectedObstacle, out hitDist);

                if(detectedObstacle == null) continue;

                float dist = Vector3.Distance(this.transform.position, detectedObstacle.transform.position);
                if(dist < minDist) {
                    minDist = dist;
                    break;
                }
            }

            _hitDist = hitDist;
            return detectedObstacle;
        }

        
        void CastRay(Vector3 _anchor, float _angle, Vector3 _dir, float _length, out GameObject _outObstacle, out float _outHitDistance){
            _outObstacle = null;
            _outHitDistance = -1f;

            //Отрисовка лучей
            Debug.DrawRay(_anchor, Quaternion.Euler(0, _angle, 0) * _dir * _length, new Color(1, 0, 0, 0.5f));

            //Луч на машине
            int layer = 1 << LayerMask.NameToLayer("AutonomousVehicle");
            int finalMask = layer;

            foreach(string layerName in trafficSystem.collisionLayers){
                int id = 1 << LayerMask.NameToLayer(layerName);
                finalMask = finalMask | id;
            }

            RaycastHit hit;
            if(Physics.Raycast(_anchor, Quaternion.Euler(0, _angle, 0) * _dir, out hit, _length, finalMask)){
                _outObstacle = hit.collider.gameObject;
                _outHitDistance = hit.distance;
            }
        }

        int GetNextSegmentId(){
            if(trafficSystem.segments[currentTarget.segment].nextSegments.Count == 0)
                return 0;
            int c = Random.Range(0, trafficSystem.segments[currentTarget.segment].nextSegments.Count);
            return trafficSystem.segments[currentTarget.segment].nextSegments[c].id;
        }

        void SetWaypointVehicleIsOn(){
            foreach(Segment segment in trafficSystem.segments){
                if(segment.IsOnSegment(this.transform.position)){
                    currentTarget.segment = segment.id;

                    //Близжайшая точка в пределах сегмента
                    float minDist = float.MaxValue;
                    for(int j=0; j<trafficSystem.segments[currentTarget.segment].waypoints.Count; j++){
                        float d = Vector3.Distance(this.transform.position, trafficSystem.segments[currentTarget.segment].waypoints[j].transform.position);

                        //Передние точки
                        Vector3 lSpace = this.transform.InverseTransformPoint(trafficSystem.segments[currentTarget.segment].waypoints[j].transform.position);
                        if(d < minDist && lSpace.z > 0){
                            minDist = d;
                            currentTarget.waypoint = j;
                        }
                    }
                    break;
                }
            }

            futureTarget.waypoint = currentTarget.waypoint + 1;
            futureTarget.segment = currentTarget.segment;

            if(futureTarget.waypoint >= trafficSystem.segments[currentTarget.segment].waypoints.Count){
                futureTarget.waypoint = 0;
                futureTarget.segment = GetNextSegmentId();
            }
        }

        public int GetSegmentVehicleIsIn(){
            int vehicleSegment = currentTarget.segment;
            bool isOnSegment = trafficSystem.segments[vehicleSegment].IsOnSegment(this.transform.position);
            if(!isOnSegment){
                bool isOnPSegement = trafficSystem.segments[pastTargetSegment].IsOnSegment(this.transform.position);
                if(isOnPSegement)
                    vehicleSegment = pastTargetSegment;
            }
            return vehicleSegment;
        }
    }
}