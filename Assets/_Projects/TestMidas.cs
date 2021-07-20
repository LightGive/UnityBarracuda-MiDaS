using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

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

    private WebCamTexture webCamTexture;

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

    }
}
