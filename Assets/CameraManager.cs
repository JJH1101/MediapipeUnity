using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Threading;

public class CameraManager : MonoBehaviour
{
    public RawImage display;
    public InputField inputField;
    public int width = 1280;
    public int height = 720;
    public int frameRate = 30;
    public string receivedData;

    WebCamTexture camTexture;
    int currentIndex = 0;
    bool isConnected = false;
    float timeCount = 0.0f;


    string serverIP;
    int port = 8000;

    TcpClient client;
    NetworkStream stream;
    Thread receiveThread;

    private void Start()
    {
        if (camTexture != null)
        {
            display.texture = null;
            camTexture.Stop();
            camTexture = null;
        }
        WebCamDevice device = WebCamTexture.devices[currentIndex];
        Debug.Log(device.name);
        camTexture = new WebCamTexture(device.name, width, height);
        display.texture = camTexture;
        Debug.Log(camTexture.height);
        camTexture.Play();
    }

    private void Update()
    {
        if (isConnected)
        {
            if (timeCount > 1.0f / frameRate)
            {
                SendDataAsync();
                timeCount = 0.0f;
            }
            timeCount += Time.deltaTime;

        }
    }

    public void ConnectToServer()
    {
        serverIP = inputField.text;
        Debug.Log(serverIP);
        try
        {
            client = new TcpClient(serverIP, port);
            stream = client.GetStream();
            Debug.Log("Connected to server");
            isConnected = true;
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("Socket error: " + e.Message);
        }
    }

    async void SendDataAsync()
    {
        try
        {
            byte[] separator = Encoding.UTF8.GetBytes("<END>");
            List<byte> sendData = new List<byte>();
            sendData.AddRange(CompressImage(ConvertWebCamTextureToTexture2D(camTexture)));
            sendData.AddRange(separator);

            if (stream.CanWrite)
            {
                await stream.WriteAsync(sendData.ToArray(), 0, sendData.Count);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in SendDataAsync: {e.Message}");
        }
    }

    void ReceiveData()
    {
        while (true)
        {
            try
            {
                byte[] lengthBuffer = new byte[4];
                int totalRead = 0;

                int bytesRead = stream.Read(lengthBuffer, 0, lengthBuffer.Length);
                if (bytesRead == lengthBuffer.Length)
                {
                    int messageLength = BitConverter.ToInt32(lengthBuffer.Reverse().ToArray(), 0);
                    byte[] dataBuffer = new byte[messageLength];

                    while (totalRead < messageLength)
                    {
                        int read = stream.Read(dataBuffer, 0, messageLength);
                        totalRead += read;
                    }

                    receivedData = Encoding.UTF8.GetString(dataBuffer);
                    Debug.Log($"Received data of length: {messageLength}");
                    Debug.Log(receivedData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Exception: " + e.Message);
            }
        }

    }

    Texture2D ConvertWebCamTextureToTexture2D(WebCamTexture webCamTexture)
    {
        Texture2D texture = new Texture2D(webCamTexture.width, webCamTexture.height);
        texture.SetPixels32(webCamTexture.GetPixels32());
        texture.Apply();
        return texture;
    }

    byte[] CompressImage(Texture2D texture)
    {
        byte[] imageBytes = texture.EncodeToJPG();
        return imageBytes;
    }
}


