using System;
using System.Reflection;
using UnityEngine;

public class AllSerialLetters : MonoBehaviour
{
    private FieldInfo _lettersField;
    private string _originalLetters;

    private void Awake()
    {
        Type serialNumberType = FindType("SerialNumber");

        if (serialNumberType == null)
        {
            Debug.LogError("[All Serial Letters] Could not find SerialNumber.");
            return;
        }

        _lettersField = serialNumberType.GetField(
            "letters",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (_lettersField == null)
        {
            Debug.LogError("[All Serial Letters] Could not find SerialNumber.letters.");
            return;
        }

        _originalLetters = _lettersField.GetValue(null) as string;

        _lettersField.SetValue(
            null,
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        );

        Debug.LogFormat(
            "[All Serial Letters] Serial letter pool changed from \"{0}\" to \"{1}\".",
            _originalLetters,
            _lettersField.GetValue(null)
        );
    }

    private Type FindType(string typeName)
    {
        Assembly[] assemblies =
            AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            Type type =
                assemblies[i].GetType(typeName);

            if (type != null)
                return type;
        }

        return null;
    }

    private void OnDestroy()
    {
        if (_lettersField == null ||
            string.IsNullOrEmpty(_originalLetters))
            return;

        object current =
            _lettersField.GetValue(null);

        if (current != null &&
            current.ToString() == "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            _lettersField.SetValue(
                null,
                _originalLetters
            );
        }
    }
}