using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatchDrawMesh : MonoBehaviour
{
    static int baseColorId = Shader.PropertyToID("_Col");

	[SerializeField]
	Mesh mesh = default;

	[SerializeField]
	Material material = default;
    const int size = 100;
    Matrix4x4[] matrices = new Matrix4x4[size];
    Vector4[] colors = new Vector4[size];
    MaterialPropertyBlock block;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < matrices.Length; i++) {
			matrices[i] = Matrix4x4.TRS(
				Random.insideUnitSphere * 10f, Quaternion.identity, Vector3.one
			);
			colors[i] =
				new Vector4(Random.value, Random.value, Random.value, 1f);
		}
    }

    // Update is called once per frame
    void Update()
    {
        if (block == null) {
			block = new MaterialPropertyBlock();
			block.SetVectorArray(baseColorId, colors);
		}
		Graphics.DrawMeshInstanced(mesh, 0, material, matrices, size, block);
    }
}
