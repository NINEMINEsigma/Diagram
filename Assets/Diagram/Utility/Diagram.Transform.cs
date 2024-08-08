using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Diagram
{
    public class TransformHelper
    {
        private class EmptyMono : MonoBehaviour { }

        private Transform transform;
        public TransformHelper(Transform transform)
        {
            this.transform = transform;
        }

        public Vector3 Position
        { 
            get { return transform.position; }
            set
            {
                if (m_RelativeLockTarget) return;
                transform.position = value;
            }
        }
        public Quaternion Rotation
        {
            get { return transform.rotation; }
            set
            {
                if (m_LookAtTarget) return;
                transform.rotation = value;
            }
        } 
        public Vector3 EulerAngles
        {
            get { return transform.eulerAngles; }
            set
            {
                if (m_LookAtTarget) return;
                transform.eulerAngles = value;
            }
        }
        public Vector3 Scale
        {
            get { return transform.localScale; }
            set { transform.localScale = value; }
        }

        private static IEnumerator FocusTransformTo(Transform from,Transform to,TransformHelper helper)
        {
            while (helper.transform != null && helper.LookAtTarget != null&& helper.transform == from && helper.LookAtTarget == to)
            {
                helper.transform.LookAt(helper.LookAtTarget);
                yield return null;
            }
        }
        private Transform m_LookAtTarget;
        public Transform LookAtTarget
        {
            get => m_LookAtTarget;
            set
            {
                if (value == m_LookAtTarget) return;
                if (value != null)
                {
                    var cat = transform.SeekComponent<MonoBehaviour>();
                    if (cat == null)
                        cat = transform.gameObject.GetOrAddComponent<EmptyMono>();
                    cat.StartCoroutine(FocusTransformTo(transform, value, this));
                }
                m_LookAtTarget = value;
            }
        }

        private static IEnumerator FocusRelativePositionAt(Transform from, Transform to, TransformHelper helper)
        {
            Vector3 dir = from.position - to.position;
            while (helper.transform != null && helper.LookAtTarget != null && helper.transform == from && helper.LookAtTarget == to)
            {
                helper.transform.position = helper.transform.position + dir;
                yield return null;
            }
        }
        private Transform m_RelativeLockTarget;
        public Transform RelativeLockTarget
        {
            get => m_RelativeLockTarget;
            set
            {
                if (value == m_RelativeLockTarget) return;
                if (value != null)
                {
                    var cat = transform.SeekComponent<MonoBehaviour>();
                    if (cat == null)
                        cat = transform.gameObject.GetOrAddComponent<EmptyMono>();
                    cat.StartCoroutine(FocusRelativePositionAt(transform, value, this));
                }
                m_RelativeLockTarget = value;
            }
        }

    }
}
