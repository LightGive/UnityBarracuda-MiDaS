using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;
using System.Linq;

public class TestMidas : MonoBehaviour
{
    [SerializeField]
    private int width;
    [SerializeField]
    private int height;
    [SerializeField]
    private RawImage debugNormalImage;
    [SerializeField]
    private RawImage depthImage;
    [SerializeField]
    private NNModel model;

    private Color[] tmpColor = new Color[256 * 256];
    private RenderTexture depthTex = null;
    private WebCamTexture webCamTexture = null;

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
        webCamTexture = new WebCamTexture(device.name, width, height);
        debugNormalImage.texture = webCamTexture;
        webCamTexture.Play();

        depthTex = new RenderTexture(256, 256, 0,RenderTextureFormat.ARGBFloat);
        depthImage.texture = depthTex;
    }


    private void LateUpdate()
    {
        if(webCamTexture == null)
        {
            return;
        }

        using var input = new Tensor(1, 256, 256, 3);
        for (var y = 0; y < 256; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                var tx = x * webCamTexture.width / 256;
                var ty = y * webCamTexture.height / 256;
                var c = webCamTexture.GetPixel(tx, ty);
                input[0, 255 - y, x, 0] = c.r;
                input[0, 255 - y, x, 1] = c.g;
                input[0, 255 - y, x, 1] = c.b;
            }
        }

        using var worker = ModelLoader.Load(model).CreateWorker();
        worker.Execute(input);
        var output = worker.PeekOutput();

        //1,1,256,256
        //1,1,1,10

        var tex = new Texture2D(256, 256);
        for (var y = 0; y < 256; y++)
        {
            for (var x = 0; x < 256; x++)
            {
                var o = output[0, 0, x, 255 - y]/1000.0f;
                tmpColor[(y * 256) + x] = new Color(o, o, o);
            }
        }

        tex.SetPixels(tmpColor);
        tex.Apply();
        Graphics.Blit(tex, depthTex);
    }
}
