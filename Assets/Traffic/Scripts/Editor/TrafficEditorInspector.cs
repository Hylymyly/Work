﻿using System;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation {
    public static class TrafficEditorInspector {
        public static void DrawInspector(TrafficSystem trafficSystem, SerializedObject serializedObject, out bool restructureSystem) {

            Header("Gizmo Config");
            Toggle("Hide Gizmos", ref trafficSystem.hideGuizmos);
            
            //Отрисовка стрелок
            DrawArrowTypeSelection(trafficSystem);
            FloatField("Waypoint Size", ref trafficSystem.waypointSize);
            EditorGUILayout.Space();
            
            Header("System Config");
            FloatField("Segment Detection Threshold", ref trafficSystem.segDetectThresh);

            PropertyField("Collision Layers", "collisionLayers", serializedObject);
            
            EditorGUILayout.Space();

            restructureSystem = Button("Re-Structure Traffic System");
        }

        private static void Label(string label) {
            EditorGUILayout.LabelField(label);
        }

        private static void Header(string label) {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static void Toggle(string label, ref bool toggle) {
            toggle = EditorGUILayout.Toggle(label, toggle);
        }

        private static void IntField(string label, ref int value) {
            value = EditorGUILayout.IntField(label, value);
        }
        
        private static void IntField(string label, ref int value, int min, int max) {
            value = Mathf.Clamp(EditorGUILayout.IntField(label, value), min, max);
        }
        
        private static void FloatField(string label, ref float value) {
            value = EditorGUILayout.FloatField(label, value);
        }

        private static void PropertyField(string label, string value, SerializedObject serializedObject){
            SerializedProperty extra = serializedObject.FindProperty(value);
            EditorGUILayout.PropertyField(extra, new GUIContent(label), true);
        }

        private static void HelpBox(string content) {
            EditorGUILayout.HelpBox(content, MessageType.Info);
        }

        private static bool Button(string label) {
            return GUILayout.Button(label);
        }
        
        private static void DrawArrowTypeSelection(TrafficSystem trafficSystem) {
            trafficSystem.arrowDrawType = (ArrowDraw) EditorGUILayout.EnumPopup("Arrow Draw Type", trafficSystem.arrowDrawType);
            EditorGUI.indentLevel++;
            
            switch (trafficSystem.arrowDrawType) {
                case ArrowDraw.FixedCount:
                    IntField("Count", ref trafficSystem.arrowCount, 1, int.MaxValue);
                    break;
                case ArrowDraw.ByLength:
                    FloatField("Distance Between Arrows", ref trafficSystem.arrowDistance);
                    break;
                case ArrowDraw.Off:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if (trafficSystem.arrowDrawType != ArrowDraw.Off) {
                FloatField("Arrow Size Waypoint", ref trafficSystem.arrowSizeWaypoint);
                FloatField("Arrow Size Intersection", ref trafficSystem.arrowSizeIntersection);
            }
            
            EditorGUI.indentLevel--;
        }
    }
}
