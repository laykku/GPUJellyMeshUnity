using UnityEngine;
using UnityEngine.Rendering;

namespace GPUJellyMesh
{
    public class MeshDeform : MonoBehaviour
    {
        private ComputeShader _deformer;
        private GraphicsBuffer _vertexBuffer;
        private ComputeBuffer _originalBuffer;
        private ComputeBuffer _displacedBuffer;
        private ComputeBuffer _velocitiesBuffer;
        private GraphicsBuffer _indexBuffer;

        private int _deformKernel;
        private int _recalcNormalsKernel;

        private const int Threads = 1;

        private void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;

            _deformer = Instantiate(Resources.Load<ComputeShader>("MeshDeform"));

            _deformKernel = _deformer.FindKernel("UpdateMesh");
            _recalcNormalsKernel = _deformer.FindKernel("RecalculateNormals");

            int vertexCount = mesh.vertexCount;
            int indexCount = (int)mesh.GetIndexCount(0);

            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;

            _vertexBuffer = mesh.GetVertexBuffer(0);
            _deformer.SetBuffer(_deformKernel, "vertices", _vertexBuffer);
            _deformer.SetBuffer(_recalcNormalsKernel, "vertices", _vertexBuffer);

            _indexBuffer = mesh.GetIndexBuffer();
            _deformer.SetBuffer(_deformKernel, "indices", _indexBuffer);
            _deformer.SetBuffer(_recalcNormalsKernel, "indices", _indexBuffer);

            _originalBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3); // ComputeBufferType.Raw
            _deformer.SetBuffer(_deformKernel, "originalVertices", _originalBuffer);

            _displacedBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3); // ComputeBufferType.Raw
            _deformer.SetBuffer(_deformKernel, "displacedVertices", _displacedBuffer);

            _velocitiesBuffer = new ComputeBuffer(vertexCount, sizeof(float) * 3);
            _deformer.SetBuffer(_deformKernel, "velocities", _velocitiesBuffer);

            int initKernel = _deformer.FindKernel("InitMeshData");

            _deformer.SetBuffer(initKernel, "vertices", _vertexBuffer);
            _deformer.SetBuffer(initKernel, "originalVertices", _originalBuffer);
            _deformer.SetBuffer(initKernel, "displacedVertices", _displacedBuffer);
            _deformer.SetBuffer(initKernel, "velocities", _velocitiesBuffer);

            _deformer.SetInt("vertexCount", vertexCount);
            _deformer.SetInt("indexCount", indexCount);
            _deformer.SetInt("positionAttributeOffset", mesh.GetVertexAttributeOffset(VertexAttribute.Position));
            _deformer.SetInt("normalAttributeOffset", mesh.GetVertexAttributeOffset(VertexAttribute.Normal));
            _deformer.SetInt("vertexDataStride", mesh.GetVertexBufferStride(0));

            _deformer.SetFloat("uniformScale", transform.localScale.x);
            _deformer.SetFloat("springForce", 20f);
            _deformer.SetFloat("damping", 5f);

            _deformer.Dispatch(initKernel, Threads, 1, 1);
        }

        private void OnDestroy()
        {
            _vertexBuffer.Dispose();
            _originalBuffer.Dispose();
            _displacedBuffer.Dispose();
            _velocitiesBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        private void Update()
        {
            _deformer.SetMatrix("inverseTransform", transform.worldToLocalMatrix);
            _deformer.SetFloat("deltaTime", Time.deltaTime);
            _deformer.SetVector("forcePoint", Vector3.zero);
            _deformer.SetFloat("force", 0f);

            if (Input.GetMouseButton(0))
            {
                RaycastHit hit;

                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    MeshDeform deformer = hit.collider.GetComponent<MeshDeform>();
                    if (deformer)
                    {
                        Vector3 point = hit.point;
                        point += hit.normal * 0.1f;
                        _deformer.SetVector("forcePoint", point);
                        _deformer.SetFloat("force", 10f);
                    }
                }
            }

            _deformer.Dispatch(_deformKernel, 1, 1, 1);
            _deformer.Dispatch(_recalcNormalsKernel, 1, 1, 1);
        }
    }
}