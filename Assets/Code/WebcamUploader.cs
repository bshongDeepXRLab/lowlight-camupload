using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class Data
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public float[] bboxes;
    public string[] pred_labels;
}

public class WebcamUploader : MonoBehaviour
{
    public RawImage rawImage;
    public TMP_Dropdown url_dropdown;
    private string uploadUrl = "http://165.194.115.91:8001/upload";
    private WebCamTexture webcamTexture;
    private Texture2D snap;
    private float captureInterval = 3.0f; // 캡처 및 업로드 주기(초)
    private float timer = 0.0f;

    public Button uploadBtn;
    public TextMeshProUGUI btnText;
    public TextMeshProUGUI logText;
    public TMP_InputField inputField;
    private bool isUploadStart = false;

    public TMP_Dropdown cameraDropdown;
    public Button refreshButton;
    private WebCamDevice[] devices;

    void Awake()
    {
        uploadBtn.onClick.AddListener(OnBtnClick);
        url_dropdown.onValueChanged.AddListener(OnDropdownEvent);
        string selectedText = url_dropdown.options[url_dropdown.value].text;
        Debug.Log("현재 선택된 옵션: " + selectedText);
    }

    void Start()
    {
        // 웹캠 시작
        StartWebcam(null);
        btnText.text = "업로드 시작";

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(UpdateDeviceList);
        }
        UpdateDeviceList();
        cameraDropdown.onValueChanged.AddListener(OnCameraSelected);
    }

    public void StartWebcam(string deviceName)
    {
        Debug.Log("Device 변경 deviceName = " + deviceName);
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        if (string.IsNullOrEmpty(deviceName))
        {
            webcamTexture = new WebCamTexture();
        }
        else
        {
            webcamTexture = new WebCamTexture(deviceName);
        }
        
        rawImage.texture = webcamTexture;
        webcamTexture.Play();
        StartCoroutine(UpdateRawImage());
    }

    void Update()
    {
        StartCoroutine(UpdateRawImage());
        timer += Time.deltaTime;
        if(isUploadStart)
        {
            if (timer >= captureInterval)
            {
                timer = 0f;
                StartCaptureAndUpload();
            }
        }
        
    }

    public void OnBtnClick()
    {
        if(!isUploadStart)
        {
            string selectedText = url_dropdown.options[url_dropdown.value].text;
            uploadUrl = selectedText;
            captureInterval = float.Parse(inputField.text);
            isUploadStart = true;
            btnText.text = "업로드 중지";
            logText.text = "";
        }
        else
        {
            isUploadStart = false;
            btnText.text = "업로드 시작";
            logText.text = "버튼을 눌러 업로드를 실행하세요";
        }
    }

    public void OnDropdownEvent(int index)
    {
        Debug.Log($"Dropdown Value : {index}");      
    }

    IEnumerator UpdateRawImage()
    {
        // 웹캠 텍스처를 Texture2D로 변환
        Rect rawImageRect = rawImage.rectTransform.rect;
        float displayWidth = rawImageRect.width;
        float displayHeight = rawImageRect.height;

        float webcamWidth = webcamTexture.width;
        float webcamHeight = webcamTexture.height;

        float webcamRatio = webcamWidth / webcamHeight;
        float displayRatio = displayWidth / displayHeight;

        int scaledWidth;
        int scaledHeight;

        
        if (webcamRatio > displayRatio)
        {
            scaledWidth = (int)displayWidth;
            scaledHeight = (int)(displayWidth / webcamRatio);
        }
        else
        {
            scaledHeight = (int)displayHeight;
            scaledWidth = (int)(displayHeight * webcamRatio);
        }

        if (snap == null || snap.width != (int)displayWidth || snap.height != (int)displayHeight)
        {
            snap = new Texture2D((int)displayWidth, (int)displayHeight);
        }
        Color[] pixels = webcamTexture.GetPixels();
        Color[] scaledPixels = new Color[(int)displayWidth * (int)displayHeight];

        // Fill with black
        for (int i = 0; i < scaledPixels.Length; i++)
        {
            scaledPixels[i] = Color.black;
        }

        int startX = (int)((displayWidth - scaledWidth) / 2);
        int startY = (int)((displayHeight - scaledHeight) / 2);

        float widthRatio = webcamWidth / scaledWidth;
        float heightRatio = webcamHeight / scaledHeight;

        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                int webcamX = Mathf.Clamp(Mathf.FloorToInt(x * widthRatio), 0, (int)webcamWidth - 1);
                int webcamY = Mathf.Clamp(Mathf.FloorToInt(y * heightRatio), 0, (int)webcamHeight - 1);

                int index = (x + startX) + (y + startY) * (int)displayWidth;
                if (index < scaledPixels.Length)
                {
                    scaledPixels[index] = pixels[webcamX + webcamY * (int)webcamWidth];
                }
            }
        }

        snap.SetPixels(scaledPixels);
        snap.Apply();
        rawImage.texture = snap;
        yield return null;
    }

    void StartCaptureAndUpload()
    {
        Debug.Log("StartCaptureAndUpload called");
        StartCoroutine(CaptureAndUpload());
    }

    IEnumerator CaptureAndUpload()
    {
        // Debug.Log("CaptureAndUpload called");
        // yield return new WaitForSeconds(2f); // 웹캠 초기화 대기

        
        // PNG/JPG로 인코딩
        byte[] imageData = snap.EncodeToJPG();

        // API #1 - 이미지 업로드
        WWWForm form = new WWWForm();
        form.AddBinaryData("image", imageData, "webcam" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg", "image/jpeg");
        
        using (UnityWebRequest uploadRequest = UnityWebRequest.Post(uploadUrl, form))
        {
            yield return uploadRequest.SendWebRequest();
            if (uploadRequest.result == UnityWebRequest.Result.Success)
            {
                logText.text += "Upload complete\n";
                Debug.Log("Upload complete");
            }
            else
            {
                logText.text += "Upload failed: " + uploadRequest.error + "\n";
                Debug.LogError("Upload failed: " + uploadRequest.error);
            }
        }
    }

    public void UpdateDeviceList()
    {
        devices = WebCamTexture.devices;
        cameraDropdown.ClearOptions();

        if (devices.Length == 0)
        {
            Debug.Log("No camera device found.");
            cameraDropdown.AddOptions(new List<string> { "No camera found" });
            return;
        }
        
        List<string> deviceNames = devices.Select(device => device.name).ToList();
        cameraDropdown.AddOptions(deviceNames);
    }

    void OnCameraSelected(int index)
    {
        if (devices.Length > index)
        {
            string selectedDeviceName = devices[index].name;
            Debug.Log("Selected Camera: " + selectedDeviceName);
            StartWebcam(selectedDeviceName);
        }
    }
}
