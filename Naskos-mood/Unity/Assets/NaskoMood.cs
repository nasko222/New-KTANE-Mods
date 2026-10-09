using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NaskoMood : MonoBehaviour
{
    public KMBombModule Module;
    public KMSelectable Button;

    private const string ServerUrl = "http://naskogdps17.7m.pl/ktane/naskoMood.php";
    private const float MaxWaitTime = 10f;

    private bool _requestInProgress;
    private bool _solved;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private void Awake()
    {
        _moduleId = _moduleIdCounter++;

        Button.OnInteract += delegate
        {
            Button.AddInteractionPunch(0.5f);

            if (!_solved && !_requestInProgress)
                StartCoroutine(CheckServer());

            return false;
        };
    }

    private IEnumerator CheckServer()
    {
        _requestInProgress = true;

        string url = ServerUrl + "?nocache=" + DateTime.UtcNow.Ticks;

        Debug.LogFormat(
            "[Nasko's Mood #{0}] Performing wellness check...",
            _moduleId
        );

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            request.SetRequestHeader("Pragma", "no-cache");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            float startTime = Time.realtimeSinceStartup;

            while (!operation.isDone)
            {
                if (Time.realtimeSinceStartup - startTime >= MaxWaitTime)
                {
                    Debug.LogFormat(
                        "[Nasko's Mood #{0}] Wellness check timed out after 10 seconds. Assuming Nasko is fine. Solving module.",
                        _moduleId
                    );

                    request.Abort();
                    Solve();
                    yield break;
                }

                yield return null;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogFormat(
                    "[Nasko's Mood #{0}] Could not contact Nasko ({1}). Assuming he is fine. Solving module.",
                    _moduleId,
                    request.error
                );

                Solve();
                yield break;
            }

            string response = request.downloadHandler.text.Trim();

            Debug.LogFormat(
                "[Nasko's Mood #{0}] Server returned: \"{1}\"",
                _moduleId,
                response
            );

            if (response == "0")
            {
                Debug.LogFormat(
                    "[Nasko's Mood #{0}] Bad mood detected. This was a poor time for a wellness check. Strike!",
                    _moduleId
                );

                _requestInProgress = false;
                Module.HandleStrike();
            }
            else if (response == "1")
            {
                Debug.LogFormat(
                    "[Nasko's Mood #{0}] Good mood detected. Crisis averted. Module solved.",
                    _moduleId
                );

                Solve();
            }
            else
            {
                Debug.LogFormat(
                    "[Nasko's Mood #{0}] Unexpected response. Refusing to interpret Nasko's emotional state. Solving module.",
                    _moduleId
                );

                Solve();
            }
        }
    }

    private void Solve()
    {
        if (_solved)
            return;

        _solved = true;
        _requestInProgress = false;
        Module.HandlePass();
    }
}
