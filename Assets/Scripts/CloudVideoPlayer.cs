using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;

public class CloudVideoPlayer : MonoBehaviour {


    private Dictionary<string, PointCloudDepth> _clouds;

    public string configFile;

    private string _videosDir;
    private string _colorStreamName;
    private string _depthStreamName;
    private string _normalStreamName;
    private int _vidWidth;
    private int _vidHeight;
    private int _layerNum;
    private bool _playing;

    void Awake()
    {
        Debug.Log("Hello Tracker");
        _clouds = new Dictionary<string, PointCloudDepth>();
        loadConfig();
    }



   

    private void loadConfig()
    { 
         Dictionary<string, string> config = ConfigProperties.load(configFile);

        _videosDir = config["videosDir"];
        _colorStreamName = config["colorStreamName"];
        _depthStreamName = config["depthStreamName"];
        _normalStreamName = config["normalStreamName"];
        _vidWidth =int.Parse(config["vidWidth"]);
        _vidHeight =int.Parse(config["vidHeight"]);
        _layerNum =int.Parse(config["numLayers"]);
        for (int i = 0; i < _layerNum; i++)
        {
            string s = "";
            s = s + i;
            string calib = config[s];
            string[] chunks = calib.Split(';');
            Matrix4x4 mat = new Matrix4x4(new Vector4(float.Parse(chunks[0]), float.Parse(chunks[4]), float.Parse(chunks[8]), float.Parse(chunks[12])),
                new Vector4(float.Parse(chunks[1]), float.Parse(chunks[5]), float.Parse(chunks[9]), float.Parse(chunks[13])),
                new Vector4(float.Parse(chunks[2]), float.Parse(chunks[6]), float.Parse(chunks[10]), float.Parse(chunks[14])),
                new Vector4(float.Parse(chunks[3]), float.Parse(chunks[7]), float.Parse(chunks[11]), float.Parse(chunks[15])));

            GameObject cloudobj = new GameObject(s);
            cloudobj.transform.localPosition = new Vector3(mat[0, 3], mat[1, 3],mat[2,3]);
            cloudobj.transform.localRotation = mat.rotation;
            cloudobj.transform.localScale = new Vector3(-1, 1, 1);
            cloudobj.AddComponent<PointCloudDepth>();
            PointCloudDepth cloud = cloudobj.GetComponent<PointCloudDepth>();


            string colorvideo = _videosDir + "\\" + s + _colorStreamName;
            string depthvideo = _videosDir + "\\" + s + _depthStreamName;
            //string normalvideo = _videosDir + "\\" + s + _normalStreamName;

            cloud.initStructs((uint)i,colorvideo, depthvideo,cloudobj);

            _clouds.Add(s, cloud);            
           
            
        }

    }



    public void hideAllClouds()
    {
        foreach (PointCloudDepth s in _clouds.Values)
        {
            s.hide();
        }

    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _playing = !_playing;
            foreach(KeyValuePair<string,PointCloudDepth> d in _clouds)
            {
                if (_playing)
                    d.Value.PlayCloudVideo();
                else
                    d.Value.PauseCloudVideo();
            }
        }
    }

    //public void processAvatarMessage(AvatarMessage av)
    //{
    //    foreach (string s in av.calibrations)
    //    {
    //        string[] chunks = s.Split(';');


    //    }
    //    Camera.main.GetComponent<MouseOrbitImproved>().target = _cloudGameObjects.First().Value.transform;
    //}
}
