
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
    }

    public void PauseCloudVideo()
    {
        _player.Pause();
        _playing = false;
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

        List<Vector3> points = new List<Vector3>();
        List<int> ind = new List<int>();
        int n = 0;
        int i = 0;

        for (float w = 0; w < _width; w++)
        {
            for (float h = 0; h < _height; h++)
            {
                Vector3 p = new Vector3(w / _width, h / _height, 0);
                points.Add(p);
                ind.Add(n);
                n++;

                if (n == 65000)
                {
                    GameObject a = new GameObject("cloud" + i);
                    MeshFilter mf = a.AddComponent<MeshFilter>();
                    MeshRenderer mr = a.AddComponent<MeshRenderer>();
                    mr.material = _mat;
                    mr.material.SetTexture("_ColorTex", play.targetTexture);
                    mf.mesh = new Mesh();
                    mf.mesh.vertices = points.ToArray();
                    mf.mesh.SetIndices(ind.ToArray(), MeshTopology.Points, 0);
                    mf.mesh.bounds = new Bounds(new Vector3(0, 0, 4.5f), new Vector3(5, 5, 5));
                    a.transform.parent = cloudGameobj.transform;
                    a.transform.localPosition = Vector3.zero;
                    a.transform.localRotation = Quaternion.identity;
                    a.transform.localScale = new Vector3(1, 1, 1);
                    n = 0;
                    i++;
                    _objs.Add(a);
                    points = new List<Vector3>();
                    ind = new List<int>();
                }
            }
        }
        GameObject afinal = new GameObject("cloud" + i);
        MeshFilter mfinal = afinal.AddComponent<MeshFilter>();
        MeshRenderer mrfinal = afinal.AddComponent<MeshRenderer>();
        mrfinal.material = _mat;
        mfinal.mesh = new Mesh();
        mfinal.mesh.vertices = points.ToArray();
        mfinal.mesh.SetIndices(ind.ToArray(), MeshTopology.Points, 0);
        afinal.transform.parent = cloudGameobj.transform;
        afinal.transform.localPosition = Vector3.zero;
        afinal.transform.localRotation = Quaternion.identity;
        afinal.transform.localScale = new Vector3(1, 1, 1);
        n = 0;
        i++;
        _objs.Add(afinal);
        points = new List<Vector3>();
        ind = new List<int>();


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
