using UnityEngine;
using UnityEditor;

namespace TrafficSimulation{
    public class VehicleEditor : Editor
    {
        [MenuItem("Component/Traffic Simulation/Setup Vehicle")]
        private static void SetupVehicle(){
            EditorHelper.SetUndoGroup("Setup Vehicle");

            GameObject selected = Selection.activeGameObject;

            //Привязать лучи
            GameObject anchor = EditorHelper.CreateGameObject("Raycast Anchor", selected.transform);

            //Добавить скрипты движения
            VehicleAI veAi = EditorHelper.AddComponent<VehicleAI>(selected);
            WheelDrive wheelDrive = EditorHelper.AddComponent<WheelDrive>(selected);

            TrafficSystem ts = GameObject.FindObjectOfType<TrafficSystem>();

            //Настрйока луча
            anchor.transform.localPosition = Vector3.zero;
            anchor.transform.localRotation = Quaternion.Euler(Vector3.zero);
            veAi.raycastAnchor = anchor.transform;

            if(ts != null) veAi.trafficSystem = ts;

            //тэг
            EditorHelper.CreateLayer("AutonomousVehicle");
           
            selected.tag = "AutonomousVehicle";
            EditorHelper.SetLayer(selected, LayerMask.NameToLayer("AutonomousVehicle"), true);
        }
    }
}