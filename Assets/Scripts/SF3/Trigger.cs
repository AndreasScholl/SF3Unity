using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shiningforce
{
    public class Trigger
    {
        public enum TriggerType
        {
            MapTransition,
            Door
        }

        public enum TriggerDirection
        {
            Up,
            Down,
            Left,
            Right
        }

        public bool Active = true;

        public int TriggerId;
        public TriggerType Type;

        public string MapName;
        public byte[] MapPages;
        public int DestinationTriggerId;
        public TriggerDirection LookDirection;
        public float ViewAngle;
        public int Object1Id = 0;
        public int Object2Id = 0;

        // animation data
        public float MoveAngle;
        public bool AnimateMove;
        public GameObject Object1;
        public GameObject Object2;
        public GameObject[] Walls;
        public float Time = 0f;

        public void Init()
        {
            int interactionLayer = LayerMask.NameToLayer("Interaction");

            switch (Type)
            {
                case TriggerType.Door:
                    if (Object1Id > 0)
                    {
                        Object1 = MapData.Instance.GetObjectById(Object1Id);
                        if (Object1)
                        {
                            AddCollider(Object1, interactionLayer);
                        }
                    }
                    if (Object2Id > 0)
                    {
                        Object2 = MapData.Instance.GetObjectById(Object2Id);
                        if (Object2)
                        {
                            AddCollider(Object2, interactionLayer);
                        }
                    }

                    Walls = MapData.Instance.GetTriggerWalls(TriggerId);
                    break;
            }
        }

        private void AddCollider(GameObject obj, int layer)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                BoxCollider collider = renderer.gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                renderer.gameObject.layer = layer;
            }
        }

        public bool HasObject(GameObject obj)
        {
            if (Object1 == obj || Object2 == obj)
            {
                return true;
            }

            return false;
        }

        public bool AnimateDoor(float deltaTime)
        {
            const float AnimDuration = 1f;
            const float MaxAngle = 85f;

            bool finished = false;

            Time += deltaTime;

            if (Time >= AnimDuration)
            {
                finished = true;
                Time = AnimDuration;

                foreach (GameObject wall in Walls)
                {
                    wall.SetActive(false);
                }
            }

            float percentage = Time / AnimDuration;

            Object1.transform.GetChild(0).localEulerAngles = new Vector3(0f, -MaxAngle * percentage, 0f);

            if (Object2)
            {
                Object2.transform.GetChild(0).localEulerAngles = new Vector3(0f, MaxAngle * percentage, 0f);
            }   

            return finished;
        }
    }
}
