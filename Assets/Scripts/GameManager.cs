﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static CameraController Camera;
    public static AudioManager AudioManager;
    public static VibrationManager VibrationManager;
    private static Canvas _pauseMenuCanvas;

    private static GameObject _resumeButton;
    private static GameObject _retryButton;
    private static GameObject _successBackButton;

    public static bool IsPlayerSneezing = false;
    private bool _finishedLevel = false;
    private float _score = -1;
    private GameObject _endScreen;
    private GameObject _fillImage;

    public static bool ResumedThisFrame = false;

    static private bool _paused;
    static public bool Paused
    {
        get
        {
            return _paused;
        }
        set
        {
            _paused = value;
            if (_pauseMenuCanvas)
            {
                _pauseMenuCanvas.enabled = _paused;
            }

            if (_paused)
            {
                FindObjectOfType<EventSystem>().SetSelectedGameObject(_resumeButton);
            }
            else
            {
                FindObjectOfType<EventSystem>().SetSelectedGameObject(null);
            }

            if (OnPause != null)
            {
                OnPause(_paused);
            }
        }
    }

    public delegate void PauseAction(bool paused);
    public static event PauseAction OnPause;

    // Non-static function for menu buttons
    public void SetPaused(bool paused)
    {
        ResumedThisFrame = true;
        Paused = paused;
    }

    public static int CurrentGameSceneIndex;

    public static List<Sneeze> _sneezes;
    public static PlayerController _playerController;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CurrentGameSceneIndex = scene.buildIndex;
        AudioManager.OnSceneLoaded(scene.buildIndex);
    }

    void Awake()
    {
        CurrentGameSceneIndex = SceneManager.GetActiveScene().buildIndex;
        _finishedLevel = false;
        IsPlayerSneezing = false;

        Camera = FindObjectOfType<CameraController>();

        AudioManager = FindObjectOfType<AudioManager>();
        if (!AudioManager)
        {
            Debug.LogError("No audio manager in scene! (use prefab)");
        }

        VibrationManager = FindObjectOfType<VibrationManager>();

        GameObject pauseMenu = GameObject.FindGameObjectWithTag("PauseMenu");
        if (pauseMenu)
        {
            _pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            if (!_pauseMenuCanvas)
            {
                Debug.LogError("Pause menu doesn't have a canvas component!");
            }
        }

        _resumeButton = GameObject.Find("ResumeButton");
        _retryButton = GameObject.Find("RetryButton");
        _successBackButton = GameObject.Find("SuccessBackButton");

        _endScreen = GameObject.Find("EndScreen");
        if (_endScreen)
        {
            _endScreen.SetActive(false);
        }

        LoadData();
    }

    private void Update()
    {
        ResumedThisFrame = false;
        if (Input.GetButtonUp("Cancel"))
        {
            Paused = !Paused;
            return;
        }


        if (_finishedLevel)
        {
            if (_score > 0)
            {
                if (Input.GetButtonUp("Sneeze"))
                {
                    SceneManager.LoadScene(0);
                }
            }

            if (!_endScreen.activeSelf)
            {
                _endScreen.SetActive(true);
                if(_score > 0)
                {
                    if (Input.GetButtonUp("Sneeze"))
                    {
                        SceneManager.LoadScene(0);
                    }
                    _endScreen.transform.GetChild(0).gameObject.SetActive(true);
                    _endScreen.transform.GetChild(1).gameObject.SetActive(false);   
                    _fillImage = _endScreen.transform.GetChild(0).transform.GetChild(1).transform.GetChild(0).gameObject;
                }
                else
                {
                    _endScreen.transform.GetChild(0).gameObject.SetActive(false);
                    _endScreen.transform.GetChild(1).gameObject.SetActive(true);
                }
            }
            else if (_fillImage)
            {
                if (_fillImage.GetComponent<Image>().fillAmount < _score *1.5f)
                {
                    _fillImage.GetComponent<Image>().fillAmount += 0.05f;
                }
            }
        }
        else
        {
            if (IsPlayerSneezing)
            {
                bool isAnyoneSneezing = false;
                bool doneSneezing = true;

                foreach (var sneeze in _sneezes)
                {
                    if (sneeze.IsSneezing())
                    {
                        isAnyoneSneezing = true;
                        break;
                    }

                    if (!sneeze.HasSneezed() && doneSneezing)
                    {
                        doneSneezing = false;
                    }
                }

                if (isAnyoneSneezing)
                {
                    AudioManager.StartChainReaction();
                    
                    //print("Still sneezing");
                }
                else
                {
                    if (doneSneezing)
                    {
                        _score = CalculateColourPercentage();
                        Debug.Log("Percentage colored: " + _score);
                        //print("everybody sneezed");
                    }
                    else
                    {
                        AudioManager.StopChainReaction();
                        //print("not everybody sneezed");
                    }
                    _finishedLevel = true;

                    if (_score > 0)
                    {
                        if (Input.GetButtonUp("Sneeze"))
                        {
                            FindObjectOfType<LevelManager>().LoadMainMenu();
                        }
                    }
                    else
                    {
                        FindObjectOfType<EventSystem>().SetSelectedGameObject(_retryButton);
                    }
                }
            }
        }
    }

    public void LoadData()
    {
        _sneezes = new List<Sneeze>();
        _sneezes.AddRange(FindObjectsOfType<Sneeze>());
    }


    // Non-static function for menu buttons
    public void CallQuit()
    {
        Quit();
    }

    public static void Quit()
    {
        Application.Quit();
    }

    float CalculateColourPercentage()
    {

        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        rt.useMipMap = false;
        rt.antiAliasing = 1;
        RenderTexture.active = rt;

        var cam = GameObject.Find("cam sneeze").GetComponent<Camera>();
        cam.targetTexture = rt;

        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);

        cam.Render();

        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
        screenshot.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        float percentage = 0;

        Color[] pixelArr = screenshot.GetPixels(0);
        float colouredPixelCount = 0;
        for (int i = 0; i < pixelArr.Length; ++i)
        {
            if ((pixelArr[i].r == pixelArr[i].g) && (pixelArr[i].r == pixelArr[i].b) && (pixelArr[i].b == pixelArr[i].g))
            {
                //greyscale pixel

            }
            else
            {
                //coloured pixel
                ++colouredPixelCount;
            }
        }
        percentage = (colouredPixelCount / pixelArr.Length);

        return percentage;
    }
}
