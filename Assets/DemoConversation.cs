using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WeaverCore.Assets.Components;

public class DemoConversation : Conversation
{
    protected override IEnumerator DoConversation()
    {
        //Displays the title card "Mike" in the bottom-left corner
        DisplayTitle("Mike");

        //Speaks two messages, split into two pages
        yield return Speak("Hello! I am a test NPC, my name is Mike",
            "I am here to test out the conversation system made by WeaverCore!");

        //Presents a yes and no question
        yield return PresentYesNoQuestion("Is this dialog box working?");

        //Was the result of the question a yes?
        if (DialogBoxResult == YesNoResult.Yes)
        {
            yield return Speak("Good, that means it's working!");
        }
        //Was the result of the question a no?
        else if (DialogBoxResult == YesNoResult.No)
        {
            yield return Speak("Well, the fact that you were able to select \"No\" in the first place means it should be working");
        }

        //Present a new question, but 5 geo is required to select "Yes"
        yield return PresentYesNoQuestion("Test the dialog box again by spending geo?", 5);

        if (DialogBoxResult == YesNoResult.Yes)
        {
            yield return Speak("Thanks for you donation", "Well, thanks for stopping by");
        }
        else if (DialogBoxResult == YesNoResult.No)
        {
            yield return Speak("Come on! It's only 5 Geo", "Well, thanks for stopping by");
        }
    }
}
