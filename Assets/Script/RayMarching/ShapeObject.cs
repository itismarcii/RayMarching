using UnityEngine;

namespace Script.RayMarching
{
    public class ShapeObject : MonoBehaviour
    {   
        [field: SerializeField] public ShapeType Shape { get; private set; }
        [field: SerializeField] public OperationType Operation { get; private set; }
        [field: SerializeField, Range(0, 1)] public float BlendStrength { get; private set; } = .5f;
        [field: SerializeField] public Color color = Color.white;

        [field: HideInInspector] public int childCount;
        private bool _IsDirty = true;
        private Vector3 _ParentScale;
        private bool _HasParent = true;
        
        public Vector3 Position => transform.position;
        public Vector3 Rotation => transform.rotation.eulerAngles;
        
        public Vector3 Scale
        {
            get
            {
                if (_IsDirty)
                {
                    _ParentScale = Vector3.one;
                    if ((transform.parent != null ? transform.parent.GetComponent<ShapeObject>() : null) != null)
                    {
                        _ParentScale = transform.parent.GetComponent<ShapeObject>().Scale;
                        _HasParent = true;
                    }

                    _IsDirty = false;
                }

                return Vector3.Scale(transform.localScale, _ParentScale);
            }

            set
            {
                if (_HasParent)
                {
                    if(transform.parent.GetComponent<ShapeObject>() == null)
                    {
                        _HasParent = false;
                    }
                    else
                    {
                        return;
                    }
                }
                
                _IsDirty = true;
                _ParentScale = value;
                transform.localScale = value;
            }
        }
    }
}
