
using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Collections;

public class PointCloudDepth : MonoBehaviour
{
    public float brightness;
    uint _id;
    Texture2D _depthTex;
    List<GameObject> _objs;
    GameObject _cloudGameobj;
    Material _mat;
    RVLDecoder _decoder;
    VideoPlayer _player;
    bool _playing;

    private bool _videoSeekActive;
    private bool _videoSeekWasPlaying;
    private double _seekTime;
    private long _seekFrame;

    // Decompressor _colorDecoder;
    byte[] _depthBytes;
   
    int _width;
    int _height;
    bool _depthStreamDone = false;
    void Awake()
    {
        _width = 512;
        _height = 424;
        _objs = null;
        _mat = Resources.Load("Materials/cloudmatDepth") as Material;
        brightness = 3;
    }

    public void PlayCloudVideo()
    {
        if (!_player.isPrepared && !_decoder.prepared)
        {
            print("Video not yet prepared");
            return;
        }
        print("Video prepared, lets play. can set time " + _player.canSetTime);
        _player.Play();
        _playing = true;
    }

    public void PauseCloudVideo()
    {
        _player.Pause();
        _playing = false;
    }

    public void StopCloudVideo()
    {
        _player.Stop();
        _player.Prepare();
        _playing = false;
        _decoder.ResetDecoder();
        hide();
    }

    public void Back5Sec()
    {
        if (!_player.canSetTime) return;

        _seekTime = _player.time - 5;
        _seekTime = _seekTime < 0 ? 0 : _seekTime;
        _seekFrame = (long)(_seekTime * 30);
        _player.time = _seekTime;
        _decoder.skipToFrame(_seekFrame);
        //StartCoroutine(SeekTimeRoutine());
    }

    public void Skip5Sec()
    {
        if (!_player.canSetTime) return;

        _seekTime = _player.time + 5;
        _seekFrame = (long)(_seekTime * 30);

        if (_seekTime > (_player.frameCount / _player.frameRate))
            StopCloudVideo();
        else
        {
            _player.time = _seekTime;
            _decoder.skipToFrame(_seekFrame);
        }
            //StartCoroutine(SeekTimeRoutine());

    }

   
    //private IEnumerator SeekTimeRoutine()
    //{
    //    _videoSeekActive = true;
    //    _videoSeekWasPlaying = _playing;
    //    hide();
       
    //    if (_player)
    //    {
    //        // stop the player
    //        _player.Stop();
        
    //        yield return null;

    //        _player.Prepare();

    //        Debug.Log("Preparing the video...");

    //        float waitTillTime = Time.time + 5f;  // wait 5 seconds max
    //        while (!_player.isPrepared && (Time.time < waitTillTime))
    //        {
    //            yield return null;
    //        }

    //        string sMessage = _player.isPrepared ? "Video prepared" : "Video NOT prepared";
    //        Debug.Log(sMessage);

    //        // start playing (otherwise it starts from time/frame 0).
    //        _player.Play();
    //        _player.Pause();

    //        yield return null;


    //        _player.time = _seekTime;

    //        //_playing = true;
    //        //OnNewFrame(_player, _player.frame);
    //        //_playing = false;


    //        Debug.Log(string.Format("VideoPlayer time set to: {0:F3}", _seekTime));
    //        _seekTime = -1.0;
    //        yield return null;
    //    }

    //    _videoSeekActive = false;
    //}

    //void VideoSeekCompleted(VideoPlayer source)
    //{
    //    print("Seeked");
    //    if (_videoSeekWasPlaying)
    //    {
    //        _decoder.skipToFrame(_seekFrame);
    //        _player.Play();
    //        _playing = true;
    //    }
    //}

    public void SetSpeed(float speed)
    {
        _player.playbackSpeed = speed;
        print("Set speed of cloud to " + speed);
    }
   
    void OnNewFrame(VideoPlayer source, long frameIdx)
    {
        if (!_playing || _videoSeekActive) return;

       
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
       // VideoClip clip = Resources.Load<VideoClip>(colorVideo) as VideoClip;
        VideoPlayer play = cloudGameobj.AddComponent<VideoPlayer>();
        play.playOnAwake = false;
        //play.clip = clip;
        play.url = colorVideo;
        play.targetTexture = new RenderTexture(_width, _height, 24, RenderTextureFormat.BGRA32);
        play.sendFrameReadyEvents = true;
        play.frameReady += this.OnNewFrame;
        play.errorReceived += this.errorReceived;
       // play.seekCompleted += this.VideoSeekCompleted;
        play.skipOnDrop = false;
        _player = play;
        _player.Prepare();

        //setup depth
        _decoder = new RVLDecoder(depthVideo,_width,_height);

       // StartCoroutine(_decoder.Prepare());
        _decoder.Prepare();
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

    public void destroy()
    {
        _player.frameReady -= this.OnNewFrame;
        foreach (GameObject a in _objs)
            GameObject.Destroy(a);
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

    void errorReceived(VideoPlayer v, string msg)
    {
        print("video player error " + msg);
    }
   
}
