using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Settings;

public class TestSettingsScript : GlobalSettings
{
    [SerializeField]
    int testNumber = 123;

    [SerializeField]
    string testString = "Cool String";

    [SerializeField]
    bool testToggle = false;
}