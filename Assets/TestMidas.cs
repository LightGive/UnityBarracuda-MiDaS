using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

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
	[SerializeField]
	private int startWebcamNo;
	[SerializeField]
	private int targetFPS;

	private ComputeBuffer tensorBuffer = null;
	private RenderTexture tmpResultDepth = null;
	private RenderTexture resultDepth = null;
	private WebCamTexture webCamTexture = null;
	private IWorker woker = null;

	IEnumerator Start()
	{
		if (WebCamTexture.devices.Length <= startWebcamNo)
		{
			Debug.Log("Cameraのデバイスが無い");
			yield break;
		}

		yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
		if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
		{
			Debug.LogError("Cameraを使用する権限が必要");
			yield break;
		}

		WebCamDevice device = WebCamTexture.devices[startWebcamNo];
		webCamTexture = new WebCamTexture(device.name);
		webCamTexture.requestedFPS = targetFPS;
		webCamTexture.Play();
		debugNormalImage.texture = webCamTexture;
		woker = ModelLoader.Load(model).CreateWorker();
		tensorBuffer = new ComputeBuffer(ImageSize * ImageSize * 3, sizeof(float));
		resultDepth = new RenderTexture(256, 256,0, format);
		tmpResultDepth = new RenderTexture(ImageSize, ImageSize, 0, format);
		depthImage.texture = resultDepth;
	}

	private void Update()
	{
		if (!Input.GetMouseButtonDown(0))
		{
			return;
		}

		var p = Camera.main.ScreenToViewportPoint(Input.mousePosition);
		var scale = (p.x > 0.5f) ?
			new Vector3(-depthImage.transform.localScale.x, depthImage.transform.localScale.y, 1.0f) :
			new Vector3(depthImage.transform.localScale.x, -depthImage.transform.localScale.y, 1.0f);
		depthImage.transform.localScale = scale;
		debugNormalImage.transform.localScale = scale;
	}

	private void LateUpdate()
	{
		if (webCamTexture == null || resultDepth == null)
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

		var reshape = new TensorShape(1, ImageSize, ImageSize, 1);
		using (var tensor = woker.PeekOutput().Reshape(reshape))
		{
			tensor.ToRenderTexture(tmpResultDepth, 0, 0, 1.0f / scale, 0);
		}
		Graphics.Blit(tmpResultDepth, resultDepth);
	}

	private void OnDestroy()
	{
		tensorBuffer?.Dispose();
		tensorBuffer = null;
		woker?.Dispose();
		woker = null;
		Destroy(resultDepth);
		Destroy(tmpResultDepth);
	}
}
