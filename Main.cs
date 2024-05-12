using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;

public class Main : MonoBehaviour
{
    public GameObject CurrentCube;
    public GameObject LastCube;
    public Text Text;
    public int Level;
    public bool Done;
    public GameObject scoreboardPanel;
    public Button scoreboardButton;
    public TMP_Text usernameText;
    public GameObject saveScoreToggle;
    public TMP_Text scoreboardText;

    DatabaseReference databaseReference;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                RetrieveScores();
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result.ToString());
            }
        });

        string username = PlayerPrefs.GetString("Username");
        usernameText.text = username;
        newBlock();
        scoreboardButton.onClick.AddListener(ShowScoreboard);
    }

    private void newBlock()
    {
        if (LastCube != null)
        {
            CurrentCube.transform.position = new Vector3(Mathf.Round(CurrentCube.transform.position.x),
                CurrentCube.transform.position.y,
                Mathf.Round(CurrentCube.transform.position.z));
            CurrentCube.transform.localScale = new Vector3(LastCube.transform.localScale.x - Mathf.Abs(CurrentCube.transform.position.x - LastCube.transform.position.x),
                                                           LastCube.transform.localScale.y,
                                                           LastCube.transform.localScale.z - Mathf.Abs(CurrentCube.transform.position.z - LastCube.transform.position.z));
            CurrentCube.transform.position = Vector3.Lerp(CurrentCube.transform.position, LastCube.transform.position, 0.5f) + Vector3.up * 5f;
            if (CurrentCube.transform.localScale.x <= 0f ||
               CurrentCube.transform.localScale.z <= 0f)
            {
                Done = true;
                Text.gameObject.SetActive(true);
                Text.text = "Game over: " + Level;

                saveScoreToggle.SetActive(true);

                StartCoroutine(RestartAfterDelay());

                return;
            }
        }

        LastCube = CurrentCube;
        CurrentCube = Instantiate(LastCube);
        CurrentCube.name = Level + "";
        CurrentCube.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.HSVToRGB((Level / 100f) % .6f, 1f, 1f));
        Level++;
        Text.text = "Score: " + Level;
        Camera.main.transform.position = CurrentCube.transform.position + new Vector3(100, 70f, -100);
        Camera.main.transform.LookAt(CurrentCube.transform.position + Vector3.down * 30f);
    }

    void Update()
    {
        if (Done)
            return;

        var time = Mathf.Abs(Time.realtimeSinceStartup % 2f - 1f);

        var pos1 = LastCube.transform.position + Vector3.up * 10f;
        var pos2 = pos1 + ((Level % 2 == 0) ? Vector3.left : Vector3.forward) * 120;

        if (Level % 2 == 0)
            CurrentCube.transform.position = Vector3.Lerp(pos2, pos1, time);
        else
            CurrentCube.transform.position = Vector3.Lerp(pos1, pos2, time);

        if (Input.GetMouseButtonDown(0))
            newBlock();
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(30f);
        SceneManager.LoadScene("SampleScene");
    }

    public void ShowScoreboard()
    {
        if (scoreboardPanel.activeSelf)
        {
            scoreboardPanel.SetActive(false);
            Done = false;
        }
        else
        {
            Done = true;
            scoreboardPanel.SetActive(true);
        }
    }

    public void SaveScore()
    {
        string username = PlayerPrefs.GetString("Username");
        int score = Level;

        if (databaseReference != null)
        {
            databaseReference.Child("scores").Child(username).GetValueAsync().ContinueWith(task =>
            {
                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (!snapshot.Exists || score > Convert.ToInt32(snapshot.Value))
                    {
                        databaseReference.Child("scores").Child(username).SetValueAsync(score).ContinueWith(saveTask =>
                        {
                            if (saveTask.IsCompleted)
                            {
                                Debug.Log("Score saved to Firebase!");
                            }
                            else if (saveTask.IsFaulted)
                            {
                                Debug.LogError("Failed to save score: " + saveTask.Exception);
                            }
                        });
                    }
                }
                else if (task.IsFaulted)
                {
                    Debug.LogError("Failed to retrieve existing score: " + task.Exception);
                }
            });
        }
    }

    public void DoNotSaveScore()
    {
        saveScoreToggle.SetActive(false);
        SceneManager.LoadScene("SampleScene");
    }

    void RetrieveScores()
    {
        DatabaseReference scoresRef = databaseReference.Child("scores");
        scoresRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to retrieve scores: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                List<KeyValuePair<string, long>> scoresList = new List<KeyValuePair<string, long>>();

                foreach (DataSnapshot scoreSnapshot in snapshot.Children)
                {
                    string username = scoreSnapshot.Key;
                    long score = Convert.ToInt64(scoreSnapshot.Value);
                    scoresList.Add(new KeyValuePair<string, long>(username, score));
                }

                // Sort the scores list
                scoresList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                // Log the retrieved scores list
                Debug.Log("Retrieved scores:");
                foreach (var scorePair in scoresList)
                {
                    Debug.Log(scorePair.Key + ": " + scorePair.Value);
                }

                // Update the scoreboard UI with the retrieved scores
                UpdateScoreboardUI(scoresList);
            }
        });
    }

void UpdateScoreboardUI(List<KeyValuePair<string, long>> scoresList)
{
    // Clear existing text
    scoreboardText.text = "";

    List<string> scoreboardLines = new List<string>();

    // Set the maximum width for the username and score
    int maxUsernameWidth = 10; // Adjust as needed
    int maxScoreWidth = 16;

    // Find the length of the longest score
    foreach (var scorePair in scoresList)
    {
        if (scorePair.Value.ToString().Length > maxScoreWidth)
        {
            maxScoreWidth = scorePair.Value.ToString().Length;
        }
    }

    // Calculate the maximum width needed for the number
    int maxNumberWidth = Math.Max(scoresList.Count.ToString().Length, 2); // 2 is for minimum two characters in the number

    // Construct scoreboard lines with dynamic spacing
    for (int i = 0; i < scoresList.Count; i++)
    {
        string username = scoresList[i].Key;
        long score = scoresList[i].Value;

        // Truncate or pad the username to fit within maxUsernameWidth
        if (username.Length > maxUsernameWidth)
        {
            username = username.Substring(0, maxUsernameWidth);
        }
        else
        {
            // Calculate padding for username to center align it
            int usernamePadding = (maxUsernameWidth - username.Length) / 2;
            username = username.PadLeft(username.Length + usernamePadding).PadRight(maxUsernameWidth);
        }

        // Convert the score to string
        string scoreString = score.ToString();

        // Calculate padding for score to center align it
        int scorePadding = (maxScoreWidth - scoreString.Length) / 2;

        // Calculate padding for rank numbers to ensure equal distance
        int rankPadding = maxNumberWidth - (i + 1).ToString().Length;

        // Construct the scoreboard line with dynamic spacing
        string scoreboardLine = string.Format("{0}{1}. {2} {3}", new string(' ', rankPadding), (i + 1), username, scoreString.PadLeft(maxScoreWidth + scorePadding));

        // Add the line to the list
        scoreboardLines.Add(scoreboardLine);
    }

    // Join the lines and set the text of the text component
    scoreboardText.text = string.Join("\n", scoreboardLines);
}


}
