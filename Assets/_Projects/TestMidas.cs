using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;
using System.Linq;

public class TestMidas : MonoBehaviour
{
	private const int ImageSize = 256;

	[SerializeField]
	private RawImage debugNormalImage;
	[SerializeField]
	private RawImage depthImage;
	[SerializeField]
	private NNModel model;
	[SerializeField]
	private ComputeShader preProcessShader;
	[SerializeField]
	private RenderTextureFormat format;
	[SerializeField]
	private float scale;
	private Color[] tmpColor = new Color[256 * 256];
	private ComputeBuffer tensorBuffer;
	private RenderTexture depthTex = null;
	private WebCamTexture webCamTexture = null;
	private IWorker woker;

	IEnumerator Start()
	{
		if (WebCamTexture.devices.Length == 0)
		{
			Debug.Log("Cameraのデバイスが無い");
			yield break;
		}

		yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
		if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			Debug.Log("カメラの許可が必要");
		}

		WebCamDevice device = WebCamTexture.devices[0];
		webCamTexture = new WebCamTexture(device.name);
		debugNormalImage.texture = webCamTexture;
		webCamTexture.Play();
		woker = ModelLoader.Load(model).CreateWorker();
		tensorBuffer = new ComputeBuffer(ImageSize * ImageSize * 3, sizeof(float));
		depthTex = new RenderTexture(256, 256,0, format);
		depthImage.texture = depthTex;
	}


	private void LateUpdate()
	{
		if (webCamTexture == null || depthTex == null)
		{
			return;
		}

		int kernelID = preProcessShader.FindKernel("Preprocess");
		preProcessShader.SetTexture(kernelID, "_Texture", webCamTexture);
		preProcessShader.SetBuffer(kernelID, "_Tensor", tensorBuffer);
		preProcessShader.Dispatch(kernelID, ImageSize / 8, ImageSize / 8, 1);
		using (var tensor = new Tensor(1, ImageSize, ImageSize, 3, tensorBuffer))
        {
			woker.Execute(tensor);
        }

		var reshape = new TensorShape(1, 1, 1, 1, 1, ImageSize, ImageSize, 1);
		var reshapedRT = RenderTexture.GetTemporary(reshape.width, reshape.height, 0, format);
		using (var tensor = woker.PeekOutput().Reshape(reshape))
		{
			tensor.ToRenderTexture(reshapedRT, 0, 0, 1.0f / scale, 0);
		}

		Graphics.Blit(reshapedRT, depthTex);
		RenderTexture.ReleaseTemporary(reshapedRT);
	}

	private void OnDestroy()
    {
		tensorBuffer?.Dispose();
		tensorBuffer = null;
		woker?.Dispose();
		woker = null;
		depthTex = null;
    }
}
