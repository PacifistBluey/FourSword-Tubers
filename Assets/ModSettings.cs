using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WeaverCore.Settings;

public class ModSettings : GlobalSettings
{
    int testNumber;

    //Called when this settings panel gets opened in the settings menu
    protected override void OnPanelOpen()
    {
        //Adds a UI Element where the player inputs a specific number. The "testNumber" field above will store the actual value
        AddFieldElement(nameof(testNumber), "Test Number", "This is a test description for the number element");

        //Adds a UI element where the player inputs a string of characters. You can specify custom getters and setter with this function
        AddPropertyElement<string>(getString, setString, "Test String Property", "This is a test of a custom string element");

        //Adds a header element. Can be useful for grouping related elements together
        var headerElement = AddHeading("This is a header");

        //Moves the header element to the top so it's the first thing that is displayed
        headerElement.MoveToTop();

        //Adds in an element that is only used for spacing elements apart
        var spaceElement = AddSpacing();

        //This makes the space element come right after the header element
        spaceElement.Order = headerElement.Order + 1;

        //Adds a button element that calls the AddNewElement() function every time it's clicked
        AddButtonElement(AddNewElement, "Add Header", "Clicking this adds a new header to the settings panel");
    }

    string stringStorage = "test_test_test";

    string getString()
    {
        return stringStorage;
    }

    void setString(string value)
    {
        stringStorage = value;
    }

    void AddNewElement()
    {
        //Add a new header element to the panel
        AddHeading("A newly created header");
    }
}