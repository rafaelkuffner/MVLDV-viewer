using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

public class InputManager : MonoBehaviour {

    public GameObject _rightHand;
    public GameObject _head;
    private SteamVR_TrackedObject _rightObj = null;
    private SteamVR_Controller.Device _rightController;
    private SteamVR_Controller.Device device;
    private MenuOpened _menu;
    private CloudVideoPlayer _video;
    private float _playSpeed;

    //Laser Pointer Variables
    private GameObject _rightHolder;
    private GameObject _rightPointer;
    private float _pointerThickness = 0.002f;
    public Color _pointerColor;


    private bool _playing;

    private GameObject controlDataset;

    private GameObject menu;
   

    public enum MenuOpened
    {
        DatasetSelect,None
    }

    void setupRightPointer()
    {
        _rightHolder = new GameObject();
        _rightHolder.transform.parent = _rightHand.transform;
        _rightHolder.transform.localPosition = Vector3.zero;
        _rightHolder.transform.localRotation = Quaternion.identity;

        _rightPointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _rightPointer.transform.parent = _rightHolder.transform;
        _rightPointer.transform.localScale = new Vector3(_pointerThickness, _pointerThickness, 100f);
        _rightPointer.transform.localPosition = new Vector3(0f, 0f, 50f);
        _rightPointer.transform.localRotation = Quaternion.identity;
        BoxCollider collider = _rightPointer.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Rigidbody rigidBody = _rightPointer.AddComponent<Rigidbody>();
        rigidBody.isKinematic = true;

        _pointerColor = new Color(0.2f, 0.2f, 0.2f);
        Material newMaterial = new Material(Shader.Find("Unlit/Color"));
        newMaterial.SetColor("_Color", _pointerColor);
        _rightPointer.GetComponent<MeshRenderer>().material = newMaterial;
    }

 
	void Start () {
        _rightObj = _rightHand.GetComponent<SteamVR_TrackedObject>();
        setupRightPointer();

        DisableRightPointer();
        _menu = MenuOpened.None;
        _video = null;
        _playSpeed = 1;

        controlDataset = Instantiate(Resources.Load("Prefabs/ControlDataset")) as GameObject;
        controlDataset.SetActive(false);

	}

    void EnableRightPointer()
    {
        _rightPointer.SetActive(true);
    }

    void DisableRightPointer()
    {
        _rightPointer.SetActive(false);
    }

    void InputOpenMenus()
    {

        if (_rightController.GetPressUp(SteamVR_Controller.ButtonMask.ApplicationMenu))
        {
            //GameObject menu;
            CloseAllMenus();

            if (_menu != MenuOpened.DatasetSelect)
            {
                menu = Instantiate(Resources.Load("Prefabs/CloudMenu")) as GameObject;
                menu.name = "DatasetSelect";
                menu.transform.position = _head.transform.position + (_head.transform.forward * 2);
                Vector3 rot = Camera.main.transform.forward;
                rot.y = 0.0f;
                menu.transform.rotation = Quaternion.LookRotation(rot);
                _menu = MenuOpened.DatasetSelect;
                EnableRightPointer();
            }
            else
            {
                _menu = MenuOpened.None;
                DisableRightPointer();
            }
        }

       
    }

    public void SetPlaybackSpeed(float speed)
    {
        _playSpeed = speed;
        print("Changing video speed to " + speed);
        _video.setSpeed(speed);
    }

    void SelectDataset()
    {
        Ray raycast = new Ray(_rightHand.transform.position, _rightHand.transform.forward);
        RaycastHit hit;
        bool bHit = Physics.Raycast(raycast, out hit);
        if (hit.transform != null) {
             Button b = hit.transform.gameObject.GetComponent<Button>();
             if (b != null) { 
                 b.Select();

                 if (_rightController.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
                 {
                     if (b.name == "PlaySpeed")
                     {
                         SetPlaybackSpeed(float.Parse(b.GetComponent<VRUIItem>().value));
                         return;
                     }
                     if (_video != null && _video.configFile == b.name)
                     {
                         return;
                     }
                     else if (_video != null && _video.configFile != b.name){
                        _video.Close();
                     }
                     
                     _video = new CloudVideoPlayer(b.name);
                     CloseAllMenus();
                     DisableRightPointer();
                     _menu = MenuOpened.None;
                 }
             }
        }
    }

     
    void CloseAllMenus()
    {
        GameObject o = GameObject.Find("DatasetSelect");
        if (o != null) Destroy(o);
        
        return;
    }

	// Update is called once per frame
	void Update () {
        if (_rightObj.index == SteamVR_TrackedObject.EIndex.None ) return;
        _rightController = SteamVR_Controller.Input((int)_rightObj.index);

      

        InputOpenMenus();

        if (_menu == MenuOpened.DatasetSelect)
        {
            SelectDataset();
        }
       
        //if no menu is opened, annotation input, and playback control
        else if (_menu == MenuOpened.None)
        {
            if (!controlDataset.activeSelf && _video != null)
            {
                controlDataset.SetActive(true);
            }
            else
            {
                controlDataset.transform.position = new Vector3(_rightHand.transform.position.x,
                    _rightHand.transform.position.y + 0.15f, _rightHand.transform.position.z);
                Vector3 rot = Camera.main.transform.forward;
                rot.y = 0.0f;
                controlDataset.transform.rotation = Quaternion.LookRotation(rot);
            }

            if (_rightController.GetPressUp(SteamVR_Controller.ButtonMask.Touchpad))
            {
                
                Vector2 touchpad = _rightController.GetAxis(EVRButtonId.k_EButton_SteamVR_Touchpad);

                if (touchpad.y > 0.7f)
                {
                    print("Pressed Stop");
                    _video.Stop();
                    _playing = false;
                    controlDataset.SetActive(false);
                }

                else if (touchpad.y < -0.7f)
                {
                    print("Pressed Play");
                    _playing = !_playing;

                    if (_playing)
                        _video.Play();
                    else
                        _video.Pause();
                }
          
                else if (touchpad.x > 0.7f)
                {
                    print("Pressed Foward");
                    _video.Skip5Sec();
                    
                }

                else if (touchpad.x < -0.7f)
                {
                    print("Pressed Backward");
                    _video.Back5Sec();
                }
            }


          /*  //Got controllers, now handle input.
            if (_rightController.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                _playing = !_playing;
             
                if (_playing)
                    _video.Play();
                else
                    _video.Pause();
            }
            */

            if (_rightController.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
            {
                EnableRightPointer();
            }
            if (_rightController.GetPressUp(SteamVR_Controller.ButtonMask.Trigger))
            {
                Ray raycast = new Ray(_rightHand.transform.position, _rightHand.transform.forward);
                RaycastHit hit;
                bool bHit = Physics.Raycast(raycast, out hit);
                if (bHit && hit.collider.gameObject.name.Equals("VRPlane"))
                {
                    Debug.Log("colide with = " + hit.collider.gameObject.name);
                    GameObject camera = GameObject.Find("[CameraRig]");
                    camera.transform.position = new Vector3(hit.point.x, camera.transform.position.y, hit.point.z);
                }
                DisableRightPointer();
            }
        }
	}


}
