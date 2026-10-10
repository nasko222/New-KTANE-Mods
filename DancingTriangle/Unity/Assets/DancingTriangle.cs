using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using KModkit;
using Newtonsoft.Json;
using UnityEngine;

public class DancingTriangle : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMSelectable[] Buttons;
    public SpriteRenderer[] DigitRenderers;
    public Renderer[] ButtonOutlines;
    public Sprite[] DigitSprites;
    public Material[] OutlineMaterials;
    public Color[] InlineColors;
    public Animator TriangleAnimator;
    public AudioSource AudioSource;
    public AudioClip LoopAudio;
    public AudioClip EndAudio;
	
	private class DancingTriangleSettings
	{
		public int Volume = 100;
	}

	private int _volume = 100;

#pragma warning disable 414
	private static readonly Dictionary<string, object>[] TweaksEditorSettings =
	{
		new Dictionary<string, object>
		{
			{ "Filename", "dancingTriangle-settings.txt" },
			{ "Name", "Dancing Triangle" },
			{
				"Listings",
				new List<Dictionary<string, object>>
				{
					new Dictionary<string, object>
					{
						{ "Key", "Volume" },
						{ "Text", "Volume" },
						{ "Description", "Music volume from 0 to 100. Default: 100." }
					}
				}
			}
		}
	};
#pragma warning restore 414

    private int[] _buttonNumbers = new int[5];
    private int[] _outlineIndexes = new int[5];
    private int[] _inlineIndexes = new int[5];

    private string _enteredCode = "";
    private string _correctCode = "";

    private bool _activated;
    private bool _submitting;
    private bool _solved;

    private static int _moduleIdCounter = 1;
    private int _moduleId;

    private readonly string[] _colorNames =
    {
        "Red",
        "Yellow",
        "Green",
        "Cyan",
        "Blue",
        "Magenta",
        "White",
        "Black"
    };

    private readonly int[] _affineValues =
    {
        1, 3, 5, 7, 9, 11,
        15, 17, 19, 21, 23, 25
    };

    private void Awake()
	{
		_moduleId = _moduleIdCounter++;

		LoadSettings();
		RandomizeButtons();

        for (int i = 0; i < Buttons.Length; i++)
        {
            int x = i;

            Buttons[i].OnInteract += delegate
            {
                HandleButtonPress(x);
                return false;
            };
        }

        StartLoopAudio();

        Module.OnActivate += Activate;
    }

    private void Activate()
    {
        if (_activated)
            return;

        _correctCode = CalculateCorrectCode();
        _activated = true;

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Module activated. Correct code: {1}",
            _moduleId,
            _correctCode
        );
    }

    private void RandomizeButtons()
    {
        if (Buttons.Length != 5 ||
            DigitRenderers.Length != 5 ||
            ButtonOutlines.Length != 5)
        {
            Debug.LogErrorFormat(
                "[Dancing Triangle #{0}] Buttons, DigitRenderers and ButtonOutlines must all contain exactly 5 objects.",
                _moduleId
            );

            return;
        }

        if (DigitSprites.Length != 10)
        {
            Debug.LogErrorFormat(
                "[Dancing Triangle #{0}] DigitSprites must contain exactly 10 sprites.",
                _moduleId
            );

            return;
        }

        if (OutlineMaterials.Length != 8 ||
            InlineColors.Length != 8)
        {
            Debug.LogErrorFormat(
                "[Dancing Triangle #{0}] OutlineMaterials and InlineColors must each contain exactly 8 entries.",
                _moduleId
            );

            return;
        }

        for (int i = 0; i < 5; i++)
        {
            _buttonNumbers[i] =
                UnityEngine.Random.Range(0, 10);

            _outlineIndexes[i] =
                UnityEngine.Random.Range(0, 8);

            _inlineIndexes[i] =
                UnityEngine.Random.Range(0, 7);

            if (_inlineIndexes[i] >= _outlineIndexes[i])
                _inlineIndexes[i]++;

            DigitRenderers[i].sprite =
                DigitSprites[_buttonNumbers[i]];

            Color inlineColor =
                InlineColors[_inlineIndexes[i]];

            inlineColor.a = 1f;

            DigitRenderers[i].color =
                inlineColor;

            ButtonOutlines[i].sharedMaterial =
                OutlineMaterials[_outlineIndexes[i]];

            Debug.LogFormat(
                "[Dancing Triangle #{0}] Button {1}: digit {2}, outline {3} ({4}), digit color {5} ({6}).",
                _moduleId,
                i + 1,
                _buttonNumbers[i],
                _colorNames[_outlineIndexes[i]],
                _outlineIndexes[i],
                _colorNames[_inlineIndexes[i]],
                _inlineIndexes[i]
            );
        }
    }

    private string CalculateCorrectCode()
    {
        if (BombInfo == null)
        {
            Debug.LogErrorFormat(
                "[Dancing Triangle #{0}] BombInfo is not assigned.",
                _moduleId
            );

            return "";
        }

        int emptyHolders =
            BombInfo.GetBatteryHolderCount(0);

        int dBatteries =
            BombInfo.GetBatteryCount(1);

        int threeBatteryHolders =
            BombInfo.GetBatteryHolderCount(3);

        int fourBatteryHolders =
            BombInfo.GetBatteryHolderCount(4);

        int totalBatteryHolders =
            BombInfo.GetBatteryHolderCount();

        int aaCells =
            BombInfo.GetBatteryCount(2) +
            BombInfo.GetBatteryCount(3) +
            BombInfo.GetBatteryCount(4);

        int B =
            aaCells +
            3 * dBatteries +
            5 * emptyHolders +
            7 * threeBatteryHolders +
            11 * fourBatteryHolders +
            13 * totalBatteryHolders;

        string[][] portPlates =
            BombInfo.GetPortPlates().ToArray();

        int portPlateCount =
            portPlates.Length;

        int emptyPortPlates =
            portPlates.Count(
                x => x != null && x.Length == 0
            );

        int P = 0;

        P += BombInfo.GetPortCount(Port.DVI) * 1;
        P += BombInfo.GetPortCount(Port.Parallel) * 2;
        P += BombInfo.GetPortCount(Port.PS2) * 3;
        P += BombInfo.GetPortCount(Port.RJ45) * 4;
        P += BombInfo.GetPortCount(Port.Serial) * 5;
        P += BombInfo.GetPortCount(Port.StereoRCA) * 6;
        P += BombInfo.GetPortCount(Port.AC) * 7;
        P += BombInfo.GetPortCount(Port.USB) * 8;
        P += BombInfo.GetPortCount(Port.VGA) * 9;
        P += BombInfo.GetPortCount(Port.PCMCIA) * 10;
        P += BombInfo.GetPortCount(Port.ComponentVideo) * 11;
        P += BombInfo.GetPortCount(Port.CompositeVideo) * 12;
        P += BombInfo.GetPortCount(Port.HDMI) * 13;

        P += 17 * emptyPortPlates;
        P += 19 * portPlateCount;

        string[] indicators =
            BombInfo.GetIndicators()
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

        string[] litIndicators =
            BombInfo.GetOnIndicators()
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

        string[] unlitIndicators =
            BombInfo.GetOffIndicators()
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

        int indicatorLetterScore = 0;

        foreach (string indicator in indicators)
        {
            foreach (char c in indicator.ToUpperInvariant())
            {
                if (c >= 'A' && c <= 'Z')
                    indicatorLetterScore += c - 'A' + 1;
            }
        }

        int I =
            indicatorLetterScore +
            7 * litIndicators.Length +
            11 * unlitIndicators.Length;

        string serial =
            BombInfo.GetSerialNumber();

        if (string.IsNullOrEmpty(serial))
            serial = "AAAAAA";

        serial =
            serial.ToUpperInvariant();

        int S = 0;

        foreach (char c in serial)
        {
            if (c >= 'A' && c <= 'Z')
                S += c - 'A' + 1;

            else if (c >= '0' && c <= '9')
                S += c - '0';
        }

        int F =
            BombInfo.GetTwoFactorCounts();

        int V =
            GetVoltageTenthsSum();

        int M;
        int Y;

        GetManufactureValues(
            out M,
            out Y
        );

        int W =
            23 * F +
            V +
            29 * M +
            31 * Y;

        int[] E = new int[5];

        E[0] = Mod(
            B +
            2 * P +
            3 * I +
            5 * W +
            7 * S,
            26
        );

        E[1] = Mod(
            2 * B +
            3 * P +
            5 * I +
            7 * W +
            11 * S,
            26
        );

        E[2] = Mod(
            3 * B +
            5 * P +
            7 * I +
            11 * W +
            13 * S,
            26
        );

        E[3] = Mod(
            5 * B +
            7 * P +
            11 * I +
            13 * W +
            17 * S,
            26
        );

        E[4] = Mod(
            7 * B +
            11 * P +
            13 * I +
            17 * W +
            19 * S,
            26
        );

        StringBuilder keyBuilder =
            new StringBuilder();

        for (int i = 0; i < 5; i++)
        {
            int buttonPosition =
                i + 1;

            int q =
                Mod(
                    11 * _buttonNumbers[i] +
                    7 * _outlineIndexes[i] +
                    5 * _inlineIndexes[i] +
                    buttonPosition * buttonPosition +
                    E[i],
                    26
                );

            keyBuilder.Append(
                (char)('A' + q)
            );
        }

        string key =
            keyBuilder.ToString();

        const string triangleMask =
            "TRIANGULAR";

        StringBuilder repeatedSerialBuilder =
            new StringBuilder();

        while (repeatedSerialBuilder.Length < 10)
            repeatedSerialBuilder.Append(serial);

        string repeatedSerial =
            repeatedSerialBuilder
                .ToString()
                .Substring(0, 10);

        StringBuilder seedBuilder =
            new StringBuilder();

        for (int i = 0; i < 10; i++)
        {
            int triangleValue =
                triangleMask[i] - 'A';

            int serialValue =
                Base36Value(
                    repeatedSerial[i]
                );

            int xorValue =
                triangleValue ^ serialValue;

            int seedValue =
                Mod(
                    xorValue,
                    26
                );

            seedBuilder.Append(
                (char)('A' + seedValue)
            );
        }

        string serialSeed =
            seedBuilder.ToString();

        StringBuilder vigenereBuilder =
            new StringBuilder();

        for (int i = 0; i < 10; i++)
        {
            int input =
                serialSeed[i] - 'A';

            int keyValue =
                key[i % 5] - 'A';

            int output =
                Mod(
                    input + keyValue,
                    26
                );

            vigenereBuilder.Append(
                (char)('A' + output)
            );
        }

        string vigenere =
            vigenereBuilder.ToString();

        int affineIndex =
            Mod(
                B + I + F,
                12
            );

        int affineA =
            _affineValues[affineIndex];

        int affineB =
            Mod(
                P + W + S,
                26
            );

        StringBuilder affineBuilder =
            new StringBuilder();

        foreach (char c in vigenere)
        {
            int x =
                c - 'A';

            int y =
                Mod(
                    affineA * x +
                    affineB,
                    26
                );

            affineBuilder.Append(
                (char)('A' + y)
            );
        }

        string affine =
            affineBuilder.ToString();

        int rails =
            2 +
            Mod(
                V +
                M +
                portPlateCount,
                3
            );

        string railFence =
            RailFenceEncrypt(
                affine,
                rails
            );

        string polybiusAlphabet =
            BuildPolybiusAlphabet(
                key
            );

        int[] rows =
            new int[10];

        int[] columns =
            new int[10];

        for (int i = 0; i < 10; i++)
        {
            char c =
                railFence[i];

            if (c == 'J')
                c = 'I';

            int index =
                polybiusAlphabet.IndexOf(c);

            if (index < 0)
                index =
                    polybiusAlphabet.IndexOf('I');

            rows[i] =
                index / 5 + 1;

            columns[i] =
                index % 5 + 1;
        }

        int X =
            Mod(
                B +
                P +
                I +
                W +
                S,
                5
            );

        StringBuilder codeBuilder =
            new StringBuilder();

        StringBuilder buttonSequenceBuilder =
            new StringBuilder();

        for (int i = 0; i < 10; i++)
        {
            int j =
                i + 1;

            int q =
                i % 5;

            int selectedButton =
                Mod(
                    rows[i] +
                    2 * columns[i] +
                    _buttonNumbers[q] +
                    _outlineIndexes[q] +
                    _inlineIndexes[q] +
                    E[q] +
                    j +
                    X,
                    5
                );

            codeBuilder.Append(
                _buttonNumbers[selectedButton]
            );

            if (i != 0)
                buttonSequenceBuilder.Append(" ");

            buttonSequenceBuilder.Append(
                selectedButton + 1
            );
        }

        string result =
            codeBuilder.ToString();

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Battery data: AA cells={1}, D batteries={2}, empty holders={3}, 3-cell holders={4}, 4-cell holders={5}, total holders={6}, B={7}.",
            _moduleId,
            aaCells,
            dBatteries,
            emptyHolders,
            threeBatteryHolders,
            fourBatteryHolders,
            totalBatteryHolders,
            B
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Port data: plates={1}, empty plates={2}, P={3}.",
            _moduleId,
            portPlateCount,
            emptyPortPlates,
            P
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Indicator data: total={1}, lit={2}, unlit={3}, I={4}.",
            _moduleId,
            indicators.Length,
            litIndicators.Length,
            unlitIndicators.Length,
            I
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Serial={1}, S={2}.",
            _moduleId,
            serial,
            S
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Optional edgework: Two-Factor count={1}, voltage tenths={2}, manufacture month sum={3}, manufacture year suffix sum={4}, W={5}.",
            _moduleId,
            F,
            V,
            M,
            Y,
            W
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Edge keys: {1}, {2}, {3}, {4}, {5}.",
            _moduleId,
            E[0],
            E[1],
            E[2],
            E[3],
            E[4]
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Button key: {1}.",
            _moduleId,
            key
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Repeated serial: {1}. Serial seed: {2}.",
            _moduleId,
            repeatedSerial,
            serialSeed
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Vigenere result: {1}.",
            _moduleId,
            vigenere
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Affine parameters: a={1}, b={2}. Result: {3}.",
            _moduleId,
            affineA,
            affineB,
            affine
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Rail Fence rails={1}. Result: {2}.",
            _moduleId,
            rails,
            railFence
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Polybius alphabet: {1}.",
            _moduleId,
            polybiusAlphabet
        );

        Debug.LogFormat(
            "[Dancing Triangle #{0}] X={1}. Required button sequence: {2}. Correct code: {3}.",
            _moduleId,
            X,
            buttonSequenceBuilder.ToString(),
            result
        );

        return result;
    }

    private int GetVoltageTenthsSum()
    {
        int total = 0;

        var responses =
            BombInfo.QueryWidgets(
                "volt",
                ""
            );

        foreach (string response in responses)
        {
            if (string.IsNullOrEmpty(response))
                continue;

            try
            {
                Dictionary<string, string> data =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        response
                    );

                if (data == null)
                    continue;

                string raw;

                if (!data.TryGetValue(
                    "voltage",
                    out raw
                ))
                    continue;

                double voltage;

                bool parsed =
                    double.TryParse(
                        raw,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out voltage
                    );

                if (!parsed)
                {
                    parsed =
                        double.TryParse(
                            raw,
                            NumberStyles.Float,
                            CultureInfo.CurrentCulture,
                            out voltage
                        );
                }

                if (!parsed)
                {
                    parsed =
                        double.TryParse(
                            raw.Replace(',', '.'),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out voltage
                        );
                }

                if (parsed)
                {
                    total +=
                        Mathf.RoundToInt(
                            (float)(voltage * 10.0)
                        );
                }
            }
            catch
            {
            }
        }

        return total;
    }

    private void GetManufactureValues(
        out int monthSum,
        out int yearSuffixSum
    )
    {
        monthSum = 0;
        yearSuffixSum = 0;

        var responses =
            BombInfo.QueryWidgets(
                "manufacture",
                ""
            );

        foreach (string response in responses)
        {
            if (string.IsNullOrEmpty(response))
                continue;

            try
            {
                Dictionary<string, string> data =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        response
                    );

                if (data == null)
                    continue;

                string month;

                if (data.TryGetValue(
                    "month",
                    out month
                ))
                {
                    monthSum +=
                        GetMonthValue(month);
                }

                string yearText;

                if (data.TryGetValue(
                    "year",
                    out yearText
                ))
                {
                    int year;

                    if (int.TryParse(
                        yearText,
                        out year
                    ))
                    {
                        yearSuffixSum +=
                            Mod(
                                year,
                                100
                            );
                    }
                }
            }
            catch
            {
            }
        }
    }

    private int GetMonthValue(
        string month
    )
    {
        if (string.IsNullOrEmpty(month))
            return 0;

        switch (month.Trim().ToUpperInvariant())
        {
            case "JAN":
            case "JANUARY":
                return 1;

            case "FEB":
            case "FEBRUARY":
                return 2;

            case "MAR":
            case "MARCH":
                return 3;

            case "APR":
            case "APRIL":
                return 4;

            case "MAY":
                return 5;

            case "JUN":
            case "JUNE":
                return 6;

            case "JUL":
            case "JULY":
                return 7;

            case "AUG":
            case "AUGUST":
                return 8;

            case "SEP":
            case "SEPT":
            case "SEPTEMBER":
                return 9;

            case "OCT":
            case "OCTOBER":
                return 10;

            case "NOV":
            case "NOVEMBER":
                return 11;

            case "DEC":
            case "DECEMBER":
                return 12;
        }

        return 0;
    }

    private int Base36Value(
        char c
    )
    {
        c =
            char.ToUpperInvariant(c);

        if (c >= '0' && c <= '9')
            return c - '0';

        if (c >= 'A' && c <= 'Z')
            return c - 'A' + 10;

        return 0;
    }

    private string RailFenceEncrypt(
        string input,
        int rails
    )
    {
        if (rails <= 1 ||
            input.Length <= 1)
        {
            return input;
        }

        StringBuilder[] rows =
            new StringBuilder[rails];

        for (int i = 0; i < rails; i++)
            rows[i] = new StringBuilder();

        int row = 0;
        int direction = 1;

        for (int i = 0; i < input.Length; i++)
        {
            rows[row].Append(
                input[i]
            );

            if (row == 0)
                direction = 1;

            else if (row == rails - 1)
                direction = -1;

            row += direction;
        }

        StringBuilder result =
            new StringBuilder();

        for (int i = 0; i < rails; i++)
            result.Append(rows[i]);

        return result.ToString();
    }

    private string BuildPolybiusAlphabet(
        string key
    )
    {
        string source =
            key.ToUpperInvariant().Replace('J', 'I') +
            "ABCDEFGHIKLMNOPQRSTUVWXYZ";

        StringBuilder result =
            new StringBuilder();

        for (int i = 0; i < source.Length; i++)
        {
            char c =
                source[i];

            if (c < 'A' ||
                c > 'Z' ||
                c == 'J')
                continue;

            if (result.ToString().IndexOf(c) < 0)
                result.Append(c);
        }

        return result.ToString();
    }

    private int Mod(
        int value,
        int modulus
    )
    {
        int result =
            value % modulus;

        if (result < 0)
            result += modulus;

        return result;
    }

    private void HandleButtonPress(
        int buttonIndex
    )
    {
        if (!_activated ||
            _submitting ||
            _solved)
            return;

        Buttons[buttonIndex]
            .AddInteractionPunch(0.4f);

        int digit =
            _buttonNumbers[buttonIndex];

        _enteredCode +=
            digit.ToString();

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Button {1} pressed. Entered code: {2}",
            _moduleId,
            buttonIndex + 1,
            _enteredCode
        );

        if (_enteredCode.Length >= 10)
            StartCoroutine(AcceptCode());
    }

    private IEnumerator AcceptCode()
    {
        if (_submitting)
            yield break;

        _submitting = true;

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Ten digits entered: {1}.",
            _moduleId,
            _enteredCode
        );

        if (AudioSource != null)
            AudioSource.Stop();

        if (AudioSource != null &&
            EndAudio != null)
        {
            AudioSource.loop = false;
            AudioSource.clip = EndAudio;
            AudioSource.Play();

            yield return new WaitForSeconds(
                EndAudio.length
            );
        }

        if (_enteredCode == _correctCode)
        {
            Debug.LogFormat(
                "[Dancing Triangle #{0}] Code is correct.",
                _moduleId
            );

            Solve();
        }
        else
        {
            Debug.LogFormat(
                "[Dancing Triangle #{0}] Code is incorrect. Strike spam commencing.",
                _moduleId
            );

            StartCoroutine(
                StrikeSpam()
            );
        }
    }

    private IEnumerator StrikeSpam()
    {
        while (!_solved)
        {
            Module.HandleStrike();

            yield return new WaitForSeconds(
                0.3f
            );
        }
    }

    private void StartLoopAudio()
    {
        if (AudioSource == null ||
            LoopAudio == null)
            return;

        AudioSource.Stop();
        AudioSource.clip = LoopAudio;
        AudioSource.loop = true;
        AudioSource.Play();
    }

    private void Solve()
    {
        if (_solved)
            return;

        _solved = true;
        _submitting = false;

        if (AudioSource != null)
        {
            AudioSource.Stop();
            AudioSource.loop = false;
        }

        if (TriangleAnimator != null)
            TriangleAnimator.enabled = false;

        Debug.LogFormat(
            "[Dancing Triangle #{0}] Music makes you lose control. Module solved.",
            _moduleId
        );

        Module.HandlePass();
    }
	
	private void LoadSettings()
	{
		_volume = 100;

		try
		{
			string directory = Path.Combine(
				Application.persistentDataPath,
				"Modsettings"
			);

			string path = Path.Combine(
				directory,
				"dancingTriangle-settings.txt"
			);

			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			if (!File.Exists(path))
			{
				DancingTriangleSettings defaults =
					new DancingTriangleSettings();

				File.WriteAllText(
					path,
					JsonConvert.SerializeObject(
						defaults,
						Formatting.Indented
					)
				);
			}

			DancingTriangleSettings settings =
				JsonConvert.DeserializeObject<DancingTriangleSettings>(
					File.ReadAllText(path)
				);

			if (settings != null)
				_volume = Mathf.Clamp(settings.Volume, 0, 100);

			Debug.LogFormat(
				"[Dancing Triangle #{0}] Settings path: {1}",
				_moduleId,
				path
			);
		}
		catch (Exception e)
		{
			Debug.LogFormat(
				"[Dancing Triangle #{0}] Failed to read settings: {1}. Using Volume 100.",
				_moduleId,
				e.Message
			);

			_volume = 100;
		}

		if (AudioSource != null)
			AudioSource.volume = _volume / 100f;

		Debug.LogFormat(
			"[Dancing Triangle #{0}] Volume: {1}%",
			_moduleId,
			_volume
		);
	}

}