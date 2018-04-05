
using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

public class PointCloudDepth : MonoBehaviour
{
    public float brightness;
    uint _id;
    Texture2D _colorTex;
    Texture2D _depthTex;
    List<GameObject> _objs;
    GameObject _cloudGameobj;
    Material _mat;
    RVLDecoder _decoder;
    VideoPlayer _player;
    string _colorpath;
    string _depthpath;
    bool _playing;

    // Decompressor _colorDecoder;
    byte[] _depthBytes;
    byte[] _colorBytes;
    int _texScale;
    int _width;
    int _height;
    bool _depthStreamDone = false;
    void Awake()
    {
        _width = 512;
        _height = 424;
        _texScale = 1;
        _objs = null;
        _mat = Resources.Load("Materials/cloudmatDepth") as Material;
        brightness = 1;
    }

    public void PlayCloudVideo()
    {
        _player.Play();
        _playing = true;
        print("play!");
    }

    public void PauseCloudVideo()
    {
        _player.Pause();
        _playing = false;
        print("pause!");
    }


    int createSubmesh(int h, int submeshHeight, int id)
    {
        List<Vector3> points = new List<Vector3>();
        //  List<int> ind = new List<int>();
        List<int> tri = new List<int>();
        int n = 0;

        for (int k = 0; k < submeshHeight; k++, h++)
        {
            for (int w = 0; w < _width; w++)
            {
                Vector3 p = new Vector3(w / (float)_width, h / (float)_height, 0);
                points.Add(p);
                // ind.Add(n);

                // Skip the last row/col
                if (w != (_width - 1) && k != (submeshHeight - 1))
                {
                    int topLeft = n;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + _width;
                    int bottomRight = bottomLeft + 1;

                    tri.Add(topLeft);
                    tri.Add(topRight);
                    tri.Add(bottomLeft);
                    tri.Add(bottomLeft);
                    tri.Add(topRight);
                    tri.Add(bottomRight);
                }
                n++;
            }
        }

        GameObject a = new GameObject("cloud" + id);
        MeshFilter mf = a.AddComponent<MeshFilter>();
        MeshRenderer mr = a.AddComponent<MeshRenderer>();
        mr.material = _mat;
        mr.material.SetTexture("_ColorTex", _player.targetTexture);
        mf.mesh = new Mesh();
        mf.mesh.vertices = points.ToArray();
        //  mf.mesh.SetIndices(ind.ToArray(), MeshTopology.Triangles, 0);
        mf.mesh.SetTriangles(tri.ToArray(), 0);
        mf.mesh.bounds = new Bounds(new Vector3(0, 0, 4.5f), new Vector3(5, 5, 5));
        a.transform.parent = this.gameObject.transform;
        a.transform.localPosition = Vector3.zero;
        a.transform.localRotation = Quaternion.identity;
        a.transform.localScale = new Vector3(1, 1, 1);
        n = 0;
        _objs.Add(a);

        return h;
    }

    void createStitchingMesh(int submeshHeight, int id)
    {
        List<Vector3> points = new List<Vector3>();
        //  List<int> ind = new List<int>();
        List<int> tri = new List<int>();
        int n = 0;

        for (int h = submeshHeight - 1; h < _height; h += submeshHeight)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int w = 0; w < _width; w++)
                {
                    Vector3 p = new Vector3(w / (float)_width, (h + i) / (float)_height, 0);

                    points.Add(p);
                    // ind.Add(n);

                    // Skip the last row/col
                    if (w != (_width - 1) && i == 0)
                    {
                        int topLeft = n;
                        int topRight = topLeft + 1;
                        int bottomLeft = topLeft + _width;
                        int bottomRight = bottomLeft + 1;

                        tri.Add(topLeft);
                        tri.Add(topRight);
                        tri.Add(bottomLeft);
                        tri.Add(bottomLeft);
                        tri.Add(topRight);
                        tri.Add(bottomRight);
                    }
                    n++;
                }
            }
        }

        GameObject a = new GameObject("cloud" + id);
        MeshFilter mf = a.AddComponent<MeshFilter>();
        MeshRenderer mr = a.AddComponent<MeshRenderer>();
        mr.material = _mat;
        mr.material.SetTexture("_ColorTex", _player.targetTexture);

        mf.mesh = new Mesh();
        mf.mesh.vertices = points.ToArray();
        //  mf.mesh.SetIndices(ind.ToArray(), MeshTopology.Triangles, 0);
        mf.mesh.SetTriangles(tri.ToArray(), 0);
        mf.mesh.bounds = new Bounds(new Vector3(0, 0, 4.5f), new Vector3(5, 5, 5));
        a.transform.parent = this.gameObject.transform;
        a.transform.localPosition = Vector3.zero;
        a.transform.localRotation = Quaternion.identity;
        a.transform.localScale = new Vector3(1, 1, 1);
        n = 0;
        _objs.Add(a);
    }


    void OnNewFrame(VideoPlayer source, long frameIdx)
    {
        
        if (!_playing) return;

       
        _depthStreamDone = !_decoder.DecompressRVL(_depthBytes, _width * _height);
        if (!_depthStreamDone) _depthTex.LoadRawTextureData(_depthBytes);
        else return;

        
        _depthTex.Apply();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer mr = renderers[i];
            mr.material.SetTexture("_DepthTex", _depthTex);
            mr.material.SetFloat("_Brightness", brightness);
        }
        show();
    }

    public void initStructs(uint id, string colorVideo, string depthVideo,GameObject cloudGameobj)
    {
        _id = id;
        _depthTex = new Texture2D(_width, _height, TextureFormat.BGRA32, false);
        _depthTex.filterMode = FilterMode.Point;
        _depthBytes = new byte[_width * _height * 4];
        _cloudGameobj = cloudGameobj;

        //Setup color
        VideoPlayer play = cloudGameobj.AddComponent<VideoPlayer>();
        play.playOnAwake = false;
        play.url = colorVideo;
        play.targetTexture = new RenderTexture(_width, _height, 24, RenderTextureFormat.BGRA32);
        play.sendFrameReadyEvents = true;
        play.frameReady += this.OnNewFrame;
        play.skipOnDrop = false;
        _player = play;
        //setup depth
        _decoder = new RVLDecoder(depthVideo,_width,_height);

        if (_objs != null)
        {
            foreach (GameObject g in _objs)
            {
                GameObject.Destroy(g);
            }
        }
        _objs = new List<GameObject>();

        int w = 0;
        int h = 0;
        int submeshes;
        for (submeshes = 0; submeshes < 4; submeshes++)
        {
            h = createSubmesh(h, _height / 4, submeshes);

        }
        createStitchingMesh(_height / 4, submeshes);

    }

    public void hide()
    {
        foreach (GameObject a in _objs)
            a.SetActive(false);
    }

    public void show()
    {
        foreach (GameObject a in _objs)
            a.SetActive(true);
    }

    void Update()
    {
        if (_depthStreamDone)
        {
            PauseCloudVideo();
            _decoder.ResetDecoder();
            _depthStreamDone = false;
            return;
        }
    }
}
