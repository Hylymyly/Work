using UnityEditor;
using UnityEngine;

namespace TrafficSimulation {
    public static class EditorHelper {
        public static void SetUndoGroup(string label) {
            //Все изменения в Undo
            Undo.SetCurrentGroupName(label);
        }
        
        public static void BeginUndoGroup(string undoName, TrafficSystem trafficSystem) {
            Undo.SetCurrentGroupName(undoName);

            //Сохранение всех изменений
            Undo.RegisterFullObjectHierarchyUndo(trafficSystem.gameObject, undoName);
        }

        public static GameObject CreateGameObject(string name, Transform parent = null) {
            GameObject newGameObject = new GameObject(name);

            Undo.RegisterCreatedObjectUndo(newGameObject, "Spawn new GameObject");
            Undo.SetTransformParent(newGameObject.transform, parent, "Set parent");

            return newGameObject;
        }

        public static T AddComponent<T>(GameObject target) where T : Component {
            return Undo.AddComponent<T>(target);
        }
        
        //Если луч попал в сферу
        public static bool SphereHit(Vector3 center, float radius, Ray r) {
            Vector3 oc = r.origin - center;
            float a = Vector3.Dot(r.direction, r.direction);
            float b = 2f * Vector3.Dot(oc, r.direction);
            float c = Vector3.Dot(oc, oc) - radius * radius;
            float discriminant = b * b - 4f * a * c;

            if (discriminant < 0f) {
                return false;
            }

            float sqrt = Mathf.Sqrt(discriminant);

            return -b - sqrt > 0f || -b + sqrt > 0f;
        }

        //Добавить новый слой
        public static void CreateLayer(string name){
            if (string.IsNullOrEmpty(name))
                throw new System.ArgumentNullException("name", "New layer name string is either null or empty.");

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layerProps = tagManager.FindProperty("layers");
            var propCount = layerProps.arraySize;

            SerializedProperty firstEmptyProp = null;

            for (var i = 0; i < propCount; i++)
            {
                var layerProp = layerProps.GetArrayElementAtIndex(i);

                var stringValue = layerProp.stringValue;

                if (stringValue == name) return;

                if (i < 8 || stringValue != string.Empty) continue;

                if (firstEmptyProp == null)
                    firstEmptyProp = layerProp;
            }

            if (firstEmptyProp == null)
            {
                UnityEngine.Debug.LogError("Maximum limit of " + propCount + " layers exceeded. Layer \"" + name + "\" not created.");
                return;
            }

            firstEmptyProp.stringValue = name;
            tagManager.ApplyModifiedProperties();
        }

        //Изменить слой
        public static void SetLayer (this GameObject gameObject, int layer, bool includeChildren = false) {
            if (!includeChildren) {
                gameObject.layer = layer;
                return;
            }
        
            foreach (var child in gameObject.GetComponentsInChildren(typeof(Transform), true)) {
                child.gameObject.layer = layer;
            }
        }
    }
}
