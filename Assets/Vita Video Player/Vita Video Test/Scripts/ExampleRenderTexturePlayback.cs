using System;
using UnityEngine;
using UnityEngine.PSVita;
using UnityEngine.SceneManagement;

public class ExampleRenderTexturePlayback : MonoBehaviour
{
    public string m_MoviePath;
    public RenderTexture m_RenderTexture;
    public GUISkin m_Skin;
    bool m_IsPlaying = false;

    void Start()
    {
        PSVitaVideoPlayer.Init(m_RenderTexture);
        PSVitaVideoPlayer.Play(m_MoviePath, PSVitaVideoPlayer.Looping.None, PSVitaVideoPlayer.Mode.RenderToTexture);
    }

    void OnPreRender()
    {
        PSVitaVideoPlayer.Update();
    }

    void OnGUI()
    {
        GUI.skin = m_Skin;
        GUILayout.BeginArea(new Rect(10,10,200,Screen.height));
        if (GUILayout.Button("Skip"))
        {
            if (m_IsPlaying)
            {
                PSVitaVideoPlayer.Stop();
                SceneManager.LoadScene("Scene_Main");
            }
            else
            {
                SceneManager.LoadScene("Scene_Main");
                /* PSVitaVideoPlayer.Init(m_RenderTexture);
                PSVitaVideoPlayer.Play(m_MoviePath, PSVitaVideoPlayer.Looping.Continuous, PSVitaVideoPlayer.Mode.RenderToTexture);*/
            }
        }
        GUILayout.EndArea();
    }

    void OnMovieEvent(int eventID)
    {
        PSVitaVideoPlayer.MovieEvent movieEvent = (PSVitaVideoPlayer.MovieEvent)eventID;
        switch (movieEvent)
        {
            case PSVitaVideoPlayer.MovieEvent.PLAY:
                m_IsPlaying = true;
                break;

            case PSVitaVideoPlayer.MovieEvent.STOP:
                //Debug.Log("movie ended");
                m_IsPlaying = false;
                SceneManager.LoadScene("Scene_Main");
                break;
        }
    }
}