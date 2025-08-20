using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeviceSelector : MonoBehaviour
{
    public TMP_Dropdown cameraDropdown;
    public Button refreshButton;
    public WebcamUploader webcamUploader;
    private WebCamDevice[] devices;

    void Start()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(UpdateDeviceList);
        }
        UpdateDeviceList();
        cameraDropdown.onValueChanged.AddListener(OnCameraSelected);
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
        if (devices.Length > index && webcamUploader != null)
        {
            string selectedDeviceName = devices[index].name;
            Debug.Log("Selected Camera: " + selectedDeviceName);
            webcamUploader.StartWebcam(selectedDeviceName);
        }
    }
}
