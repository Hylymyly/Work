﻿using UnityEngine;

namespace TrafficSimulation {
    public class Waypoint : MonoBehaviour {
        [HideInInspector] public Segment segment;

        public void Refresh(int _newId, Segment _newSegment) {
            segment = _newSegment;
            name = "Waypoint-" + _newId;
            tag = "Waypoint";
           
            gameObject.layer = 0;
            
            RemoveCollider();
        }

        public void RemoveCollider() {
            if (GetComponent<SphereCollider>()) {
                DestroyImmediate(gameObject.GetComponent<SphereCollider>());
            }
        }

        public Vector3 GetVisualPos() {
            return transform.position + new Vector3(0, 0.5f, 0);
        }
    }
}
