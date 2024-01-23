using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.RayMarching
{
    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    public class RayMarchingManager : MonoBehaviour
    {
        [Serializable]
        public struct ContainerExtra
        {
            public Camera Camera;
            public Light LightSource;
        }

        [field: SerializeField] public ContainerExtra Container;
        [field: SerializeField] public ComputeShader RayMarchingShader { get; private set; }

        private List<ComputeBuffer> _BuffersToDispose = new List<ComputeBuffer>();
        private RenderTexture _RenderTexture;
        private int _ThreadGroupX, _ThreadGroupY;

        private static readonly int
            SourceID = Shader.PropertyToID("Source"),
            DestinationID = Shader.PropertyToID("Destination"),
            ShapesID = Shader.PropertyToID("Shapes"),
            NumberShapesID = Shader.PropertyToID("numberShapes"),
            CameraToWorldID = Shader.PropertyToID("_CameraToWorld"),
            CameraInverseProjectionID = Shader.PropertyToID("_CameraInverseProjection"),
            LightID = Shader.PropertyToID("_Light"),
            PositionLightID = Shader.PropertyToID("positionLight");

        private void Init()
        {
            _ThreadGroupX = Mathf.CeilToInt(Container.Camera.pixelWidth / 8f);
            _ThreadGroupY = Mathf.CeilToInt(Container.Camera.pixelHeight / 8f);
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Container.Camera = Camera.current;
            Container.LightSource = FindObjectOfType<Light>();

            if (!RayMarchingShader) return;

            _BuffersToDispose.Clear();

            InitRenderTexture();
            CreateScene();
            SetParameters();

            Init();

            RayMarchingShader.SetTexture(0, SourceID, source);
            RayMarchingShader.SetTexture(0, DestinationID, _RenderTexture);
            RayMarchingShader.Dispatch(0, _ThreadGroupX, _ThreadGroupY, 1);
            
            Graphics.Blit(_RenderTexture, destination);

            foreach (var buffer in _BuffersToDispose)
            {
                buffer.Dispose();
            }
        }

        private void CreateScene()
        {
            var allShapes = new List<ShapeObject>(FindObjectsOfType<ShapeObject>());
            allShapes.Sort((a, b) => a.Operation.CompareTo(b.Operation));

            var orderedShapes = new List<ShapeObject>();

            foreach (var shape in allShapes)
            {                
                orderedShapes.Add(shape);
                AddChildShape(ref orderedShapes, shape);
            }
            
            var shapeData = new ShapeData[orderedShapes.Count];

            for (var i = 0; i < orderedShapes.Count; i++)
            {
                var shape = orderedShapes[i];
                var colour = new Vector3(shape.color.r, shape.color.g, shape.color.b);

                shapeData[i] = new ShapeData()
                {
                    position = shape.Position,
                    rotation = shape.Rotation,
                    size = shape.Scale,
                    colour = colour,
                    shapeType = (int) shape.Shape,
                    operation = (int) shape.Operation,
                    blendStrength = shape.BlendStrength * 3,
                    numChildren = shape.childCount
                };
            }

            var shapeBuffer = new ComputeBuffer(shapeData.Length, ShapeData.GetSize());
            shapeBuffer.SetData(shapeData);
            RayMarchingShader.SetBuffer(0, ShapesID, shapeBuffer);
            RayMarchingShader.SetInt(NumberShapesID, shapeData.Length);

            _BuffersToDispose.Add(shapeBuffer);
            return;

            void AddChildShape(ref List<ShapeObject> shapeObjects, in ShapeObject parentShape)
            {
                var parent = parentShape.transform;
                parentShape.childCount = parent.childCount;

                if(parentShape.transform.childCount <= 0) return;
                
                for (var i = 0; i < parentShape.childCount; i++)
                {                
                    if (parent.GetChild(i).GetComponent<ShapeObject>() == null) continue;
                    
                    var childShape = parent.GetChild(i).GetComponent<ShapeObject>();
                    shapeObjects.Add(childShape);

                    AddChildShape(ref shapeObjects, childShape);
                }
            }
        }

        private void SetParameters()
        {
            var cam = Container.Camera;
            var lightSource = Container.LightSource;
            var lightIsDirectional = lightSource.type == LightType.Directional;

            RayMarchingShader.SetMatrix(CameraToWorldID, cam.cameraToWorldMatrix);
            RayMarchingShader.SetMatrix(CameraInverseProjectionID, cam.projectionMatrix.inverse);
            RayMarchingShader.SetVector(LightID,
                (lightIsDirectional) ? cam.transform.forward : lightSource.transform.position);
            RayMarchingShader.SetBool(PositionLightID, !lightIsDirectional);
        }

        private void InitRenderTexture()
        {
            var cam = Container.Camera;

            if (_RenderTexture != null && _RenderTexture.width == cam.pixelWidth &&
                _RenderTexture.height == cam.pixelHeight) return;

            if (_RenderTexture != null) _RenderTexture.Release();

            _RenderTexture = new RenderTexture
                (cam.pixelWidth, cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                {
                    enableRandomWrite = true
                };

            _RenderTexture.Create();
        }
    }
}
