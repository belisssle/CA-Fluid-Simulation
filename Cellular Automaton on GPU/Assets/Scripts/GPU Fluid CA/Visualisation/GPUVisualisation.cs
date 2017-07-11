﻿using UnityEngine;

namespace GPUFluid
{
    /// <summary>
    /// This class is the superclass for the visualisations of a cellular automaton, that runs on the GPU and is therefore stored in a ComputeBuffer.
    /// Every subclass has to create a mesh, that will be drawn in the OnPostRender-method.
    /// The colouring is done via a 3D-Texture in this class.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public abstract class GPUVisualisation : MonoBehaviour
    {
        //The offset of the visualisation relative to the point (0,0,0)
        public Vector3 offset;

        //The scale of the visualisation
        public Vector3 scale;

        //The size of the CellularAutomaton
        protected GridDimensions dimensions;

        //The compute shader that generates a texture3D out of a cellular automaton
        protected ComputeShader texture3DCS;
        protected int texture3DCSKernel;
        protected RenderTexture texture3D;

        //The material for the mesh
        protected Material material;

        //The compute buffer for the mesh
        protected ComputeBuffer mesh;

        //A compute buffer that stores the number of triangles/quads generated by the Marching Cubes algorith
        protected ComputeBuffer args;
        protected int[] data;

        public Vector3 CellSize()
        {
            return new Vector3(scale.x / (dimensions.x * 16), scale.y / (dimensions.y * 16), scale.z / (dimensions.z * 16));
        }

        public void Initialize(GridDimensions dimensions)
        {
            this.dimensions = dimensions;

            InitializeComputeBuffer();
            InitializeComputeShader();

            InitializeTexture3D();
            InitializeMaterial();

            material.SetVector("offset", new Vector4(offset.x, offset.y, offset.z, 1));
            material.SetVector("scale", new Vector4(scale.x, scale.y, scale.z, 1));
            material.SetVector("dimensions", new Vector4( 1.0f / (dimensions.x * 32), 1.0f / (dimensions.y * 32), 1.0f / (dimensions.z * 32), 1 ));
            material.SetTexture("_MainTex", texture3D);
            material.SetBuffer("mesh", mesh);
        }


        public abstract void Render(ComputeBuffer cells);


        protected abstract void InitializeComputeBuffer();

        protected abstract void InitializeComputeShader();

        protected abstract void InitializeMaterial();


        /// <summary>
        /// Paints the 3D-Texture out of a cellular automaton. The different fluid-types are represented with different color.
        /// </summary>
        /// <param name="cells">The cells of a CellularAutomaton</param>
        protected void RenderTexture3D(ComputeBuffer cells)
        {
            texture3DCS.SetBuffer(texture3DCSKernel, "currentGeneration", cells);
            texture3DCS.Dispatch(texture3DCSKernel, dimensions.x, dimensions.y * 2, dimensions.z * 2);
        }


        /// <summary>
        /// Initializes the 3D-RenderTexture used to color the mesh, produced by the Marching Cubes algorithm.
        /// Loads the compute shader, that paints the texture, according to the content of the cellular automaton.
        /// </summary>
        private void InitializeTexture3D()
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB4444))
                texture3D = new RenderTexture(dimensions.x * 16, dimensions.y * 16, 1, RenderTextureFormat.ARGB4444);
            else
                texture3D = new RenderTexture(dimensions.x * 16, dimensions.y * 16, 1);
            texture3D.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture3D.filterMode = FilterMode.Trilinear;
            texture3D.volumeDepth = dimensions.z * 16;
            texture3D.enableRandomWrite = true;
            texture3D.Create();

            texture3DCS = Resources.Load<ComputeShader>("ComputeShader/Visualisation/CA2Texture3D"); ;

            texture3DCS.SetInts("size", new int[] { dimensions.x * 16, dimensions.y * 16, dimensions.z * 16 });
            texture3DCSKernel = texture3DCS.FindKernel("CSMain");
            texture3DCS.SetTexture(texture3DCSKernel, "Result", texture3D);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(offset + scale/2, scale);
        }

        void OnPostRender()
        {
            material.SetPass(0);
            Graphics.DrawProceduralIndirect(MeshTopology.Points, args);
        }
    }
}