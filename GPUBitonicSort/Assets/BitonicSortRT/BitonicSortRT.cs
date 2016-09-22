using UnityEngine;
using System.Collections;

public class BitonicSortRT : MonoBehaviour
{
    public Shader BitonicSortShader;

    Material _bitonicSortMat;

    Texture2D _seedTex;

    RenderTexture _sortBuffer;
    RenderTexture _sortTempBuffer;

    const int TEXTURE_WIDTH  = 512;
    const int TEXTURE_HEIGHT = 512;

    int _logSize            = 0;
    int _sortCount          = 0;
    int _maxCountNeedToSort = 0;

    void Start()
    {
        Init();

        Debug.Log("<color=lime>1 key :Sort one by one , 2 key : Sort at once, 3 key : Reset</color>");
    }

    void Update()
    {
        if (Input.GetKeyUp("1"))
        {
            GPUSortOneByOne();
        }

        if (Input.GetKeyUp("2"))
        {
            GPUSort();
        }

        if (Input.GetKeyUp("3"))
        {
            Reset();
        }
    }

    void Init()
    {
        if (_bitonicSortMat == null)
        {
            _bitonicSortMat = new Material(BitonicSortShader);
            _bitonicSortMat.hideFlags = HideFlags.HideAndDontSave;
        }

        _logSize = Mathf.FloorToInt(Mathf.Log(TEXTURE_WIDTH * TEXTURE_HEIGHT, 2.0f));
        _maxCountNeedToSort = Mathf.FloorToInt(_logSize * (_logSize + 1) / (float)2);

        // Create Seed Texture
        _seedTex = new Texture2D(TEXTURE_WIDTH, TEXTURE_HEIGHT, TextureFormat.RGBAFloat, false);
        _seedTex.filterMode = FilterMode.Point;
        _seedTex.wrapMode = TextureWrapMode.Clamp;
        _seedTex.hideFlags = HideFlags.HideAndDontSave;
        for (var x = 0; x < TEXTURE_WIDTH; x++)
        {
            for (var y = 0; y < TEXTURE_HEIGHT; y++)
            {
                var col = new Color(x / (float)TEXTURE_WIDTH, y / (float)TEXTURE_HEIGHT, 0.5f, Random.value);
                _seedTex.SetPixel(x, y, col);
            }
        }
        _seedTex.Apply();

        // Create Sorted Buffer
        _sortBuffer                = new RenderTexture(TEXTURE_WIDTH, TEXTURE_HEIGHT, 0, RenderTextureFormat.ARGBFloat);
        _sortBuffer.filterMode     = FilterMode.Point;
        _sortBuffer.wrapMode       = TextureWrapMode.Clamp;
        _sortBuffer.hideFlags      = HideFlags.HideAndDontSave;
        _sortTempBuffer            = new RenderTexture(TEXTURE_WIDTH, TEXTURE_HEIGHT, 0, RenderTextureFormat.ARGBFloat);
        _sortTempBuffer.filterMode = FilterMode.Point;
        _sortTempBuffer.wrapMode   = TextureWrapMode.Clamp;
        _sortTempBuffer.hideFlags  = HideFlags.HideAndDontSave;

        Graphics.Blit(_seedTex, _sortBuffer);
        Graphics.Blit(_seedTex, _sortTempBuffer);
    }

    void Reset()
    {
        for (var x = 0; x < TEXTURE_WIDTH; x++)
        {
            for (var y = 0; y < TEXTURE_HEIGHT; y++)
            {
                var col = new Color(x / (float)TEXTURE_WIDTH, y / (float)TEXTURE_HEIGHT, 0.5f, Random.value);
                _seedTex.SetPixel(x, y, col);
            }
        }
        _seedTex.Apply();

        Graphics.Blit(_seedTex, _sortBuffer);
        Graphics.Blit(_seedTex, _sortTempBuffer);

        _sortCount = 0;
    }

    /// <summary>
    /// 一度にソート
    /// </summary>
    void GPUSort()
    {

        for (var i = 0; i < _maxCountNeedToSort; i++)
        {
            int step = i;
            int rank;
            for (rank = 0; rank < step; rank++)
            {
                step -= rank + 1;
            }

            float stepno = (float)(1 << (rank + 1));
            float offset = (float)(1 << (rank - step));
            float stage = 2 * offset;

            _bitonicSortMat.SetFloat("_SortBlockSize", stage);
            _bitonicSortMat.SetFloat("_SortSize", stepno);
            _bitonicSortMat.SetFloat("_Offset", offset);
            _bitonicSortMat.SetVector("_TextureSize", new Vector4(TEXTURE_WIDTH, TEXTURE_HEIGHT, 0, 0));
            Graphics.Blit(_sortBuffer, _sortTempBuffer, _bitonicSortMat);
            SwapBuffer(ref _sortBuffer, ref _sortTempBuffer);

        }

        Debug.Log("Sort");
    }

    /// <summary>
    /// 順次ソート
    /// </summary>
    void GPUSortOneByOne()
    {

        int step = _sortCount;
        int rank;
        for (rank = 0; rank < step; rank++)
        {
            step -= rank + 1;
        }

        float stepno = (float)(1 << (rank + 1));
        float offset = (float)(1 << (rank - step));
        float stage = 2 * offset;

        Debug.Log("stepno : " + stepno + ", offset : " + offset + ", stage : " + stage);
        
        _bitonicSortMat.SetFloat("_SortBlockSize", stage);
        _bitonicSortMat.SetFloat("_SortSize", stepno);
        _bitonicSortMat.SetFloat("_Offset", offset);
        _bitonicSortMat.SetVector("_TextureSize", new Vector4(TEXTURE_WIDTH, TEXTURE_WIDTH, 0, 0));
        Graphics.Blit(_sortBuffer, _sortTempBuffer, _bitonicSortMat);
        SwapBuffer(ref _sortBuffer, ref _sortTempBuffer);

        _sortCount++;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ping"></param>
    /// <param name="pong"></param>
    void SwapBuffer(ref RenderTexture ping, ref RenderTexture pong)
    {
        RenderTexture tmp = ping;
        ping = pong;
        pong = tmp;
    }

    void OnDestroy()
    {

        if (_bitonicSortMat != null)
        {
            DestroyImmediate(_bitonicSortMat);
        }
        _bitonicSortMat = null;

        if (_seedTex != null)
        {
            DestroyImmediate(_seedTex);
        }
        _seedTex = null;

        if (_sortBuffer != null)
        {
            _sortBuffer.Release();
        }
        _sortBuffer = null;

        if (_sortTempBuffer != null)
        {
            _sortTempBuffer.Release();
        }
        _sortTempBuffer = null;
    }

    void OnGUI()
    {
        var r00 = new Rect(TEXTURE_WIDTH * 0, TEXTURE_HEIGHT * 0, TEXTURE_WIDTH, TEXTURE_HEIGHT);
        var r10 = new Rect(TEXTURE_WIDTH * 1, TEXTURE_HEIGHT * 0, TEXTURE_WIDTH, TEXTURE_HEIGHT);

        GUI.skin.label.fontSize = 36;
        
        GUI.DrawTexture(r00, _seedTex);
        GUI.DrawTexture(r10, _sortBuffer);

        GUI.Label(r00, "SeedTex");
        GUI.Label(r10, "SortBuffer");
    }
}
