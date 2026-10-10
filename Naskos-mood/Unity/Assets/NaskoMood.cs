using System;
using System.Collections;
using System.Reflection;
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

            if (_solved || _requestInProgress)
                return false;

            string missionId;

            if (IsMissionSetting(out missionId))
            {
                Debug.LogFormat(
                    "[Nasko's Mood #{0}] Mission setting detected ({1}). Module solved automatically.",
                    _moduleId,
                    missionId
                );

                Solve();
            }
            else
            {
                StartCoroutine(CheckServer());
            }

            return false;
        };
    }

    private bool IsMissionSetting(out string missionId)
    {
        missionId = null;

        if (Application.isEditor)
            return false;

        try
        {
            Type gameplayStateType = FindType("GameplayState");

            if (gameplayStateType == null)
                return false;

            FieldInfo missionField = gameplayStateType.GetField(
                "MissionToLoad",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
            );

            if (missionField == null)
                return false;

            object missionValue = missionField.GetValue(null);

            if (missionValue == null)
                return false;

            missionId = missionValue.ToString();

            if (string.IsNullOrEmpty(missionId))
                return false;

            string freeplayId = GetStaticString(
                "FreeplayMissionGenerator",
                "FREEPLAY_MISSION_ID"
            );

            string customId = GetStaticString(
                "ModMission",
                "CUSTOM_MISSION_ID"
            );

            if (!string.IsNullOrEmpty(freeplayId) && missionId == freeplayId)
                return false;

            if (!string.IsNullOrEmpty(customId) && missionId == customId)
                return false;

            if (missionId.Equals("freeplay", StringComparison.InvariantCultureIgnoreCase))
                return false;

            if (missionId.Equals("custom", StringComparison.InvariantCultureIgnoreCase))
                return false;

            return true;
        }
        catch (Exception e)
        {
            Debug.LogFormat(
                "[Nasko's Mood #{0}] Could not determine mission setting: {1}",
                _moduleId,
                e.Message
            );

            return false;
        }
    }

    private Type FindType(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type[] types;

            try
            {
                types = assemblies[i].GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            if (types == null)
                continue;

            for (int j = 0; j < types.Length; j++)
            {
                Type type = types[j];

                if (type == null)
                    continue;

                if (type.Name == typeName || type.FullName == typeName)
                    return type;
            }
        }

        return null;
    }

    private string GetStaticString(string typeName, string fieldName)
    {
        Type type = FindType(typeName);

        if (type == null)
            return null;

        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
        );

        if (field == null)
            return null;

        object value = field.GetValue(null);

        return value == null ? null : value.ToString();
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
