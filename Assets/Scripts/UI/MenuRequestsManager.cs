using Assets.Scripts.Core;
using Game.UI;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuRequestsManager : MonoBehaviour
{
    private bool assignedToTitleSceneEvents = false;
    private bool assignedToGameSceneEvents = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        Scene activeScene = SceneManager.GetActiveScene();
        assignToEventsInScene(activeScene);
    }

    private void SceneManager_sceneLoaded(Scene loadedScene, LoadSceneMode arg1)
    {
        assignToEventsInScene(loadedScene);
    }

    private void assignToEventsInScene(Scene scene)
    {
        if (scene.name == "Title" && !assignedToTitleSceneEvents)           
        { 
            assignToEventsInTitleScene(); 
            assignedToTitleSceneEvents = true;
        }
        else if (scene.name == "Game" && !assignedToGameSceneEvents)            
        { 
            assignToEventsInGameScene();
            assignedToGameSceneEvents = true;
        }
    }

    private void assignToEventsInGameScene()
    {
        PauseMenu pauseMenu = FindFirstObjectByType<PauseMenu>();
        if (!pauseMenu) return;

        pauseMenu.RestartRequested += PauseMenu_RestartRequested;
        pauseMenu.MainMenuRequested += PauseMenu_MainMenuRequested;
        pauseMenu.QuitGameRequested += PauseMenu_QuitGameRequested;
    }

    private void assignToEventsInTitleScene()
    {
        TitleMenu titleMenu = FindFirstObjectByType<TitleMenu>();
        if (!titleMenu) return;

        titleMenu.StartGameRequested += TitleMenu_StartGameRequested;
        titleMenu.QuitGameRequested += TitleMenu_QuitGameRequested;
    }

    private void PauseMenu_QuitGameRequested(object sender, EventArgs e)
    {
        General.QuitGame();
    }

    private void PauseMenu_MainMenuRequested(object sender, EventArgs e)
    {
        General.LoadMainMenu();
    }

    private void PauseMenu_RestartRequested(object sender, EventArgs e)
    {
        General.LoadNewGame();
    }

    private void TitleMenu_StartGameRequested(object sender, EventArgs e)
    {
        General.LoadNewGame();
    }

    private void TitleMenu_QuitGameRequested(object sender, EventArgs e)
    {
        General.QuitGame();
    }
}

