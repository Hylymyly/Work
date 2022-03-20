using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TrafficSimulation {
    public static class TrafficSystemGizmos {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
        private static void DrawGizmo(TrafficSystem script, GizmoType gizmoType) {
            if (script.hideGuizmos) {
                return;
            }

            foreach (Segment segment in script.segments) {
                //Отрисовка имен сегментов
                GUIStyle style = new GUIStyle {normal = {textColor = new Color(1, 0, 0)}, fontSize = 15};
                Handles.Label(segment.transform.position, segment.name, style);

                //Отрисовка Точек
                for (int j = 0; j < segment.waypoints.Count; j++) {
                    //Позиция точек
                    Vector3 p = segment.waypoints[j].GetVisualPos();

                    Gizmos.color = new Color(0f, 0f, 1f, (j + 1) / (float) segment.waypoints.Count);
                    Gizmos.DrawSphere(p, script.waypointSize);
                    
                    //Следующая точка
                    Vector3 pNext = Vector3.zero;

                    if (j < segment.waypoints.Count - 1 && segment.waypoints[j + 1] != null) {
                        pNext = segment.waypoints[j + 1].GetVisualPos();
                    }

                    if (pNext != Vector3.zero) {
                        if (segment == script.curSegment) {
                            Gizmos.color = new Color(1f, .3f, .1f);
                        } else {
                            Gizmos.color = new Color(1f, 0f, 0f);
                        }

                        //Линия между точками
                        Gizmos.DrawLine(p, pNext);

                        //Количество стрелок
                        int arrows = GetArrowCount(p, pNext, script);

                        //Отрисовка стрелок
                        for (int i = 1; i < arrows + 1; i++) {
                            Vector3 point = Vector3.Lerp(p, pNext, (float) i / (arrows + 1));
                            DrawArrow(point, p - pNext, script.arrowSizeWaypoint);
                        }
                    }
                }

                //Линия между сегментами
                foreach (Segment nextSegment in segment.nextSegments) {
                    if (nextSegment != null){
                        Vector3 p1 = segment.waypoints.Last().GetVisualPos();
                        Vector3 p2 = nextSegment.waypoints.First().GetVisualPos();

                        Gizmos.color = new Color(1f, 1f, 0f);
                        Gizmos.DrawLine(p1, p2);

                        if (script.arrowDrawType != ArrowDraw.Off) {
                            DrawArrow((p1 + p2) / 2f, p1 - p2, script.arrowSizeIntersection);
                        }
                    }
                }
            }
        }

        private static void DrawArrow(Vector3 point, Vector3 forward, float size) {
            forward = forward.normalized * size;
            Vector3 left = Quaternion.Euler(0, 45, 0) * forward;
            Vector3 right = Quaternion.Euler(0, -45, 0) * forward;

            Gizmos.DrawLine(point, point + left);
            Gizmos.DrawLine(point, point + right);
        }

        private static int GetArrowCount(Vector3 pointA, Vector3 pointB, TrafficSystem script) {
            switch (script.arrowDrawType) {
                case ArrowDraw.FixedCount:
                    return script.arrowCount;
                case ArrowDraw.ByLength:
                    return Mathf.Max(1, (int) (Vector3.Distance(pointA, pointB) / script.arrowDistance));
                case ArrowDraw.Off:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
