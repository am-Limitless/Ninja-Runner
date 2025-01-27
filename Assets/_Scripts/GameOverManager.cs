using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [SerializeField]
    private GameObject gameOverCanvas;
    [SerializeField]
    private GameObject gameUiCanvas;
    [SerializeField]
    private TMP_Text scoreText;

    public void StopGame(int score)
    {
        gameUiCanvas.SetActive(false);
        gameOverCanvas.SetActive(true);
        scoreText.text = score.ToString();
    }

    public void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
